using UnityEngine;
using System.Collections;

public enum ObstacleBehavior
{
    Standard,
    FakeOut,
    PistonLeft,
    PistonRight,
    Guillotine,
    DiagonalSpinner,
    SuddenBlockade,
    SuddenOpening_Wall,
    SuddenOpening_Disappear,
    AnimatedCheckerboard,
    AnimatedSnake,
    CornerChaser,
    ShadowHunter
}

public class Obstacle : MonoBehaviour
{
    [Header("State")]
    public ObstacleBehavior currentBehavior = ObstacleBehavior.Standard;
    
    private Transform playerTransform;
    private GameManager gameManager;
    private bool hasTriggered = false;
    private Vector3 targetWorldPosition;
    private Vector3 initialWorldPosition;
    private bool isLerping = false;

    [Header("Animation")]
    public float animOffset = 0f;
    public float animSpeed = 5f;

    private static readonly Vector2[] snakePath = new Vector2[] {
        new Vector2(-1, 1), new Vector2(1, 1),
        new Vector2(1, -1), new Vector2(-1, -1)
    };

    private float triggerDistance;
    private float lerpSpeed;
    private float wobbleDuration = 0.1f;
    
    private Vector3 originalScale;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    public void Initialize(Transform player, GameManager gm, ObstacleBehavior behavior, Vector3 targetPos)
    {
        playerTransform = player;
        gameManager = gm;
        hasTriggered = false;
        isLerping = false;
        currentBehavior = behavior;
        targetWorldPosition = targetPos;
        transform.localScale = originalScale;
        transform.localRotation = Quaternion.identity;

        SetupInitialState();
    }

    public void SetFakeOutTarget(Vector3 worldTargetPos)
    {
        currentBehavior = ObstacleBehavior.FakeOut;
        targetWorldPosition = worldTargetPos;
    }

    public void SetAnimOffset(float offset)
    {
        animOffset = offset;
    }

    private void SetupInitialState()
    {
        switch (currentBehavior)
        {
            case ObstacleBehavior.Standard:
            case ObstacleBehavior.FakeOut:
            case ObstacleBehavior.SuddenOpening_Wall:
                transform.position = targetWorldPosition;
                break;
            case ObstacleBehavior.SuddenOpening_Disappear:
                transform.position = targetWorldPosition;
                break;
            case ObstacleBehavior.PistonLeft:
                initialWorldPosition = targetWorldPosition + Vector3.left * 10f;
                transform.position = initialWorldPosition;
                break;
            case ObstacleBehavior.PistonRight:
                initialWorldPosition = targetWorldPosition + Vector3.right * 10f;
                transform.position = initialWorldPosition;
                break;
            case ObstacleBehavior.Guillotine:
                initialWorldPosition = targetWorldPosition + Vector3.up * 10f;
                transform.position = initialWorldPosition;
                break;
            case ObstacleBehavior.DiagonalSpinner:
                transform.position = targetWorldPosition;
                transform.localRotation = Quaternion.Euler(0, 0, 45); // 45 derece eğik
                break;
            case ObstacleBehavior.SuddenBlockade:
                // Çok yukarıdan veya aşağıdan gelebilir
                initialWorldPosition = targetWorldPosition + Vector3.down * 15f;
                transform.position = initialWorldPosition;
                break;
            case ObstacleBehavior.AnimatedCheckerboard:
                initialWorldPosition = targetWorldPosition;
                animOffset = initialWorldPosition.x > 0 ? 0f : Mathf.PI;
                animSpeed = 15f; 
                break;
            case ObstacleBehavior.AnimatedSnake:
            case ObstacleBehavior.CornerChaser:
                initialWorldPosition = targetWorldPosition;
                animSpeed = 15f; 
                break;
            case ObstacleBehavior.ShadowHunter:
                initialWorldPosition = targetWorldPosition;
                break;
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        // Karakterin gerisinde kaldıysa yok et
        if (transform.position.z < playerTransform.position.z - 10f)
        {
            gameObject.SetActive(false);
            return;
        }

        float distance = transform.position.z - playerTransform.position.z;

        // Animasyon Davranışları
        if (currentBehavior == ObstacleBehavior.DiagonalSpinner)
        {
            transform.Rotate(0, 0, 180f * Time.deltaTime);
        }
        else if (currentBehavior == ObstacleBehavior.AnimatedCheckerboard)
        {
            float newX = Mathf.Cos(Time.time * animSpeed + animOffset) * 1f;
            transform.position = new Vector3(newX, initialWorldPosition.y, transform.position.z);
        }
        else if (currentBehavior == ObstacleBehavior.AnimatedSnake || currentBehavior == ObstacleBehavior.CornerChaser)
        {
            float currentT = (Time.time * animSpeed + animOffset) % 4f;
            if (currentT < 0) currentT += 4f;
            int index1 = Mathf.FloorToInt(currentT);
            int index2 = (index1 + 1) % 4;
            float lerpT = currentT - index1;
            Vector2 pos2D = Vector2.Lerp(snakePath[index1], snakePath[index2], lerpT);
            transform.position = new Vector3(pos2D.x, pos2D.y, transform.position.z);
        }
        else if (currentBehavior == ObstacleBehavior.ShadowHunter)
        {
            if (distance > 15f)
            {
                // X ve Y ekseninde oyuncuyu takip et
                Vector3 newPos = transform.position;
                newPos.x = Mathf.Lerp(newPos.x, playerTransform.position.x, Time.deltaTime * 20f);
                newPos.y = Mathf.Lerp(newPos.y, playerTransform.position.y, Time.deltaTime * 20f);
                transform.position = newPos;
            }
        }

        if (!hasTriggered && distance > 0)
        {
            CheckBehaviors(distance);
        }

        if (isLerping)
        {
            transform.position = Vector3.Lerp(transform.position, targetWorldPosition, Time.deltaTime * lerpSpeed);
        }
    }

    private void CheckBehaviors(float distance)
    {
        switch (currentBehavior)
        {
            case ObstacleBehavior.FakeOut:
                triggerDistance = gameManager.CurrentPlayerSpeed * gameManager.fakeOutTimeAllowance;
                if (distance <= triggerDistance)
                {
                    lerpSpeed = gameManager.fakeOutLerpSpeedCurve.Evaluate(gameManager.score);
                    StartCoroutine(FakeOutRoutine());
                }
                break;
            case ObstacleBehavior.PistonLeft:
            case ObstacleBehavior.PistonRight:
            case ObstacleBehavior.Guillotine:
                if (distance <= 30f) // 30 birim kala uzan/düş
                {
                    lerpSpeed = 20f;
                    isLerping = true;
                    hasTriggered = true;
                }
                break;
            case ObstacleBehavior.SuddenBlockade:
                if (distance <= 15f) // 15 birim kala aniden yerleş (çok hızlı)
                {
                    lerpSpeed = 40f;
                    isLerping = true;
                    hasTriggered = true;
                }
                break;
            case ObstacleBehavior.SuddenOpening_Disappear:
                if (distance <= 15f) // 15 birim kala geçiş açılsın
                {
                    // Blok kaybolsun veya küçülsün
                    StartCoroutine(ShrinkAndDisable());
                    hasTriggered = true;
                }
                break;
        }
    }

    private IEnumerator FakeOutRoutine()
    {
        hasTriggered = true;
        
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

        isLerping = true;
    }

    private IEnumerator ShrinkAndDisable()
    {
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        while (elapsed < 0.2f)
        {
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / 0.2f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
