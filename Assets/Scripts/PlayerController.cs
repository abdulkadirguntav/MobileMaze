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
    private Animator anim;
    private float verticalVelocity;

    // Durum Kontrolleri (State Machine)
    public bool isSliding { get; private set; } = false;
    public bool isJumping { get; private set; } = false;
    public bool isWallRunning { get; private set; } = false;
    public bool isDead { get; private set; } = false;
    
    // Güçlendirme Envanteri & Durumları
    public bool isInvincible { get; private set; } = false;
    public bool isAcrobaticDodging { get; private set; } = false;

    [Header("QTE & Auto-Boost Ayarları")]
    public float autoBoostCooldown = 120f;
    private float currentBoostTimer;
    private bool inTimingZone = false;

    [Header("Güçlendirme Ayarları")]
    [SerializeField] private float dashDuration = 5f;
    [SerializeField] private float dashSpeed = 50f;

    [Header("Mobil Swipe Ayarları")]
    [Tooltip("Kaydırmanın algılanması için gereken minimum piksel mesafesi")]
    [SerializeField] private float swipeThreshold = 50f;
    private Vector2 startTouch;
    private Vector2 swipeDelta;
    private bool isSwiping;

    [Header("Görsel Ayarlar (Third Person)")]
    [Tooltip("Duvar koşusunda karakter modelinin eğilme açısı (Örn: 90)")]
    [SerializeField] private float wallRunTiltAngle = 60f;
    [Tooltip("Eğilme hızının yumuşaklığı")]
    [SerializeField] private float wallRunTiltSpeed = 10f;
    [Tooltip("Karakterin havada kalmasını engellemek için Y eksenindeki ofseti (Aşağı çekmek için eksi değerler)")]
    [SerializeField] private float visualYOffset = -1.08f;
    
    private float currentTilt = 0f;
    private int currentWallDirection = 0; // -1 Sol, 1 Sağ

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
        anim = GetComponentInChildren<Animator>(); // Karakter modelinin içindeki Animator'u bul
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
        UpdateCharacterTilt(); // Duvar koşusu eğilimini (Tilt) uygula
    }

    private void UpdateCharacterTilt()
    {
        if (anim == null) return;

        // Hedef eğimi belirle (Tilt)
        float targetTilt = 0f;
        if (isWallRunning)
        {
            // Sağ duvar (1) ise sola yatsın (+ açı). Sol duvar (-1) ise sağa yatsın (- açı).
            targetTilt = currentWallDirection * wallRunTiltAngle; 
        }

        // Yumuşak geçiş
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * wallRunTiltSpeed);

        // Beyblade (kendi etrafında dönme) sorununu çözmek için Quaternion kullanıyoruz:
        // Sadece Z ekseninde bük, X ve Y sıfır kalsın. (Zaten karakterin dönüş yönünü rotasyon değil script hallediyor)
        anim.transform.localRotation = Quaternion.Euler(0, 0, currentTilt);

        // --- ZORUNLU AŞAĞI ÇEKME (OFFSET) ---
        // Eğer Animator ana objenin içindeyse (child ise) modeli aşağı/yukarı kaydırmamıza izin ver
        if (anim.transform != this.transform)
        {
            Vector3 localPos = anim.transform.localPosition;
            anim.transform.localPosition = new Vector3(localPos.x, visualYOffset, localPos.z);
        }
    }

    private void CheckLanding()
    {
        if (controller.isGrounded && !wasGrounded)
        {
            if (isWallRunning) StopWallRun(); // Yere indiğinde duvar koşusunu kes
            
            if (anim != null) anim.SetBool("IsJump", false);
            OnLand?.Invoke();
            isJumping = false;
        }
        wasGrounded = controller.isGrounded;
    }

    private void HandleInput()
    {
        bool actionTaken = false;
        
        bool swipeLeft = false;
        bool swipeRight = false;
        bool swipeUp = false;
        bool swipeDown = false;

        // === MOBİL SWIPE (KAYDIRMA) KONTROLLERİ ===
        if (Input.touches.Length > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                isSwiping = true;
                startTouch = t.position;
            }
            else if (t.phase == TouchPhase.Canceled || t.phase == TouchPhase.Ended)
            {
                isSwiping = false;
            }
            else if (t.phase == TouchPhase.Moved && isSwiping)
            {
                swipeDelta = t.position - startTouch;
                if (swipeDelta.magnitude > swipeThreshold)
                {
                    // Hangi yöne kaydırıldığına karar ver
                    float x = swipeDelta.x;
                    float y = swipeDelta.y;
                    
                    if (Mathf.Abs(x) > Mathf.Abs(y))
                    {
                        // Yatay
                        if (x < 0) swipeLeft = true;
                        else swipeRight = true;
                    }
                    else
                    {
                        // Dikey
                        if (y > 0) swipeUp = true;
                        else swipeDown = true;
                    }

                    // Bir kez algılandıktan sonra sıfırla ki ard arda tetiklenmesin
                    isSwiping = false;
                }
            }
        }

        // Şerit Değiştirme (X Ekseninde Sol/Sağ) veya Wall Run Başlatma
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) || swipeLeft)
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
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) || swipeRight)
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
        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || swipeUp))
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
        if ((Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || swipeDown))
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
            if (isJumping)
            {
                if (anim != null) anim.SetBool("IsJump", false);
                isJumping = false;
            }
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
        if (anim != null) anim.SetBool("IsJump", true);
        OnJump?.Invoke();
    }

    private IEnumerator SlideRoutine()
    {
        isSliding = true;
        if (anim != null) anim.SetBool("IsSlide", true); // Animasyon tetikle
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
        if (anim != null) anim.SetBool("IsSlide", false); // Animasyon bitir
        OnSlideEnd?.Invoke();
    }

    private void StartWallRun(int wallDirection)
    {
        if (isSliding) return;

        isWallRunning = true;
        currentWallDirection = wallDirection; // Yönü kaydet (görsel eğilme için)
        
        // Zıplama durumunu iptal et ki koşu animasyonuna (Running) geri dönebilsin.
        // Böylece Running animasyonu çalışırken biz de modeli eğmiş (Tilt) olacağız.
        if (isJumping)
        {
            isJumping = false;
            if (anim != null) anim.SetBool("IsJump", false);
        }
        
        // EĞER DUVAR KOŞUSU İÇİN ÖZEL ANİMASYONUN YOKSA BUNLARI YORUMA ALABİLİRSİN:
        /*
        if (anim != null)
        {
            if (wallDirection == -1) // Sol Duvar
                anim.SetBool("IsWallRunLeft", true);
            else if (wallDirection == 1) // Sağ Duvar
                anim.SetBool("IsWallRunRight", true);
        }
        */
        
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
        currentWallDirection = 0; // Eğim sıfırlanacak

        // ÖZEL ANİMASYONLARI YORUMA ALDIYSAK BUNU DA ALMALIYIZ:
        /*
        if (anim != null)
        {
            anim.SetBool("IsWallRunLeft", false);
            anim.SetBool("IsWallRunRight", false);
        }
        */

        if (wallRunCoroutine != null) StopCoroutine(wallRunCoroutine);
        OnWallRunEnd?.Invoke();
    }

    private void SetHeight(float newHeight)
    {
        if (controller == null) return;
        
        // ÖNEMLİ DÜZELTME: Kapsül boyu (Height), Çapının (Radius) 2 katından küçük olamaz.
        // Eğer kayma boyun 0.5 ise, yarıçap 0.25'ten büyük olamaz. Yoksa Unity fizik kapsülünü 
        // 1.0 birime zorlar ve karakteri (0.33 kadar) havaya kaldırır!
        float maxAllowedRadius = newHeight / 2f;
        // Eğer normal radius'un 0.5 ise, maksimum 0.5'e kadar büyümesine izin ver
        controller.radius = maxAllowedRadius < 0.5f ? maxAllowedRadius : 0.5f;

        // Karakterin Collider boyunu ayarla
        controller.height = newHeight;
        
        // Karakterin uçmaması için (Y=0.08 Skin Width hariç)
        controller.center = new Vector3(0, newHeight / 2f, 0);
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

            // Senaryo 1: Normal Senaryo (Başarısız Zamanlama & Boost Yok) -> ÖLÜM
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
