using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    public Transform cameraTransform; // Gắn Camera vào đây
    public float parallaxFactor = 0.5f; // Càng nhỏ thì lớp càng xa

    private Vector3 previousCameraPosition;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        previousCameraPosition = cameraTransform.position;
    }

    void LateUpdate()
    {
        Vector3 delta = cameraTransform.position - previousCameraPosition;
        transform.position += new Vector3(delta.x * (parallaxFactor), delta.y * (parallaxFactor), 0);
        previousCameraPosition = cameraTransform.position;
    }
}
