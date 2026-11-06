using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float lifetime = 5f;
    private int damage;
    private LayerMask targetLayer;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    // Nhận cả lượng sát thương và thông tin Layer từ con bọ truyền sang
    public void Setup(int damageAmount, LayerMask playerLayer)
    {
        damage = damageAmount;
        targetLayer = playerLayer;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Kiểm tra xem vật va chạm có nằm trong Layer tuyển chọn (Player Layer) không
        if (((1 << collision.gameObject.layer) & targetLayer) != 0)
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player == null) player = collision.GetComponentInParent<PlayerController>();

            if (player != null)
            {
                player.ChangeHealth(-damage);
                Debug.Log($"[Đạn Địch] Đã bắn trúng Player bằng LayerMask! Gây {damage} sát thương.");
                Destroy(gameObject);
                return;
            }
        }

        // Tự hủy nếu va chạm trúng địa hình nền đất
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") || collision.gameObject.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}