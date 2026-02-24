using UnityEngine;
using System.Collections;

public class Obstacle : MonoBehaviour
{
    [Header("Fake-out Settings")]
    public bool isFakeOut = false;
    
    private Transform playerTransform;
    private GameManager gameManager;
    private bool hasTriggered = false;
    private Vector3 targetWorldPosition;
    private bool isLerping = false;

    // Dinamik hesaplanan değerler
    private float triggerDistance;
    private float lerpSpeed;
    private float wobbleDuration = 0.1f; // Zorluk Artışı: Daha da hızlı anticipiation

    public void Initialize(Transform player, GameManager gm)
    {
        playerTransform = player;
        gameManager = gm;
        hasTriggered = false;
        isLerping = false;
        isFakeOut = false; 
        transform.localScale = Vector3.one; 
    }

    public void SetFakeOutTarget(Vector3 worldTargetPos)
    {
        isFakeOut = true;
        targetWorldPosition = worldTargetPos;
    }

    void Update()
    {
        if (playerTransform == null) return;

        // Karakterin gerisinde kaldıysa, kendini havuza iade et
        if (transform.position.z < playerTransform.position.z - 10f)
        {
            gameObject.SetActive(false);
            return;
        }

        // Eğer Fake-out objesiyse ve henüz tetiklenmediyse
        if (isFakeOut && !hasTriggered)
        {
            float distance = transform.position.z - playerTransform.position.z;
            
            // Dinamik Zorluk Tetikleyicisi (Son Milisaniye)
            // Oyuncunun hızı * TimeAllowance = gereken mesafe
            triggerDistance = gameManager.CurrentPlayerSpeed * gameManager.fakeOutTimeAllowance;
            
            if (distance <= triggerDistance && distance > 0)
            {
                // Lerp hızını da curve üzerinden al (Snap geçiş hissi)
                lerpSpeed = gameManager.fakeOutLerpSpeedCurve.Evaluate(gameManager.score);
                StartCoroutine(FakeOutRoutine());
            }
        }

        // Lerp ile yeni (hedef) yerine geçiş
        if (isLerping)
        {
            transform.position = Vector3.Lerp(transform.position, targetWorldPosition, Time.deltaTime * lerpSpeed);
        }
    }

    private IEnumerator FakeOutRoutine()
    {
        hasTriggered = true;
        
        // 1. Wobble (Titreme - Anticipation)
        Vector3 origScale = transform.localScale;
        float elapsed = 0f;
        while (elapsed < wobbleDuration)
        {
            float scaleY = origScale.y + Mathf.Sin(elapsed * 50f) * 0.2f;
            float scaleX = origScale.x + Mathf.Cos(elapsed * 50f) * 0.2f;
            transform.localScale = new Vector3(scaleX, scaleY, origScale.z);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = origScale; 

        // 2. Yeni Boş Slot'a Zıpla
        isLerping = true;
    }
}
