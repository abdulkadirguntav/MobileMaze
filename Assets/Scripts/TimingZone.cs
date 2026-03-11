using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TimingZone : MonoBehaviour
{
    private void Start()
    {
        // Collider'ın kesinlikle trigger olduğundan emin olalım (içinden geçilebilir)
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                // Karakter QTE zamanlama bölgesine girdi
                player.SetTimingZoneStatus(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                // Karakter zamanlama bölgesinden çıktı (eğer tuşa basamadıysa kaçırdı)
                player.SetTimingZoneStatus(false);
            }
        }
    }
}
