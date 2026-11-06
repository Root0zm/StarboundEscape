using UnityEngine;

public class BugEnemyController : EnemyController
{
    private enum EnemyState { Idle, Chasing, Attacking, Dead }
    [SerializeField] private EnemyState currentState = EnemyState.Idle;

    [Header("References")]
    [SerializeField] private LayerMask playerLayer;       // Đã xuất hiện lại ở đây để bạn cấu hình đồng bộ

    [Header("Bug Base Stats")]
    public float bugMaxHealth = 35f;
    public int bugDamage = 10;
    public int bugMoney = 4;

    [Header("Movement & AI Ranges")]
    [SerializeField] private float flySpeed = 3.5f;
    [SerializeField] private float chaseRange = 16f;
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float personalSpace = 4f;

    [Header("Ranged Attack Settings")]
    [SerializeField] private float attackCooldown = 2.0f;
    private float attackTimer;

    [Header("Ranged Projectile References")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float bulletSpeed = 8f;

    private float originalGravityScale;

    protected override void Awake()
    {
        base.Awake();
        if (bugMaxHealth <= 0f) bugMaxHealth = 35f;

        maxHealth = bugMaxHealth;
        currentHealth = maxHealth;
        damageAmount = bugDamage;
        moneyReward = bugMoney;

        ApplyDifficultyScaling();
    }

    protected override void Start()
    {
        base.Start();
        if (rb != null)
        {
            originalGravityScale = rb.gravityScale;
            rb.gravityScale = 0f;
        }
        attackTimer = attackCooldown;
    }

    void Update()
    {
        if (isDead || player == null) return;

        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;
            return;
        }

        if (currentState == EnemyState.Dead) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        attackTimer += Time.deltaTime;

        LookAtPlayer();

        if (distanceToPlayer <= attackRange)
        {
            if (attackTimer >= attackCooldown && currentState != EnemyState.Attacking)
            {
                currentState = EnemyState.Attacking;
                TriggerShootAttack();
            }
            else if (currentState != EnemyState.Attacking)
            {
                if (distanceToPlayer < personalSpace) currentState = EnemyState.Chasing;
                else currentState = EnemyState.Idle;
            }
        }
        else if (distanceToPlayer <= chaseRange && currentState != EnemyState.Attacking)
        {
            currentState = EnemyState.Chasing;
        }
        else if (currentState != EnemyState.Attacking)
        {
            currentState = EnemyState.Idle;
        }

        if (anim != null)
        {
            anim.SetBool("isFlying", currentState != EnemyState.Dead);
        }
    }

    void FixedUpdate()
    {
        if (isDead || knockbackTimer > 0f) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (currentState == EnemyState.Chasing)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            if (distanceToPlayer < personalSpace)
            {
                rb.linearVelocity = -direction * (flySpeed * 0.7f) * speedMultiplier;
            }
            else
            {
                rb.linearVelocity = direction * flySpeed * speedMultiplier;
            }
        }
        else if (currentState == EnemyState.Idle || currentState == EnemyState.Attacking)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void TriggerShootAttack()
    {
        attackTimer = 0f;
        if (anim != null)
        {
            anim.ResetTrigger("Shoot");
            anim.SetTrigger("Shoot");
        }
        PlayAttackSound();
    }

    // GỌI BẰNG ANIMATION EVENT
    public void ExecuteBugShoot()
    {
        if (isDead || player == null || bulletPrefab == null || shootPoint == null) return;

        GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, Quaternion.identity);
        Vector2 shootDirection = (player.position - shootPoint.position).normalized;

        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = shootDirection * bulletSpeed;
            float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        // TRUYỀN CẢ SÁT THƯƠNG VÀ PLAYER LAYER SANG CHO VIÊN ĐẠN
        EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
        if (bulletScript != null)
        {
            bulletScript.Setup(damageAmount, playerLayer);
        }
    }

    public void ResetAttackState()
    {
        if (currentState != EnemyState.Dead && !isDead)
        {
            currentState = EnemyState.Idle;
            if (anim != null) anim.ResetTrigger("Shoot");
        }
    }

    private void LookAtPlayer()
    {
        if (player.position.x > transform.position.x && !isFacingRight) Flip();
        else if (player.position.x < transform.position.x && isFacingRight) Flip();
    }

    protected override void ApplyKnockback()
    {
        base.ApplyKnockback();
        if (currentState != EnemyState.Dead && !isDead) currentState = EnemyState.Idle;
    }

    protected override void PlayDeathAnimation()
    {
        currentState = EnemyState.Dead;
        if (rb != null)
        {
            rb.gravityScale = originalGravityScale > 0 ? originalGravityScale : 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (anim != null)
        {
            anim.SetTrigger("Die");
            anim.Play("Die");
        }
        this.enabled = false;
    }
}