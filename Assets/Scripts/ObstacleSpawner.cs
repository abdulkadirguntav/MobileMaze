using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PatternProbability
{
    public ObstaclePattern pattern;
    [Range(0f, 1f)]
    public float weight = 1f;
}

public enum ObstaclePattern
{
    RandomBlocks,
    CornerChaser,
    FakeOut,
    ShadowHunter,
    SpiralNinja,
    Crusher
}

public class ObstacleSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject obstaclePrefab;
    public GameManager gameManager;
    public Transform playerTransform;

    [Header("Pool Settings")]
    public int poolSize = 100; // Artan hız ve mesafe için havuz kapasitesi 100'e çıkarıldı
    private List<GameObject> obstaclePool = new List<GameObject>();

    [Header("Power-Up Settings")]
    [Tooltip("Sırasıyla eklenecek PowerUp prefableri")]
    public GameObject[] powerUpPrefabs;
    [Range(0f, 1f)] public float powerUpSpawnChance = 0.05f;
    private List<GameObject> powerUpPool = new List<GameObject>();

    [Header("Spawn Settings")]
    public float spawnAheadDistance = 200f; // Pop-in hissini yok etmek için 200'e çıkarıldı
    public int wavesBeforeBreather = 3;
    
    [Header("Pattern Weights")]
    public List<PatternProbability> patternWeights = new List<PatternProbability>
    {
        new PatternProbability { pattern = ObstaclePattern.RandomBlocks, weight = 1f },
        new PatternProbability { pattern = ObstaclePattern.CornerChaser, weight = 0.5f },
        new PatternProbability { pattern = ObstaclePattern.FakeOut, weight = 0.5f },
        new PatternProbability { pattern = ObstaclePattern.ShadowHunter, weight = 0.5f },
        new PatternProbability { pattern = ObstaclePattern.SpiralNinja, weight = 0.5f },
        new PatternProbability { pattern = ObstaclePattern.Crusher, weight = 0.5f }
    };

    private float nextSpawnZ;
    private int wavesSpawnedCount = 0;
    private int breatherRemaining = 0;
    private int lastSafeSlot = 2; // Başlangıçta orta sol güvenli varsayalım

    // 2x2 Grid Merkez Noktaları (0 = Sol Üst | 1 = Sağ Üst | 2 = Sol Alt | 3 = Sağ Alt)
    // 2 birimlik aralıklarla ölçeklendirilmiş hali
    private readonly Vector2[] gridSlots = new Vector2[]
    {
        new Vector2(-1f, 1f),   // 0: Sol Üst
        new Vector2(1f, 1f),    // 1: Sağ Üst
        new Vector2(-1f, -1f),  // 2: Sol Alt
        new Vector2(1f, -1f)    // 3: Sağ Alt
    };

    void Start()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(obstaclePrefab, transform);
            obj.SetActive(false);
            obstaclePool.Add(obj);
        }

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
            
            // İki engel dalgası arasındaki boş mesafe (orta nokta)
            float currentInterval = gameManager.spawnIntervalCurve.Evaluate(gameManager.score);
            
            // %5 (veya ayarlanan) ihtimalle engel dalgaları arasına Power-Up spawnla
            if (powerUpPrefabs != null && powerUpPrefabs.Length > 0 && Random.value < powerUpSpawnChance)
            {
                SpawnPowerUp(nextSpawnZ + currentInterval * 0.5f);
            }

            nextSpawnZ += currentInterval;
        }
    }

    public void ClearPath(float maxDistance)
    {
        // Oyuncunun önündeki aktif engelleri temizler (Clear Path PowerUp)
        foreach (var obj in obstaclePool)
        {
            if (obj.activeInHierarchy)
            {
                float zDist = obj.transform.position.z - playerTransform.position.z;
                if (zDist > 0 && zDist <= maxDistance)
                {
                    obj.SetActive(false);
                }
            }
        }
    }

    private void SpawnObstaclePattern(float zPosition)
    {
        // 1. Nefes Alma (Breather) Boşluğu Kontrolü
        if (breatherRemaining > 0)
        {
            breatherRemaining--;
            return; // Sadece boşluk bırak, engel spawn etme (Power-Up şansı Update içinde atılıyor)
        }

        wavesSpawnedCount++;
        if (wavesSpawnedCount >= wavesBeforeBreather)
        {
            wavesSpawnedCount = 0;
            breatherRemaining = Random.Range(1, 3); // 1 veya 2 boş dalga
        }

        // 2. Rastgele bir patern seç
        ObstaclePattern selectedPattern = GetRandomPattern();
        
        switch (selectedPattern)
        {
            case ObstaclePattern.RandomBlocks:
                SpawnRandomBlocks(zPosition);
                break;
            case ObstaclePattern.CornerChaser:
                SpawnCornerChaser(zPosition);
                break;
            case ObstaclePattern.FakeOut:
                SpawnFakeOut(zPosition);
                break;
            case ObstaclePattern.ShadowHunter:
                SpawnShadowHunter(zPosition);
                break;
            case ObstaclePattern.SpiralNinja:
                SpawnSpiralNinja(zPosition);
                break;
            case ObstaclePattern.Crusher:
                SpawnCrusher(zPosition);
                break;
        }
    }

    private ObstaclePattern GetRandomPattern()
    {
        float totalWeight = 0f;
        foreach (var pw in patternWeights) totalWeight += pw.weight;
        
        float randomVal = Random.Range(0, totalWeight);
        float currentSum = 0f;
        
        foreach (var pw in patternWeights)
        {
            currentSum += pw.weight;
            if (randomVal <= currentSum)
                return pw.pattern;
        }
        return ObstaclePattern.RandomBlocks;
    }

    private void SpawnRandomBlocks(float zPosition)
    {
        int obstacleCount = Random.Range(1, 3); // 2x2 gridde 1 veya 2 engel (daha adil)

        // 1. Garantili Güvenli slot seç (bir öncekine komşu)
        int newSafeSlot = GetSafeAdjacentSlot(lastSafeSlot);
        lastSafeSlot = newSafeSlot;

        // 2. Kalan slotları (safe slot hariç) bul
        List<int> availableSlots = new List<int>();
        for (int i = 0; i < 4; i++) {
            if (i != newSafeSlot) availableSlots.Add(i);
        }
        ShuffleList(availableSlots);

        for (int i = 0; i < obstacleCount; i++)
        {
            SpawnSingleObstacle(availableSlots[i], zPosition, ObstacleBehavior.Standard, out _);
        }
    }

    private void SpawnCornerChaser(float zPosition)
    {
        // 1 tam tur atacak şekilde (offset kullanarak) 1 blok
        int startOffset = Random.Range(0, 4);
        SpawnSingleObstacle(0, zPosition, ObstacleBehavior.CornerChaser, out GameObject obsObj);
        Obstacle obs = obsObj.GetComponent<Obstacle>();
        if (obs != null) obs.SetAnimOffset(startOffset);
        
        lastSafeSlot = GetSafeAdjacentSlot(lastSafeSlot); 
    }

    private void SpawnFakeOut(float zPosition)
    {
        // 4 bloğun hepsi var, 1'i (safe slot yaparsak adil olur) oyuncu yaklaşınca kaybolacak
        int forcedSafeSlot = GetSafeAdjacentSlot(lastSafeSlot);
        lastSafeSlot = forcedSafeSlot;

        for (int i = 0; i < 4; i++)
        {
            ObstacleBehavior behav = (i == forcedSafeSlot) ? ObstacleBehavior.SuddenOpening_Disappear : ObstacleBehavior.SuddenOpening_Wall;
            SpawnSingleObstacle(i, zPosition, behav, out _);
        }
    }

    private void SpawnShadowHunter(float zPosition)
    {
        SpawnSingleObstacle(0, zPosition, ObstacleBehavior.ShadowHunter, out _);
        lastSafeSlot = GetSafeAdjacentSlot(lastSafeSlot);
    }

    private void SpawnSpiralNinja(float zPosition)
    {
        // Spiral sırası (saat yönünde vb)
        int[] spiralPath = { 0, 1, 3, 2 }; // Sol Üst, Sağ Üst, Sağ Alt, Sol Alt
        
        // Z mesafesinde çok küçük farklarla 4 tane doğur (Ardışık)
        float spiralZOffset = 15f; 
        for (int i = 0; i < 4; i++)
        {
            SpawnSingleObstacle(spiralPath[i], zPosition + (i * spiralZOffset), ObstacleBehavior.Standard, out _);
        }
        
        lastSafeSlot = 0; // Spiralin sonu 2 (Sol Alt), komşusu 0 (Sol Üst) diyebiliriz.
        
        // Bu pattern Z ekseninde yer kapladığı için bir sonraki spawn mesafesini uzatalım
        nextSpawnZ += spiralZOffset * 3;
    }

    private void SpawnCrusher(float zPosition)
    {
        // Üst satır veya Alt satırda sağlı sollu 2 Piston
        int row = Random.Range(0, 2);
        int leftSlot = row * 2;
        int rightSlot = row * 2 + 1;

        SpawnSingleObstacle(leftSlot, zPosition, ObstacleBehavior.PistonLeft, out _);
        SpawnSingleObstacle(rightSlot, zPosition, ObstacleBehavior.PistonRight, out _);

        // Kapanan satır dışında kalan satır güvenli
        int safeRow = (row == 0) ? 1 : 0;
        lastSafeSlot = safeRow * 2; // O satırın solu güvenli kabul edilebilir
    }

    // Ortak Yardımcı Metodlar

    private void SpawnSingleObstacle(int slotIndex, float zPosition, ObstacleBehavior behavior, out GameObject spawned)
    {
        spawned = GetPooledObstacle();
        if (spawned != null)
        {
            Vector2 pos = gridSlots[slotIndex];
            Vector3 targetWorldPos = new Vector3(transform.position.x + pos.x, transform.position.y + pos.y, zPosition);
            
            spawned.SetActive(true);

            // Tema Sistemi (Eski koddaki)
            if (ThemeManager.Instance != null)
            {
                Material obstacleMat = ThemeManager.Instance.GetObstacleMaterial();
                if (obstacleMat != null)
                {
                    MeshRenderer[] renderers = spawned.GetComponentsInChildren<MeshRenderer>();
                    foreach (MeshRenderer r in renderers) r.material = obstacleMat;
                }
            }

            Obstacle obsScript = spawned.GetComponent<Obstacle>();
            if (obsScript != null)
            {
                obsScript.Initialize(playerTransform, gameManager, behavior, targetWorldPos);
            }
        }
    }

    private int GetSafeAdjacentSlot(int previousSlot)
    {
        // 0(SolÜst)   1(SağÜst)
        // 2(SolOrta)  3(SağOrta)
        // 4(SolAlt)   5(SağAlt)
        List<int> validNeighbors = new List<int>();

        switch (previousSlot)
        {
            case 0: validNeighbors.AddRange(new int[] { 0, 1, 2, 3 }); break;
            case 1: validNeighbors.AddRange(new int[] { 0, 1, 2, 3 }); break;
            case 2: validNeighbors.AddRange(new int[] { 0, 1, 2, 3, 4, 5 }); break;
            case 3: validNeighbors.AddRange(new int[] { 0, 1, 2, 3, 4, 5 }); break;
            case 4: validNeighbors.AddRange(new int[] { 2, 3, 4, 5 }); break;
            case 5: validNeighbors.AddRange(new int[] { 2, 3, 4, 5 }); break;
            default: return Random.Range(0, 6);
        }

        return validNeighbors[Random.Range(0, validNeighbors.Count)];
    }

    private GameObject GetPooledObstacle()
    {
        foreach (GameObject obj in obstaclePool)
        {
            if (!obj.activeInHierarchy) return obj;
        }
        return null; // Havuz dolduysa null döner
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

    // -- Power-Up Logic --

    private void SpawnPowerUp(float zPosition)
    {
        int prefabIndex = Random.Range(0, powerUpPrefabs.Length);
        GameObject prefabToUse = powerUpPrefabs[prefabIndex];
        if (prefabToUse == null) return;

        GameObject puObj = GetPooledPowerUp(prefabToUse.name);
        if (puObj == null)
        {
            puObj = Instantiate(prefabToUse, transform);
            puObj.name = prefabToUse.name; // Tag veya tanımlama için ismi sabitliyoruz
            powerUpPool.Add(puObj);
        }

        int rndSlot = Random.Range(0, 4);
        Vector2 pos = gridSlots[rndSlot];
        puObj.transform.position = new Vector3(transform.position.x + pos.x, transform.position.y + pos.y, zPosition);
        puObj.SetActive(true);
    }

    private GameObject GetPooledPowerUp(string prefabName)
    {
        foreach (GameObject obj in powerUpPool)
        {
            if (!obj.activeInHierarchy && obj.name == prefabName) return obj;
        }
        return null; // Yoksa null döner, yeni üretilir
    }
}
