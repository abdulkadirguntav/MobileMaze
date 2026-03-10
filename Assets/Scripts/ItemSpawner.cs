using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [Header("1. Güvenli Spawn Noktaları")]
    [Tooltip("Tünel prefabının içine elle koyduğunuz boş Noktalar (Transformlar)")]
    public List<Transform> spawnPoints;

    [Header("2. Üretilecek Objeler (Prefablar)")]
    [Tooltip("Altın/Coin Prefabı")]
    public GameObject coinPrefab;
    [Tooltip("Listeden rastgele seçilecek PowerUp Prefabları")]
    public GameObject[] powerUpPrefabs;

    [Header("3. İhtimaller (Toplam 100 Olmalı)")]
    [Range(0, 100)] public float emptyChance = 70f;
    [Range(0, 100)] public float coinChance = 10f;
    [Range(0, 100)] public float powerUpChance = 20f;

    // Sahnede üretilen eşyaları havuz temizliği için hafızada tutarız
    private List<GameObject> spawnedItems = new List<GameObject>();

    // ChunkSpawner tarafından bu tünel sahneye çıkarıldığında (Object Pooling - Aktif olduğunda) çalışır
    private void OnEnable()
    {
        SpawnItems();
    }

    // Tünel oyuncunun arkasında kalıp havuza geri atıldığında (Deaktif olduğunda) çalışır
    private void OnDisable()
    {
        ClearItems();
    }

    private void SpawnItems()
    {
        if (spawnPoints == null || spawnPoints.Count == 0) return;

        // Her ihtimale karşı yüzdeleri toplamına göre normalize edelim ki mantık hatası olmasın
        float totalWeight = emptyChance + coinChance + powerUpChance;

        foreach (Transform point in spawnPoints)
        {
            // O nokta için 0 ile toplam(örn:100) arası rastgele zar at
            float randomVal = Random.Range(0f, totalWeight);
            GameObject spawnedObj = null;

            if (randomVal <= emptyChance)
            {
                // Zarlar "Boş İhtimaline" denk geldi, bu noktaya hiçbir şey üretme
                continue;
            }
            else if (randomVal <= emptyChance + coinChance)
            {
                // Zarlar "Altın (Coin)" ihtimaline denk geldi
                if (coinPrefab != null)
                {
                    spawnedObj = Instantiate(coinPrefab, point.position, point.rotation, point);
                }
            }
            else
            {
                // Zarlar "Power-Up" ihtimaline denk geldi
                if (powerUpPrefabs != null && powerUpPrefabs.Length > 0)
                {
                    int randIdx = Random.Range(0, powerUpPrefabs.Length);
                    spawnedObj = Instantiate(powerUpPrefabs[randIdx], point.position, point.rotation, point);
                }
            }

            // Eğer bir eşya ürettiysek, silmek için listeye kaydet
            if (spawnedObj != null)
            {
                spawnedItems.Add(spawnedObj);
            }
        }
    }

    private void ClearItems()
    {
        // Tünel havuza giderken içinde kalan alınmamış Altın ve PowerUpları yok et ki birikmesin
        foreach (GameObject item in spawnedItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        
        spawnedItems.Clear();
    }
}
