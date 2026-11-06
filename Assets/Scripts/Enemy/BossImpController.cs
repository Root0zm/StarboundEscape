using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossImpController : BossController
{
    [Header("Animator Trigger Names (Khớp với Animator của bạn)")]
    public string skill1TriggerName = "Skill1"; // Trigger kích hoạt Skill 1 (Ví dụ: Skill1)
    public string skill2TriggerName = "Skill2"; // Trigger kích hoạt Skill 2 (Ví dụ: Skill2)
    public string skill3TriggerName = "Skill3"; // Trigger kích hoạt Skill 3 (Ví dụ: Skill3)
    public string skill4TriggerName = "Skill4"; // Trigger kích hoạt Skill 4 (Ví dụ: Skill4)

    [Header("Skill 1: Slash In Place (Chém tại chỗ)")]
    public float skill1Cooldown = 3f;
    public float skill1Damage = 25f;
    public float skill1Range = 2f;
    private float skill1Timer;

    [Header("Skill 2: Dash Slash (Chém lướt 3 lần)")]
    public float skill2Cooldown = 8f;
    public float skill2Damage = 35f;
    public float skill2DashSpeed = 8f;
    [Range(1, 5)] public int skill2RepeatCount = 3; // Mặc định lặp lại 3 lần
    private float skill2Timer;
    private int skill2RepeatCounter = 0;
    private bool isDashing = false;

    [Header("Skill 3: Teleport Skill (Biến chiêu tiếp cận)")]
    public float skill3Cooldown = 12f;
    public float skill3TriggerRange = 7f; // Khi người chơi đứng quá xa, dùng để áp sát
    public float skill3Offset = 1.5f; // Khoảng cách xuất hiện cạnh người chơi
    private float skill3Timer;

    [Header("Skill 4: Orb Shooting (Bắn quả cầu ma pháp)")]
    public float skill4Cooldown = 6f;
    public float skill4Damage = 20f;
    public float skill4ProjSpeed = 6f; // Tốc độ bay của quả cầu (Có thể chỉnh sửa trong Inspector)
    public GameObject skill4ProjPrefab; // Prefab của BossProjectile
    public Transform firePoint; // Điểm bắn quả cầu
    private float skill4Timer;

    [Header("Attack Range Check")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 1f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Boss SFX Config")]
    [SerializeField] private AudioClip skill1Sound;
    [SerializeField] private AudioClip skill2Sound;
    [SerializeField] private AudioClip skill3Sound;
    [SerializeField] private AudioClip skill4Sound;

    private int currentSkillNum = 0; // 0: None, 1: Skill1, 2: Skill2, 3: Skill3, 4: Skill4

    protected override void Start()
    {
        // Gọi Start của lớp cha BossController (khởi tạo trạng thái và tìm kiếm Player)
        base.Start();

        // Tự động kiểm tra và đồng bộ tên Trigger khớp với Animator để tránh kẹt lỗi cấu hình Inspector
        ValidateAndFixTriggerNames();

        // Sẵn sàng dùng chiêu khi vừa spawn
        skill1Timer = skill1Cooldown;
        skill2Timer = skill2Cooldown;
        skill3Timer = skill3Cooldown;
        skill4Timer = skill4Cooldown;
    }

    void Update()
    {
        if (isDead || player == null) return;

        // Xử lý đẩy lùi (nếu không miễn nhiễm)
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;
            return;
        }

        // [HỖ TRỢ LỖI] Cảnh báo nếu Boss bị kẹt ở trạng thái xuất hiện quá lâu
        if (currentState == BossState.Spawning)
        {
            if (Time.time - spawnStartTime > 5f)
            {
                if (Time.frameCount % 300 == 0)
                {
                    Debug.LogWarning($"[HỖ TRỢ LỖI] Boss '{gameObject.name}' đang dừng ở trạng thái xuất hiện (Spawning). Vui lòng kiểm tra xem bạn đã gắn Animation Event 'FinishSpawning' ở frame cuối cùng của hoạt ảnh Spawn chưa, hoặc tắt 'Play Spawn Animation On Start' trong Inspector.");
                }
            }
            return;
        }

        // [HỖ TRỢ LỖI] Cảnh báo nếu Boss bị kẹt ở trạng thái thi triển Skill quá lâu (Tự động cứu về Idle để tránh treo game)
        if (currentState == BossState.UsingSkill)
        {
            float maxDuration = 4.5f;
            if (currentSkillNum == 2) maxDuration = 11f;

            if (Time.time - skillStartTime > maxDuration)
            {
                Debug.LogWarning($"[HỖ TRỢ LỖI] Boss '{gameObject.name}' bị kẹt ở trạng thái dùng Skill {currentSkillNum} quá lâu! Đang tự động giải cứu về Idle để tiếp tục chiến đấu.\n👉 MẸO UNITY: Hãy mở hoạt ảnh 'shoot{currentSkillNum}' lên, click vào điểm gắn Event 'OnSkill{currentSkillNum}Finished' và DỊCH NÓ SANG TRÁI 1 ĐẾN 2 FRAME (tránh đặt sát sạt ở frame cuối cùng). Điều này giúp Unity kịp chạy Event trước khi nhảy sang Walk!");
                ResetToIdle(currentSkillNum); // Tự động cứu vật lý và AI
            }
            return;
        }

        // Tăng thời gian hồi chiêu của các skill
        skill1Timer += Time.deltaTime;
        skill2Timer += Time.deltaTime;
        skill3Timer += Time.deltaTime;
        skill4Timer += Time.deltaTime;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Quay mặt về phía người chơi khi ở trạng thái bình thường
        LookAtPlayer();

        // --- BỘ NÃO QUYẾT ĐỊNH LỰA CHỌN TRẠNG THÁI AI (BẮT CHƯỚC PHONG CÁCH IMP) ---
        
        // Vùng 1: Người chơi đứng ở tầm xa -> Ưu tiên dịch chuyển áp sát hoặc bắn cầu ma pháp
        if (distanceToPlayer > skill3TriggerRange)
        {
            if (skill3Timer >= skill3Cooldown)
            {
                TriggerSkill3();
            }
            else if (skill4Timer >= skill4Cooldown)
            {
                TriggerSkill4();
            }
            else
            {
                currentState = BossState.Moving;
            }
        }
        // Vùng 2: Người chơi đứng ở cự ly gần -> Dừng di chuyển và ưu tiên dùng chiêu cận chiến (Skill 2 hoặc Skill 1)
        else if (distanceToPlayer <= skill1Range)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Đứng im chuẩn bị ra đòn

            if (skill2Timer >= skill2Cooldown)
            {
                TriggerSkill2();
            }
            else if (skill1Timer >= skill1Cooldown)
            {
                TriggerSkill1();
            }
            else if (skill4Timer >= skill4Cooldown)
            {
                TriggerSkill4();
            }
            else
            {
                currentState = BossState.Idle; // Tất cả chiêu đang hồi -> Đứng im nhìn người chơi
            }
        }
        // Vùng 3: Người chơi đứng ở cự ly trung bình -> Ưu tiên bắn phép cấu rỉa, nếu không hồi chiêu thì đi bộ lại gần
        else
        {
            if (skill4Timer >= skill4Cooldown)
            {
                TriggerSkill4();
            }
            else
            {
                currentState = BossState.Moving;
            }
        }

        // Cập nhật Animator hoạt ảnh đi bộ
        if (anim != null)
        {
            anim.SetBool("isWalking", currentState == BossState.Moving);
        }
    }

    protected override void FixedUpdate()
    {
        // Gọi FixedUpdate của lớp cha BossController để xử lý va chạm Platform linh hoạt
        base.FixedUpdate();

        if (isDead) return;

        // Di chuyển áp sát người chơi giống phong cách của Imp
        if (currentState == BossState.Moving && player != null)
        {
            float direction = (player.position.x - transform.position.x) > 0 ? 1f : -1f;
            rb.linearVelocity = new Vector2(direction * moveSpeed * speedMultiplier, rb.linearVelocity.y);
        }
        // Đang thực hiện lướt trong Skill 2 (Lướt chém)
        else if (currentState == BossState.UsingSkill && currentSkillNum == 2 && isDashing)
        {
            float direction = isFacingRight ? 1f : -1f;
            rb.linearVelocity = new Vector2(direction * skill2DashSpeed, rb.linearVelocity.y);
        }
        // Đứng im khi chém tại chỗ, dịch chuyển hoặc bắn cầu phép
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    private void ResetToIdle(int skillNum)
    {
        currentState = BossState.Idle;
        isDashing = false;
        if (rb != null) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        currentSkillNum = 0;

        // Reset thời gian hồi chiêu của đúng chiêu thức vừa thi triển xong
        if (skillNum == 1) skill1Timer = 0f;
        else if (skillNum == 2) skill2Timer = 0f;
        else if (skillNum == 3) skill3Timer = 0f;
        else if (skillNum == 4) skill4Timer = 0f;
    }

    // Ghi đè lực đẩy lùi: Boss ngắt mọi chiêu đang thi triển dở dang nếu bị gián đoạn
    protected override void ApplyKnockback()
    {
        if (immuneToKnockback)
        {
            knockbackTimer = 0f;
            return;
        }

        base.ApplyKnockback();
        if (anim != null) anim.SetBool("isWalking", false);
        if (currentState != BossState.Dead && !isDead)
        {
            currentState = BossState.Idle;
        }
    }

    // =================================================================
    // KÍCH HOẠT CÁC CHIÊU THỨC THỜI GIAN THỰC (ANIMATION EVENT-DRIVEN)
    // =================================================================
    
    private void TriggerSkill1()
    {
        currentState = BossState.UsingSkill;
        currentSkillNum = 1;
        skillStartTime = Time.time;
        
        if (anim != null)
        {
            anim.ResetTrigger(skill1TriggerName);
            anim.SetTrigger(skill1TriggerName);
        }
        Debug.Log($"[Boss AI] Kích hoạt dùng chiêu Skill 1 (Kích Trigger: '{skill1TriggerName}')!");

        if (AudioManager.Instance != null && skill1Sound != null)
            AudioManager.Instance.PlaySFX3D(skill1Sound, transform.position);
        else
            PlayAttackSound();
    }

    private void TriggerSkill2()
    {
        currentState = BossState.UsingSkill;
        currentSkillNum = 2;
        skill2RepeatCounter = 1; // Bắt đầu đếm lượt lướt đầu tiên
        skillStartTime = Time.time;
        
        if (anim != null)
        {
            anim.ResetTrigger(skill2TriggerName);
            anim.SetTrigger(skill2TriggerName);
        }
        Debug.Log($"[Boss AI] Kích hoạt dùng chiêu Skill 2 (Kích Trigger: '{skill2TriggerName}')!");

        if (AudioManager.Instance != null && skill2Sound != null)
            AudioManager.Instance.PlaySFX3D(skill2Sound, transform.position);
        else
            PlayAttackSound();
    }

    private void TriggerSkill3()
    {
        currentState = BossState.UsingSkill;
        currentSkillNum = 3;
        skillStartTime = Time.time;
        
        if (anim != null)
        {
            anim.ResetTrigger(skill3TriggerName);
            anim.SetTrigger(skill3TriggerName);
        }
        Debug.Log($"[Boss AI] Kích hoạt dùng chiêu Skill 3 (Kích Trigger: '{skill3TriggerName}')!");

        if (AudioManager.Instance != null && skill3Sound != null)
            AudioManager.Instance.PlaySFX3D(skill3Sound, transform.position);
        else
            PlayAttackSound();
    }

    private void TriggerSkill4()
    {
        currentState = BossState.UsingSkill;
        currentSkillNum = 4;
        skillStartTime = Time.time;
        
        if (anim != null)
        {
            anim.ResetTrigger(skill4TriggerName);
            anim.SetTrigger(skill4TriggerName);
        }
        Debug.Log($"[Boss AI] Kích hoạt dùng chiêu Skill 4 (Kích Trigger: '{skill4TriggerName}')!");

        if (AudioManager.Instance != null && skill4Sound != null)
            AudioManager.Instance.PlaySFX3D(skill4Sound, transform.position);
        else
            PlayAttackSound();
    }

    // =================================================================
    // HÀM ĐƯỢC GỌI BỞI ANIMATION EVENTS (SỰ KIỆN HOẠT ẢNH)
    // =================================================================

    // --- SKILL 1 EVENTS ---
    public void ExecuteSkill1Damage()
    {
        Debug.Log("[Boss Event] Thực hiện gây sát thương Skill 1!");
        if (attackPoint == null) return;
        Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint.position, attackRadius, playerLayer);
        if (hitPlayer != null)
        {
            PlayerController pc = hitPlayer.GetComponent<PlayerController>();
            if (pc != null) pc.ChangeHealth(-(int)skill1Damage);
        }
    }

    public void OnSkill1Finished()
    {
        Debug.Log("[Boss Event] Hoàn tất Skill 1 ➔ Quay lại trạng thái Idle và reset hồi chiêu.");
        ResetToIdle(1);
    }

    // --- SKILL 2 EVENTS (Lướt chém liên hoàn 3 lần) ---
    public void StartSkill2Dash()
    {
        Debug.Log("[Boss Event] Bắt đầu lướt nhanh của Skill 2.");
        isDashing = true;
    }

    public void StopSkill2Dash()
    {
        Debug.Log("[Boss Event] Dừng lướt nhanh của Skill 2.");
        isDashing = false;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    public void ExecuteSkill2Damage()
    {
        Debug.Log("[Boss Event] Thực hiện gây sát thương diện rộng của Skill 2.");
        if (attackPoint == null) return;
        Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint.position, attackRadius * 1.5f, playerLayer);
        if (hitPlayer != null)
        {
            PlayerController pc = hitPlayer.GetComponent<PlayerController>();
            if (pc != null) pc.ChangeHealth(-(int)skill2Damage);
        }
    }

    public void OnSkill2Finished()
    {
        isDashing = false;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        Debug.Log($"[Boss Event] Kết thúc lượt lướt {skill2RepeatCounter}/{skill2RepeatCount} của Skill 2.");

        if (skill2RepeatCounter < skill2RepeatCount)
        {
            skill2RepeatCounter++;
            LookAtPlayer(); // Quay mặt đuổi theo Player trước khi lướt tiếp
            skillStartTime = Time.time; // Gia hạn thêm thời gian bắt đầu lướt tiếp theo
            
            if (anim != null)
            {
                anim.ResetTrigger(skill2TriggerName);
                anim.SetTrigger(skill2TriggerName);
                anim.Play(skill2TriggerName, -1, 0f); // Ép chạy lại hoạt ảnh từ đầu ngay lập tức
            }
        }
        else
        {
            Debug.Log("[Boss Event] Hoàn thành đủ 3 lượt lướt Skill 2 ➔ Quay lại Idle và reset hồi chiêu.");
            ResetToIdle(2);
        }
    }

    // --- SKILL 3 EVENTS (Dịch chuyển áp sát) ---
    public void ExecuteBossTeleport()
    {
        Debug.Log("[Boss Event] Thực hiện dịch chuyển vị trí của Boss.");
        if (player == null) return;

        if (groundLayer == 0)
        {
            groundLayer = LayerMask.GetMask("Ground") | LayerMask.GetMask("Platform");
        }

        // Xuất hiện bên trái hoặc bên phải Player tùy thuộc vị trí hiện tại
        float direction = player.position.x > transform.position.x ? -skill3Offset : skill3Offset;
        Vector3 targetPos = new Vector3(player.position.x + direction, player.position.y, transform.position.z);

        // Kiểm tra va chạm vật lý với môi trường bằng phương thức IsPositionSafe kế thừa từ lớp cha
        if (!IsPositionSafe(targetPos))
        {
            // Thử đổi sang bên ngược lại của Player
            float oppositeDir = -direction;
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

        transform.position = targetPos;
        
        LookAtPlayer();
    }

    public void OnSkill3Finished()
    {
        Debug.Log("[Boss Event] Hoàn tất Skill 3 ➔ Quay lại trạng thái Idle và reset hồi chiêu.");
        ResetToIdle(3);
    }

    // --- SKILL 4 EVENTS (Bắn quả cầu ma pháp) ---
    public void ExecuteSkill4Shoot()
    {
        if (skill4ProjPrefab == null)
        {
            Debug.LogError($"[LỖI CẤU HÌNH] Boss '{gameObject.name}' chưa được gán 'Skill 4 Proj Prefab' (Prefab Quả Cầu) trong Inspector!");
            return;
        }
        if (firePoint == null)
        {
            Debug.LogError($"[LỖI CẤU HÌNH] Boss '{gameObject.name}' chưa được gán 'Fire Point' (Điểm bắn) trong Inspector!");
            return;
        }

        GameObject projectileObj = Instantiate(skill4ProjPrefab, firePoint.position, Quaternion.identity);
        BossProjectile projectile = projectileObj.GetComponent<BossProjectile>();
        
        if (projectile != null)
        {
            // Xác định hướng bắn dựa theo hướng mặt của Boss
            Vector2 direction = isFacingRight ? Vector2.right : Vector2.left;
            projectile.Setup(skill4Damage, skill4ProjSpeed, direction);
            Debug.Log($"[Boss Event] Bắn thành công Quả cầu ma pháp!");
        }
        else
        {
            Debug.LogError($"[LỖI CẤU HÌNH] Prefab quả cầu '{skill4ProjPrefab.name}' của bạn chưa được gắn Script 'BossProjectile'!");
        }
    }

    public void OnSkill4Finished()
    {
        Debug.Log("[Boss Event] Hoàn tất Skill 4 ➔ Quay lại trạng thái Idle và reset hồi chiêu.");
        ResetToIdle(4);
    }

    // --- HOẠT ẢNH CHẾT (DEATH ANIMATION) ---
    protected override void PlayDeathAnimation()
    {
        currentState = BossState.Dead;
        if (anim != null)
        {
            anim.ResetTrigger(deathTriggerName);
            anim.SetTrigger(deathTriggerName);
            anim.Play(deathStateName, -1, 0f); // Ép chạy hoạt ảnh chết của Boss ngay lập tức
        }
        this.enabled = false; // Tắt trí thông minh AI
        Debug.Log("BOSS IMP ĐÃ BỊ TIÊU DIỆT! Bạn nhận được " + moneyReward + " vàng.");
    }

    // Tự động kiểm tra và đồng bộ tên Trigger khớp với Animator
    private void ValidateAndFixTriggerNames()
    {
        if (anim == null) return;

        HashSet<string> animParams = new HashSet<string>();
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Trigger)
            {
                animParams.Add(param.name);
            }
        }

        skill1TriggerName = CheckAndCorrectTrigger(skill1TriggerName, "Skill1", animParams, 1);
        skill2TriggerName = CheckAndCorrectTrigger(skill2TriggerName, "Skill2", animParams, 2);
        skill3TriggerName = CheckAndCorrectTrigger(skill3TriggerName, "Skill3", animParams, 3);
        skill4TriggerName = CheckAndCorrectTrigger(skill4TriggerName, "Skill4", animParams, 4);
    }

    private string CheckAndCorrectTrigger(string currentName, string expectedFallback, HashSet<string> animParams, int skillNum)
    {
        if (animParams.Contains(currentName))
        {
            return currentName;
        }

        if (animParams.Contains(expectedFallback))
        {
            Debug.LogWarning($"[HỖ TRỢ TỰ ĐỘNG] Phát hiện Trigger '{currentName}' cho Skill {skillNum} không tồn tại trong Animator của Boss. Đã tự động sửa thành '{expectedFallback}' cho khớp với Animator!");
            return expectedFallback;
        }

        string validTriggers = string.Join(", ", animParams);
        Debug.LogError($"[LỖI CẤU HÌNH] Animator của Boss không có Trigger '{currentName}' cho Skill {skillNum}.\n" +
                       $"👉 Hãy kiểm tra lại cột Parameters trong Animator. Danh sách Trigger hợp lệ hiện tại: [{validTriggers}]");
        return currentName;
    }
}
