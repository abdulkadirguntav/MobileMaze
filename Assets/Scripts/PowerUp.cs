using UnityEngine;

public enum PowerUpType
{
    Bomb,   // Önündeki engelleri yok eder
    Dash,   // Hızlanır ve ölümsüz olur
    Health, // Bir çarpışmayı tolere eder (Kalkan)
    Time    // Zamanı yavaşlatır
}

[RequireComponent(typeof(Collider))]
public class PowerUp : MonoBehaviour
{
    [Header("Güçlendirme Türü")]
    public PowerUpType powerUpType;

    private void Start()
    {
        // Collider'ın trigger olarak işaretlendiğinden emin oluyoruz
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Player'a çarptı mı?
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.CollectPowerUp(powerUpType);
                Destroy(gameObject); // Alındıktan sonra kendini yok et
            }
        }
    }
}
