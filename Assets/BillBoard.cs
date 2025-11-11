using UnityEngine;

public class BillBoard : MonoBehaviour
{
    public Transform cam;
    void Awake() => cam = Camera.main.transform;

    void LateUpdate()
    {
        // misma orientación que la cámara (sin roll)
        transform.rotation = Quaternion.LookRotation(cam.forward, Vector3.up);
        // Alternativa: que “mire” a la cámara
        // transform.LookAt(transform.position + cam.forward, Vector3.up);
    }
}
