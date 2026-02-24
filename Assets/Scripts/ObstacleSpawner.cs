using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject obstaclePrefab;
    public GameManager gameManager;
    public Transform playerTransform;

    [Header("Pool Settings")]
    public int poolSize = 30;
    private List<GameObject> obstaclePool = new List<GameObject>();

    [Header("Spawn Settings")]
    [Tooltip("Oyuncunun Z ekseninde ne kadar ilerisine engel atılacak")]
    public float spawnAheadDistance = 50f;
    
    // Artık spawnInterval GameManager üzerinden dinamik hesaplanacak
    
    private float nextSpawnZ;
    private int deathZigZagRemaining = 0;
    private int lastEmptySlot = -1;

    // 2x2 Grid Merkez Noktaları (Karakter Controller'daki min/max değerlere uyumlu)
    private readonly Vector2[] gridSlots = new Vector2[]
    {
        new Vector2(-0.5f, 0.5f),  // Sol Üst
        new Vector2(0.5f, 0.5f),   // Sağ Üst
        new Vector2(-0.5f, -0.5f), // Sol Alt
        new Vector2(0.5f, -0.5f)   // Sağ Alt
    };

    void Start()
    {
        // Havuzu doldur
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

        // Karakter sıradaki engelin atılacağı mesafeye yaklaştıysa
        if (playerTransform.position.z + spawnAheadDistance > nextSpawnZ)
        {
            SpawnObstaclePattern(nextSpawnZ);
            
            // Yeni mesafe (zorluk) oyun yöneticisindeki curve üzerinden belirleniyor
            float currentInterval = gameManager.spawnIntervalCurve.Evaluate(gameManager.score);
            nextSpawnZ += currentInterval;
        }
    }

    private void SpawnObstaclePattern(float zPosition)
    {
        int phase = gameManager.currentPhase;
        int obstacleCount = 1;
        int forcedEmptySlot = -1;

        // Death Zig-Zag kontrolü
        if (deathZigZagRemaining > 0)
        {
            obstacleCount = 3; // Daima 3 dolu, 1 boş
            forcedEmptySlot = GetDiagonalSlot(lastEmptySlot);
            lastEmptySlot = forcedEmptySlot;
            deathZigZagRemaining--;
        }
        else
        {
            // Death Zig-Zag başlatma ihtimali (Zorluk Artışı)
            if (phase >= 3 && Random.value < 0.2f) // %20 ihtimalle başla
            {
                deathZigZagRemaining = Random.Range(3, 5); // 3 veya 4 ardışık 3'lü
                obstacleCount = 3;
                forcedEmptySlot = Random.Range(0, 4);
                lastEmptySlot = forcedEmptySlot;
                deathZigZagRemaining--;
            }
            else
            {
                // Normal Faz Kalıpları
                List<int> possibleCounts = new List<int> { 1, 2 }; 
                if (phase >= 2) possibleCounts.Add(3); 
                if (phase >= 3) { possibleCounts.Add(3); possibleCounts.Add(3); } // 3 ihtimali daha yüksek

                obstacleCount = possibleCounts[Random.Range(0, possibleCounts.Count)];
            }
        }

        List<int> slots = new List<int> { 0, 1, 2, 3 };
        
        if (forcedEmptySlot != -1)
        {
            slots.Remove(forcedEmptySlot); // Sadece boş kalacak slotu çıkart, kalan 3'ü dolu olsun
        }
        else
        {
            ShuffleList(slots);
        }

        List<GameObject> spawnedInThisPattern = new List<GameObject>();
        List<int> occupiedSlots = new List<int>();
        List<int> emptySlots = new List<int>();

        for (int i = 0; i < obstacleCount; i++)
        {
            GameObject ob = GetPooledObstacle();
            if (ob != null)
            {
                Vector2 pos = gridSlots[slots[i]];
                
                float spawnX = transform.position.x + pos.x;
                float spawnY = transform.position.y + pos.y;
                ob.transform.position = new Vector3(spawnX, spawnY, zPosition);
                ob.SetActive(true);

                Obstacle obsScript = ob.GetComponent<Obstacle>();
                if (obsScript != null)
                {
                    obsScript.Initialize(playerTransform, gameManager);
                    spawnedInThisPattern.Add(ob);
                }
                occupiedSlots.Add(slots[i]);
            }
        }

        for (int i = 0; i < 4; i++)
        {
            if (!occupiedSlots.Contains(i)) emptySlots.Add(i);
        }

        // Şaşırtmaca (Fake-out) Sadece Phase 3'te aktif
        if (phase >= 3)
        {
            // Çiftli Şaşırtmaca (Double Mind-Game): Eğer tam 2 küp varsa ve fake out olursa
            if (obstacleCount == 2 && Random.value <= 0.6f) // %60 ihtimal
            {
                ShuffleList(emptySlots); 
                List<GameObject> actors = new List<GameObject>(spawnedInThisPattern);
                ShuffleGameObjectList(actors);

                Obstacle obs1 = actors[0].GetComponent<Obstacle>();
                Obstacle obs2 = actors[1].GetComponent<Obstacle>();

                // 1. Küp boş bir hedefe gitsin
                Vector2 targetPos1 = gridSlots[emptySlots[0]];
                Vector3 fakeTarget1 = new Vector3(transform.position.x + targetPos1.x, transform.position.y + targetPos1.y, zPosition);
                
                // 2. Küp 1. Küp'ün ESKİ yerine geçsin (Zihin oyunları)
                Vector3 fakeTarget2 = actors[0].transform.position; // Eski pozisyonu

                obs1.SetFakeOutTarget(fakeTarget1);

                // İkinci kübün gidişini bir tık geciktirebilmek veya aynı anda yapmak
                obs2.SetFakeOutTarget(fakeTarget2);
            }
            else
            {
                // Normal şaşırtmaca: En fazla boş slot sayısı kadar olabilir.
                int maxFakeOuts = Mathf.Min(emptySlots.Count, spawnedInThisPattern.Count);
                
                if (Random.value <= 0.6f && maxFakeOuts > 0)
                {
                    ShuffleList(emptySlots); 
                    List<GameObject> actors = new List<GameObject>(spawnedInThisPattern);
                    ShuffleGameObjectList(actors);

                    int fakeOutCount = Random.Range(1, maxFakeOuts + 1);

                    for (int i = 0; i < fakeOutCount; i++)
                    {
                        Obstacle obsScript = actors[i].GetComponent<Obstacle>();
                        if (obsScript != null)
                        {
                            Vector2 targetPos2D = gridSlots[emptySlots[i]];
                            Vector3 fakeOutTarget = new Vector3(transform.position.x + targetPos2D.x, transform.position.y + targetPos2D.y, zPosition);
                            obsScript.SetFakeOutTarget(fakeOutTarget);
                        }
                    }
                }
            }
        }
    }

    private int GetDiagonalSlot(int previousEmpty)
    {
        // gridSlots İndeksleri:
        // 0: Sol-Üst, 1: Sağ-Üst
        // 2: Sol-Alt, 3: Sağ-Alt
        if (previousEmpty == 0) return 3;
        if (previousEmpty == 1) return 2;
        if (previousEmpty == 2) return 1;
        if (previousEmpty == 3) return 0;
        return Random.Range(0, 4);
    }

    private GameObject GetPooledObstacle()
    {
        foreach (GameObject obj in obstaclePool)
        {
            if (!obj.activeInHierarchy)
            {
                return obj;
            }
        }
        return null;
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

    private void ShuffleGameObjectList(List<GameObject> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            GameObject temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}
