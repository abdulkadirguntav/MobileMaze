using UnityEngine;

[CreateAssetMenu(fileName = "NewTheme", menuName = "Theme System/Theme Data", order = 1)]
public class ThemeData : ScriptableObject
{
    [Tooltip("Temanın İsmi (Menüde Göstermek İçin)")]
    public string themeName;

    [Tooltip("Tünel için kullanılacak Material")]
    public Material tunnelMaterial;

    [Tooltip("Engeller için kullanılacak Material")]
    public Material obstacleMaterial;
}
