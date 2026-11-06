using UnityEngine;

public class Projectile : MonoBehaviour
{
    private float damage;
    private Vector2 direction;
    private float speed = 20f;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        // Tự động vô hiệu hóa đạn sau 3 giây để tránh bay vô hạn ngoài màn hình
        Invoke("DisableProjectile", 3f);
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    private void DisableProjectile()
    {
        gameObject.SetActive(false);
    }

         public void Setup(float bulletDamage, Vector2 shootDirection)
    {
        this.damage = bulletDamage;
        this.direction = shootDirection;

                 rb.linearVelocity = direction * speed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        EnemyController enemy = collision.collider.GetComponent<EnemyController>();
        if (enemy == null)
        {
            enemy = collision.collider.GetComponentInParent<EnemyController>();
        }

        if (enemy != null)
        {
            float finalDmg = damage;

            // Kích hoạt Hiệu ứng Đòn đánh (On Hit Triggers) và tính sát thương chí mạng/bổ trợ
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                PlayerStats stats = playerObj.GetComponent<PlayerStats>();
                if (stats == null) stats = playerObj.GetComponentInParent<PlayerStats>();
                if (stats != null)
                {
                    var result = stats.CalculateAttackDamage(damage, enemy.IsFullHP(), enemy.IsBoss());
                    finalDmg = result.damage;
                }

                PlayerInventory inv = playerObj.GetComponent<PlayerInventory>();
                if (inv == null) inv = playerObj.GetComponentInParent<PlayerInventory>();

                bool hasAbyssNova = (inv != null && inv.GetItemStack("abyss_nova") > 0);

                // Sinh hạt hiệu ứng trúng đòn (Hit Particle)
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.SpawnHitParticle(enemy.transform.position, hasAbyssNova);
                }

                if (inv != null)
                {
                    inv.TriggerOnHitEffects(enemy, damage);
                }
            }

            enemy.TakeDamage(finalDmg);
            DisableProjectile();
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") || collision.gameObject.CompareTag("Ground"))
        {
            DisableProjectile();
        }
    }
}