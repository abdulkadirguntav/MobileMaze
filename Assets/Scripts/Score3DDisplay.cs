using UnityEngine;
using TMPro;

public class Score3DDisplay : MonoBehaviour
{
    [Tooltip("3D Text objesi (TextMeshPro kullanarak)")]
    public TextMeshPro scoreText;
    
    [Tooltip("Oyuncuyu takip etmesi için referans")]
    public Transform playerTransform;

    [Tooltip("Oyuncunun Z ekseninde ne kadar ilerisinde duracak (Z offset)")]
    public float zOffset = 40f;
    
    [Tooltip("Y ekseni offset'i (Örn: Zeminde hologram gibi durması için eksi değer)")]
    public float yOffset = -2f;
    [Tooltip("X ekseni offset'i")]
    public float xOffset = 0f;

    [Tooltip("Hologram parlama/saydamlık hızı")]
    public float pulseSpeed = 2f;

    void Update()
    {
        if (playerTransform != null)
        {
            // Sabit bir X ve Y'de, sadece Z ekseninde oyuncunun önünde uçması
            Vector3 targetPos = new Vector3(xOffset, yOffset, playerTransform.position.z + zOffset);
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 10f);
        }

        if (scoreText != null)
        {
            // Ufak bir renk/saydamlık dalgalanması (hologram animasyonu)
            Color c = scoreText.color;
            c.a = 0.5f + Mathf.Sin(Time.time * pulseSpeed) * 0.3f;
            scoreText.color = c;
        }
    }

    public void UpdateScore(float score)
    {
        if (scoreText != null)
        {
            scoreText.text = "SCORE: " + Mathf.FloorToInt(score).ToString();
        }
    }
}
