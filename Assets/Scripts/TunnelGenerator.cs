using System.Collections.Generic;
using UnityEngine;

public class TunnelGenerator : MonoBehaviour
{
    [Tooltip("Tünel modülünün Prefab'ı")]
    public GameObject tunnelPrefab;
    
    [Tooltip("Karakterin Transform'u")]
    public Transform playerTransform;

    [Tooltip("Bir tünel parçasının Z eksenindeki uzunluğu")]
    public float tunnelLength = 20f;

    [Tooltip("Sahnede aynı anda bulunacak tünel parça sayısı")]
    public int activeTunnelsCount = 5;

    [Tooltip("Karakter tünelin sonuna ne kadar yaklaştığında yeni tünel öne alınsın (Z ekseni mesafesi)")]
    public float recycleDistance = 25f;

    private List<GameObject> activeTunnels = new List<GameObject>();
    private float spawnZ = 0f;

    void Start()
    {
        // Başlangıçta belirlediğimiz sayıda tünel parçasını üret (havuzu doldur)
        for (int i = 0; i < activeTunnelsCount; i++)
        {
            SpawnTunnel();
        }
    }

    void Update()
    {
        // Karakterin Z pozisyonu, ilk tünelin başından 'recycleDistance' kadar uzaklaşmışsa, baştakini alıp sona ekle
        if (playerTransform.position.z - tunnelLength > activeTunnels[0].transform.position.z + recycleDistance)
        {
            RecycleTunnel();
        }
    }

    private void SpawnTunnel()
    {
        // Yeni bir tünel instantiate et
        Vector3 spawnPos = new Vector3(transform.position.x, transform.position.y, transform.position.z + spawnZ);
        GameObject go = Instantiate(tunnelPrefab, spawnPos, Quaternion.identity);
        go.transform.SetParent(transform);
        
        // --- Tema Sisteminden Materyal Uygulama ---
        if (ThemeManager.Instance != null)
        {
            Material tunnelMat = ThemeManager.Instance.GetTunnelMaterial();
            if (tunnelMat != null)
            {
                // Tünelin içindeki MeshRenderer'ı bulup materyalini değiştir.
                // Not: Tünelin prefab yapısına göre GetComponentsInChildren de kullanılabilir.
                MeshRenderer[] renderers = go.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer r in renderers)
                {
                    r.material = tunnelMat;
                }
            }
        }

        // Listeye ekle ve bir sonraki tünelin spawn noktasını güncelle
        activeTunnels.Add(go);
        spawnZ += tunnelLength;
    }

    private void RecycleTunnel()
    {
        // En arkadaki (listede 0. indisteki) tüneli al
        GameObject oldTunnel = activeTunnels[0];
        
        // Listeden çıkar
        activeTunnels.RemoveAt(0);
        
        // Pozisyonunu en ileriye taşı
        Vector3 newPos = new Vector3(transform.position.x, transform.position.y, transform.position.z + spawnZ);
        oldTunnel.transform.position = newPos;

        
        // Listeye en sona (yeni en uç) tekrar ekle
        activeTunnels.Add(oldTunnel);
        
        // Bir sonraki tünelin noktasını güncelle
        spawnZ += tunnelLength;
    }
}
