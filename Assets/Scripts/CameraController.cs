using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("Kamera objesi")]
    [SerializeField] private Camera cam;
    [Tooltip("Oyuncu kontrolcüsü (Events için)")]
    [SerializeField] private PlayerController player;

    [Header("Head Bobbing Ayarları")]
    [SerializeField] private float bobFrequency = 1.5f;
    [SerializeField] private float bobAmplitudeX = 0.05f;
    [SerializeField] private float bobAmplitudeY = 0.05f;
    private float bobTimer = 0f;
    private Vector3 defaultLocalPos;

    [Header("Slide Juice (Kayma Hissi)")]
    [SerializeField] private float slideFovIncrease = 15f;
    [SerializeField] private float slideTiltX = -10f; // Kamera yukarı bakar
    [SerializeField] private float slideTransitionSpeed = 10f;

    [Header("Jump Juice (Zıplama Hissi)")]
    [SerializeField] private float jumpTiltX = 8f;  // Kamera aşağı bakar
    [SerializeField] private float landShakeIntensity = 0.2f;
    [SerializeField] private float landShakeDuration = 0.15f;

    [Header("Wall-Run Juice (Duvarda Koşma Hissi)")]
    [Tooltip("Sağ duvarda sola(+), sol duvarda sağa(-) yatar.")]
    [SerializeField] private float wallRunRollZ = 15f; 
    [SerializeField] private float wallRunTransitionSpeed = 8f;

    [Header("Dash (Kinetik Kalkan) Hissi")]
    [SerializeField] private float dashFovIncrease = 30f;
    [SerializeField] private float dashTransitionSpeed = 8f;

    // Temel değerler
    private float baseFov;
    private Quaternion baseLocalRotation;
    
    // Hedef değerler
    private float targetTiltX;
    private float targetRollZ;

    // Shake
    private float shakeTimer;
    private Vector3 shakeOffset;
    private float currentLandShakeIntensity;

    private Quaternion currentRotationOffset = Quaternion.identity;

    void Start()
    {
        if (cam == null) cam = GetComponentInChildren<Camera>();
        if (cam == null) cam = Camera.main;

        if (player == null) player = GetComponentInParent<PlayerController>();
        if (player == null) player = FindObjectOfType<PlayerController>();

        baseFov = cam.fieldOfView;
        baseLocalRotation = cam.transform.localRotation;
        defaultLocalPos = cam.transform.localPosition;
        
        targetTiltX = 0f;
        targetRollZ = 0f;
        currentLandShakeIntensity = landShakeIntensity;

        if(player != null)
        {
            player.OnJump += HandleJump;
            player.OnLand += HandleLand;
            player.OnSlideStart += HandleSlideStart;
            player.OnSlideEnd += HandleSlideEnd;
            player.OnWallRunStart += HandleWallRunStart;
            player.OnWallRunEnd += HandleWallRunEnd;
        }
    }

    void OnDestroy()
    {
        if(player != null)
        {
            player.OnJump -= HandleJump;
            player.OnLand -= HandleLand;
            player.OnSlideStart -= HandleSlideStart;
            player.OnSlideEnd -= HandleSlideEnd;
            player.OnWallRunStart -= HandleWallRunStart;
            player.OnWallRunEnd -= HandleWallRunEnd;
        }
    }

    void Update()
    {
        UpdateJumpRestoration();
        UpdateShake();
        Vector3 bobPos = CalculateHeadBobbing();

        // Dash veya Slide kontrolü
        float baseTargetFov = player != null && player.isInvincible ? baseFov + dashFovIncrease : baseFov;
        float finalTargetFov = player != null && player.isSliding ? baseTargetFov + slideFovIncrease : baseTargetFov;

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, finalTargetFov, Time.deltaTime * (player != null && player.isSliding ? slideTransitionSpeed : dashTransitionSpeed));

        // Eğer öldüysek kamerayı yere doğru devir (FPS yığılma hissi)
        if (player != null && player.isDead)
        {
            targetTiltX = 60f; // Yere bak
            targetRollZ = 25f; // Sola veya sağa yat
        }

        Quaternion targetRot = Quaternion.Euler(targetTiltX, 0, targetRollZ);
        currentRotationOffset = Quaternion.Slerp(currentRotationOffset, targetRot, Time.deltaTime * (player != null && player.isDead ? 3f : wallRunTransitionSpeed));
        
        cam.transform.localRotation = baseLocalRotation * currentRotationOffset;
        
        // Final position (Default + Bobbing + Shake)
        cam.transform.localPosition = defaultLocalPos + bobPos + shakeOffset; 
    }

    private Vector3 CalculateHeadBobbing()
    {
        if (player == null) return Vector3.zero;

        // Karakter yerdeyse ve hareket ediyorsa bobbing yap (Z ekseninde koşuyor varsayıyoruz)
        if (!player.isJumping && !player.isSliding && !player.isWallRunning)
        {
            float speedMultiplier = player.GetCurrentSpeed() / 20f; // 20f normal koşu hızı referansı
            bobTimer += Time.deltaTime * bobFrequency * speedMultiplier;

            float bobX = Mathf.Sin(bobTimer / 2f) * bobAmplitudeX;
            float bobY = Mathf.Sin(bobTimer) * bobAmplitudeY;

            return new Vector3(bobX, bobY, 0f);
        }
        else
        {
            // Havada veya kayıyorken bobbing'i sıfıra yumuşatarak çek
            bobTimer = 0f;
            return Vector3.Lerp(cam.transform.localPosition - defaultLocalPos - shakeOffset, Vector3.zero, Time.deltaTime * 5f);
        }
    }

    private void UpdateShake()
    {
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            shakeOffset = UnityEngine.Random.insideUnitSphere * currentLandShakeIntensity * (shakeTimer / landShakeDuration);
        }
        else
        {
            shakeOffset = Vector3.Lerp(shakeOffset, Vector3.zero, Time.deltaTime * 10f);
            currentLandShakeIntensity = landShakeIntensity; // Reset intensity
        }
    }

    private void UpdateJumpRestoration()
    {
        if (targetTiltX == jumpTiltX)
        {
            targetTiltX = Mathf.Lerp(targetTiltX, 0f, Time.deltaTime * 1.5f);
        }
    }

    private void HandleJump()
    {
        targetTiltX = jumpTiltX;
    }

    private void HandleLand()
    {
        targetTiltX = 0; 
        shakeTimer = landShakeDuration; 
        currentLandShakeIntensity = landShakeIntensity;
    }

    private void HandleSlideStart()
    {
        targetTiltX = slideTiltX; 
    }

    private void HandleSlideEnd()
    {
        targetTiltX = 0f;
    }

    private void HandleWallRunStart(int direction)
    {
        // En sol şerit (Sol duvar: -1) -> Z ekseninde SAĞA (-15 derece) 
        // En sağ şerit (Sağ duvar: 1) -> Z ekseninde SOLA (+15 derece)
        targetRollZ = wallRunRollZ * direction; 
    }

    private void HandleWallRunEnd()
    {
        targetRollZ = 0f;
    }

    public void TriggerHitStopShake()
    {
        shakeTimer = landShakeDuration * 2f;
        currentLandShakeIntensity = landShakeIntensity * 1.5f; 
    }
}
