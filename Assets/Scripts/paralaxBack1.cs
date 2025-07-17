using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    public Transform cameraTransform; // Gắn Camera vào đây
    public float parallaxFactor = 0.5f; // Càng nhỏ thì lớp càng xa

    // Thêm biến để thay đổi sprite khi chuyển phase
    private SpriteRenderer spriteRenderer;
    public Sprite phase2Sprite; // Kéo thả sprite phase 2 vào đây

    private Vector3 previousCameraPosition;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        previousCameraPosition = cameraTransform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        Vector3 delta = cameraTransform.position - previousCameraPosition;
        transform.position += new Vector3(delta.x * (parallaxFactor), delta.y * (parallaxFactor), 0);
        previousCameraPosition = cameraTransform.position;
    }

    // Thêm hàm để thay đổi sprite khi chuyển phase
    public void SwitchToPhase2()
    {
        if (spriteRenderer != null && phase2Sprite != null)
        {
            spriteRenderer.sprite = phase2Sprite;
        }
    }
}