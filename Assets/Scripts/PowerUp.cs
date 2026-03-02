using UnityEngine;
using System.Collections;

public enum PowerUpType
{
    KineticShield, // Dash + Invincibility
    ChronoStasis,  // Heavy slow motion
    EMPBlast       // Destroy all electronic obstacles nearby
}

public class PowerUp : MonoBehaviour
{
    [Header("Power-Up Ayarları")]
    public PowerUpType powerUpType;
    
    [Header("Kinetic Shield (Dash)")]
    public float dashDuration = 5f;

    [Header("Chrono-Stasis")]
    public float slowMotionDuration = 4f;
    public float timeScaleTarget = 0.4f;

    [Header("EMP Blast")]
    public float empRadius = 100f; // Ne kadar ilerideki engelleri yok edecek
    public GameObject empEffectPrefab;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                ApplyPowerUp(player);
                Destroy(gameObject); // Alındıktan sonra kendini yok et
            }
        }
    }

    private void ApplyPowerUp(PlayerController player)
    {
        // GameManager veya GameController üzerinden Audio çalınabilir

        switch (powerUpType)
        {
            case PowerUpType.KineticShield:
                player.ActivateDash(dashDuration);
                break;
                
            case PowerUpType.ChronoStasis:
                GameManager.Instance.StartCoroutine(ChronoStasisRoutine());
                break;

            case PowerUpType.EMPBlast:
                TriggerEMPBlast(player.transform.position);
                break;
        }
    }

    private IEnumerator ChronoStasisRoutine()
    {
        // Zamanı yavaşlat
        Time.timeScale = timeScaleTarget;
        Time.fixedDeltaTime = 0.02f * Time.timeScale; // Fiziğin pürüzsüz kalması için

        // Süreyi gerçek zaman dilimiyle (Realtime) bekle
        yield return new WaitForSecondsRealtime(slowMotionDuration);

        // Normale dön
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    private void TriggerEMPBlast(Vector3 center)
    {
        // Efekt oluştur
        if (empEffectPrefab != null)
        {
            Instantiate(empEffectPrefab, center, Quaternion.identity);
        }

        // Çevredeki "Obstacle" tagine sahip olan ve "Electronic" sayılabilecek objeleri bulup yok et.
        // Şimdilik ekrandaki tüm engelleri temizliyoruz (Cam hariç vs. olabilir, projeye bağlı).
        Collider[] hitColliders = Physics.OverlapSphere(center, empRadius);
        foreach (Collider col in hitColliders)
        {
            if (col.CompareTag("Obstacle"))
            {
                GlassObstacle glass = col.GetComponent<GlassObstacle>();
                // Cam elektronik olmadığı için EMP'den etkilenmeyecek diyebiliriz (isteğe bağlı).
                // Eğer her şey yok edilecekse direkt Destroy.
                if (glass == null)
                {
                    Destroy(col.gameObject);
                }
            }
        }
    }
}
