using UnityEngine;

public class ClosingDoor : MonoBehaviour
{
    public Transform doorMesh; // Aşağı inecek olan model
    public float closeSpeed = 5f; // Kapanma hızı (deneyerek bul)
    public float closedYPosition = 1f; // Kapandığında Y ekseninde nerede duracak?
    
    private Vector3 startPosition;
    private bool isClosing = false;

    void Start()
    {
        // Başlangıç pozisyonunu kaydet (Pooling için önemli)
        startPosition = doorMesh.localPosition; 
    }

    void Update()
    {
        // Eğer tetiklendiyse ve hedef pozisyona henüz ulaşmadıysa YAVAŞÇA aşağı in
        if (isClosing && doorMesh.localPosition.y > closedYPosition)
        {
            doorMesh.localPosition = Vector3.MoveTowards(doorMesh.localPosition, 
                new Vector3(doorMesh.localPosition.x, closedYPosition, doorMesh.localPosition.z), 
                closeSpeed * Time.deltaTime);
        }
    }

    // Görünmez kutuya (Trigger) oyuncu çarptığında burası çalışır
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            isClosing = true;
        }
    }

    // OBJECT POOLING İÇİN KRİTİK: Tünel havuza gidip geri gelince kapıyı yukarı kaldır!
    private void OnEnable()
    {
        isClosing = false;
        if(doorMesh != null) {
            doorMesh.localPosition = startPosition; 
        }
    }
}