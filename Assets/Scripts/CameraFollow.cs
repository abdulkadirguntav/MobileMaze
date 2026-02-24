using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    void Start()
    {
        
    }

    void Update()
    {
        transform.position = target.position + new Vector3(0, 0, 0);
    }
}
