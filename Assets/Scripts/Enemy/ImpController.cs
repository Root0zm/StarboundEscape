using System.Collections;
using UnityEngine;

public class ImpController : EnemyController
{
    private enum EnemyState { Spawning, Idle, Chasing, Attacking, Teleporting, Dead }
    [SerializeField] private EnemyState currentState = EnemyState.Spawning;

    [Header("References")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask playerLayer;

    [Header("Imp Base Stats")]
    public float impMaxHealth = 50f;
    public int impDamage = 15;
    public int impMoney = 5;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float chaseRange = 20f;    // Khoảng cách phát hiện người chơi
    [SerializeField] private float attackRange = 1.5f;   // Khoảng cách để vung kiếm chém

    [Header("Teleport Settings (Skill 2)")]
    [SerializeField] private float teleportCooldown = 7f; // 7 giây dịch chuyển 1 lần
    [SerializeField] private float teleportTriggerRange = 6f; // Khoảng cách xa quá thì sẽ dùng biến chiêu để áp sát
    private float teleportTimer;

    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 1.5f; // Khoảng cách giữa các lần chém
    [SerializeField] private float attackRadius = 0.8f;   // Bán kính quét gây sát thương
    private float attackTimer;

    protected override void Awake()
    {
        // Gọi Awake của lớp cha để nạp Rigidbody, Animator và phát hiện gắn trùng script
        base.Awake();

        // Đảm bảo các chỉ số tối thiểu cho quái
        if (impMaxHealth <= 0f) impMaxHealth = 50f;

        // Đồng bộ các chỉ số của lớp cha EnemyController
        maxHealth = impMaxHealth;
        currentHealth = maxHealth;
        damageAmount = impDamage;
        moneyReward = impMoney;

        ApplyDifficultyScaling();
    }

    protected override void Start()
    {
        // Gọi Start của lớp cha để tự động dò tìm vị trí Player bằng Tag
        base.Start();

        teleportTimer = teleportCooldown; // Sẵn sàng dịch chuyển sau khi spawn
        // Trạng thái đầu tiên là Spawning, Animator sẽ tự chạy Anim dịch chuyển lúc đầu
    }

    void Update()
    {
        // Nếu chết hoặc không có player thì dừng hoạt động
        if (isDead || player == null) return;

        // Xử lý thời gian bị đẩy lùi (Knockback)
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;
            return; // Đóng băng AI di chuyển và dùng chiêu khi bị đánh trúng
        }

        if (currentState == EnemyState.Dead || currentState == EnemyState.Spawning || currentState == EnemyState.Teleporting) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        teleportTimer += Time.deltaTime;

        // Tăng thời gian hồi đòn chém theo thời gian thực
        attackTimer += Time.deltaTime;

        LookAtPlayer();

        // --- BỘ NÃO QUYẾT ĐỊNH TRẠNG THÁI AI ---
        if (distanceToPlayer <= attackRange)
        {
            // CHỈ CHÉM KHI ĐÃ HẾT THỜI GIAN HỒI CHIÊU CHIÊU CẬN CHIẾN
            if (attackTimer >= attackCooldown)
            {
                currentState = EnemyState.Attacking;
                TriggerSlashAttack();
            }
            else
            {
                // Nếu chưa hồi chiêu xong thì đứng im thở (Idle) nhìn Player chứ không chém bậy
                currentState = EnemyState.Idle;
            }
        }
        else if (distanceToPlayer > teleportTriggerRange && teleportTimer >= teleportCooldown)
        {
            StartCoroutine(TeleportSkillRoutine());
        }
        else if (distanceToPlayer <= chaseRange)
        {
            currentState = EnemyState.Chasing;
        }
        else
        {
            currentState = EnemyState.Idle;
        }

        if (anim != null)
        {
            anim.SetBool("isWalking", currentState == EnemyState.Chasing);
        }
    }

    private void TriggerSlashAttack()
    {
        attackTimer = 0f; // RESET LẠI BỘ ĐẾM CHIÊU NGAY KHI VỪA BẤM TRIGGER CHÉM
        if (anim != null)
        {
            // Xóa sạch các lệnh chém cũ còn sót trong hàng đợi trước khi ra đòn mới
            anim.ResetTrigger("Slash");
            anim.SetTrigger("Slash");
        }
        PlayAttackSound();
    }

    // =================================================================
    // HÀM ĐƯỢC GỌI BỞI ANIMATION EVENT 'ExecuteSlashAttack' TRONG HOẠT ẢNH 'shoot'
    // =================================================================
    public void ExecuteSlashAttack()
    {
        if (isDead) return;

        Debug.Log($"[Imp Event] Thực hiện gây sát thương của Imp! Bán kính quét: {attackRadius}, Sát thương: {damageAmount}");
        if (attackPoint == null)
        {
            Debug.LogError($"[LỖI CẤU HÌNH] Đối tượng '{gameObject.name}' chưa được gán 'Attack Point' trong Inspector!");
            return;
        }

        Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint.position, attackRadius, playerLayer);
        if (hitPlayer != null)
        {
            PlayerController playerScript = hitPlayer.GetComponent<PlayerController>();
            if (playerScript == null)
            {
                playerScript = hitPlayer.GetComponentInParent<PlayerController>();
            }

            if (playerScript != null)
            {
                playerScript.ChangeHealth(-damageAmount);
                Debug.Log($"[Imp Event] Gây thành công {damageAmount} sát thương cho Player!");
            }
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;

        if (currentState == EnemyState.Chasing)
        {
            // Di chuyển áp sát Player
            Vector2 direction = (player.position - transform.position).normalized;
            rb.linearVelocity = new Vector2(direction.x * moveSpeed * speedMultiplier, rb.linearVelocity.y);
        }
        else if (currentState == EnemyState.Idle || currentState == EnemyState.Attacking || currentState == EnemyState.Teleporting)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Đứng im khi dùng chiêu hoặc nghỉ
        }
    }

    // Kêu gọi từ Animation Event khi kết thúc hoạt ảnh chém để quái quay lại trạng thái cũ
    public void ResetAttackState()
    {
        if (currentState != EnemyState.Dead && !isDead)
        {
            currentState = EnemyState.Idle;
            if (anim != null)
            {
                // Khi hoạt ảnh chém kết thúc, lập tức xóa bỏ lệnh Trigger Slash 
                // để tránh việc Unity tự động chém phát nữa ngoài ý muốn
                anim.ResetTrigger("Slash");
            }
        }
    }

    // --- ĐÒN ĐÁNH 2: DỊCH CHUYỂN ÁP SÁT (Teleport) ---
    private IEnumerator TeleportSkillRoutine()
    {
        currentState = EnemyState.Teleporting;
        teleportTimer = 0f;

        // 1. Kích hoạt hoạt ảnh Dịch Chuyển bên ngoài Animator
        // Hoạt ảnh này sẽ tự chạy 2 frame đầu tiên tại chỗ
        if (anim != null) anim.SetTrigger("Teleport");

        // Code Coroutine lúc này KHÔNG dùng WaitForSeconds để đoán mò thời gian nữa.
        // Nó sẽ tạm thời "treo" ở đây, đợi cho đến khi Animation chạy tới Event ở giữa 4 frame.
        yield return null;
    }

    // =================================================================
    // HÀM MA THUẬT: ĐƯỢC GỌI BỞI ANIMATION EVENT Ở GIỮA FRAME 2 VÀ FRAME 3
    // =================================================================
    public void ExecuteCoordinateShift()
    {
        // Chỉ thực hiện dịch chuyển nếu quái còn sống và đang trong trạng thái dùng chiêu
        if (player != null && currentState == EnemyState.Teleporting && !isDead)
        {
            if (groundLayer == 0)
            {
                groundLayer = LayerMask.GetMask("Ground") | LayerMask.GetMask("Platform");
            }

            // Tính toán khoảng cách để xuất hiện sát cạnh Player (cách 1.2 đoạn để sẵn sàng chém)
            float directionToMe = player.position.x > transform.position.x ? -1.2f : 1.2f;

            Vector3 targetPos = new Vector3(player.position.x + directionToMe, player.position.y, transform.position.z);

            // Kiểm tra va chạm vật lý với môi trường bằng phương thức IsPositionSafe từ lớp cha
            if (!IsPositionSafe(targetPos))
            {
                // Thử đổi sang bên ngược lại của Player
                float oppositeDir = -directionToMe;
                Vector3 oppositePos = new Vector3(player.position.x + oppositeDir, player.position.y, transform.position.z);
                if (IsPositionSafe(oppositePos))
                {
                    targetPos = oppositePos;
                }
                else
                {
                    // Nếu cả hai bên đều kẹt, dịch chuyển thẳng tới vị trí an toàn của Player
                    targetPos = new Vector3(player.position.x, player.position.y, transform.position.z);
                }
            }

            // Bốc quái đặt bộp một phát sang tọa độ mới
            transform.position = targetPos;

            // Sau khi đổi tọa độ xong, Animator sẽ tự động chạy nốt 2 frame sau (hiện hình ra)
            // Lúc này chúng ta mở Coroutine nhỏ để chờ hoạt ảnh chạy nốt rồi trả về Idle
            StartCoroutine(FinishTeleportAfterAnimation());
        }
    }

    private IEnumerator FinishTeleportAfterAnimation()
    {
        // Chờ một khoảng cực ngắn để 2 frame xuất hiện chạy xong xuôi (khoảng 0.2 giây)
        yield return new WaitForSeconds(0.2f);

        if (!isDead)
        {
            currentState = EnemyState.Idle;
        }
    }

    // --- HÀM XỬ LÝ LÚC VỪA SINH RA (SPAWN) ---
    // Sẽ được gọi bằng Animation Event ở FRAME CUỐI CÙNG của hoạt ảnh Spawn/Teleport lúc đầu game
    public void FinishSpawning()
    {
        if (currentState == EnemyState.Spawning)
        {
            currentState = EnemyState.Idle;
        }
    }

    private void LookAtPlayer()
    {
        if (player.position.x > transform.position.x && !isFacingRight) Flip();
        else if (player.position.x < transform.position.x && isFacingRight) Flip();
    }

    // Khi nhận sát thương và bị đẩy lùi, tắt ngay hoạt ảnh đi bộ và reset trạng thái AI
    protected override void ApplyKnockback()
    {
        base.ApplyKnockback();
        if (anim != null) anim.SetBool("isWalking", false);
        if (currentState != EnemyState.Dead && !isDead)
        {
            currentState = EnemyState.Idle;
        }
    }

    // Định nghĩa hoạt ảnh chết của riêng Imp kế thừa từ EnemyController
    protected override void PlayDeathAnimation()
    {
        currentState = EnemyState.Dead;
        if (anim != null)
        {
            anim.SetTrigger("Die");
            anim.Play("Die"); // Ép chạy hoạt ảnh chết ngay lập tức
        }
        this.enabled = false; // Tắt script để ngưng Update
        Debug.Log("Imp ngã xuống theo phong cách riêng và rơi ra " + moneyReward + " vàng.");
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
    }
}