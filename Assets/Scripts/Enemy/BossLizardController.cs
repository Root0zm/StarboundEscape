using UnityEngine;

public class BossLizardController : BossController
{
    [Header("Slash Settings")]
    public string shootTrigger = "Shoot";
    public float attackCooldown = 2.0f;
    public float attackDamage = 30f;
    public float attackRange = 3.0f;

    [Header("Teleport Settings")]
    public float maxTeleportDistance = 15f; // Khoảng cách xa bao nhiêu thì dịch chuyển
    public float teleportOffset = 2f;      // Vị trí xuất hiện gần Player

    [Header("Attack References")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 1.5f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Aura Damage Settings")]
    public float touchDamage = 10f;
    public float damageInterval = 1f;
    private float lastDamageTime;
    private float attackTimer;

    protected override void Start()
    {
        base.Start();
        rb.freezeRotation = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        attackTimer = attackCooldown;
    }

    void Update()
    {
        if (isDead || player == null || currentState == BossState.Spawning) return;

        // Nếu đang chém thì khóa mọi logic di chuyển
        if (currentState == BossState.UsingSkill) 
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);

        // 1. LOGIC TELEPORT: Nếu Player chạy quá xa
        if (dist > maxTeleportDistance)
        {
            TeleportToPlayer();
            return;
        }

        // 2. LOGIC AI (VỚI VÙNG ĐỆM CHỐNG NHẤP NHÁY)
        attackTimer += Time.deltaTime;
        LookAtPlayer();

        if (dist <= attackRange && attackTimer >= attackCooldown)
        {
            TriggerAttack();
        }
        else
        {
            // Sử dụng vùng đệm 0.5f để tránh việc chuyển đổi Idle/Moving liên tục
            if (dist > attackRange + 0.5f)
                UpdateState(BossState.Moving);
            else if (dist < attackRange)
                UpdateState(BossState.Idle);
        }
    }

    private void TeleportToPlayer()
    {
        // Tính toán vị trí xuất hiện: phía trước hoặc phía sau player
        float direction = (player.position.x - transform.position.x) > 0 ? 1f : -1f;
        Vector2 targetPos = new Vector2(player.position.x - (direction * teleportOffset), player.position.y);
        
        transform.position = targetPos;
        rb.linearVelocity = Vector2.zero; // Reset vận tốc sau khi dịch chuyển
        
        UpdateState(BossState.Idle);
        Debug.Log("Boss đã dịch chuyển đến gần Player!");
    }

    private void UpdateState(BossState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        
        if (anim != null)
        {
            anim.SetBool("isWalking", currentState == BossState.Moving);
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (currentState == BossState.Moving && player != null)
        {
            float direction = (player.position.x - transform.position.x) > 0 ? 1f : -1f;
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        }
    }

    private void TriggerAttack()
    {
        currentState = BossState.UsingSkill;
        attackTimer = 0;
        rb.linearVelocity = Vector2.zero;
        if (anim != null) anim.SetTrigger(shootTrigger);
    }

    public void ExecuteDamage()
    {
        Collider2D hit = Physics2D.OverlapCircle(attackPoint.position, attackRadius, playerLayer);
        if (hit != null) hit.GetComponent<PlayerController>()?.ChangeHealth(-(int)attackDamage);
    }

    public void OnShootFinished()
    {
        currentState = BossState.Idle;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time - lastDamageTime >= damageInterval)
            {
                collision.gameObject.GetComponent<PlayerController>()?.ChangeHealth(-(int)touchDamage);
                lastDamageTime = Time.time;
            }
        }
    }
}