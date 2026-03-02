using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(UnityEngine.CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("İleri Hareket (Forward Movement)")]
    [SerializeField] private float baseForwardSpeed = 15f;
    private float currentForwardSpeed;

    [Header("Şerit Ayarları (Lanes)")]
    [SerializeField] private float laneDistance = 3f; // Şeritler arası mesafe
    [SerializeField] private float sideLerpSpeed = 10f; // Sağa sola geçiş pürüzsüzlüğü
    private int currentLane = 1; // 0 = Sol, 1 = Orta, 2 = Sağ
    
    [Header("Zıplama & Yerçekimi (Jump & Gravity)")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = -20f;
    
    [Header("Kayma Ayarları (Slide)")]
    [SerializeField] private float slideDuration = 1f;
    [SerializeField] private float slideHeightMultiplier = 0.5f; // Capsule collider'ın ne kadar küçüleceği
    
    [Header("Duvarda Koşma (Wall-Run)")]
    [SerializeField] private float wallRunDuration = 1.5f;
    [SerializeField] private float wallRunGravityMultiplier = 0.2f; // Wall-run sırasında yavaşça aşağı kayma

    [Header("Overclock & Vaulting")]
    [SerializeField] private float overclockSpeedMultiplier = 1.4f;
    [SerializeField] private float vaultJumpForceMultiplier = 1.2f;
    [SerializeField] private float vaultGravityMultiplier = 1.5f; // Daha hızlı yukarı ve aşağı kavisli atlayış

    [Header("Power-Ups")]
    [SerializeField] private float dashSpeedMultiplier = 2f;
    
    // Bileşenler (Components)
    private UnityEngine.CharacterController controller;
    
    // Durumlar (States)
    private float verticalVelocity;
    private float originalHeight;
    private float originalCenterY;
    
    public bool isSliding { get; private set; } = false;
    private float slideTimer;

    public bool isWallRunning { get; private set; } = false;
    private float wallRunTimer;
    private int wallRunDirection = 0; // -1: Sol, 1: Sağ

    public bool isOverclocked { get; private set; } = false;
    public bool isInvincible { get; private set; } = false; // Dash gücü için
    public bool isDead { get; private set; } = false;

    // Kamera ve Juice için Olaylar (Events)
    public event Action OnJump;
    public event Action OnLand;
    public event Action OnSlideStart;
    public event Action OnSlideEnd;
    public event Action<int> OnWallRunStart;
    public event Action OnWallRunEnd;

    private bool wasGrounded;

    void Start()
    {
        controller = GetComponent<UnityEngine.CharacterController>();
        
        originalHeight = controller.height;
        originalCenterY = controller.center.y;
        currentForwardSpeed = baseForwardSpeed;
    }

    void Update()
    {
        if (isDead) return;

        HandleInput();
        HandleWallRunAndGravity();
        HandleSlide();
        CheckLanding();

        // Şerit Hedef Pozisyonunu Hesapla
        int targetLaneIndex = currentLane - 1; 
        float targetX = targetLaneIndex * laneDistance;

        // X ekseninde pürüzsüz Lerp geçişi
        float currentX = Mathf.Lerp(transform.position.x, targetX, sideLerpSpeed * Time.deltaTime);
        
        // Hız Çarpanlarını Hesapla
        float moveSpeed = currentForwardSpeed;
        if (isInvincible) moveSpeed *= dashSpeedMultiplier; // Dash aktifse ekstra hız
        else if (isOverclocked) moveSpeed *= overclockSpeedMultiplier;

        // Hareket vektörü: X ekseninde değişim, Y ekseninde yerçekimi, Z ekseninde sürekli ileri
        Vector3 displacement = new Vector3(currentX - transform.position.x, verticalVelocity * Time.deltaTime, moveSpeed * Time.deltaTime);

        controller.Move(displacement);
    }

    private void CheckLanding()
    {
        if (controller.isGrounded && !wasGrounded)
        {
            OnLand?.Invoke();
        }
        wasGrounded = controller.isGrounded;
    }

    private void HandleInput()
    {
        bool swipeLeft = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
        bool swipeRight = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
        bool swipeUp = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        bool swipeDown = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);

        // Sola Geçiş veya Sol Duvarda Koşma
        if (swipeLeft)
        {
            if (currentLane > 0) 
            {
                currentLane--;
                if (isWallRunning) StopWallRun(); // Şerit değiştirince duvarı bırak
            }
            else if (currentLane == 0 && !isWallRunning && !controller.isGrounded)
            {
                StartWallRun(-1);
            }
        }

        // Sağa Geçiş veya Sağ Duvarda Koşma
        if (swipeRight)
        {
            if (currentLane < 2) 
            {
                currentLane++;
                if (isWallRunning) StopWallRun();
            }
            else if (currentLane == 2 && !isWallRunning && !controller.isGrounded)
            {
                StartWallRun(1);
            }
        }

        // Zıplama
        if (swipeUp && controller.isGrounded && !isSliding)
        {
            Jump();
        }

        // Kayma
        if (swipeDown && !isSliding && !isWallRunning)
        {
            StartSlide();
        }
        else if (swipeDown && isWallRunning)
        {
            StopWallRun(); // Duvardayken aşağı swipe yaparsa duvardan inip kaymaya başlasın
            StartSlide();
        }
    }

    private void Jump()
    {
        verticalVelocity = isOverclocked ? jumpForce * vaultJumpForceMultiplier : jumpForce;
        OnJump?.Invoke();
    }

    private void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;

        controller.height = originalHeight * slideHeightMultiplier;
        controller.center = new Vector3(0, originalCenterY * slideHeightMultiplier, 0);

        if (!controller.isGrounded)
            verticalVelocity = -jumpForce * 1.5f;

        OnSlideStart?.Invoke();
    }

    private void HandleSlide()
    {
        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0)
            {
                isSliding = false;
                controller.height = originalHeight;
                controller.center = new Vector3(0, originalCenterY, 0);
                OnSlideEnd?.Invoke();
            }
        }
    }

    private void StartWallRun(int direction)
    {
        isWallRunning = true;
        wallRunTimer = wallRunDuration;
        wallRunDirection = direction;

        // Düşüşü yavaşça baştan başlat
        verticalVelocity = 0f; 
        OnWallRunStart?.Invoke(direction);
    }

    private void StopWallRun()
    {
        isWallRunning = false;
        OnWallRunEnd?.Invoke();
    }

    private void HandleWallRunAndGravity()
    {
        float currentGravity = isOverclocked ? gravity * vaultGravityMultiplier : gravity;

        if (isWallRunning)
        {
            wallRunTimer -= Time.deltaTime;
            // Yerçekimi çok azalarak yavaşça aşağı kayma hissi verir
            verticalVelocity += currentGravity * wallRunGravityMultiplier * Time.deltaTime; 

            if (wallRunTimer <= 0 || controller.isGrounded)
            {
                StopWallRun();
            }
        }
        else
        {
            if (controller.isGrounded && verticalVelocity < 0)
            {
                verticalVelocity = -2f; // Yerde tutunma
            }
            else
            {
                verticalVelocity += currentGravity * Time.deltaTime;
            }
        }
    }

    // --- DIŞ DEVLET (EXTERNAL STATE) KONTROLLERİ ---
    
    public void SetOverclockState(bool state)
    {
        isOverclocked = state;
    }

    public void SetForwardSpeed(float newSpeed)
    {
        baseForwardSpeed = newSpeed;
        currentForwardSpeed = baseForwardSpeed;
    }

    public void Die()
    {
        isDead = true;
        currentForwardSpeed = 0f;
    }

    // Power-up: Kinetik Kalkan (Dash)
    public void ActivateDash(float duration)
    {
        StartCoroutine(DashRoutine(duration));
    }

    private IEnumerator DashRoutine(float duration)
    {
        isInvincible = true;
        yield return new WaitForSeconds(duration);
        isInvincible = false;
    }

    // --- CARPISMA (COLLISION) KONTROLLERİ ---
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (isDead) return;

        // Engel çarpışması
        if (hit.gameObject.CompareTag("Obstacle"))
        {
            if (isInvincible)
            {
                // Kinetik Kalkan -> Engeli Yık
                Destroy(hit.gameObject);
                // Ses/Efekt eklenebilir
            }
            else
            {
                // Normal çarpışma - Fakat bu cam ise GlassObstacle scripti devreye girer
                GlassObstacle glass = hit.gameObject.GetComponent<GlassObstacle>();
                if (glass != null)
                {
                    glass.OnHit();
                }
                else
                {
                    GameManager.Instance.GameOver();
                }
            }
        }
    }

    // Trigger çarpışmaları (Collectible veya Trigger tabanlı tuzaklar için)
    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        if (other.CompareTag("Obstacle"))
        {
            if (isInvincible)
            {
                Destroy(other.gameObject);
            }
            else
            {
                GlassObstacle glass = other.GetComponent<GlassObstacle>();
                if (glass != null)
                {
                    glass.OnHit();
                }
                else
                {
                    GameManager.Instance.GameOver();
                }
            }
        }
        else if (other.CompareTag("Coin") || other.CompareTag("DataCore"))
        {
            GameManager.Instance.CollectDataCore();
            Destroy(other.gameObject);
        }
    }
}
