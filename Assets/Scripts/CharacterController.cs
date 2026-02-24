using UnityEngine;

public class CharacterController : MonoBehaviour
{
    [SerializeField] private float forwardSpeed = 5f;
    [SerializeField] private float laneChangeSpeed = 10f;
    
    // Tünel 2x2 olduğu için X ve Y'de gidebileceği maksimum ve minimum noktalar (1 birim aralıklarla)
    // Karakter 0,0 merkezli değilse bu değerleri Unity Editor'dan başlangıç pozisyonuna göre -0.5 ve 0.5 gibi ayarlayabilirsiniz.
    [SerializeField] private float minX = -0.5f;
    [SerializeField] private float maxX = 0.5f;
    [SerializeField] private float minY = -0.5f;
    [SerializeField] private float maxY = 0.5f;

    private Vector2 targetXY;
    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;
    private bool isSwiping = false;

    public void SetForwardSpeed(float speed)
    {
        forwardSpeed = speed;
    }

    void Start()
    {
        // Hedef X ve Y pozisyonunu karakterin başlangıç pozisyonuna eşitle
        targetXY.x = Mathf.Clamp(transform.position.x, minX, maxX);
        targetXY.y = Mathf.Clamp(transform.position.y, minY, maxY);
    }

    void Update()
    {
        // Z ekseninde durmadan sonsuza kadar hareket
        transform.position += Vector3.forward * forwardSpeed * Time.deltaTime;

        // Kullanıcı girdilerini (Klavye ve Dokunma/Kaydırma) kontrol et
        HandleInput();

        // X ve Y eksenlerinde hedeflenen pozisyona yumuşak(smooth) geçiş yap
        Vector3 newPos = transform.position;
        newPos.x = Mathf.Lerp(newPos.x, targetXY.x, laneChangeSpeed * Time.deltaTime);
        newPos.y = Mathf.Lerp(newPos.y, targetXY.y, laneChangeSpeed * Time.deltaTime);
        transform.position = newPos;
    }

    private void HandleInput()
    {
        // PC'de test etmek için klavye desteği
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) Move(Vector2.up);
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) Move(Vector2.down);
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) Move(Vector2.left);
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) Move(Vector2.right);

        // Mobil için Swipe (Kaydırma) desteği
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                startTouchPosition = touch.position;
                isSwiping = true;
            }
            else if (touch.phase == TouchPhase.Ended && isSwiping)
            {
                endTouchPosition = touch.position;
                DetectSwipe();
                isSwiping = false;
            }
        }
    }

    private void DetectSwipe()
    {
        Vector2 swipeDelta = endTouchPosition - startTouchPosition;

        // Yanlışlıkla dokunmaları engellemek için minimum kaydırma mesafesi
        if (swipeDelta.magnitude > 50f) 
        {
            // Kaydırmanın yatay mı dikey mi olduğunu belirle
            if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
            {
                // Yatay kaydırma
                if (swipeDelta.x > 0) Move(Vector2.right);
                else Move(Vector2.left);
            }
            else
            {
                // Dikey kaydırma
                if (swipeDelta.y > 0) Move(Vector2.up);
                else Move(Vector2.down);
            }
        }
    }

    private void Move(Vector2 direction)
    {
        // Her kaydırmada/basışta hedef pozisyonu 1 birim kaydır
        targetXY.x += direction.x;
        targetXY.y += direction.y;

        // Hedef pozisyonu 2x2 tünel dışına çıkmaması için sınırlandır
        targetXY.x = Mathf.Clamp(targetXY.x, minX, maxX);
        targetXY.y = Mathf.Clamp(targetXY.y, minY, maxY);
    }

    // Engellerin `Is Trigger` işaretli bir collider'a sahip olması gerekir.
    private void OnTriggerEnter(Collider other)
    {
        // Temas edilen obje "Obstacle" tagine sahipse
        if (other.CompareTag("Obstacle"))
        {
            GameManager gm = FindObjectOfType<GameManager>();
            if (gm != null)
            {
                gm.GameOver();
            }
        }
    }

    // Eğer trigger yerine fiziksel çarpışma kullanıyorsanız bunu aktif edebilirsiniz
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            GameManager gm = FindObjectOfType<GameManager>();
            if (gm != null)
            {
                gm.GameOver();
            }
        }
    }
}
