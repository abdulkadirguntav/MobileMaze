using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("Kamera objesi")]
    [SerializeField] private Camera cam;
    [Tooltip("Oyuncu kontrolcüsü (Takip edilecek hedef)")]
    public Transform playerTransform;

    [Header("Third Person Ayarları")]
    [Tooltip("Kameranın karakterin neresinde duracağı (Örn: X=0, Y=3, Z=-5)")]
    public Vector3 offset = new Vector3(0f, 3f, -5f);
    
    [Tooltip("Kameranın ne kadar yumuşak takip edeceği (Düşük = Daha yumuşak)")]
    public float followSpeed = 10f;
    
    [Tooltip("Kamera karaktere mi baksın? (LookAt)")]
    public bool lookAtPlayer = true;
    
    [Tooltip("Kamera karakterin tam merkezine mi yoksa biraz üzerine mi baksın? (Örn: Y=1)")]
    public Vector3 lookAtOffset = new Vector3(0f, 1f, 0f);

    [Header("Sarsıntı (Death Effect)")]
    [SerializeField] private float hitShakeIntensity = 0.5f;
    [SerializeField] private float hitShakeDuration = 0.5f;
    private float shakeTimer;
    private Vector3 shakeOffset;

    private void Start()
    {
        if (cam == null) cam = GetComponentInChildren<Camera>();
        if (cam == null) cam = Camera.main;

        // Player atanmadıysa sahnede bulmaya çalış
        if (playerTransform == null)
        {
            PlayerController pc = FindObjectOfType<PlayerController>();
            if (pc != null)
            {
                playerTransform = pc.transform;
            }
        }
    }

    private void LateUpdate()
    {
        if (playerTransform == null) return;

        UpdateShake();

        // Hedef pozisyon (Player'ın konumu + offset)
        Vector3 targetPosition = playerTransform.position + offset;

        // Yumuşak geçiş (Smooth follow)
        // Z ekseninde daha katı (hızlı) takip etmesi istenirse sadece Z için farklı bir lerp kullanılabilir
        // Ancak Subway Surfers tarzında X ve Z genelde beraber akıcı takip edilir.
        Vector3 smoothedPosition = Vector3.Lerp(cam.transform.position, targetPosition, followSpeed * Time.deltaTime);

        // Pozisyonu uygula (Sarsıntı varsa ekle)
        cam.transform.position = smoothedPosition + shakeOffset;

        // Hedefe bak
        if (lookAtPlayer)
        {
            Vector3 lookTarget = playerTransform.position + lookAtOffset;
            
            // Eğer sarsıntı varsa bakış açısında da hafif titreşim hissedilmesi için 
            // lookTarget'a da eklenebilir ama genelde sadece pozisyon sarsıntısı yeterlidir.
            cam.transform.LookAt(lookTarget);
        }
    }

    private void UpdateShake()
    {
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            // Küre içinde rastgele bir nokta seçip şiddetle çarp
            shakeOffset = UnityEngine.Random.insideUnitSphere * hitShakeIntensity * (shakeTimer / hitShakeDuration);
        }
        else
        {
            shakeOffset = Vector3.zero;
        }
    }

    // GameManager (veya Player) öldüğünde bunu çağırır
    public void TriggerHitStopShake()
    {
        shakeTimer = hitShakeDuration;
    }
}
