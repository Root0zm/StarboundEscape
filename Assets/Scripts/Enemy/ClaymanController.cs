using UnityEngine;

public class ClaymanController : EnemyController
{
         public float claymanMaxHealth = 70f;
    public int claymanDamage = 10;
    public int claymanMoney = 3;
    public float moveSpeed = 2.5f;

    [Header("Clayman Stats & AI Ranges")]
    public float chaseRange = 16f;
    public float attackRange = 1.2f;
    public float attackCooldown = 1.5f;
    private float nextAttackTime;

    public Transform attackPoint;
    public float attackRadius = 0.5f;
    public LayerMask playerLayer;

    protected override void Awake()
    {
        base.Awake();

        if (claymanMaxHealth <= 0f) claymanMaxHealth = 70f;
        if (claymanDamage <= 0) claymanDamage = 10;
        if (claymanMoney <= 0) claymanMoney = 3;

        maxHealth = claymanMaxHealth;
        currentHealth = maxHealth;
        damageAmount = claymanDamage;
        moneyReward = claymanMoney;

        ApplyDifficultyScaling();
    }

    protected override void Start()
    {
                 base.Start();
    }

    void Update()
    {
                 if (isDead || player == null) return;

                 if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;
            return;          }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

                 if (anim != null)
        {
            bool isGrounded = CheckGrounded();
            anim.SetBool("isJumping", !isGrounded);
        }

                 if (distanceToPlayer <= attackRange)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            if (anim != null) anim.SetBool("isWalking", false);  
            if (Time.time >= nextAttackTime)
            {
                ExecuteSlashAttack();
                nextAttackTime = Time.time + attackCooldown;
            }
        }
                 else if (distanceToPlayer <= chaseRange)
        {
            ChasePlayer();
        }
                 else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            if (anim != null) anim.SetBool("isWalking", false);          }
    }

    private void ChasePlayer()
    {
        float direction = (player.position.x - transform.position.x) > 0 ? 1 : -1;
        rb.linearVelocity = new Vector2(direction * moveSpeed * speedMultiplier, rb.linearVelocity.y);

        if (anim != null) anim.SetBool("isWalking", true);  
        if (direction > 0 && !isFacingRight) Flip();
        else if (direction < 0 && isFacingRight) Flip();

        // Tự động quét và nhảy tránh vật cản hoặc vực sâu khi đuổi theo người chơi
        HandleObstacleAndGapAvoidance();
    }

         protected override void ApplyKnockback()
    {
        base.ApplyKnockback();
        if (anim != null) anim.SetBool("isWalking", false);
    }

    private void ExecuteSlashAttack()
    {
        if (anim != null) anim.SetTrigger("Shoot");
        PlayAttackSound();
    }
    public void Clayman_DealDamageEvent()
    {
        
        Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint.position, attackRadius, playerLayer);
        if (hitPlayer != null)
        {
            PlayerController playerScript = hitPlayer.GetComponent<PlayerController>();
            if (playerScript != null)
            {
               
                playerScript.ChangeHealth(-damageAmount);
               
            }
        }
    }


    protected override void PlayDeathAnimation()
    {
        if (anim != null)
        {
            anim.SetTrigger("Death");
            anim.Play("Death");          }
        Debug.Log("Clayman died" + moneyReward + " vàng.");
    }
}