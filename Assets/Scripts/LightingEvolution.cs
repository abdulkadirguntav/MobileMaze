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

    private Material tunnelMat;
    private Material obstacleMat;
    private Color originalTunnelColor;
    private Color originalObstacleColor;
    private Color originalTunnelEmission;
    private Color originalObstacleEmission;
    private bool materialsCached = false;

    void Start()
    {
        // 1. Materyalleri ve Orijinal Renkleri Önbelleğe Al
        if (ThemeManager.Instance != null)
        {
            tunnelMat = ThemeManager.Instance.GetTunnelMaterial();
            obstacleMat = ThemeManager.Instance.GetObstacleMaterial();

            if (tunnelMat != null)
            {
                originalTunnelColor = tunnelMat.HasProperty("_BaseColor") ? tunnelMat.GetColor("_BaseColor") : tunnelMat.color;
                if (tunnelMat.HasProperty("_EmissionColor")) originalTunnelEmission = tunnelMat.GetColor("_EmissionColor");
            }

            if (obstacleMat != null)
            {
                originalObstacleColor = obstacleMat.HasProperty("_BaseColor") ? obstacleMat.GetColor("_BaseColor") : obstacleMat.color;
                if (obstacleMat.HasProperty("_EmissionColor")) originalObstacleEmission = obstacleMat.GetColor("_EmissionColor");
            }
            materialsCached = true;
        }

        if (evolutionColors.Length > 0)
        {
            targetColor = evolutionColors[0];
            ApplyColorInstantly(targetColor);
        }
    }

    void Update()
    {
        // Işık, Sis ve Ortam Işığını (Ambient Light) Güncelle
        if (directionalLight != null)
        {
            directionalLight.color = Color.Lerp(directionalLight.color, targetColor, Time.deltaTime * lerpSpeed);
        }
        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, targetColor, Time.deltaTime * lerpSpeed);
        RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, targetColor, Time.deltaTime * lerpSpeed);
        
        Shader.SetGlobalColor("_GlobalThemeEmission", Color.Lerp(Shader.GetGlobalColor("_GlobalThemeEmission"), targetColor, Time.deltaTime * lerpSpeed));

        // 2. Tünel ve Engel Materyallerini Senkronize Lerp Et (Ana Renk ve Emission)
        UpdateMaterialColor(tunnelMat);
        UpdateMaterialColor(obstacleMat);
    }

    private void UpdateMaterialColor(Material mat)
    {
        if (mat == null) return;

        // Base/Main Color
        Color currentColor = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
        Color newColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * lerpSpeed);
        
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", newColor);
        else mat.color = newColor;

        // Emission Color
        if (mat.HasProperty("_EmissionColor"))
        {
            Color currentEmission = mat.GetColor("_EmissionColor");
            mat.SetColor("_EmissionColor", Color.Lerp(currentEmission, targetColor, Time.deltaTime * lerpSpeed));
        }
    }

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
        
        targetIndex = Mathf.Clamp(targetIndex, 0, evolutionColors.Length - 1);

        if (targetIndex < scoreThresholds.Length - 1 && targetIndex < evolutionColors.Length - 1)
        {
            float currentThreshold = scoreThresholds[targetIndex];
            float nextThreshold = scoreThresholds[targetIndex + 1];
            float progress = Mathf.Clamp01((currentScore - currentThreshold) / (nextThreshold - currentThreshold));
            
            Color currentColor = evolutionColors[targetIndex];
            Color nextColor = evolutionColors[targetIndex + 1];
            
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
        RenderSettings.ambientLight = color;
        Shader.SetGlobalColor("_GlobalThemeEmission", color);

        ApplyMaterialColorInstantly(tunnelMat, color);
        ApplyMaterialColorInstantly(obstacleMat, color);
    }

    private void ApplyMaterialColorInstantly(Material mat, Color color)
    {
        if (mat == null) return;
        
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        else mat.color = color;

        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color);
    }

    // 3. Oyun Restart veya Quit olduğunda materyalleri orijinal rengine sıfırla
    private void OnDestroy()
    {
        ResetMaterialsToOriginal();
    }

    private void OnApplicationQuit()
    {
        ResetMaterialsToOriginal();
    }

    private void ResetMaterialsToOriginal()
    {
        if (!materialsCached) return;

        if (tunnelMat != null)
        {
            if (tunnelMat.HasProperty("_BaseColor")) tunnelMat.SetColor("_BaseColor", originalTunnelColor);
            else tunnelMat.color = originalTunnelColor;

            if (tunnelMat.HasProperty("_EmissionColor")) tunnelMat.SetColor("_EmissionColor", originalTunnelEmission);
        }

        if (obstacleMat != null)
        {
            if (obstacleMat.HasProperty("_BaseColor")) obstacleMat.SetColor("_BaseColor", originalObstacleColor);
            else obstacleMat.color = originalObstacleColor;

            if (obstacleMat.HasProperty("_EmissionColor")) obstacleMat.SetColor("_EmissionColor", originalObstacleEmission);
        }
    }
}
