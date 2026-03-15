using System.Collections.Generic;
using UnityEngine;

public class ChunkSpawner : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("Karakterin Transform'u (Z eksenini takip etmek için)")]
    public Transform playerTransform;

    [Header("Tünel (Chunk) Ayarları")]
    [Tooltip("Oyun başladığında oyuncunun içinde doğacağı BOŞ tünel prefab'ı")]
    public GameObject emptyTunnelPrefab;

    [Tooltip("Elinde bulunan tünel prefab'ları (İçinde Saw olanları Inspector'dan silebilirsin)")]
    public GameObject[] tunnelPrefabs;
    
    [Tooltip("Her bir tünelin Z eksenindeki net uzunluğu")]
    public float chunkLength = 25f;

    [Tooltip("Sahnede oyun başladığında ve koşarken aynı anda bulunacak aktif tünel parça sayısı")]
    public int activeChunkCount = 5;

    [Tooltip("Havuzun (Pool) başlangıçtaki toplam kapasitesi (Aktifler + Yedekler)")]
    public int initialPoolSize = 15;

    [Tooltip("Oyuncu bir tünelin içine ne kadar girdiğinde arkadaki havuza dönsün?")]
    public float recycleDistance = 30f;

    // Sahnede aktif olan tünellerin listesi (sıralı dizilim)
    private List<GameObject> activeChunks = new List<GameObject>();

    // Sahnede olmayan, bekleyen tüneller (Object Pooling)
    private List<GameObject> chunkPool = new List<GameObject>();
    
    // Bir sonraki tünelin doğacağı Z ekseni noktası
    private float nextSpawnZ = 0f;

    // İlk başlangıç tüneli mi?
    private bool isFirstChunk = true;

    void Start()
    {
        if (tunnelPrefabs == null || tunnelPrefabs.Length == 0)
        {
            Debug.LogError("Lütfen Inspector üzerinden Tunnel Prefab'larını atayın!");
            return;
        }

        if (playerTransform == null)
        {
            Debug.LogError("Lütfen Inspector üzerinden Player objesini atayın!");
            return;
        }

        InitializePool();

        // Oyun başladığında başlangıç tünellerini hizala ve aktifleştir
        for (int i = 0; i < activeChunkCount; i++)
        {
            SpawnChunk();
        }
    }

    void Update()
    {
        if (playerTransform == null || activeChunks.Count == 0) return;

        float playerZ = playerTransform.position.z;
        float oldestChunkZ = activeChunks[0].transform.position.z;

        // Oyuncu, en eski tüneli yeterince geçtiyse onu havuza at ve ileriden yenisini çek
        if (playerZ - oldestChunkZ > recycleDistance)
        {
            RecycleOldestChunk();
            SpawnChunk();
        }
    }

    private void InitializePool()
    {
        // Havuzu prefab'lardan rastgele seçerek baştan doldur ve inaktif olarak beklet
        for (int i = 0; i < initialPoolSize; i++)
        {
            int randomIndex = Random.Range(0, tunnelPrefabs.Length);
            GameObject obj = Instantiate(tunnelPrefabs[randomIndex], Vector3.zero, Quaternion.identity);
            obj.transform.SetParent(this.transform);
            
            ResetChunkState(obj.transform); // Fail-safe: İlk yaratılışta trigger ve aktiflik ayarlarını yap
            
            obj.SetActive(false);
            chunkPool.Add(obj);
        }
    }

    private void SpawnChunk()
    {
        GameObject chunkToSpawn = null;

        if (isFirstChunk && emptyTunnelPrefab != null)
        {
            // OYUN STARTI: İlk tüneli her zaman "emptyTunnelPrefab" yap.
            // Bunu pooling döngüsüne katmıyoruz (veya katabiliriz, ama ilk tünel olarak Instantiate ediyoruz).
            chunkToSpawn = Instantiate(emptyTunnelPrefab, Vector3.zero, Quaternion.identity);
            chunkToSpawn.transform.SetParent(this.transform);
            ResetChunkState(chunkToSpawn.transform);
            isFirstChunk = false;
        }
        else
        {
            // Normal Havuzdan (Pool) tünel çek
            if (chunkPool.Count > 0)
            {
                int randomIndex = Random.Range(0, chunkPool.Count);
                chunkToSpawn = chunkPool[randomIndex];
                chunkPool.RemoveAt(randomIndex);
            }
            else
            {
                // Edge case koruması: Sadece havuz yetersiz gelirse yeni üret
                int prefabIndex = Random.Range(0, tunnelPrefabs.Length);
                chunkToSpawn = Instantiate(tunnelPrefabs[prefabIndex], Vector3.zero, Quaternion.identity);
                chunkToSpawn.transform.SetParent(this.transform);
                ResetChunkState(chunkToSpawn.transform); 
                Debug.LogWarning("ChunkSpawner: Havuz yetersiz kaldı, boyutu artırılmalı. Yeni üretildi.");
            }
        }

        // Z Ekseni Start Noktası (Kaydet)
        float currentSpawnZ = nextSpawnZ;

        // Pozisyonla, aktifleştir ve listeye ekle
        chunkToSpawn.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z + currentSpawnZ);
        chunkToSpawn.SetActive(true);
        activeChunks.Add(chunkToSpawn);

        // Z noktasını ilerlet
        nextSpawnZ += chunkLength;
    }

    private void RecycleOldestChunk()
    {
        GameObject oldChunk = activeChunks[0];
        
        // Aktif listeden çıkar
        activeChunks.RemoveAt(0);

        // Kapat ve havuza geri at (Sıfır Destroy mantığı)
        oldChunk.SetActive(false);
        // INFO: Eğer EmptyTunnel'ın da sonradan tekrar gelmemesini istiyorsak,
        // onu havuza eklemeyebiliriz. Ancak sonsuz koşu için havuza dahil olmasında
        // sakınca yoktur, oyun sırasında nadiren "boş tünel" geçişi sağlayabilir.
        chunkPool.Add(oldChunk);

        // --- OBJECT POOLING RESETLEME MANTIĞI ---
        ResetChunkState(oldChunk.transform);
    }

    private void ResetChunkState(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (child.CompareTag("Obstacle"))
            {
                child.gameObject.SetActive(true); // Engeli geri getir
                
                // FAIL-SAFE: Fiziksel çarpışmayı (sekme/duvara toslama) önlemek için her zaman trigger yap
                Collider col = child.GetComponent<Collider>();
                if (col != null && !col.isTrigger)
                {
                    col.isTrigger = true;
                }
            }
            else if (child.CompareTag("SecretObject"))
            {
                child.gameObject.SetActive(false); // Sırrı tekrar gizle
            }

            // Kendi içinde alt objeleri (çocukları) varsa onlar için de aynı işlemi yap
            if (child.childCount > 0)
            {
                ResetChunkState(child);
            }
        }
    }
}
