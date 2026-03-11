using UnityEngine;

public class SawProjectile : MonoBehaviour
{
    public float moveSpeed = 15f; // Karşı duvara uçma hızı
    public float rotationSpeed = 720f; // Kendi etrafında dönme hızı (720 derece = saniyede 2 tur)
    public float lifeTime = 4f; // Testere kaç saniye sonra silinsin?

    void Start()
    {
        // Object pooling projelerinde normalde Destroy kullanılmaz ama 
        // bu tarz saniyelik mermilerde (projectile) havuz kurmak kodu çok karmaşıklaştırır.
        // Tünel çok dar olduğu için 2 saniye sonra duvara çarpmış ve silinmiş gibi olması en temizi.
        Destroy(gameObject, lifeTime); 
    }

    void Update()
    {
        // 1. İleri Doğru Uç (Çıkış noktasının baktığı yöne doğru)
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime, Space.Self);

        // 2. Kendi Etrafında Dön (Testere modelinin yönüne göre X, Y veya Z olabilir. 
        // Eğer yanlış eksende dönerse (0, 0, rotationSpeed) kısmını (rotationSpeed, 0, 0) yap)
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }

    // Oyuncuya çarparsa (PlayerController'daki kod halledecek gerçi ama tetikleyici olsun)
    private void OnTriggerEnter(Collider other)
    {
        // Eğer duvara çarpınca kıvılcım çıkmasını falan istersen buraya yazabilirsin.
        // Şimdilik sadece oyuncuyu delip geçecek, oyuncu ölme kodunu kendi scriptinde çalıştıracak.
    }
}