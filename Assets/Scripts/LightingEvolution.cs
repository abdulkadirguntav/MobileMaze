using UnityEngine;

public class LightingEvolution : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Sahnenin ana Directional Light'ı")]
    public Light directionalLight;

    [Header("Evolution Settings")]
    [Tooltip("Renk basamakları sırasıyla: Açık Mavi -> Yeşil -> Sarı -> Kırmızı -> Lacivert -> Mor")]
    public Color[] evolutionColors = new Color[] 
    {
        new Color(0.3f, 0.8f, 1f),   // Açık Mavi
        new Color(0.2f, 1f, 0.2f),   // Yeşil
        new Color(1f, 1f, 0.2f),     // Sarı
        new Color(1f, 0.2f, 0.2f),   // Kırmızı
        new Color(0f, 0f, 0.5f),     // Lacivert
        new Color(0.5f, 0f, 0.5f)    // Mor
    };

    [Tooltip("Her bir renk geçişi için gereken skor eşikleri (Örn: 0, 500, 1000...)")]
    public float[] scoreThresholds = new float[] { 0f, 500f, 1000f, 1500f, 2000f, 2500f };

    [Tooltip("Renk geçişinin ne kadar keskin veya pürüzsüz olacağını ayarlar")]
    public float lerpSpeed = 1f;

    private Color targetColor;

    void Start()
    {
        if (evolutionColors.Length > 0)
        {
            targetColor = evolutionColors[0];
            ApplyColorInstantly(targetColor);
        }
    }

    void Update()
    {
        // Renkleri pürüzsüzce (Lerp ile) değiştir
        if (directionalLight != null)
        {
            directionalLight.color = Color.Lerp(directionalLight.color, targetColor, Time.deltaTime * lerpSpeed);
        }
        
        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, targetColor, Time.deltaTime * lerpSpeed);
        
        // Engellerin emission rengi için Global bir variable ayarlıyoruz. 
        // Bunu engelin Material'inde "_EmissionColor" (veya kullandığınız shader'a göre) kullanabilirsiniz.
        // HDRP/URP kullanıyorsanız Shader Graph üzerinden "GlobalEmissionColor" isimli bir değişken tanımlayıp okuyabilirsiniz.
        Shader.SetGlobalColor("_GlobalThemeEmission", Color.Lerp(Shader.GetGlobalColor("_GlobalThemeEmission"), targetColor, Time.deltaTime * lerpSpeed));
    }

    /// <summary>
    /// GameManager tarafından skor değiştikçe çağrılır.
    /// Hedef rengi (targetColor) günceller.
    /// </summary>
    public void UpdateLighting(float currentScore)
    {
        if (evolutionColors.Length == 0 || scoreThresholds.Length == 0) return;

        // Geçerli hedef rengi bul
        int targetIndex = 0;
        for (int i = 0; i < scoreThresholds.Length; i++)
        {
            if (currentScore >= scoreThresholds[i])
            {
                targetIndex = i;
            }
        }
        
        // En son indeksi aşmamak için sınırla
        targetIndex = Mathf.Clamp(targetIndex, 0, evolutionColors.Length - 1);

        // Skora dayalı olarak bir sonraki renge doğru oran (0-1 arası) hesaplama
        if (targetIndex < scoreThresholds.Length - 1 && targetIndex < evolutionColors.Length - 1)
        {
            float currentThreshold = scoreThresholds[targetIndex];
            float nextThreshold = scoreThresholds[targetIndex + 1];
            float progress = Mathf.Clamp01((currentScore - currentThreshold) / (nextThreshold - currentThreshold));
            
            Color currentColor = evolutionColors[targetIndex];
            Color nextColor = evolutionColors[targetIndex + 1];
            
            // Mevcut skor içindeki ilerlemeye göre hassas bir hedef renk belirliyoruz
            targetColor = Color.Lerp(currentColor, nextColor, progress);
        }
        else
        {
            targetColor = evolutionColors[targetIndex];
        }
    }

    private void ApplyColorInstantly(Color color)
    {
        if (directionalLight != null) directionalLight.color = color;
        RenderSettings.fogColor = color;
        Shader.SetGlobalColor("_GlobalThemeEmission", color);
    }
}
