using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    private float damage;
    private float speed;
    private Vector2 direction;
    private Rigidbody2D rb;
    private Animator anim;
    private bool isCollided = false;

    [Header("Explosion Fallback")]
    [SerializeField] private float destroyFallbackDelay = 1f; // Tự hủy sau N giây nếu không có Animation Event để tránh bị kẹt

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    // Hàm khởi tạo quả cầu từ Boss
    public void Setup(float projectileDamage, float projectileSpeed, Vector2 flyDirection)
    {
        this.damage = projectileDamage;
        this.speed = projectileSpeed;
        this.direction = flyDirection.normalized;

        // Cho quả cầu bay
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }

        // Tự quay mặt quả cầu theo hướng bay (nếu cần)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        Debug.Log($"[BossProjectile] Khởi tạo đạn thành công: Sát thương = {damage}, Tốc độ = {speed}, Hướng bay = {direction}");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollided) return;

        // Thử tìm PlayerController trực tiếp hoặc ở đối tượng cha (đề phòng Collider nằm ở đối tượng con)
        PlayerController player = collision.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            player = collision.GetComponent<PlayerController>();
        }

        // Kiểm tra xem có trúng Player không
        if (collision.CompareTag("Player") || player != null)
        {
            Debug.Log($"[BossProjectile] Đạn kích hoạt va chạm trúng Player! Đối tượng va chạm: {collision.gameObject.name}. Sát thương truyền đi: {damage}");
            if (player != null)
            {
                player.ChangeHealth(-(int)damage);
                Debug.Log($"[BossProjectile] Đã gọi hàm ChangeHealth trên Player với lượng sát thương: -{(int)damage}");
            }
            else
            {
                Debug.LogError($"[BossProjectile] LỖI: Phát hiện va chạm trúng Player nhưng không tìm thấy component PlayerController trên đối tượng hoặc cha của nó!");
            }
            Explode();
        }
        // Nếu trúng sàn nhà (Ground)
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Debug.Log($"[BossProjectile] Đạn chạm mặt đất (Layer Ground) ➔ Kích nổ quả cầu.");
            Explode();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isCollided) return;

        // Thử tìm PlayerController trực tiếp hoặc ở đối tượng cha (đề phòng Collider nằm ở đối tượng con)
        PlayerController player = collision.gameObject.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            player = collision.gameObject.GetComponent<PlayerController>();
        }

        if (collision.gameObject.CompareTag("Player") || player != null)
        {
            Debug.Log($"[BossProjectile] Đạn va chạm vật lý trúng Player! Đối tượng va chạm: {collision.gameObject.name}. Sát thương truyền đi: {damage}");
            if (player != null)
            {
                player.ChangeHealth(-(int)damage);
                Debug.Log($"[BossProjectile] Đã gọi hàm ChangeHealth trên Player với lượng sát thương: -{(int)damage}");
            }
            else
            {
                Debug.LogError($"[BossProjectile] LỖI: Phát hiện va chạm vật lý trúng Player nhưng không tìm thấy component PlayerController trên đối tượng hoặc cha của nó!");
            }
            Explode();
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Debug.Log($"[BossProjectile] Đạn va chạm vật lý chạm mặt đất (Layer Ground) ➔ Kích nổ quả cầu.");
            Explode();
        }
    }

    private void Explode()
    {
        isCollided = true;

        // Dừng chuyển động quả cầu
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Tắt Collider để không va chạm thêm lần nữa
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Kiểm tra xem Animator của đạn có Parameter tên "Hit" hay không
        bool hasHitParameter = false;
        if (anim != null)
        {
            foreach (AnimatorControllerParameter param in anim.parameters)
            {
                if (param.name == "Hit" && param.type == AnimatorControllerParameterType.Trigger)
                {
                    hasHitParameter = true;
                    break;
                }
            }
        }

        // Chạy hoạt ảnh nổ nếu có Animator hỗ trợ
        if (anim != null && hasHitParameter)
        {
            anim.SetTrigger("Hit");
            anim.Play("Hit"); // Ép chạy hoạt ảnh nổ ngay lập tức
            
            // Bảo hiểm: Tự hủy sau một khoảng thời gian nếu người chơi quên đặt Animation Event
            Destroy(gameObject, destroyFallbackDelay);
            Debug.Log($"[BossProjectile] Đang chạy hoạt ảnh nổ 'Hit' và quả cầu sẽ tự hủy sau {destroyFallbackDelay}s.");
        }
        else
        {
            // Nếu không hỗ trợ hoạt ảnh nổ, tự hủy quả cầu ngay lập tức để tránh rác bộ nhớ
            Debug.Log($"[BossProjectile] Animator của đạn không hỗ trợ Trigger/State 'Hit' (hoặc không có Animator). Hủy quả cầu ngay lập tức!");
            Destroy(gameObject);
        }
    }

    // =================================================================
    // HÀM ĐƯỢC GỌI BỞI ANIMATION EVENT Ở FRAME CUỐI CÙNG CỦA HOẠT ẢNH NỔ
    // =================================================================
    public void DestroyProjectile()
    {
        Destroy(gameObject);
    }
}
