using UnityEngine;

public enum PowerUpType
{
    SlowMotion,
    DestructiveDash,
    ClearPath
}

public class PowerUpObtain : MonoBehaviour
{
    [Tooltip("Bu objenin hangi güçlendirmeyi vereceğini seçin")]
    public PowerUpType powerUpType;

    [Tooltip("Kendi etrafında dönme hızı (Görsel animasyon)")]
    public float rotationSpeed = 100f;

    void Update()
    {
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }
}
