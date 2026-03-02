using UnityEngine;
using System;

[RequireComponent(typeof(UnityEngine.CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("İleri Hareket (Forward Movement)")]
    [SerializeField] private float forwardSpeed = 15f;

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
    [SerializeField] private float wallRunGravityMultiplier = 0.2f; // Wall-run sırasında düşüş yavaşlaması

    // Bileşenler (Components)
    private UnityEngine.CharacterController controller;
    
    // Durumlar (States)
    private float verticalVelocity;
    private float originalHeight;
    private float originalCenterY;
    
    private bool isSliding = false;
    private float slideTimer;

    private bool isWallRunning = false;
    private float wallRunTimer;
    private int wallRunDirection = 0; // -1: Sol, 1: Sağ

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
        
        // Başlangıç collider boyutlarını kaydet
        originalHeight = controller.height;
        originalCenterY = controller.center.y;
    }

    void Update()
    {
        HandleInput();
        HandleWallRunAndGravity();
        HandleSlide();
        CheckLanding();

        // Şerit Hedef Pozisyonunu Hesapla
        // Şeritler: -1 (Sol), 0 (Orta), 1 (Sağ) - currentLane değerinden ortalayarak alıyoruz
        int targetLaneIndex = currentLane - 1; 
        float targetX = targetLaneIndex * laneDistance;

        // X ekseninde pürüzsüz Lerp geçişi
        float currentX = Mathf.Lerp(transform.position.x, targetX, sideLerpSpeed * Time.deltaTime);
        
        // Hareket vektörü: X ekseninde değişim, Y ekseninde yerçekimi, Z ekseninde sürekli ileri
        Vector3 displacement = new Vector3(currentX - transform.position.x, verticalVelocity * Time.deltaTime, forwardSpeed * Time.deltaTime);

        controller.Move(displacement);
    }

    private void CheckLanding()
    {
        // Karakter havada iken yere değerse (Head bob / Sarsıntı için)
        if (controller.isGrounded && !wasGrounded)
        {
            OnLand?.Invoke();
        }
        wasGrounded = controller.isGrounded;
    }

    private void HandleInput()
    {
        // Basit Klavye / Swipe Temsili Girdiler
        bool swipeLeft = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
        bool swipeRight = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
        bool swipeUp = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        bool swipeDown = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);

        // Sola Geçiş veya Sol Duvarda Koşma
        if (swipeLeft)
        {
            if (currentLane > 0) 
            {
                currentLane--; // Şerit değiştir
            }
            else if (currentLane == 0 && !isWallRunning && !controller.isGrounded)
            {
                // En soldayız, havadayız ve tekrar sola swipe yaparsak -> WallRun
                StartWallRun(-1);
            }
        }

        // Sağa Geçiş veya Sağ Duvarda Koşma
        if (swipeRight)
        {
            if (currentLane < 2) 
            {
                currentLane++; // Şerit değiştir
            }
            else if (currentLane == 2 && !isWallRunning && !controller.isGrounded)
            {
                // En sağdayız, havadayız ve tekrar sağa swipe yaparsak -> WallRun
                StartWallRun(1);
            }
        }

        // Zıplama (Sadece yerdeyken ve kaymıyorken)
        if (swipeUp && controller.isGrounded && !isSliding)
        {
            Jump();
        }

        // Kayma (Yerdeyken veya havadayken hızlı inmek için)
        if (swipeDown && !isSliding && !isWallRunning)
        {
            StartSlide();
        }
    }

    private void Jump()
    {
        verticalVelocity = jumpForce;
        OnJump?.Invoke();
    }

    private void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;

        // Collider yarı yarıya küçülür
        controller.height = originalHeight * slideHeightMultiplier;
        controller.center = new Vector3(0, originalCenterY * slideHeightMultiplier, 0);

        // Havada kayma basılırsa hızlıca yere in
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
                // Collider eski haline döner
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

        verticalVelocity = 0f; // Düşüşü anlık durdur
        OnWallRunStart?.Invoke(direction);
    }

    private void HandleWallRunAndGravity()
    {
        if (isWallRunning)
        {
            wallRunTimer -= Time.deltaTime;
            // Yerçekimi çok azalır (Duvar tutunma hissi)
            verticalVelocity += gravity * wallRunGravityMultiplier * Time.deltaTime; 

            // Süre biterse veya yere değersek bırak
            if (wallRunTimer <= 0 || controller.isGrounded)
            {
                isWallRunning = false;
                OnWallRunEnd?.Invoke();
            }
        }
        else
        {
            // Normal Yerçekimi
            if (controller.isGrounded && verticalVelocity < 0)
            {
                verticalVelocity = -2f; // Yerde tutunma payı
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }
        }
    }

    public void SetForwardSpeed(float newSpeed)
    {
        forwardSpeed = newSpeed;
    }
}
