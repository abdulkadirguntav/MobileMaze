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
    public AudioClip shieldBreakSfxClip; // Yeni: Kalkan kırılma sesi

    [Header("Game State")]
    public bool isGameOver { get; private set; } = false;
    private float startTime;

    [Header("Difficulty Curves")]
    public AnimationCurve playerSpeedCurve = AnimationCurve.Linear(0, 120f, 600, 360f); // Zaman bazlı hız
    public float CurrentPlayerSpeed { get; private set; }

    [Header("UI")]
    public GameObject gameOverPanel;

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
        startTime = Time.time;

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
        UpdateSpeed();
    }

    private void UpdateSpeed()
    {
        if (playerController != null && !playerController.isDead)
        {
            float timeAlive = Time.time - startTime;
            CurrentPlayerSpeed = playerSpeedCurve.Evaluate(timeAlive);
            playerController.SetForwardSpeed(CurrentPlayerSpeed);
        }
    }

    // Herhangi bir ölümcül engele çarpıldığında çağrılacak yeni fonksiyon
    public void HandleCollision()
    {
        if (isGameOver) return;

        if (playerController != null)
        {
            // Eğer Dash açıksa ölümsüzdür, hiçbir şey olmaz
            if (playerController.isInvincible)
            {
                return; 
            }

            // Eğer Health PowerUp kalkanı aktifse kalkan kırılır ama oyun devam eder
            if (playerController.healthShieldCount > 0)
            {
                playerController.ConsumeHealthShield();
                
                if (shieldBreakSfxClip != null)
                    AudioSource.PlayClipAtPoint(shieldBreakSfxClip, Camera.main != null ? Camera.main.transform.position : transform.position);
                
                return;
            }
        }

        // Kalkan ve Dash yoksa oyun biter
        GameOver();
    }

    private void GameOver()
    {
        isGameOver = true;
        
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

        yield return new WaitForSecondsRealtime(2f); // 2 Saniye bekle (Kamera devrilirken izlemek için)
        
        RestartGame(); // Otomatik Restart
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
