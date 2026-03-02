using UnityEngine;
using UnityEngine.SceneManagement; // Sahneleri yeniden yüklemek için

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Hızı güncellenecek karakter kontrolcüsü")]
    public PlayerController playerController;
    [Tooltip("Z mesafesini hesaplamak için oyuncu referansı")]
    public Transform playerTransform;
    [Tooltip("Dinamik ışıklandırma referansı")]
    public LightingEvolution lightingEvolution;

    [Header("Audio System")]
    [Tooltip("Arka plan müziğini çalacak AudioSource")]
    public AudioSource bgmAudioSource;
    [Tooltip("Oyuncu öldüğünde çalacak tek seferlik ses")]
    public AudioClip crashSfxClip;

    [Header("3D Score System")]
    [Tooltip("Diegetic 3D Skor script referansı")]
    public Score3DDisplay score3DDisplay;

    [Header("Game State")]
    public float score;
    public float bestScore;
    public int currentPhase = 1;
    
    [Header("Hardcore Difficulty Settings")]
    [Tooltip("Skora göre engel atılma sıklığı (X: Skor, Y: Z Mesafesi)")]
    public AnimationCurve spawnIntervalCurve = AnimationCurve.Linear(0, 10f, 3000, 3f);
    
    [Tooltip("Skora göre oyuncu hızı (X: Skor, Y: Hız) - Öncekine göre 3 KAT hızlandırıldı")]
    public AnimationCurve playerSpeedCurve = AnimationCurve.Linear(0, 120f, 3000, 360f);

    [Tooltip("Skora göre Fake-out geçiş (Lerp) hızı")]
    public AnimationCurve fakeOutLerpSpeedCurve = AnimationCurve.Linear(0, 30f, 3000, 80f);

    [Tooltip("Oyuncunun engele çarpmasına kaç saniye kala fake atılacak? (Daha düşük = Daha zor)")]
    public float fakeOutTimeAllowance = 0.15f; 

    public float CurrentPlayerSpeed { get; private set; }
    
    private float startZ;

    [Header("UI & Game Over")]
    public GameObject gameOverPanel;
    private bool isGameOver = false;

    void Start()
    {
        if (playerTransform != null)
        {
            startZ = playerTransform.position.z;
        }

        bestScore = PlayerPrefs.GetFloat("BestScore", 0f);

        // BGM (Arka Plan Müziği) başlat (Eğer atandıysa)
        if (bgmAudioSource != null && bgmAudioSource.clip != null)
        {
            bgmAudioSource.loop = true;
            bgmAudioSource.Play();
        }
        
        // İlk hızı ata
        UpdatePhaseAndSpeed();
    }

    void Update()
    {
        if (playerTransform == null || isGameOver) return;

        // Skoru, ilerlenen Z mesafesi olarak (tamsayı) hesapla
        score = Mathf.Floor(playerTransform.position.z - startZ);
        if (score > bestScore)
        {
            bestScore = score;
        }

        CheckPhases();
        
        // Dinamik ışıklandırmayı skora göre güncelle
        if (lightingEvolution != null)
        {
            lightingEvolution.UpdateLighting(score);
        }

        // Hızı her frame güncelle (Daralan tünel ve hardcore hızlanma)
        UpdatePhaseAndSpeed();
        
        // 3D Skoru Güncelle
        if (score3DDisplay != null)
        {
            score3DDisplay.UpdateScore(score, bestScore);
        }
    }

    private void CheckPhases()
    {
        currentPhase = 1;
        if (score >= 1500) currentPhase = 3;
        else if (score >= 500) currentPhase = 2;
    }

    private void UpdatePhaseAndSpeed()
    {
        CurrentPlayerSpeed = playerSpeedCurve.Evaluate(score);
        if (playerController != null)
        {
            playerController.SetForwardSpeed(CurrentPlayerSpeed);
        }
    }

    public void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        
        PlayerPrefs.SetFloat("BestScore", bestScore);
        PlayerPrefs.Save();
        
        StartCoroutine(HitStopRoutine());
    }

    private System.Collections.IEnumerator HitStopRoutine()
    {
        // BGM durdur ve Çarpışma sesini çal
        if (bgmAudioSource != null) bgmAudioSource.Stop();
        if (crashSfxClip != null)
        {
            AudioSource.PlayClipAtPoint(crashSfxClip, Camera.main != null ? Camera.main.transform.position : transform.position);
        }

        // --- JUICE: Hit-Stop ---
        Time.timeScale = 0.05f;

        // Camera Shake tetikle
        if (Camera.main != null)
        {
            CameraController cam = Camera.main.GetComponent<CameraController>();
            if (cam != null) cam.TriggerHitStopShake();
        }

        // 1 saniye gerçek zamanda bekle (timeScale 0.05 olduğu için)
        yield return new WaitForSecondsRealtime(1f);

        Time.timeScale = 0f;
        
        // Karakteri durdur
        if (playerController != null)
        {
            playerController.SetForwardSpeed(0f);
            playerController.enabled = false; // Girdi almasını engelle
        }

        // Oyun Bitti UI ekranını aç
        if (gameOverPanel != null)
        {
            Debug.Log("UI açıldı");
            gameOverPanel.SetActive(true);
        }
    }

    // Butona basıldığında çağrılacak
    public void RestartGame()
    {
        Time.timeScale = 1f; // Hit-stop sonrası restart yapılırken zamanı düzelt
        // Mevcut sahneyi en baştan yükle
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
