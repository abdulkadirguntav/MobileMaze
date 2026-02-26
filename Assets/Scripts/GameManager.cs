using UnityEngine;
using UnityEngine.SceneManagement; // Sahneleri yeniden yüklemek için

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Hızı güncellenecek karakter kontrolcüsü")]
    public CharacterController playerController;
    [Tooltip("Z mesafesini hesaplamak için oyuncu referansı")]
    public Transform playerTransform;
    [Tooltip("Dinamik ışıklandırma referansı")]
    public LightingEvolution lightingEvolution;

    [Header("Atmosphere")]
    [Tooltip("Oyun başında çalacak oyuncuyu irkilten ses")]
    public AudioClip scareAudioClip;

    [Header("Game State")]
    public float score;
    public int currentPhase = 1;
    
    [Header("Hardcore Difficulty Settings")]
    [Tooltip("Skora göre engel atılma sıklığı (X: Skor, Y: Z Mesafesi)")]
    public AnimationCurve spawnIntervalCurve = AnimationCurve.Linear(0, 10f, 3000, 3f);
    
    [Tooltip("Skora göre oyuncu hızı (X: Skor, Y: Hız)")]
    public AnimationCurve playerSpeedCurve = AnimationCurve.Linear(0, 40f, 3000, 120f);

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

        if (scareAudioClip != null)
        {
            AudioSource.PlayClipAtPoint(scareAudioClip, Camera.main != null ? Camera.main.transform.position : transform.position);
        }
        
        // İlk hızı ata
        UpdatePhaseAndSpeed();
    }

    void Update()
    {
        if (playerTransform == null || isGameOver) return;

        // Skoru, ilerlenen Z mesafesi olarak (tamsayı) hesapla
        score = Mathf.Floor(playerTransform.position.z - startZ);
        CheckPhases();
        
        // Dinamik ışıklandırmayı skora göre güncelle
        if (lightingEvolution != null)
        {
            lightingEvolution.UpdateLighting(score);
        }

        // Hızı her frame güncelle (Daralan tünel ve hardcore hızlanma)
        UpdatePhaseAndSpeed();
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
        // Mevcut sahneyi en baştan yükle
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
