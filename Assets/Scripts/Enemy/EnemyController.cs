using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    protected float maxHealth = 70f;
    protected float currentHealth = 70f;
    protected int damageAmount = 10;
    protected int moneyReward = 3;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    // Hiệu ứng làm chậm (Roguelike Item Effects)
    protected float speedMultiplier = 1f;
    private Coroutine slowCoroutine;

    [Header("Knockback Settings")]
    public float knockbackForce = 6f;      public float knockbackDuration = 0.18f;      public float knockbackUpwardForce = 3f;      protected float knockbackTimer;

    protected Rigidbody2D rb;
    protected Animator anim;
    protected Transform player;

    protected bool isDead = false;
    protected bool isFacingRight = true;

    [Header("Hit Flash Settings")]
    protected SpriteRenderer spriteRenderer;
    private Material originalMaterial;
    private static Material whiteFlashMaterial;
    private Coroutine flashCoroutine;

    [Header("AI Obstacle Avoidance Settings")]
    [SerializeField] protected bool enableAutoJump = true;       // Cho phép quái nhảy tránh chướng ngại vật/hố sâu
    [SerializeField] protected float enemyJumpForce = 7f;         // Lực nhảy tương tự Player
    [SerializeField] protected LayerMask groundLayer;             // Layer địa hình
    [SerializeField] protected float obstacleCheckDistance = 0.8f;// Khoảng cách quét tường ngang
    [SerializeField] protected float gapCheckDistance = 1.0f;     // Khoảng cách nhô ra trước đầu để quét hố

    [Header("Enemy Audio Config")]
    [SerializeField] protected AudioClip hitSound;
    [SerializeField] protected AudioClip deathSound;
    [SerializeField] protected AudioClip attackSound;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        anim = GetComponent<Animator>();
        if (anim == null)
        {
            anim = GetComponentInChildren<Animator>();
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        if (spriteRenderer != null)
        {
            originalMaterial = spriteRenderer.sharedMaterial;
        }

        if (whiteFlashMaterial == null)
        {
            Shader whiteShader = Shader.Find("Sprites/SolidWhite");
            if (whiteShader != null)
            {
                whiteFlashMaterial = new Material(whiteShader);
            }
        }

                if (GetType() == typeof(EnemyController))
        {
            EnemyController subclass = GetComponent<ClaymanController>();
            if (subclass != null)
            {
                Debug.LogError($"[LỖI NGHIÊM TRỌNG] Đối tượng '{gameObject.name}' đang bị gắn TRÙNG cả hai script 'EnemyController' và 'ClaymanController'!\nHãy click vào đối tượng '{gameObject.name}' trong cửa sổ Hierarchy/Inspector và REMOVE (Xóa) script 'EnemyController' đi, CHỈ GIỮ LẠI 'ClaymanController' nhé!");
            }
            ApplyDifficultyScaling();
        }
    }

    protected virtual void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    public virtual void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"[Enemy: {gameObject.name}] Nhận {damage} sát thương. Máu còn lại: {currentHealth}/{maxHealth}");

        // Cập nhật giao diện máu Boss gộp nếu kẻ địch là Boss
        if (IsBoss() && UIManager.Instance != null)
        {
            UIManager.Instance.UpdateBossHealthUI();
        }

        PlayHurtAnimation();

        ApplyKnockback();

        // Nhấp nháy màu trắng (Hit Flash Effect) khi dính sát thương
        FlashWhite();

        // Phát tiếng bị thương
        if (AudioManager.Instance != null && hitSound != null)
        {
            AudioManager.Instance.PlaySFX3DThrottled("Enemy_Hit", hitSound, transform.position, 0.05f, volumeScale: 0.5f);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Kích hoạt hiệu ứng nhấp nháy trắng (Hit Flash) trong 2 khung hình (khoảng 0.04-0.05 giây)
    /// </summary>
    public void FlashWhite()
    {
        if (spriteRenderer == null || whiteFlashMaterial == null) return;
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(FlashWhiteRoutine());
    }

    private IEnumerator FlashWhiteRoutine()
    {
        spriteRenderer.material = whiteFlashMaterial;
        // Đợi 2 frame để hiển thị màu trắng chớp nhoáng
        yield return null; // Frame 1
        yield return null; // Frame 2
        
        // Trả lại chất liệu (material) gốc cho sprite
        if (spriteRenderer != null)
        {
            spriteRenderer.material = originalMaterial;
        }
        flashCoroutine = null;
    }

         protected virtual void ApplyKnockback()
    {
        if (player == null || rb == null) return;

                 float knockbackDirX = (transform.position.x - player.position.x) > 0 ? 1f : -1f;

                 rb.linearVelocity = new Vector2(knockbackDirX * knockbackForce, knockbackUpwardForce);

                 knockbackTimer = knockbackDuration;
    }

         protected virtual void PlayHurtAnimation()
    {
             }

         protected virtual void Die()
    {
        isDead = true;

        // Phát tiếng kêu khi chết
        if (AudioManager.Instance != null && deathSound != null)
        {
            AudioManager.Instance.PlaySFX3D(deathSound, transform.position);
        }

        if (GameManager.Instance != null)
        {
            if (IsBoss())
            {
                GameManager.Instance.bossesKilledCount++;
            }
            else
            {
                GameManager.Instance.enemiesKilledCount++;
            }
        }

        // Cập nhật giao diện máu Boss gộp khi Boss chết hẳn
        if (IsBoss() && UIManager.Instance != null)
        {
            UIManager.Instance.UpdateBossHealthUI();
        }

        knockbackTimer = 0f;  
                 rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;  
                 Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

                 if (anim != null)
        {
                         anim.SetBool("isJumping", false);

                         anim.ResetTrigger("Shoot");
        }

                 // Thêm thưởng tiền vàng khi tiêu diệt kẻ địch
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            PlayerStats playerStats = playerObj.GetComponent<PlayerStats>();
            if (playerStats == null)
            {
                playerStats = playerObj.GetComponentInParent<PlayerStats>();
            }

            if (playerStats != null)
            {
                playerStats.AddMoney(moneyReward);
            }

            // --- Rising Nebula: quái vật bị tiêu diệt sinh ra cột lửa ---
            PlayerInventory inv = playerObj.GetComponent<PlayerInventory>();
            if (inv == null) inv = playerObj.GetComponentInParent<PlayerInventory>();
            if (inv != null)
            {
                inv.TriggerRisingNebula(transform.position, maxHealth);
            }

            // --- parallel_cross: quái chết có 2% rơi ra item ---
            if (Random.Range(0f, 100f) < 2f)
            {
                if (inv != null)
                {
                    inv.AddItemDirectly("parallel_cross");
                }
            }
        }

        PlayDeathAnimation();

                 Destroy(gameObject, 2.0f);
    }

         protected virtual void PlayDeathAnimation()
    {
             }

    public void ApplySlow(float amount, float duration)
    {
        if (isDead) return;
        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
        }
        slowCoroutine = StartCoroutine(SlowRoutine(amount, duration));
    }

    private IEnumerator SlowRoutine(float amount, float duration)
    {
        speedMultiplier = Mathf.Max(0f, 1f - amount);
        yield return new WaitForSeconds(duration);
        speedMultiplier = 1f;
        slowCoroutine = null;
    }

    public bool IsFullHP()
    {
        return currentHealth >= maxHealth;
    }

    public virtual bool IsBoss()
    {
        return this is BossController;
    }

    protected virtual void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.Rotate(0f, 180f, 0f);
    }

    // Hàm kiểm tra mặt đất bằng Raycast từ đáy Collider
    public bool CheckGrounded()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return false;

        // Tự động gán layer mặc định nếu chưa cấu hình
        if (groundLayer == 0)
        {
            groundLayer = LayerMask.GetMask("Ground") | LayerMask.GetMask("Platform");
        }

        // Vị trí đáy của Collider quái vật
        float bottomY = col.bounds.min.y;
        float centerX = col.bounds.center.x;

        // Bắn tia từ đáy xuống dưới 0.15m để kiểm tra xem có đứng trên đất
        Vector2 origin = new Vector2(centerX, bottomY + 0.05f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 0.2f, groundLayer);

        return hit.collider != null;
    }

    // Hàm tự động quét và nhảy tránh vật cản hoặc vực sâu khi đuổi theo
    protected void HandleObstacleAndGapAvoidance()
    {
        if (!enableAutoJump || isDead || rb == null) return;

        // Tự động gán layer mặc định nếu chưa cấu hình
        if (groundLayer == 0)
        {
            groundLayer = LayerMask.GetMask("Ground") | LayerMask.GetMask("Platform");
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        float halfWidth = col.bounds.size.x / 2f;
        float moveDir = isFacingRight ? 1f : -1f;

        // 1. QUÉT TƯỜNG (OBSTACLE): Bắn tia từ rìa ngoài cơ thể ra trước (lùi lại 0.05m tránh kẽ hở)
        float checkHeight = col.bounds.min.y + 0.2f;
        Vector2 horizontalOrigin = new Vector2(col.bounds.center.x + moveDir * (halfWidth - 0.05f), checkHeight);
        Vector2 horizontalDirection = new Vector2(moveDir, 0f);
        RaycastHit2D obstacleHit = Physics2D.Raycast(horizontalOrigin, horizontalDirection, obstacleCheckDistance + 0.05f, groundLayer);

        bool hasObstacle = obstacleHit.collider != null;

        // 2. QUÉT VỰC (GAP): Bắn tia dọc đi xuống từ điểm nhô ra trước rìa ngoài cơ thể quái vật
        float checkAheadX = col.bounds.center.x + moveDir * (halfWidth + gapCheckDistance);
        Vector2 verticalOrigin = new Vector2(checkAheadX, col.bounds.min.y + 0.05f);
        RaycastHit2D gapHit = Physics2D.Raycast(verticalOrigin, Vector2.down, 2.0f, groundLayer);

        bool hasGap = gapHit.collider == null; // Không chạm đất cứng tức là phía trước là vực sâu

        // 3. THỰC HIỆN NHẢY: Nếu gặp tường hoặc hố và đang đứng trên mặt đất
        if ((hasObstacle || hasGap) && CheckGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, enemyJumpForce);
            if (anim != null)
            {
                anim.SetBool("isJumping", true);
            }
        }
    }

    public void PlayAttackSound()
    {
        if (AudioManager.Instance != null && attackSound != null && !isDead)
        {
            // Throttled nhẹ để tránh ồn khi nhiều quái đánh cùng lúc
            AudioManager.Instance.PlaySFX3DThrottled("Enemy_Attack", attackSound, transform.position, 0.15f, volumeScale: 0.8f);
        }
    }

    /// <summary>
    /// Kiểm tra xem một vị trí đề xuất có an toàn (không bị đè lên địa hình Ground/Platform) hay không,
    /// bằng cách quét OverlapBox khớp với kích thước Collider thực tế của quái vật.
    /// </summary>
    public bool IsPositionSafe(Vector2 targetPos)
    {
        // Đảm bảo layer được gán
        if (groundLayer == 0)
        {
            groundLayer = LayerMask.GetMask("Ground") | LayerMask.GetMask("Platform");
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col == null) col = GetComponentInChildren<Collider2D>();

        if (col != null)
        {
            Vector2 size = Vector2.one;
            Vector2 offset = Vector2.zero;
            float scaleX = Mathf.Abs(transform.localScale.x);
            float scaleY = Mathf.Abs(transform.localScale.y);

            if (col is BoxCollider2D box)
            {
                size = new Vector2(box.size.x * scaleX, box.size.y * scaleY);
                offset = new Vector2(box.offset.x * transform.localScale.x, box.offset.y * transform.localScale.y);
            }
            else if (col is CapsuleCollider2D capsule)
            {
                size = new Vector2(capsule.size.x * scaleX, capsule.size.y * scaleY);
                offset = new Vector2(capsule.offset.x * transform.localScale.x, capsule.offset.y * transform.localScale.y);
            }
            else if (col is CircleCollider2D circle)
            {
                float diameter = circle.radius * 2f;
                size = new Vector2(diameter * scaleX, diameter * scaleY);
                offset = new Vector2(circle.offset.x * transform.localScale.x, circle.offset.y * transform.localScale.y);
            }

            Vector2 checkPos = targetPos + offset;
            // Thu nhỏ nhẹ size kiểm tra (nhân 0.95f) để tránh bị coi là va chạm chỉ vì đứng sát sạt mép địa hình
            Collider2D overlap = Physics2D.OverlapBox(checkPos, size * 0.95f, 0f, groundLayer);
            return overlap == null;
        }

        // Dự phòng bằng vòng tròn nhỏ nếu không tìm thấy collider
        Collider2D fallbackOverlap = Physics2D.OverlapCircle(targetPos, 0.4f, groundLayer);
        return fallbackOverlap == null;
    }

    protected void ApplyDifficultyScaling()
    {
        if (GameManager.Instance != null)
        {
            float d = GameManager.Instance.gameDifficultyCoeff;
            int level = GameManager.Instance.CurrentLevel;

            if (IsBoss())
            {
                // HP Boss: HP Boss gốc * (1 + D) * Màn chơi * 1.5
                maxHealth = maxHealth * (1f + d) * level * 1.5f;
            }
            else
            {
                // HP quái thường: HP gốc * (1 + D) * (1 + (Màn chơi - 1) * 0.25)
                maxHealth = maxHealth * (1f + d) * (1f + (level - 1) * 0.25f);
            }
            currentHealth = maxHealth;

            // Sát thương quái: Sát thương gốc * (1 + D * 1.25)
            damageAmount = Mathf.RoundToInt(damageAmount * (1f + d * 1.25f));

            // Tiền rơi: Tiền gốc * Màn chơi * (1 + D/2)
            moneyReward = Mathf.RoundToInt(moneyReward * level * (1f + d * 0.5f));

            Debug.Log($"[Difficulty Scale] {gameObject.name} (Boss={IsBoss()}): MaxHP={maxHealth}, Sát thương={damageAmount}, Tiền rơi={moneyReward}");
        }
    }
}