using System.Collections;
using UnityEngine;

public class SawLauncher : MonoBehaviour
{
    [Header("Ayarlar")]
    public GameObject sawPrefab; // Fırlatılacak testere prefabı
    public Transform[] firePoints; // 3 adet çıkış noktası (Üst, Orta, Alt)
    public MeshRenderer[] warningLights; // 3 adet ışık objesi
    public float fireRate = 3f; // Kaç saniyede bir testere atsın?

    [Header("Renkler")]
    public Material redMat;    // Ateş eden bölmenin ışığı
    public Material yellowMat; // Sıradaki bölmenin ışığı
    public Material offMat;    // Kapalı bölmenin ışığı

    private int currentIndex = 0; // Şu an hangi bölmedeyiz?
    private Coroutine fireRoutine;

    void OnEnable()
    {
        // Tünel spawn olduğunda döngüyü başlat
        currentIndex = 0;
        fireRoutine = StartCoroutine(FireSequence());
    }

    void OnDisable()
    {
        // Tünel havuza (poole) gidince döngüyü durdur
        if (fireRoutine != null) StopCoroutine(fireRoutine);
    }

    IEnumerator FireSequence()
    {
        while (true) // Tünel aktif olduğu sürece sonsuza kadar döner
        {
            UpdateLights(); // Işıkları ayarla
            
            yield return new WaitForSeconds(fireRate / 2); // Ateşlemeden önce biraz sarı/kırmızı ışığı görsün

            // Testereyi oluştur ve fırlat!
            Instantiate(sawPrefab, firePoints[currentIndex].position, firePoints[currentIndex].rotation);

            yield return new WaitForSeconds(fireRate / 2); // Diğer bölmeye geçmeden önce bekle

            // Bir sonraki bölmeye geç (0->1->2->0 şeklinde başa sarar)
            currentIndex = (currentIndex + 1) % firePoints.Length; 
        }
    }

    void UpdateLights()
    {
        // Hangi ışık yanacak hesapla
        int nextIndex = (currentIndex + 1) % warningLights.Length;
        int offIndex = (currentIndex + 2) % warningLights.Length;

        // Materyalleri ata
        warningLights[currentIndex].material = redMat; // Şu anki Kırmızı
        warningLights[nextIndex].material = yellowMat; // Sonraki Sarı
        warningLights[offIndex].material = offMat;     // Kalan Kapalı
    }
}