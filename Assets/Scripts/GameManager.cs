using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance { get { return _instance; } }

    [Header("References")]
    [Tooltip("Hızı güncellenecek karakter kontrolcüsü")]
    public PlayerController playerController;
    [Tooltip("Z mesafesini hesaplamak için oyuncu referansı")]
    public Transform playerTransform;

    [Header("Audio System")]
    public AudioSource bgmAudioSource;
    public AudioClip crashSfxClip;
    public AudioClip overclockSfxClip;
    public AudioClip glassBreakSfxClip;

    [Header("Game State & Score")]
    public float score;
    public float bestScore;
    private float startZ;
    public bool isGameOver { get; private set; } = false;

    [Header("Hardcore Difficulty Settings")]
    public AnimationCurve playerSpeedCurve = AnimationCurve.Linear(0, 120f, 3000, 360f);
    public AnimationCurve spawnIntervalCurve = AnimationCurve.Linear(0, 10f, 3000, 3f);
    public AnimationCurve fakeOutLerpSpeedCurve = AnimationCurve.Linear(0, 30f, 3000, 80f);
    public float fakeOutTimeAllowance = 0.15f; 
    public float CurrentPlayerSpeed { get; private set; }

    [Header("Overclock System")]
    [Tooltip("Overclock moduna girmek için hatasız geçilmesi gereken engel sayısı")]
    public int obstaclesToOverclock = 10;
    
    private int consecutiveObstaclesPassed = 0;
    public bool isOverclocked { get; private set; } = false;

    [Header("UI")]
    public GameObject gameOverPanel;
    
    [Header("Economy")]
    public int dataCoresCollected = 0;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    void Start()
    {
        if (playerTransform != null)
        {
            startZ = playerTransform.position.z;
        }

        bestScore = PlayerPrefs.GetFloat("BestScore", 0f);

        if (bgmAudioSource != null && bgmAudioSource.clip != null)
        {
            bgmAudioSource.loop = true;
            bgmAudioSource.Play();
        }
        
        UpdateSpeed();
    }

    void Update()
    {
        if (playerTransform == null || isGameOver) return;

        score = Mathf.Floor(playerTransform.position.z - startZ);
        if (score > bestScore)
        {
            bestScore = score;
        }

        UpdateSpeed();
    }

    private void UpdateSpeed()
    {
        if (playerController != null && !playerController.isDead)
        {
            CurrentPlayerSpeed = playerSpeedCurve.Evaluate(score);
            playerController.SetForwardSpeed(CurrentPlayerSpeed);
        }
    }

    // Engel geçildiğinde çağrılır (Obstacle.cs içinden)
    public void RegisterObstaclePassed()
    {
        if (isGameOver) return;

        consecutiveObstaclesPassed++;

        if (consecutiveObstaclesPassed >= obstaclesToOverclock && !isOverclocked)
        {
            ActivateOverclock();
        }
    }

    private void ActivateOverclock()
    {
        isOverclocked = true;
        if (playerController != null) playerController.SetOverclockState(true);

        if (overclockSfxClip != null)
            AudioSource.PlayClipAtPoint(overclockSfxClip, Camera.main != null ? Camera.main.transform.position : transform.position);

        Debug.Log("OVERCLOCK ACTIVATED!");
    }

    public void DeactivateOverclock()
    {
        isOverclocked = false;
        consecutiveObstaclesPassed = 0; // Sayacı sıfırla
        if (playerController != null) playerController.SetOverclockState(false);
        Debug.Log("OVERCLOCK DEACTIVATED!");
    }

    public void HandleGlassCollision()
    {
        if (isOverclocked)
        {
            // Camı kır (Overclock biter)
            if (glassBreakSfxClip != null)
                AudioSource.PlayClipAtPoint(glassBreakSfxClip, Camera.main != null ? Camera.main.transform.position : transform.position);
            
            DeactivateOverclock();
        }
        else if (playerController != null && playerController.isInvincible)
        {
            // Dash (Kinetik Kalkan) aktifse sadece kır, game over olmaz. Overclock sayacı sıfırlanmaz.
            if (glassBreakSfxClip != null)
                AudioSource.PlayClipAtPoint(glassBreakSfxClip, Camera.main != null ? Camera.main.transform.position : transform.position);
        }
        else
        {
            // Normal çarpışma
            GameOver();
        }
    }

    public void CollectDataCore()
    {
        dataCoresCollected++;
        PlayerPrefs.SetInt("TotalDataCores", PlayerPrefs.GetInt("TotalDataCores", 0) + 1);
        PlayerPrefs.Save();
    }

    public void GameOver()
    {
        if (isGameOver) return;
        
        // Kinetik kalkan varsa (Dash) ölümsüzdür
        if (playerController != null && playerController.isInvincible) return;

        isGameOver = true;
        
        PlayerPrefs.SetFloat("BestScore", bestScore);
        PlayerPrefs.Save();
        
        StartCoroutine(HitStopRoutine());
    }

    private System.Collections.IEnumerator HitStopRoutine()
    {
        if (bgmAudioSource != null) bgmAudioSource.Stop();
        if (crashSfxClip != null)
        {
            AudioSource.PlayClipAtPoint(crashSfxClip, Camera.main != null ? Camera.main.transform.position : transform.position);
        }

        Time.timeScale = 0.05f;

        if (Camera.main != null)
        {
            CameraController cam = Camera.main.GetComponent<CameraController>();
            if (cam != null) cam.TriggerHitStopShake();
        }

        yield return new WaitForSecondsRealtime(1f);

        Time.timeScale = 0f;
        
        if (playerController != null)
        {
            playerController.Die();
            playerController.enabled = false; 
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
