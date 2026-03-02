using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;
    public Transform playerTransform;

    [Header("Lab Course Prefabs")]
    [Tooltip("Lazer, Asit, Kapı, Fan, Cam Prefabları buraya atanacak.")]
    public GameObject[] labObstaclePrefabs;
    
    [Header("Power-Up Prefabs")]
    [Tooltip("Kinetic Shield, Chrono-Stasis, EMP prefabları")]
    public GameObject[] powerUpPrefabs;
    [Range(0f, 1f)] public float powerUpSpawnChance = 0.05f;

    [Header("Data Core Settings")]
    public GameObject dataCorePrefab;
    [Range(0f, 1f)] public float dataCoreSpawnChance = 0.05f;

    [Header("Spawn Settings")]
    public float laneDistance = 3f; // Şerit aralıkları (PlayerController ile aynı olmalı)
    public float spawnAheadDistance = 200f; 
    
    // Nefes Alma / Spawn Boşluğu Kontrolleri
    public int wavesBeforeBreather = 3;
    private int wavesSpawnedCount = 0;
    private int breatherRemaining = 0;

    private float nextSpawnZ;

    // Performans için obje havuzu yerine, temizlik yapan dinamik liste 
    // (Yeni mekanikleri test edebilmen için Instantiate modeline geçildi)
    private List<GameObject> activeObjects = new List<GameObject>();

    void Start()
    {
        if (playerTransform != null)
        {
            nextSpawnZ = playerTransform.position.z + spawnAheadDistance;
        }
    }

    void Update()
    {
        if (playerTransform == null || gameManager == null) return;

        if (playerTransform.position.z + spawnAheadDistance > nextSpawnZ)
        {
            SpawnObstaclePattern(nextSpawnZ);

            float currentInterval = gameManager.spawnIntervalCurve.Evaluate(gameManager.score);
            nextSpawnZ += currentInterval;
        }

        CleanupBehindPlayer();
    }

    private void SpawnObstaclePattern(float zPos)
    {
        // Nefes Alma (Breather) Boşluğu Kontrolü
        if (breatherRemaining > 0)
        {
            breatherRemaining--;
            return; 
        }

        wavesSpawnedCount++;
        if (wavesSpawnedCount >= wavesBeforeBreather)
        {
            wavesSpawnedCount = 0;
            breatherRemaining = Random.Range(1, 3); // 1 veya 2 boş dalga
        }

        // 3 Şerit: -laneDistance, 0, +laneDistance
        float[] xPositions = { -laneDistance, 0, laneDistance };
        
        // 1 veya 2 şeridi engelle (En az 1 şerit boş kalsın)
        int obstaclesToSpawn = Random.Range(1, 3); 
        
        List<int> availableLanes = new List<int> { 0, 1, 2 };
        ShuffleList(availableLanes);

        for (int i = 0; i < obstaclesToSpawn; i++)
        {
            int lane = availableLanes[i];
            SpawnRandomObstacle(xPositions[lane], zPos);
        }

        // Kalan güvenli şerit (Boşluk)
        int safeLane = availableLanes[2];
        float safeX = xPositions[safeLane];

        // Güvenli şeritte ödül şansı (Data Core > PowerUp)
        if (dataCorePrefab != null && Random.value < dataCoreSpawnChance)
        {
            SpawnObject(dataCorePrefab, safeX, zPos);
        }
        else if (powerUpPrefabs != null && powerUpPrefabs.Length > 0 && Random.value < powerUpSpawnChance)
        {
            int rnd = Random.Range(0, powerUpPrefabs.Length);
            SpawnObject(powerUpPrefabs[rnd], safeX, zPos);
        }
    }

    private void SpawnRandomObstacle(float xPos, float zPos)
    {
        if (labObstaclePrefabs == null || labObstaclePrefabs.Length == 0) return;

        int rnd = Random.Range(0, labObstaclePrefabs.Length);
        GameObject prefab = labObstaclePrefabs[rnd];
        
        // Pervane veya Karantina Kapısı gibi objeler genelde tüm koridoru kaplar ve merkezde (x=0) doğması istenir.
        // Şimdilik sistemin çalışması için her biri bir şeridin merkezine spawn edilecek şekilde tasarlandı.
        // Daha gelişmiş ayarlar için prefab özellikleri Inspector'dan "Geniş Engel" olarak işaretlenip xPos=0 yapılabilir.
        SpawnObject(prefab, xPos, zPos);
    }

    private GameObject SpawnObject(GameObject prefab, float xPos, float zPos)
    {
        GameObject obj = Instantiate(prefab, new Vector3(xPos, transform.position.y, zPos), Quaternion.identity);
        obj.transform.parent = this.transform;
        activeObjects.Add(obj);
        return obj;
    }

    // Clear Path (Dash / Yenilmezlik vb. yetenekler için) engelleri haritadan temizler
    public void ClearPath(float maxDistance)
    {
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = activeObjects[i];
            if (obj != null && obj.activeInHierarchy)
            {
                float zDist = obj.transform.position.z - playerTransform.position.z;
                if (zDist > 0 && zDist <= maxDistance)
                {
                    // Engeller kapatılır veya kırılır
                    Destroy(obj);
                    activeObjects.RemoveAt(i);
                }
            }
        }
    }

    private void CleanupBehindPlayer()
    {
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = activeObjects[i];
            if (obj == null) // Oyun içinde bir yetenekle yok edilmişse (Dash/EMP) listeden çıkar
            {
                activeObjects.RemoveAt(i);
                continue;
            }

            // Karakteri geçen engelleri sil
            if (obj.transform.position.z < playerTransform.position.z - 20f)
            {
                Destroy(obj);
                activeObjects.RemoveAt(i);
            }
        }
    }

    private void ShuffleList(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}
