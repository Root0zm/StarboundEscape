using UnityEngine;

public class GolemController : EnemyController
{
    [Header("Golem Role Designation")]
    [Tooltip("Đánh dấu nếu đây là Boss Golem/Colossus chính của màn 3")]
    public bool isBoss_Map3 = false;

    [Header("Golem AI Stats")]
    [SerializeField] private float chaseRange = 15.0f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 2.0f;
    [SerializeField] private float baseSpeed = 3.0f;

    [Header("Golem Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.2f);

    private bool isGrounded = false;
    private float lastAttackTime = 0f;
    private float patrolDirection = 1f;

    protected override void Awake()
    {
        base.Awake();

        // Tự động gán layer đất nếu chưa cấu hình
        if (groundLayer == 0)
        {
            groundLayer = LayerMask.GetMask("Ground") | LayerMask.GetMask("Platform");
        }

        // Thiết lập các chỉ số tương thích dựa trên vai trò là Boss hay Quái thường
        maxHealth = isBoss_Map3 ? 500f : 200f;
        currentHealth = maxHealth;
        damageAmount = isBoss_Map3 ? 30 : 20;
        moneyReward = isBoss_Map3 ? 100 : 10;
    }

    protected override void Start()
    {
        base.Start();

        // Tự động tìm Player nếu chưa được gắn
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void Update()
    {
        if (isDead) return;

        UpdateGroundedStatus();
        UpdateAnimations();

        if (player == null) return;
        PlayerController pc = player.GetComponent<PlayerController>();

        // Nếu người chơi đã hy sinh, quay lại trạng thái đi tuần tra (Patrol)
        if (pc != null && pc.isDead)
        {
            Patrol();
            HandleObstacleAndGapAvoidance();
            return;
        }

        float distToPlayer = Vector2.Distance(transform.position, player.position);
        float xDistance = Mathf.Abs(player.position.x - transform.position.x);

        // Logic săn lùng người chơi (Chase/Attack AI)
        if (distToPlayer <= chaseRange)
        {
            if (xDistance > 0.8f && distToPlayer > attackRange)
            {
                ChasePlayer();
                HandleObstacleAndGapAvoidance();
            }
            else
            {
                StopMoving();
                if (distToPlayer <= attackRange)
                {
                    AttackPlayer();
                }
            }
        }
        else
        {
            Patrol();
            HandleObstacleAndGapAvoidance();
        }
    }

    private void UpdateGroundedStatus()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
        }
    }

    private void UpdateAnimations()
    {
        if (anim != null)
        {
            bool movingHorizontally = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
            anim.SetBool("isWalking", movingHorizontally && isGrounded);
            anim.SetBool("isFalling", !isGrounded && rb.linearVelocity.y < -0.1f);
        }
    }

    private void Patrol()
    {
        if (rb == null) return;

        rb.linearVelocity = new Vector2(patrolDirection * (baseSpeed * 0.5f), rb.linearVelocity.y);
        
        // Cập nhật hướng mặt logic để đồng bộ kiểm tra nhảy tránh vật cản
        isFacingRight = (patrolDirection > 0);

        // Điều chỉnh hướng mặt của sprite
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = (patrolDirection < 0);
        }

        // Đổi hướng đi tuần tra khi đâm vào chướng ngại vật
        if (Mathf.Abs(rb.linearVelocity.x) < 0.05f && isGrounded)
        {
            patrolDirection *= -1;
        }
    }

    private void ChasePlayer()
    {
        if (rb == null || player == null) return;

        float directionX = Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(directionX * baseSpeed, rb.linearVelocity.y);
        
        // Cập nhật hướng mặt logic để đồng bộ kiểm tra nhảy tránh vật cản
        isFacingRight = (directionX > 0);

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = (directionX < 0);
        }
    }

    private void StopMoving()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    private void AttackPlayer()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            if (anim != null)
            {
                anim.SetTrigger("Shoot");
            }
            lastAttackTime = Time.time;
        }
    }

    /// <summary>
    /// Được gọi bởi Animation Event trong hoạt ảnh đánh trúng của Golem
    /// </summary>
    public void AnimationEvent_DealDamage()
    {
        if (isDead || player == null) return;

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null && !pc.isDead)
        {
            float distToPlayer = Vector2.Distance(transform.position, player.position);
            if (distToPlayer <= attackRange + 0.5f)
            {
                pc.ChangeHealth(-damageAmount);
            }
        }
    }

    // Override chỉ số báo Boss cho UIManager
    public override bool IsBoss()
    {
        return isBoss_Map3;
    }

    protected override void Die()
    {
        // Gọi Die gốc để cộng tiền, cộng số quái diệt, và cập nhật UI Boss
        base.Die();

        if (anim != null)
        {
            anim.SetTrigger("Death");
        }

        Destroy(gameObject, 3.0f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        if (groundCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }
}
