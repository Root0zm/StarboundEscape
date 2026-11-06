using UnityEngine;

[DefaultExecutionOrder(1000)]
public class ParallaxBackground : MonoBehaviour
{
    [Header("Parallax Settings")]
    [Tooltip("Hệ số trôi: 1 = di chuyển cùng Camera, 0 = không di chuyển, 0.95 = trôi xa xăm")]
    [SerializeField] private float parallaxEffect = 0.95f;

    private Transform cameraTransform;
    private Vector2 startCameraPos;
    private Vector2 startMoonPos;

    void Start()
    {
        // 1. Xác định vị trí Camera chính trong Scene
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            startCameraPos = new Vector2(cameraTransform.position.x, cameraTransform.position.y);
        }
        else
        {
            Debug.LogError("[ParallaxBackground] Không tìm thấy Main Camera trong Scene!");
        }

        // Lưu lại vị trí trục X/Y ban đầu của đối tượng (Mặt Trăng)
        startMoonPos = new Vector2(transform.position.x, transform.position.y);
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // 3. Tính toán khoảng cách Camera đã di chuyển so với điểm xuất phát
        float travelX = cameraTransform.position.x - startCameraPos.x;
        float travelY = cameraTransform.position.y - startCameraPos.y;

        // Nhân khoảng cách đó với hệ số parallaxEffect
        float newX = startMoonPos.x + (travelX * parallaxEffect);
        float newY = startMoonPos.y + (travelY * parallaxEffect);

        // Cập nhật lại vị trí của đối tượng (Giữ nguyên Z gốc)
        transform.position = new Vector3(newX, newY, transform.position.z);
    }
}
