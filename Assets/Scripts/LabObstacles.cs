using UnityEngine;

public class LabObstacles : MonoBehaviour
{
    // Bu script modüler alt-sınıfları ve mantığı yönetmek için referans dosyasıdır.
}

public class LaserObstacle : MonoBehaviour
{
    [Header("Laser Settings")]
    public bool isMoving = true;
    public float moveSpeed = 5f;
    public float moveDistance = 3f;
    
    public bool isBlinking = false;
    public float blinkInterval = 1f;

    private Vector3 startPos;
    private Collider laserCollider;
    private MeshRenderer laserRenderer;

    void Start()
    {
        startPos = transform.position;
        laserCollider = GetComponent<Collider>();
        laserRenderer = GetComponentInChildren<MeshRenderer>();

        if (isBlinking)
        {
            InvokeRepeating(nameof(ToggleLaser), blinkInterval, blinkInterval);
        }
    }

    void Update()
    {
        if (isMoving)
        {
            transform.position = startPos + new Vector3(Mathf.Sin(Time.time * moveSpeed) * moveDistance, 0, 0);
        }
    }

    void ToggleLaser()
    {
        bool state = !laserCollider.enabled;
        laserCollider.enabled = state;
        if (laserRenderer != null) laserRenderer.enabled = state;
    }
}

public class AcidLeakObstacle : MonoBehaviour
{
    [Header("Acid Settings")]
    [Tooltip("Asit yerdendir, içinden kayarak geçilmez sadece zıplanır")]
    public float damageHeight = 0.5f; // Asitin maksimum yüksekliği

    // Asit temasında Game Over PlayerController'daki OnTriggerEnter'da 
    // veya OnControllerColliderHit'te algılanır çünkü bu bir Trigger veya küçük Collider olacaktır.
    // Ancak oyuncu yerden belli bir yükseklikteyse (zıplıyorsa) bu çalışmamalı.
    
    // Yere yakınsa öldür kontrolünü CharacterController yapar
}

public class QuarantineDoor : MonoBehaviour
{
    [Header("Door Settings")]
    public float closeSpeed = 2f;
    private bool isClosing = false;
    private Vector3 targetPos;

    void Start()
    {
        // Kapı başlangıçta yüksekte, oyuncu yaklaşınca inecek şekilde ayarlanabilir.
        targetPos = transform.position - new Vector3(0, 3f, 0); // 3 birim aşağıya iner
    }

    void Update()
    {
        if (isClosing)
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * closeSpeed);
        }
    }

    // Spawner veya uzaklık trigger'ı kapıyı tetikleyebilir
    public void TriggerDoorClose()
    {
        isClosing = true;
    }
}

public class CoolingFan : MonoBehaviour
{
    [Header("Fan Settings")]
    public float rotationSpeed = 180f;

    void Update()
    {
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }
}
