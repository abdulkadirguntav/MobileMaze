using System.Collections.Generic;
using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    [Header("Ayarlar")]
    [Tooltip("Sahnede spawn olabilecek Power-Up prefabları")]
    public GameObject[] powerUpPrefabs;

    [Tooltip("Her bir tünel (chunk) spawn olduğunda PowerUp çıkma ihtimali (0 ile 1 arası, 0.3 = %30)")]
    [Range(0f, 1f)]
    public float spawnChance = 0.3f;

    [Tooltip("Şerit X Pozisyonları (Sol, Orta, Sağ)")]
    public float[] lanePositions = { -3f, 0f, 3f }; // PlayerController'daki laneDistance değerleriyle uyumlu olmalı

    [Tooltip("PowerUp'ın yerden yüksekliği (Y Ekseninde)")]
    public float spawnHeight = 1f;

    // Aktif spawnlanmış power-up'ları takip etmek için basit bir liste
    private List<GameObject> activePowerUps = new List<GameObject>();

    /// <summary>
    /// ChunkSpawner tarafından yeni bir tünel spawn edildiğinde çağrılır.
    /// </summary>
    /// <param name="chunkZPosition">Tünelin Z eksenindeki başlama noktası</param>
    /// <param name="chunkLength">Tünelin Z eksenindeki uzunluğu</param>
    public void TrySpawnPowerUpInChunk(float chunkZPosition, float chunkLength)
    {
        if (powerUpPrefabs == null || powerUpPrefabs.Length == 0) return;

        // Rastgele şans hesaplaması
        if (Random.value <= spawnChance)
        {
            // Rastgele bir PowerUp Prefabı Seç
            int prefabIndex = Random.Range(0, powerUpPrefabs.Length);
            GameObject selectedPrefab = powerUpPrefabs[prefabIndex];

            // 3 Şeritten rastgele birini seç
            float randomX = lanePositions[Random.Range(0, lanePositions.Length)];

            // Tünelin (Chunk) Z ekseninde rastgele bir derinlik noktası seç
            float randomZ = chunkZPosition + Random.Range(5f, chunkLength - 5f);

            Vector3 spawnPosition = new Vector3(randomX, spawnHeight, randomZ);

            // PowerUp'ı Spawn Et
            GameObject spawnedPowerUp = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
            
            // Hiyerarşi düzeni için bu objenin altına koy
            spawnedPowerUp.transform.SetParent(this.transform);

            activePowerUps.Add(spawnedPowerUp);
        }
    }

    /// <summary>
    /// ChunkSpawner bir tüneli havuza geri gönderdiğinde, o tüneldeki (arkada kalan) kullanılmamış PowerUp'ları temizler.
    /// </summary>
    public void CleanUpPowerUpsBehind(float playerZPosition, float recycleDistance)
    {
        // Geriye doğru döngü yaparak arkada kalanları güvenle siliyoruz
        for (int i = activePowerUps.Count - 1; i >= 0; i--)
        {
            GameObject pu = activePowerUps[i];
            
            // Eğer power-up oyuncu tarafından alınmış (Destroy olmuş) ise listeden çıkar
            if (pu == null)
            {
                activePowerUps.RemoveAt(i);
                continue;
            }

            // Eğer oyuncunun recycleDistance kadar arkasında kaldıysa yok et
            if (playerZPosition - pu.transform.position.z > recycleDistance)
            {
                Destroy(pu);
                activePowerUps.RemoveAt(i);
            }
        }
    }
}
