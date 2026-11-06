using UnityEngine;

public class BossController : EnemyController
{
    protected enum BossState { Spawning, Idle, Moving, UsingSkill, Dead }
    [Header("Boss Core State")]
    [SerializeField] protected BossState currentState = BossState.Spawning;

    [Header("Boss General Stats")]
    public float bossMaxHealth = 500f;
    public int bossDamage = 20;
    public int bossMoney = 100;
    public float moveSpeed = 2f;
    public float chaseRange = 30f;
    [SerializeField] protected bool immuneToKnockback = true; // Boss mặc định miễn nhiễm đẩy lùi
    [SerializeField] protected bool playSpawnAnimationOnStart = false; // Mặc định tắt để tránh kẹt nếu người chơi chưa cấu hình hoạt ảnh Spawn
    
    [Header("Animator State Names")]
    public string idleStateName = "idle"; // Mặc định khớp với 'idle' viết thường của bạn
    public string walkStateName = "walk"; // Mặc định khớp với 'walk' viết thường của bạn
    public string deathStateName = "death"; // Mặc định khớp với 'death' viết thường của bạn
    public string deathTriggerName = "Die"; // Mặc định khớp với trigger 'Die' trong cột Parameters của bạn

    protected float skillStartTime;
    protected float spawnStartTime;

    public bool IsBossActuallyDead { get { return isDead; } }

    [Header("Slope & Step Climb Settings")]
    [SerializeField] private float maxStepHeight = 0.4f;      // Chiều cao tối đa của bậc thang có thể bước lên
    [SerializeField] private float stepCheckDistance = 0.1f;  // Khoảng cách quét phía trước collider của Boss
    [SerializeField] private LayerMask groundAndPlatformLayers; // Các layer địa hình (Ground & Platform)

    protected override void Awake()
    {
        // Gọi Awake của lớp cha để nạp Rigidbody, Animator và phát hiện gắn trùng script
        base.Awake();

        // Đồng bộ chỉ số máu và tiền thưởng cho Boss
        if (bossMaxHealth <= 0f) bossMaxHealth = 500f;
        maxHealth = bossMaxHealth;
        currentHealth = maxHealth;
        damageAmount = bossDamage;
        moneyReward = bossMoney;

        ApplyDifficultyScaling();
    }

    protected override void Start()
    {
        // Tìm người chơi tự động bằng Tag ở lớp cha EnemyController
        base.Start();

        spawnStartTime = Time.time; // Khởi tạo thời gian xuất hiện của Boss

        // Nếu chơi hoạt ảnh Spawn lúc đầu, giữ trạng thái Spawning. Ngược lại, vào Idle ngay.
        if (playSpawnAnimationOnStart)
        {
            currentState = BossState.Spawning;
        }
        else
        {
            currentState = BossState.Idle;
        }

        // Tự động gán các layer địa hình mặc định nếu người dùng chưa cấu hình trong Inspector
        if (groundAndPlatformLayers == 0)
        {
            groundAndPlatformLayers = LayerMask.GetMask("Ground") | LayerMask.GetMask("Platform");
        }
    }

    // =================================================================
    // SỰ KIỆN HOẠT ẢNH SPAWN (ĐƯỢC GỌI BỞI ANIMATION EVENT CỦA BOSS)
    // =================================================================
    public void FinishSpawning()
    {
        if (currentState == BossState.Spawning)
        {
            currentState = BossState.Idle;
            
        }
    }

    protected virtual void LookAtPlayer()
    {
        if (player == null) return;
        if (player.position.x > transform.position.x && !isFacingRight) Flip();
        else if (player.position.x < transform.position.x && isFacingRight) Flip();
    }

    // Ghi đè lực đẩy lùi: Boss có thể chọn miễn nhiễm hoàn toàn
    protected override void ApplyKnockback()
    {
        if (immuneToKnockback)
        {
            knockbackTimer = 0f; // Reset ngay lập tức để không khựng AI
            return;
        }

        // Nếu bật đẩy lùi, giảm 70% lực đẩy để tạo cảm giác Boss rất nặng
        base.ApplyKnockback();
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.3f, rb.linearVelocity.y * 0.3f);
    }

    protected virtual void FixedUpdate()
    {
        HandlePlatformCollision();
        ClimbStep();
    }

    private void HandlePlatformCollision()
    {
        if (rb == null) return;

        Collider2D bossCol = GetComponent<Collider2D>();
        if (bossCol == null) return;

        int platformLayer = LayerMask.NameToLayer("Platform");
        if (platformLayer == -1) return;

        // Quét các platform colliders xung quanh Boss (bán kính 3m)
        int platformLayerMask = 1 << platformLayer;
        Collider2D[] platforms = Physics2D.OverlapCircleAll(transform.position, 3f, platformLayerMask);

        float bossFeetY = bossCol.bounds.min.y;

        foreach (Collider2D plat in platforms)
        {
            if (plat != null)
            {
                // Tìm bề mặt trên cùng của platform này
                float platformTopY = plat.bounds.max.y;

                // Nếu boss đang đi lên (linearVelocity.y > 0.1f) HOẶC chân boss nằm dưới bề mặt platform (bossFeetY < platformTopY - 0.05f)
                if (rb.linearVelocity.y > 0.1f || bossFeetY < platformTopY - 0.05f)
                {
                    Physics2D.IgnoreCollision(bossCol, plat, true); // Đi xuyên qua
                }
                else
                {
                    Physics2D.IgnoreCollision(bossCol, plat, false); // Đứng trên
                }
            }
        }
    }

    private void ClimbStep()
    {
        if (rb == null) return;

        float velocityX = rb.linearVelocity.x;
        if (Mathf.Abs(velocityX) < 0.1f) return;

        Collider2D bossCol = GetComponent<Collider2D>();
        if (bossCol == null) return;

        // Xác định hướng di chuyển ngang của Boss
        Vector2 direction = velocityX > 0f ? Vector2.right : Vector2.left;

        // Tọa độ đáy của Collider Boss
        float bottomY = bossCol.bounds.min.y;
        
        // Vị trí trục X của mặt trước Collider Boss (tùy theo hướng di chuyển)
        float checkX = direction == Vector2.right ? bossCol.bounds.max.x : bossCol.bounds.min.x;

        // 1. Quét tia thấp (ở sát đất, cách đáy 0.05m) để tìm bậc thang cản trở
        Vector2 rayLowOrigin = new Vector2(checkX + (direction == Vector2.right ? 0.05f : -0.05f), bottomY + 0.05f);
        RaycastHit2D hitLow = Physics2D.Raycast(rayLowOrigin, direction, stepCheckDistance, groundAndPlatformLayers);

        if (hitLow.collider != null)
        {
            // 2. Quét tia cao (ở độ cao bước tối đa) để kiểm tra xem trên bậc có bị cản trở bởi trần hay tường cao không
            Vector2 rayHighOrigin = new Vector2(checkX + (direction == Vector2.right ? 0.05f : -0.05f), bottomY + maxStepHeight);
            RaycastHit2D hitHigh = Physics2D.Raycast(rayHighOrigin, direction, stepCheckDistance, groundAndPlatformLayers);

            if (hitHigh.collider == null)
            {
                // Cho phép Boss lướt lên bằng cách nhích nhẹ vị trí Y và đẩy nhẹ lực hướng lên
                rb.position = new Vector2(rb.position.x, rb.position.y + 0.08f);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, 2.5f));
                
                Debug.Log($"[Boss Physics] Đã tự động lướt qua bậc thang/độ dốc tại vị trí X: {checkX}");
            }
        }
    }

    protected override void Die()
    {
        if (isDead) return;

        isDead = true; // Gán thẳng vào biến của EnemyController
        currentState = BossState.Dead;

        base.Die(); 

            Debug.Log($"{gameObject.name} (Boss) đã bị tiêu diệt hoàn toàn!");
    }
}