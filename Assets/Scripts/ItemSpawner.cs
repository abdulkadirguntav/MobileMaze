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

    [Header("3. İhtimaller (Toplam 100 Olmalı)")]
    [Range(0, 100)] public float emptyChance = 70f;
    [Range(0, 100)] public float coinChance = 30f;

    // Sahnede üretilen eşyaları havuz temizliği için hafızada tutarız
    private List<GameObject> spawnedItems = new List<GameObject>();
    private bool hasStarted = false;

    private void Start()
    {
        hasStarted = true;
        SpawnItems(); // İlk yaratılış (Instantiate) anında eşyaları bas
    }

    // ChunkSpawner tarafından bu tünel sahneye çıkarıldığında (Object Pooling - Aktif olduğunda) çalışır
    private void OnEnable()
    {
        // Start'tan önce OnEnable çalışır, ilk Instantiate anında çifte spawn olmasını engelliyoruz.
        if (hasStarted)
        {
            SpawnItems();
        }
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
        float totalWeight = emptyChance + coinChance;

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
            else
            {
                // Zarlar "Altın (Coin)" ihtimaline denk geldi
                if (coinPrefab != null)
                {
                    spawnedObj = Instantiate(coinPrefab, point.position, point.rotation, point);
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
        // Tünel havuza giderken içinde kalan alınmamış Altınları yok et ki birikmesin
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
