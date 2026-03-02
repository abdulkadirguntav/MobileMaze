using UnityEngine;

public class GlassObstacle : MonoBehaviour
{
    [Header("Settings")]
    public GameObject unbrokenModel;
    public GameObject shatteredParticlesPrefab;

    public void OnHit()
    {
        GameManager.Instance.HandleGlassCollision();

        // Cam kırma efekti
        if (unbrokenModel != null)
            unbrokenModel.SetActive(false);

        if (shatteredParticlesPrefab != null)
        {
            Instantiate(shatteredParticlesPrefab, transform.position, transform.rotation);
        }

        // Collider'ı kapatarak bir daha çarpmasını engelle
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Veya objeyi tamamen yok et (Destroy)
        // Destroy(gameObject, 0.5f);
    }
}
