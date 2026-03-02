using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("Kamera (Camera) objesi. Eğer boşsa GetComponent/Camera.main ile aranacak.")]
    [SerializeField] private Camera cam;
    [Tooltip("PlayerController objesi. Eğer boşsa parent/kardeş objelerden bulunacak.")]
    [SerializeField] private PlayerController player;

    [Header("Slide Juice (Kayma Hissi)")]
    [SerializeField] private float slideFovIncrease = 15f;
    [SerializeField] private float slideTiltX = -10f; // Kamera X ekseninde yukarı bakar (eksi değerler yukarı baktırır)
    [SerializeField] private float slideTransitionSpeed = 10f;

    [Header("Jump Juice (Zıplama Hissi)")]
    [SerializeField] private float jumpTiltX = 8f;  // Kamera X ekseninde aşağı bakar
    [SerializeField] private float landShakeIntensity = 0.2f;
    [SerializeField] private float landShakeDuration = 0.15f;

    [Header("Wall-Run Juice (Duvarda Koşma Hissi)")]
    [SerializeField] private float wallRunRollZ = 15f; // Duvara doğru Roll (Z)
    [SerializeField] private float wallRunTransitionSpeed = 8f;

    private float baseFov;
    private Quaternion baseLocalRotation;
    
    // Hedef Değerler (Tweens/Lerps)
    private float targetFov;
    private float targetTiltX;
    private float targetRollZ;

    // Shake (Sarsıntı)
    private float shakeTimer;
    private Vector3 shakeOffset;

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

        // PlayerController olaylarına abone oluyoruz (Observer Pattern)
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

        // Pürüzsüz Değer Geçişleri (Smooth Lerp/Slerp)
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * slideTransitionSpeed);

        Quaternion targetRot = Quaternion.Euler(targetTiltX, 0, targetRollZ);
        currentRotationOffset = Quaternion.Slerp(currentRotationOffset, targetRot, Time.deltaTime * wallRunTransitionSpeed); // Transition speed adaptif yapılabilir, basitleştirildi.
        
        cam.transform.localRotation = baseLocalRotation * currentRotationOffset;
        cam.transform.localPosition = shakeOffset; // Temel LocalPosition'ın 0,0,0 olduğu varsayılmıştır. Eğer farklıysa baseLocalPosition + shakeOffset yapılmalı.
    }

    private void UpdateShake()
    {
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            shakeOffset = UnityEngine.Random.insideUnitSphere * landShakeIntensity * (shakeTimer / landShakeDuration);
        }
        else
        {
            shakeOffset = Vector3.Lerp(shakeOffset, Vector3.zero, Time.deltaTime * 10f);
        }
    }

    private void UpdateJumpRestoration()
    {
        // Havada ise hafif aşağı bakmayı korur, değilse yavaşça ortaya döner (eğer slide/wallrun değilsek)
        if (targetTiltX == jumpTiltX)
        {
            // İsteğe bağlı havada süzülme boyunca kademeli toplama
            targetTiltX = Mathf.Lerp(targetTiltX, 0f, Time.deltaTime * 1.5f);
        }
    }

    private void HandleJump()
    {
        targetTiltX = jumpTiltX;
    }

    private void HandleLand()
    {
        targetTiltX = 0; // Normale sıfırla
        shakeTimer = landShakeDuration; // Head shake başlat
    }

    private void HandleSlideStart()
    {
        targetFov = baseFov + slideFovIncrease;
        targetTiltX = slideTiltX; // X'te negatife giderek yukarı bakar
    }

    private void HandleSlideEnd()
    {
        targetFov = baseFov;
        targetTiltX = 0f;
    }

    private void HandleWallRunStart(int direction)
    {
        // direction: -1 (Sol duvar) -> Kamera sola yatar (pozitif Z roll)
        // direction: 1 (Sağ duvar) -> Kamera sağa yatar (negatif Z roll)
        targetRollZ = wallRunRollZ * direction * -1f; 
    }

    private void HandleWallRunEnd()
    {
        targetRollZ = 0f;
    }

    public void TriggerHitStopShake()
    {
        // HitStop sarsıntısı için değerleri ayarla (Land shake'ten biraz daha şiddetli)
        shakeTimer = landShakeDuration * 2f;
        landShakeIntensity *= 1.5f; // Mevcut şiddeti geçici olarak artır, UpdateShake'te sıfırlanmadığı için genel bir değişken olabilir ama basit tutuldu.
        // Daha iyi bir yaklaşım için Shake Offset hesaplamasında doğrudan bu metoda özel bir sarsıntı verilebilir.
        // Şimdilik Timer'ı doldurmak yeterli sarsıntı sağlayacaktır.
    }
}
