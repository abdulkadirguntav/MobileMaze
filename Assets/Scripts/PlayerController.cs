using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(UnityEngine.CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Matris & Şerit Ayarları (3x3 Grid)")]
    [Tooltip("Sol (-X), Orta (0), Sağ (+X) şeritler arası mesafe")]
    [SerializeField] private float laneDistance = 3f;
    [Tooltip("Şeritler arası yana geçiş hızı")]
    [SerializeField] private float sideLerpSpeed = 15f;
    private int currentLane = 1; // 0 = Sol, 1 = Orta, 2 = Sağ

    [Header("Katman (Layer) & Fizik Ayarları")]
    [Tooltip("Normal koşu hızı (Z ekseninde)")]
    [SerializeField] private float forwardSpeed = 20f;
    [Tooltip("Zıplama gücü")]
    [SerializeField] private float jumpForce = 12f;
    [Tooltip("Yerçekimi gücü")]
    [SerializeField] private float gravity = -25f;
    
    [Header("Kayma (Slide) Ayarları")]
    [Tooltip("Kayma modunda kalma süresi")]
    [SerializeField] private float slideDuration = 0.8f;
    [Tooltip("Normal Karakter Boyu")]
    [SerializeField] private float normalHeight = 2f;
    [Tooltip("Kayma Karakter Boyu")]
    [SerializeField] private float slideHeight = 1f;

    [Header("Wall Run Ayarları")]
    [Tooltip("Duvarda kalma süresi")]
    [SerializeField] private float wallRunDuration = 1.2f;
    [Tooltip("Duvar koşusunda yerçekimi çarpanı (Örn: 0 ise düşmez, 0.2 ise yavaş düşer)")]
    [SerializeField] private float wallRunGravityMultiplier = 0f;

    // Bileşenler ve Sabit Değerler
    private UnityEngine.CharacterController controller;
    private float verticalVelocity;

    // Durum Kontrolleri (State Machine)
    public bool isSliding { get; private set; } = false;
    public bool isJumping { get; private set; } = false;
    public bool isWallRunning { get; private set; } = false;
    public bool isDead { get; private set; } = false;
    
    // Güçlendirme Envanteri & Durumları
    public bool isInvincible { get; private set; } = false;
    
    [Header("Güçlendirme Envanteri (Debug)")]
    public int healthShieldCount { get; private set; } = 0;

    [Header("Güçlendirme Ayarları")]
    [SerializeField] private float dashDuration = 5f;
    [SerializeField] private float dashSpeed = 50f;
    [SerializeField] private float slowMotionDuration = 4f;
    [SerializeField] private float timeScaleTarget = 0.4f;
    [Tooltip("Bomba patladığında önündeki kaç birimlik engeli silecek?")]
    [SerializeField] private float bombClearDistance = 100f;
    [SerializeField] private GameObject bombEffectPrefab;

    // Etkinlikler (FPS Kamera için)
    public event Action OnJump;
    public event Action OnLand;
    public event Action OnSlideStart;
    public event Action OnSlideEnd;
    public event Action<int> OnWallRunStart; // -1: Sol Duvar, 1: Sağ Duvar
    public event Action OnWallRunEnd;
    public event Action OnDeath; // Kamera yere yığılma efekti için

    private bool wasGrounded;
    private Coroutine wallRunCoroutine;
    private Coroutine slideCoroutine;

    void Start()
    {
        controller = GetComponent<UnityEngine.CharacterController>();
        SetHeight(normalHeight);
    }

    void Update()
    {
        if (isDead) return;

        HandleInput();
        CalculateVerticalMovement();

        // 3 Şeritli X Pozisyonunu Hesaplama
        int targetLaneIndex = currentLane - 1; // 0 -> -1(Sol), 1 -> 0(Orta), 2 -> 1(Sağ)
        float targetX = targetLaneIndex * laneDistance;

        float currentX = Mathf.Lerp(transform.position.x, targetX, sideLerpSpeed * Time.deltaTime);

        // Hareket Vektörü
        Vector3 displacement = new Vector3(
            currentX - transform.position.x, 
            verticalVelocity * Time.deltaTime, 
            forwardSpeed * Time.deltaTime
        );

        controller.Move(displacement);
        CheckLanding();
    }

    private void CheckLanding()
    {
        if (controller.isGrounded && !wasGrounded && !isWallRunning)
        {
            OnLand?.Invoke();
            isJumping = false;
        }
        wasGrounded = controller.isGrounded;
    }

    private void HandleInput()
    {
        // Şerit Değiştirme (X Ekseninde Sol/Sağ) veya Wall Run Başlatma
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (currentLane > 0)
            {
                currentLane--;
                StopWallRun(); // Şerit değiştiriyorsa wall run biter
            }
            else if (currentLane == 0 && !controller.isGrounded && !isWallRunning)
            {
                // En sol şeritteyken havada tekrar sola basarsa Wall Run (Sol Duvar)
                StartWallRun(-1);
            }
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (currentLane < 2)
            {
                currentLane++;
                StopWallRun();
            }
            else if (currentLane == 2 && !controller.isGrounded && !isWallRunning)
            {
                // En sağ şeritteyken havada tekrar sağa basarsa Wall Run (Sağ Duvar)
                StartWallRun(1);
            }
        }

        // Zıplama
        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)))
        {
            if (isWallRunning)
            {
                // Duvardan Zıplama (Düşüşü keser, tekrar havalanır)
                StopWallRun();
                Jump();
            }
            else if (controller.isGrounded && !isSliding)
            {
                Jump();
            }
        }

        // Kayma
        if ((Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)))
        {
            if (isWallRunning) StopWallRun();
            
            if (!isSliding)
            {
                if (slideCoroutine != null) StopCoroutine(slideCoroutine);
                slideCoroutine = StartCoroutine(SlideRoutine());
            }
        }

    }

    private void CalculateVerticalMovement()
    {
        if (controller.isGrounded && verticalVelocity < 0 && !isWallRunning)
        {
            verticalVelocity = -2f;
            isJumping = false;
        }
        else
        {
            float currentGravity = isWallRunning ? (gravity * wallRunGravityMultiplier) : gravity;
            verticalVelocity += currentGravity * Time.deltaTime;
        }
    }

    private void Jump()
    {
        verticalVelocity = jumpForce;
        isJumping = true;
        OnJump?.Invoke();
    }

    private IEnumerator SlideRoutine()
    {
        isSliding = true;
        OnSlideStart?.Invoke();

        // Havadaysa hızlıca yere in
        if (!controller.isGrounded && !isWallRunning)
        {
            verticalVelocity = -jumpForce * 1.5f;
        }

        SetHeight(slideHeight);

        yield return new WaitForSeconds(slideDuration);

        SetHeight(normalHeight);
        
        isSliding = false;
        OnSlideEnd?.Invoke();
    }

    private void StartWallRun(int wallDirection)
    {
        if (isSliding) return;

        isWallRunning = true;
        verticalVelocity = Mathf.Max(verticalVelocity, 0f); // Düşüşü SFX/Görsel için anlık durdur (veya sekecekse tut)
        OnWallRunStart?.Invoke(wallDirection);

        if (wallRunCoroutine != null) StopCoroutine(wallRunCoroutine);
        wallRunCoroutine = StartCoroutine(WallRunRoutine());
    }

    private IEnumerator WallRunRoutine()
    {
        yield return new WaitForSeconds(wallRunDuration);
        StopWallRun();
    }

    private void StopWallRun()
    {
        if (!isWallRunning) return;
        isWallRunning = false;
        if (wallRunCoroutine != null) StopCoroutine(wallRunCoroutine);
        OnWallRunEnd?.Invoke();
    }

    private void SetHeight(float newHeight)
    {
        if (controller == null) return;
        
        // Karakterin Collider boyunu ayarla
        controller.height = newHeight;
        
        // Kapsülün her zaman 'en alt' kısmının sabit kalması için Pivot merkezini hesaplıyoruz.
        // Standart bir Unity kapsülünde pivot tam ortadadır, bunu ayaklardan esnetmek için center.y değişmelidir.
        // Normal boyumuz 2, yeni boyumuz 1 ise -> ayak kısmını korumak için merkezi -0.5 aşağı kaydırıyoruz.
        controller.center = new Vector3(0, (newHeight - normalHeight) / 2f, 0);
    }

    // --- DIŞ SİSTEMLERLE İLETİŞİM (PowerUps & GameManager) ---

    public void ActivateDash(float duration, float boostSpeed)
    {
        StartCoroutine(DashRoutine(duration, boostSpeed));
    }

    private IEnumerator DashRoutine(float duration, float boostSpeed)
    {
        isInvincible = true;
        float originalSpeed = forwardSpeed;
        forwardSpeed = boostSpeed;

        yield return new WaitForSeconds(duration);

        forwardSpeed = originalSpeed;
        isInvincible = false;
    }

    public void CollectPowerUp(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.Bomb: 
                TriggerBomb(); 
                break;
            case PowerUpType.Dash: 
                if(!isInvincible) ActivateDash(dashDuration, dashSpeed); 
                break;
            case PowerUpType.Health: 
                healthShieldCount++; 
                break;
            case PowerUpType.Time: 
                if(Time.timeScale >= 1f) StartCoroutine(TimeRoutine()); 
                break;
        }
    }

    private void TriggerBomb()
    {
        if (bombEffectPrefab != null)
        {
            Instantiate(bombEffectPrefab, transform.position, Quaternion.identity);
        }

        GameObject[] allObstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (GameObject obs in allObstacles)
        {
            float zDiff = obs.transform.position.z - transform.position.z;
            if (zDiff > 0 && zDiff <= bombClearDistance)
            {
                Destroy(obs);
            }
        }
    }

    private IEnumerator TimeRoutine()
    {
        Time.timeScale = timeScaleTarget;
        Time.fixedDeltaTime = 0.02f * Time.timeScale; 

        yield return new WaitForSecondsRealtime(slowMotionDuration);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    public void ConsumeHealthShield()
    {
        healthShieldCount--;
        // Kalkan kırılma efekti burada tetiklenebilir
    }

    public void SetForwardSpeed(float newSpeed)
    {
        // Dash aktifken GameManager'dan gelen genel hız güncellemelerini yoksay
        if (!isInvincible && !isDead)
        {
            forwardSpeed = newSpeed;
        }
    }

    public float GetCurrentSpeed()
    {
        return forwardSpeed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        if (other.CompareTag("Obstacle"))
        {
            // 1. Durum: Dash aktifse engeli parçala / yoksay
            if (isInvincible)
            {
                Debug.Log("Dash aktif: Engel parçalandı!");
                // İleride buraya engel parçalanma efekti veya Destroy(other.gameObject) eklenebilir.
                return;
            }

            // 2. Durum: Kalkan (Health) aktifse kalkanı kır ama hayatta kal
            if (healthShieldCount > 0)
            {
                Debug.Log("Kalkan kırıldı: Hayatta kalındı!");
                // Kalkan kırılma efekti / sesi oynatılır
                GameManager.Instance.HandleCollision(); // GameManager kalkan durumunu işleyip devam edecek
                Destroy(other.gameObject); // Vurduğumuz engeli yokedelim ki içinden geçerken tekrar tetiklenmesin
                return;
            }

            // 3. Durum: İkisi de yoksa ÖLÜM
            Die();
            GameManager.Instance.HandleCollision(); // GameManager Game Over sürecini (HitStop vb) başlatacak
        }
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;
        forwardSpeed = 0f;
        verticalVelocity = 0f; // Havadaysa olduğu yere yığılsın
        
        StopAllCoroutines();

        // FPS Ölüm Hissi: Kameranın yere düşüşünü simüle etmek için event fırlatıyoruz
        // (Bunu CameraController dinleyip işleyecek)
        OnDeath?.Invoke();
    }
}
