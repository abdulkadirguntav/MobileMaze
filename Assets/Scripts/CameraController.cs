using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("Kamera objesi")]
    [SerializeField] private Camera cam;
    [Tooltip("Oyuncu kontrolcüsü (Events için)")]
    [SerializeField] private PlayerController player;

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

    [Header("Overclock Juice (Akış Hissi)")]
    [SerializeField] private float overclockFovIncrease = 10f;
    [SerializeField] private float overclockTransitionSpeed = 5f;

    // Temel değerler
    private float baseFov;
    private Quaternion baseLocalRotation;
    
    // Hedef değerler
    private float targetFov;
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
        
        targetFov = baseFov;
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

        // Overclock kontrolü
        float baseTargetFov = player != null && player.isOverclocked ? baseFov + overclockFovIncrease : baseFov;
        // Slide FOV artışı Overclock FOV artışının üzerine eklenecek
        float finalTargetFov = player != null && player.isSliding ? baseTargetFov + slideFovIncrease : baseTargetFov;

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, finalTargetFov, Time.deltaTime * (player != null && player.isSliding ? slideTransitionSpeed : overclockTransitionSpeed));

        Quaternion targetRot = Quaternion.Euler(targetTiltX, 0, targetRollZ);
        currentRotationOffset = Quaternion.Slerp(currentRotationOffset, targetRot, Time.deltaTime * wallRunTransitionSpeed);
        
        cam.transform.localRotation = baseLocalRotation * currentRotationOffset;
        cam.transform.localPosition = shakeOffset; 
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
        // Matematiksel olarak: direction * wallRunRollZ = 1 * 15 = 15 (pozitif = sol)
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
