using UnityEngine;

public class ThemeManager : MonoBehaviour
{
    // Singleton yapısı
    public static ThemeManager Instance { get; private set; }

    [Header("Current Theme")]
    [Tooltip("Şu anki aktif tema. Oyuna başlamadan Inspector'dan atayabilirsiniz.")]
    public ThemeData activeTheme;

    private void Awake()
    {
        // Singleton pattern kurulumu
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Eğer menü ile sahne farklıysa bunu açın
        }
        else
        {
            Destroy(gameObject); // Birden fazla ThemeManager olmasını engelle
        }
    }

    /// <summary>
    /// Aktif temanın tünel materyalini döndürür.
    /// </summary>
    public Material GetTunnelMaterial()
    {
        if (activeTheme != null && activeTheme.tunnelMaterial != null)
        {
            return activeTheme.tunnelMaterial;
        }
        return null;
    }

    /// <summary>
    /// Aktif temanın engel materyalini döndürür.
    /// </summary>
    public Material GetObstacleMaterial()
    {
        if (activeTheme != null && activeTheme.obstacleMaterial != null)
        {
            return activeTheme.obstacleMaterial;
        }
        return null;
    }
}
