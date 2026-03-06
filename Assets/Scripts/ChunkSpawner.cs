using System.Collections.Generic;
using UnityEngine;

public class ChunkSpawner : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("Karakterin Transform'u (Z eksenini takip etmek için)")]
    public Transform playerTransform;

    [Header("Tünel (Chunk) Ayarları")]
    [Tooltip("Elinde bulunan 48 adet tünel prefab'ı")]
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
            
            ApplyTheme(obj);
            
            obj.SetActive(false);
            chunkPool.Add(obj);
        }
    }

    private void SpawnChunk()
    {
        GameObject chunkToSpawn = null;

        // Havuzda kullanılabilir tünel var mı?
        if (chunkPool.Count > 0)
        {
            // Havuzdaki tünellerden rastgele birini seç
            int randomIndex = Random.Range(0, chunkPool.Count);
            chunkToSpawn = chunkPool[randomIndex];
            chunkPool.RemoveAt(randomIndex);
        }
        else
        {
            // Sadece havuz yetersiz gelirse yeni üret (Edge case koruması)
            int prefabIndex = Random.Range(0, tunnelPrefabs.Length);
            chunkToSpawn = Instantiate(tunnelPrefabs[prefabIndex], Vector3.zero, Quaternion.identity);
            chunkToSpawn.transform.SetParent(this.transform);
            ApplyTheme(chunkToSpawn);
            Debug.LogWarning("ChunkSpawner: Havuz yetersiz kaldı, boyutu artırılmalı. Yeni üretildi.");
        }

        // Pozisyonla, aktifleştir ve listeye ekle
        chunkToSpawn.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z + nextSpawnZ);
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
        chunkPool.Add(oldChunk);
    }

    private void ApplyTheme(GameObject chunk)
    {
        // Projedeki mevcut ThemeManager ile tünel materyalini entegre eder
        if (ThemeManager.Instance != null)
        {
            Material tunnelMat = ThemeManager.Instance.GetTunnelMaterial();
            if (tunnelMat != null)
            {
                MeshRenderer[] renderers = chunk.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer r in renderers)
                {
                    r.material = tunnelMat;
                }
            }
        }
    }
}
