using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject obstaclePrefab;
    public GameManager gameManager;
    public Transform playerTransform;

    [Header("Pool Settings")]
    public int poolSize = 60; // Grid arttığı için havuzu büyütüyoruz
    private List<GameObject> obstaclePool = new List<GameObject>();

    [Header("Spawn Settings")]
    public float spawnAheadDistance = 50f;
    
    private float nextSpawnZ;
    private int deathZigZagRemaining = 0;
    private int lastEmptySlot = -1;

    // 2x3 Grid Merkez Noktaları (0, 1 = Üst | 2, 3 = Orta | 4, 5 = Alt)
    // 2 birimlik aralıklarla ölçeklendirilmiş hali
    private readonly Vector2[] gridSlots = new Vector2[]
    {
        new Vector2(-1f, 2f),   // 0: Sol Üst
        new Vector2(1f, 2f),    // 1: Sağ Üst
        new Vector2(-1f, 0f),   // 2: Sol Orta
        new Vector2(1f, 0f),    // 3: Sağ Orta
        new Vector2(-1f, -2f),  // 4: Sol Alt
        new Vector2(1f, -2f)    // 5: Sağ Alt
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
            
            float currentInterval = gameManager.spawnIntervalCurve.Evaluate(gameManager.score);
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
        int phase = gameManager.currentPhase;

        // Rastgele bir pattern seç
        float rand = Random.value;
        
        // Devam eden bir sekans varsa onu oynat (Death ZigZag)
        if (deathZigZagRemaining > 0)
        {
            SpawnDeathZigZag(zPosition);
            return;
        }

        if (phase >= 3)
        {
            // Yeni Animated Snake ve Checkerboard patternlerini yüksek oranda çağır
            if (rand < 0.15f) { InitiateAnimatedSnake(zPosition); return; }
            if (rand < 0.30f) { InitiateAnimatedCheckerboard(zPosition); return; }
            if (rand < 0.40f) { SpawnSuddenBlockade(zPosition); return; }
            if (rand < 0.50f) { SpawnSuddenOpening(zPosition); return; }
            if (rand < 0.60f) { SpawnClosingGate(zPosition); return; }
            if (rand < 0.70f) { SpawnDiagonalSpinner(zPosition); return; }
            if (rand < 0.80f) { SpawnGuillotine(zPosition); return; }
            if (rand < 0.90f) { InitiateDeathZigZag(zPosition); return; }
        }
        else if (phase >= 2)
        {
            if (rand < 0.15f) { InitiateAnimatedSnake(zPosition); return; }
            if (rand < 0.30f) { InitiateAnimatedCheckerboard(zPosition); return; }
            if (rand < 0.40f) { SpawnClosingGate(zPosition); return; }
            if (rand < 0.50f) { SpawnDiagonalSpinner(zPosition); return; }
        }

        // --- Normal Standart Spawn (1 ile 4 arası küp) ---
        int obstacleCount = Random.Range(1, 4); // 6 slot olduğu için 3-4 e kadar çıkabilir
        if (phase >= 3) obstacleCount = Random.Range(2, 5); 

        List<int> slots = new List<int> { 0, 1, 2, 3, 4, 5 };
        ShuffleList(slots);

        List<GameObject> spawned = new List<GameObject>();
        for (int i = 0; i < obstacleCount; i++)
        {
            SpawnSingleObstacle(slots[i], zPosition, ObstacleBehavior.Standard, out GameObject ob);
            if (ob != null) spawned.Add(ob);
        }

        // Fake-out (Şaşırtmaca) ihtimali
        if (phase >= 3 && Random.value < 0.3f && spawned.Count > 0)
        {
            Obstacle obs = spawned[Random.Range(0, spawned.Count)].GetComponent<Obstacle>();
            
            // Kullanılmayan boş bir slota (örn: slots[obstacleCount]) geçmesi için FakeOut ayarla
            int emptySlot = slots[obstacleCount]; 
            Vector2 targetPos2D = gridSlots[emptySlot];
            Vector3 fakeOutTarget = new Vector3(transform.position.x + targetPos2D.x, transform.position.y + targetPos2D.y, zPosition);
            
            obs.currentBehavior = ObstacleBehavior.FakeOut;
            obs.SetFakeOutTarget(fakeOutTarget);
        }
    }

    // -- Yeni Animated Checkerboard ve Snake Patternleri --

    private void InitiateAnimatedCheckerboard(float zPosition)
    {
        // 0-3-4 (Sol Üst, Sağ Orta, Sol Alt) doğur, sağ-sola hareketlenip 1-2-5 yerlerine geçerler.
         bool startRight = Random.value > 0.5f;
         List<int> slots = startRight ? new List<int> { 1, 2, 5 } : new List<int> { 0, 3, 4 };
        
        foreach (int slot in slots)
        {
            SpawnSingleObstacle(slot, zPosition, ObstacleBehavior.AnimatedCheckerboard, out _);
        }
    }

    private void InitiateAnimatedSnake(float zPosition)
    {
        int blocks = Random.Range(4, 6); // 4 or 5 blocks (leaving 1 or 2 holes)
        int startOffset = Random.Range(0, 6);
        for (int i = 0; i < blocks; i++)
        {
            float offset = (startOffset + i);
            SpawnSingleObstacle(0, zPosition, ObstacleBehavior.AnimatedSnake, out GameObject obsObj);
            Obstacle obs = obsObj.GetComponent<Obstacle>();
            if (obs != null) obs.SetAnimOffset(offset);
        }
    }

    // -- Özel Pattern Dağıtımları --

    private void SpawnDeathZigZag(float zPosition)
    {
        int forcedEmptySlot = GetNextZigZagSlot(lastEmptySlot);
        lastEmptySlot = forcedEmptySlot;
        deathZigZagRemaining--;

        for (int i = 0; i < 6; i++)
        {
            if (i != forcedEmptySlot)
                SpawnSingleObstacle(i, zPosition, ObstacleBehavior.Standard, out _);
        }
    }

    private void InitiateDeathZigZag(float zPosition)
    {
        deathZigZagRemaining = Random.Range(3, 5);
        lastEmptySlot = Random.Range(0, 6);
        SpawnDeathZigZag(zPosition);
    }

    private void SpawnSuddenBlockade(float zPosition)
    {
        // 4 veya 5 blok aniden belirir
        int count = Random.Range(4, 6);
        List<int> slots = new List<int> { 0, 1, 2, 3, 4, 5 };
        ShuffleList(slots);

        for (int i = 0; i < count; i++)
        {
            SpawnSingleObstacle(slots[i], zPosition, ObstacleBehavior.SuddenBlockade, out _);
        }
    }

    private void SpawnSuddenOpening(float zPosition)
    {
        // 6 bloğun hepsi var, 1'i oyuncu yaklaşınca kaybolacak
        int openingSlot = Random.Range(0, 6);
        for (int i = 0; i < 6; i++)
        {
            ObstacleBehavior behav = (i == openingSlot) ? ObstacleBehavior.SuddenOpening_Disappear : ObstacleBehavior.SuddenOpening_Wall;
            SpawnSingleObstacle(i, zPosition, behav, out _);
        }
    }

    private void SpawnClosingGate(float zPosition)
    {
        // Aynı yatay sırada sağlı sollu 2 Piston
        // Random satır seç (Üst: 0-1, Orta: 2-3, Alt: 4-5)
        int row = Random.Range(0, 3);
        int leftSlot = row * 2;
        int rightSlot = row * 2 + 1;

        SpawnSingleObstacle(leftSlot, zPosition, ObstacleBehavior.PistonLeft, out _);
        SpawnSingleObstacle(rightSlot, zPosition, ObstacleBehavior.PistonRight, out _);
    }

    private void SpawnGuillotine(float zPosition)
    {
        // Sadece üst satırda (0 veya 1 numaralı slotlar) spawn olmalı ve alt slotlara doğru düşmeli
        int col = Random.Range(0, 2); // 0 (Sol) veya 1 (Sağ)
        // Alt satıra kadar düşecek, biz hedef olarak alt veya orta satırı verebiliriz (böylece orayı kapatır)
        int targetSlot = (Random.value > 0.5f) ? col + 2 : col + 4; // Orta veya Alt slot
        
        SpawnSingleObstacle(targetSlot, zPosition, ObstacleBehavior.Guillotine, out _);
    }

    private void SpawnDiagonalSpinner(float zPosition)
    {
        int slot = Random.Range(0, 6);
        SpawnSingleObstacle(slot, zPosition, ObstacleBehavior.DiagonalSpinner, out _);
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

    private int GetNextZigZagSlot(int previous)
    {
        // Basit bir yakın slot seçici (Tam karşısına geçebilir veya çapraz ilerleyebilir)
        List<int> valid = new List<int>();
        for(int i = 0; i < 6; i++) { if (i != previous) valid.Add(i); }
        return valid[Random.Range(0, valid.Count)];
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
}
