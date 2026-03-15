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
    [Tooltip("Kayma Karakter Boyu (Engellere çarpmamak için iyice küçültüldü)")]
    [SerializeField] private float slideHeight = 0.5f;

    [Header("Wall Run Ayarları")]
    [Tooltip("Duvarda kalma süresi")]
    [SerializeField] private float wallRunDuration = 1.2f;
    [Tooltip("Duvar koşusunda yerçekimi çarpanı (Örn: 0 ise düşmez, 0.2 ise yavaş düşer)")]
    [SerializeField] private float wallRunGravityMultiplier = 0.2f;

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
    public bool isAcrobaticDodging { get; private set; } = false;
    
    [Header("Güçlendirme Envanteri (Debug)")]
    public int healthShieldCount { get; private set; } = 0;

    [Header("QTE & Auto-Boost Ayarları")]
    public float autoBoostCooldown = 120f;
    private float currentBoostTimer;
    private bool inTimingZone = false;

    [Header("Güçlendirme Ayarları")]
    [SerializeField] private float dashDuration = 5f;
    [SerializeField] private float dashSpeed = 50f;
    public float slowMotionDuration = 4f;
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
        currentBoostTimer = autoBoostCooldown;
    }

    void Update()
    {
        if (isDead) return;

        UpdateAutoBoostTimer();
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
        bool actionTaken = false;

        // Şerit Değiştirme (X Ekseninde Sol/Sağ) veya Wall Run Başlatma
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            actionTaken = true;
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
            actionTaken = true;
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
            actionTaken = true;
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
            actionTaken = true;
            if (isWallRunning) StopWallRun();
            
            if (!isSliding)
            {
                if (slideCoroutine != null) StopCoroutine(slideCoroutine);
                slideCoroutine = StartCoroutine(SlideRoutine());
            }
        }

        if (actionTaken)
        {
            TryAcrobaticDodge();
        }
    }

    private void UpdateAutoBoostTimer()
    {
        if (isInvincible) return; // Zaten Boost (Ölümsüzlük) modundaysa sayma

        currentBoostTimer -= Time.deltaTime;
        if (currentBoostTimer <= 0f)
        {
            currentBoostTimer = autoBoostCooldown;
            ActivateDash(dashDuration, dashSpeed);
            Debug.Log("AUTO-BOOST AKTİF! Oynanış Hızlanıyor!");
        }
    }

    public void SetTimingZoneStatus(bool status)
    {
        inTimingZone = status;
    }

    private void TryAcrobaticDodge()
    {
        if (inTimingZone)
        {
            Debug.Log("QTE BAŞARILI! Akrobatik Geçiş: +100 Puan!");
            inTimingZone = false; // Tek bir engel için bir defa puan verilir
            
            // Şov Senaryosu: Kısa süreliğine engelin içinden geçme hakkı
            StartCoroutine(AcrobaticInvincibilityRoutine());
        }
    }

    private IEnumerator AcrobaticInvincibilityRoutine()
    {
        isAcrobaticDodging = true;
        yield return new WaitForSeconds(0.4f); // Engelin içinden zararsız geçmek için süre
        isAcrobaticDodging = false;
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

        // Unity'de kapalı (SetActive(false)) objeler FindGameObjectsWithTag ile BULUNAMAZ!
        // Bu yüzden tünelleri (ChunkSpawner) bularak, onun içindeki kapalı objeleri tarıyoruz.
        ChunkSpawner spawner = FindObjectOfType<ChunkSpawner>();
        if (spawner != null)
        {
            // Spawner'ın altındaki tüm tünelleri dön
            foreach (Transform chunk in spawner.transform)
            {
                // Sadece Sahnede o an aktif olan tünelleri tara
                if (!chunk.gameObject.activeInHierarchy) continue;

                // Tünelin içindeki TÜM objeleri (Kapalılar Dahil -> 'true' parametresi ile) al
                Transform[] allChildren = chunk.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in allChildren)
                {
                    float zDiff = child.position.z - transform.position.z;
                    if (zDiff > 0 && zDiff <= bombClearDistance)
                    {
                        if (child.CompareTag("Obstacle"))
                        {
                            child.gameObject.SetActive(false);
                        }
                        else if (child.CompareTag("SecretObject"))
                        {
                            child.gameObject.SetActive(true);
                        }
                    }
                }
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
            // Senaryo 3: Juggernaut Senaryosu (Auto-Boost aktif)
            if (isInvincible)
            {
                Debug.Log("Juggernaut Modu: Engel parçalandı!");
                other.gameObject.SetActive(false); // Engeli ezip geçer (kırar)
                return;
            }

            // Senaryo 2: Şov Senaryosu (QTE başarılı, animasyonda)
            if (isAcrobaticDodging)
            {
                Debug.Log("Şov Modu: Engelden zararsızca sıyrıldı!");
                // Çarpmıyoruz, yavaşlamıyoruz, içinden estetikle kayıp geçiyoruz
                return;
            }

            // Kurtarıcı: Kalkan (Health) aktifse kalkanı kır ama hayatta kal
            if (healthShieldCount > 0)
            {
                Debug.Log("Kalkan kırıldı: Hayatta kalındı!");
                GameManager.Instance.HandleCollision(); 
                other.gameObject.SetActive(false); // Vurduğumuz engeli kapatalım (Pooling uyumlu)
                return;
            }

            // Senaryo 1: Normal Senaryo (Başarısız Zamanlama & Boost Yok & Kalkan Yok) -> ÖLÜM
            Die();
            GameManager.Instance.HandleCollision(); // GameManager Game Over sürecini başlatacak
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
