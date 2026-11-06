using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    #region Member Variables & Fields

    [Header("References")]
    private Rigidbody2D rb;
    private PlayerStats stats;
    private Animator anim;

    [Header("Input Actions")]
    public InputAction moveAction;
    public InputAction jumpAction;
    public InputAction slashAction;
    public InputAction shootAction;
    public InputAction interactAction;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    private bool isGrounded;
    private int jumpsRemaining;

    [Header("Melee Attack")]
    public Transform attackPoint;
    public float attackRange = 1.5f;
    public LayerMask enemyLayer;
    [SerializeField] private float slashCooldown = 1f; // Cooldown đòn chém (mặc định 1 giây)
    private float slashCooldownTimer = 0f; // Bộ đếm cooldown đòn chém

    [Header("Ranged Attack ")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("Interaction System")]
    public float interactRadius = 2f;
    public LayerMask interactableLayer;

    [Header("Health System")]
    private float maxHealth = 100f;
    private float currentHealth;
    private float regenTimer;

    [Header("Climbing Settings ")]
    [SerializeField] private float climbSpeed = 4f;
    [SerializeField] private LayerMask climbableLayer;
    private bool isNearLadder;
    private bool isClimbing;
    private float originalGravity;

    [Header("Player Audio Config")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landSound;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip slashSound;
    [SerializeField] private AudioClip reloadStartSound;
    [SerializeField] private AudioClip reloadFinishSound;
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private float footstepCooldown = 0.35f;
    private float footstepTimer;
    private float nextHurtSoundTime = 0f;

    [Header("Roguelike Item Effects Variables")]
    private float shield = 0f;
    private float noDamageTimer = 3f; // Mặc định 3s để có giáp ảo ngay khi spawn
    private bool shieldApplied = false;
    private float groundSnowTimer = 0f;
    private float flusterEscapeCooldownTimer = 0f;
    private float immunityTimer = 0f;
    private bool waitingForDeathLanding = false;

    [Header("Ground Snow Visual Effect")]
    [SerializeField] private GameObject groundSnowEffectPrefab; // Prefab hiệu ứng tuyết rơi quanh chân Player
    private GameObject activeGroundSnowEffect; // Thực thể hiệu ứng đang chạy

    [Header("Fluster Escape Config & Effects")]
    [SerializeField] private float flusterEscapeBaseCooldown = 90f; // Cooldown cơ bản 90 giây (theo GDD mới)
    [SerializeField] private GameObject flusterEscapeEffectPrefab;  // Prefab hiệu ứng hình ảnh (VFX/Particles)
    [SerializeField] private AudioClip flusterEscapeSound;          // Âm thanh kích hoạt hiệu ứng (SFX)
    [SerializeField] private UnityEngine.Events.UnityEvent onFlusterEscapeTriggered; // Sự kiện liên kết Editor tùy biến

    // Các biến hỗ trợ lưu Input đọc từ moveAction
    private float moveInputX;
    private float moveInputY;

    [Header("Weapon Stats")]
    private int currentAmmo = 5;
    private int maxAmmo = 5;
    private float reloadDuration = 2f;
    private float reloadTimer;
    private bool isReloading;
    private float noUseDuration = 3f;
    private float noUseTimer;

    private bool isFacingRight = true;
    private bool slashDamageExecuted = false;

    public static Action<string, float> OnPlayerAttack;
    public float Health { get { return currentHealth; } }
    public float MaxHealth => maxHealth;
    public bool isDead => currentHealth <= 0f;

    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();
        anim = GetComponent<Animator>();

        InitializeDefaultInputs();
        InitializeDefaultLayersAndPoints();
    }

    private void InitializeDefaultInputs()
    {
        // 1. Move Action (Value)
        if (moveAction == null || moveAction.bindings.Count == 0)
        {
            if (moveAction == null) moveAction = new InputAction("Move", type: InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/s")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/a")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/d")
                .With("Right", "<Keyboard>/rightArrow");
        }

        // 2. Jump Action (Button)
        if (jumpAction == null || jumpAction.bindings.Count == 0)
        {
            if (jumpAction == null) jumpAction = new InputAction("Jump", type: InputActionType.Button);
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.AddBinding("<Keyboard>/w");
            jumpAction.AddBinding("<Keyboard>/upArrow");
        }

        // 3. Shoot Action (Button)
        if (shootAction == null || shootAction.bindings.Count == 0)
        {
            if (shootAction == null) shootAction = new InputAction("Shoot", type: InputActionType.Button);
            shootAction.AddBinding("<Mouse>/leftButton");
            shootAction.AddBinding("<Keyboard>/j");
            shootAction.AddBinding("<Keyboard>/e");
        }

        // 4. Slash Action (Button)
        if (slashAction == null || slashAction.bindings.Count == 0)
        {
            if (slashAction == null) slashAction = new InputAction("Slash", type: InputActionType.Button);
            slashAction.AddBinding("<Mouse>/rightButton");
            slashAction.AddBinding("<Keyboard>/k");
            slashAction.AddBinding("<Keyboard>/q");
        }

        // 5. Interact Action (Button)
        if (interactAction == null || interactAction.bindings.Count == 0)
        {
            if (interactAction == null) interactAction = new InputAction("Interact", type: InputActionType.Button);
            interactAction.AddBinding("<Keyboard>/f");
            interactAction.AddBinding("<Keyboard>/e");
        }
    }

    private void InitializeDefaultLayersAndPoints()
    {
        // Tự động gán LayerMask nếu để trống ngoài Inspector
        if (groundLayer.value == 0) groundLayer = LayerMask.GetMask("Ground");
        if (climbableLayer.value == 0)
        {
            int ladderLayerIndex = LayerMask.NameToLayer("Ladder");
            if (ladderLayerIndex != -1) climbableLayer = 1 << ladderLayerIndex;
            else climbableLayer = LayerMask.GetMask("Climbable");
        }
        if (enemyLayer.value == 0)
        {
            enemyLayer = LayerMask.GetMask("Enemy");
            int bossLayerIndex = LayerMask.NameToLayer("Boss");
            if (bossLayerIndex != -1) enemyLayer |= (1 << bossLayerIndex);
        }
        if (interactableLayer.value == 0) interactableLayer = LayerMask.GetMask("Interactable");

        // Tự động gán điểm kiểm tra va chạm / bắn súng nếu null ngoài Inspector
        if (attackPoint == null)
        {
            Transform found = transform.Find("AttackPoint");
            if (found != null) attackPoint = found;
            else
            {
                GameObject go = new GameObject("AttackPoint");
                go.transform.SetParent(transform);
                go.transform.localPosition = new Vector3(1.5f, 0f, 0f);
                attackPoint = go.transform;
            }
        }

        if (firePoint == null)
        {
            Transform found = transform.Find("FirePoint");
            if (found != null) firePoint = found;
            else
            {
                GameObject go = new GameObject("FirePoint");
                go.transform.SetParent(transform);
                go.transform.localPosition = new Vector3(1.5f, 0f, 0f);
                firePoint = go.transform;
            }
        }

        if (groundCheck == null)
        {
            Transform found = transform.Find("GroundCheck");
            if (found != null) groundCheck = found;
            else
            {
                GameObject go = new GameObject("GroundCheck");
                go.transform.SetParent(transform);
                go.transform.localPosition = new Vector3(0f, -1f, 0f);
                groundCheck = go.transform;
            }
        }
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        slashAction.Enable();
        shootAction.Enable();
        interactAction.Enable();

        jumpAction.performed += OnJumpPerformed;
        slashAction.performed += OnSlashPerformed;
        shootAction.performed += OnShootPerformed;
        interactAction.performed += OnInteractPerformed;
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        slashAction.Disable();
        shootAction.Disable();
        interactAction.Disable();

        if (activeGroundSnowEffect != null)
        {
            Destroy(activeGroundSnowEffect);
            activeGroundSnowEffect = null;
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
        isNearLadder = false;
        // Lưu lại trọng lực ban đầu của Phi hành gia ngoài Editor
        originalGravity = rb.gravityScale;

        // Tự động bổ sung Layer "Boss" và "Enemy" vào LayerMask enemyLayer để đảm bảo đòn chém trúng quái
        int bossLayerIndex = LayerMask.NameToLayer("Boss");
        if (bossLayerIndex != -1)
        {
            enemyLayer |= (1 << bossLayerIndex);
        }
        int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
        if (enemyLayerIndex != -1)
        {
            enemyLayer |= (1 << enemyLayerIndex);
        }

        // Cập nhật giao diện thanh máu khi bắt đầu game
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdatePlayerHealth(currentHealth, maxHealth);
        }
        UpdateAmmoUI();

        // Tự động đăng ký và khởi tạo Pool đạn để tối ưu hóa bộ nhớ (Memory Management)
        if (SimpleObjectPooler.Instance != null && projectilePrefab != null)
        {
            SimpleObjectPooler.Instance.CreatePool("PlayerBullet", projectilePrefab, 20);
        }
    }

    private void UpdateAmmoUI()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdatePlayerAmmo(currentAmmo, maxAmmo, isReloading);
        }
    }

    private void Update()
    {
        if (currentHealth <= 0)
        {
            if (waitingForDeathLanding && groundCheck != null)
            {
                // Triệt tiêu tốc độ ngang để rơi thẳng đứng xuống
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

                isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.3f, groundLayer);
                anim.SetBool("isGrounded", isGrounded);
                anim.SetFloat("VerticalVelocity", rb.linearVelocity.y);

                // Chạm đất thành công và không di chuyển lên trên nữa
                if (isGrounded && rb.linearVelocity.y <= 0)
                {
                    ExecuteDeath();
                }
            }
            return;
        }

       
        
        Vector2 inputVector = moveAction.ReadValue<Vector2>();
        moveInputX = inputVector.x;
        moveInputY = inputVector.y;

        
        if (isNearLadder && Mathf.Abs(moveInputY) > 0.1f)
        {
            isClimbing = true;
        }

        
        if (!isNearLadder)
        {
            isClimbing = false;
        }

        // --- GROUND CHECK & HOẠT ẢNH MẶT ĐẤT ---
        if (groundCheck != null)
        {
            bool wasGrounded = isGrounded;
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.3f, groundLayer);
            if (!isClimbing)
            {
                anim.SetBool("isGrounded", isGrounded);
            }

            // Phát tiếng đáp đất
            if (isGrounded && !wasGrounded && rb.linearVelocity.y <= 0)
            {
                if (AudioManager.Instance != null && landSound != null)
                {
                    AudioManager.Instance.PlaySFX2D(landSound);
                }
            }
        }

        if (!isClimbing)
        {
            anim.SetFloat("VerticalVelocity", rb.linearVelocity.y);
            if (isGrounded && rb.linearVelocity.y <= 0)
            {
                jumpsRemaining = stats.MaxJumps;
            }
        }

        HandleReloadLogic();
        HandleHealthRegen();
        HandleCsharpFootsteps();
        HandleFairyQueenShield();
        HandleGroundSnow();
        HandleFlusterEscape();
        CheckForInteractables();

        // Cập nhật bộ đếm cooldown đòn chém
        if (slashCooldownTimer > 0f)
        {
            slashCooldownTimer -= Time.deltaTime;
        }

        // Cập nhật tốc độ hoạt ảnh tương thích với Tốc độ đánh
        if (anim != null)
        {
            anim.speed = stats.AttackSpeedMultiplier;
        }
    }

    private void FixedUpdate()
    {
        if (currentHealth <= 0) return;

        if (isClimbing)
        {
            
            rb.gravityScale = 0f;

      
            rb.linearVelocity = new Vector2(moveInputX * stats.MoveSpeed * 0.5f, moveInputY * climbSpeed);

            Collider2D[] groundColliders = Physics2D.OverlapCircleAll(transform.position, 2f, groundLayer);
            Collider2D playerCollider = GetComponent<Collider2D>();

            if (playerCollider != null)
            {
                foreach (Collider2D groundInWay in groundColliders)
                {
                    
                    Physics2D.IgnoreCollision(playerCollider, groundInWay, true);
                }
            }

            if (anim != null)
            {
                anim.SetBool("isClimbing", true);

                
                if (Mathf.Abs(moveInputY) > 0.1f)
                {
                    anim.SetFloat("climbSpeedMultiplier", 1f);
                }
                else
                {
                    
                    anim.SetFloat("climbSpeedMultiplier", 0f);
                }
            }
        }
        else
        {
          
            rb.gravityScale = originalGravity;

            rb.linearVelocity = new Vector2(moveInputX * stats.MoveSpeed, rb.linearVelocity.y);

            Collider2D[] groundColliders = Physics2D.OverlapCircleAll(transform.position, 2f, groundLayer);
            Collider2D playerCollider = GetComponent<Collider2D>();
            if (playerCollider != null)
            {
                foreach (Collider2D groundInWay in groundColliders)
                {
                    Physics2D.IgnoreCollision(playerCollider, groundInWay, false); 
                }
            }

            if (anim != null)
            {
                anim.SetBool("isClimbing", false);
                anim.SetFloat("Speed", Mathf.Abs(moveInputX));
                if (Mathf.Abs(moveInputX) < 0.01f)
                {
                    anim.SetFloat("Speed", 0f);
                }
            }

            // Xoay mặt nhân vật
            if (moveInputX > 0 && !isFacingRight) Flip();
            else if (moveInputX < 0 && isFacingRight) Flip();
        }
    }

    #endregion

    #region Player Input Actions & Combat Callbacks

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (Time.timeScale == 0f) return; // Không thực hiện khi game đang Pause
        if (currentHealth <= 0 || isClimbing) return; // Không cho nhảy khi đang leo dây  
        
        if (isClimbing)
        {
            isClimbing = false;
            rb.gravityScale = originalGravity;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, stats.JumpForce * 0.8f);
            return;
        }

        // Nhảy bình thường dưới đất hoặc double jump
        if (jumpsRemaining > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, stats.JumpForce);
            jumpsRemaining--;
            anim.SetBool("isGrounded", false);

            if (AudioManager.Instance != null && jumpSound != null)
            {
                AudioManager.Instance.PlaySFX2D(jumpSound);
            }
        }
    }

    private void OnSlashPerformed(InputAction.CallbackContext context)
    {
        if (Time.timeScale == 0f) return; // Không thực hiện khi game đang Pause
        if (currentHealth <= 0 || isClimbing) return; // Không cho chém khi đang leo dây

        // Kiểm tra cooldown đòn chém
        if (slashCooldownTimer > 0f) return;

        // Thiết lập lại bộ đếm cooldown đòn chém
        if (stats != null)
        {
            slashCooldownTimer = slashCooldown / stats.AttackSpeedMultiplier;
        }
        else
        {
            slashCooldownTimer = slashCooldown;
        }

        slashDamageExecuted = false;
        anim.SetTrigger("Slash");

        // Phát âm thanh tiếng chém (vẫn phát ở đây để tạo phản hồi nút bấm lập tức)
        if (AudioManager.Instance != null && slashSound != null)
        {
            AudioManager.Instance.PlaySFX2D(slashSound, randomPitch: true);
        }
    }

    /// <summary>
    /// Hàm thực thi sát thương đòn chém hoặc shotgun (Được gọi thông qua Animation Event tại frame chém trúng thực tế hoặc coroutine dự phòng)
    /// </summary>
    public void ExecuteSlashDamage()
    {
        if (currentHealth <= 0 || isClimbing) return;
        if (slashDamageExecuted) return;
        slashDamageExecuted = true;

        float actualRange = attackRange;
        bool isShotgun = false;

        if (attackPoint != null)
        {
            string nameLower = attackPoint.name.ToLower();
            if (nameLower.Contains("shotgun") || nameLower.Contains("shotgurn") || nameLower.Contains("gun"))
            {
                isShotgun = true;
                // Nếu phát hiện shotgun, tự động đảm bảo phạm vi quét tối thiểu là 3.0f để phù hợp với đạn shotgun tỏa ra
                if (actualRange < 3.0f)
                {
                    actualRange = 3.0f;
                }
            }
        }

        Vector3 searchPos = attackPoint != null ? attackPoint.position : transform.position;

        Debug.Log($"[Shotgun/Slash DMG] Thực hiện gây sát thương! Vũ khí: {(isShotgun ? "Shotgun" : "Cận chiến")}, Vị trí quét: {searchPos}, Bán kính: {actualRange}, Layer lọc: {enemyLayer.value}");

        // Quét tìm tất cả các collider trong phạm vi dựa trên layer mask của quái
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(searchPos, actualRange, enemyLayer);

        if (hitEnemies.Length == 0)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(enemyLayer);
            filter.useTriggers = true;
            System.Collections.Generic.List<Collider2D> results = new System.Collections.Generic.List<Collider2D>();
            Physics2D.OverlapCircle(searchPos, actualRange, filter, results);
            hitEnemies = results.ToArray();
        }

        // --- CHẨN ĐOÁN LỖI (DIAGNOSTIC LOG): Quét rộng hơn 3.0m và KHÔNG lọc layer để ghi logs giải thích lý do không trúng quái ---
        Collider2D[] allNearby = Physics2D.OverlapCircleAll(searchPos, actualRange + 3.0f);
        if (allNearby.Length > 0)
        {
            foreach (var col in allNearby)
            {
                if (col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;

                string layerName = LayerMask.LayerToName(col.gameObject.layer);
                float dist = Vector2.Distance(searchPos, col.transform.position);

                // Quét các script quái vật có thể có trên đối tượng này
                var ec = col.GetComponentInParent<EnemyController>();
                if (ec == null) ec = col.GetComponent<EnemyController>();

                var lg = col.GetComponentInParent<Golem_Map3>();
                if (lg == null) lg = col.GetComponent<Golem_Map3>();

                var e3 = col.GetComponentInParent<EnemyControllerMap3>();
                if (e3 == null) e3 = col.GetComponent<EnemyControllerMap3>();

                bool hasEnemyScript = (ec != null || lg != null || e3 != null);
                bool inRange = dist <= actualRange;
                bool inLayerMask = ((1 << col.gameObject.layer) & enemyLayer.value) != 0;

                Debug.Log($"[Shotgun/Slash Diagnostic] Thấy vật thể gần: '{col.name}' | Layer: '{layerName}' (Index: {col.gameObject.layer}) | Hợp lệ LayerMask: {inLayerMask} | Trong tầm bắn: {inRange} (Cự ly: {dist:F2}m) | Có script quái vật: {hasEnemyScript}");
            }
        }

        System.Collections.Generic.HashSet<object> damagedObjects = new System.Collections.Generic.HashSet<object>();
        float baselineDamage = stats.CalculateAttackDamage(stats.SwordDamage, false, false).damage;
        int damageCount = 0;

        foreach (Collider2D enemyCollider in hitEnemies)
        {
            // 1. Quái vật kế thừa từ EnemyController chuẩn
            EnemyController enemy = enemyCollider.GetComponentInParent<EnemyController>();
            if (enemy == null)
            {
                enemy = enemyCollider.GetComponent<EnemyController>();
            }

            if (enemy != null)
            {
                if (!damagedObjects.Contains(enemy))
                {
                    damagedObjects.Add(enemy);
                    var (finalDamage, isCrit) = stats.CalculateAttackDamage(stats.SwordDamage, enemy.IsFullHP(), enemy.IsBoss());
                    enemy.TakeDamage(finalDamage);
                    damageCount++;

                    Debug.Log($"[Shotgun/Slash DMG] Đã bắn trúng quái chuẩn '{enemyCollider.name}' gây {finalDamage} sát thương!");

                    // Kích hoạt Hiệu ứng Đòn đánh (On Hit Triggers)
                    PlayerInventory inv = GetComponent<PlayerInventory>();
                    bool hasAbyssNova = (inv != null && inv.GetItemStack("abyss_nova") > 0);

                    // Sinh hạt hiệu ứng trúng đòn (Hit Particle)
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.SpawnHitParticle(enemy.transform.position, hasAbyssNova);
                    }

                    if (inv != null)
                    {
                        inv.TriggerOnHitEffects(enemy, stats.SwordDamage);
                    }
                }
                continue;
            }

            // 2. Quái vật dùng Golem_Map3 cũ (nếu người chơi chưa đổi script trên Prefab Colossus)
            Golem_Map3 legacyGolem = enemyCollider.GetComponentInParent<Golem_Map3>();
            if (legacyGolem == null)
            {
                legacyGolem = enemyCollider.GetComponent<Golem_Map3>();
            }

            if (legacyGolem != null)
            {
                if (!damagedObjects.Contains(legacyGolem))
                {
                    damagedObjects.Add(legacyGolem);
                    var (finalDamage, _) = stats.CalculateAttackDamage(stats.SwordDamage, false, legacyGolem.isBoss_Map3);
                    legacyGolem.TakeDamage(Mathf.RoundToInt(finalDamage));
                    damageCount++;

                    Debug.Log($"[Shotgun/Slash DMG] Đã bắn trúng Boss Golem cũ '{enemyCollider.name}' gây {finalDamage} sát thương!");

                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.SpawnHitParticle(legacyGolem.transform.position, false);
                    }
                }
                continue;
            }

            // 3. Quái vật dùng EnemyControllerMap3 cũ (Robot hỏng)
            EnemyControllerMap3 legacyEnemy3 = enemyCollider.GetComponentInParent<EnemyControllerMap3>();
            if (legacyEnemy3 == null)
            {
                legacyEnemy3 = enemyCollider.GetComponent<EnemyControllerMap3>();
            }

            if (legacyEnemy3 != null)
            {
                if (!damagedObjects.Contains(legacyEnemy3))
                {
                    damagedObjects.Add(legacyEnemy3);
                    legacyEnemy3.Fix();
                    damageCount++;

                    Debug.Log($"[Shotgun/Slash DMG] Đã sửa/sát thương quái cũ Robot '{enemyCollider.name}'!");
                }
                continue;
            }
        }

        if (damageCount == 0)
        {
            Debug.LogWarning("[Shotgun/Slash DMG] Bắn trượt hoặc không thể gây sát thương cho quái vật nào! Vui lòng đọc danh sách vật thể gần đó ở Console log để biết nguyên nhân.");
        }

        OnPlayerAttack?.Invoke("Sword", baselineDamage);
        noUseTimer = 0f;
    }

    private void OnShootPerformed(InputAction.CallbackContext context)
    {
        if (Time.timeScale == 0f) return; // Không thực hiện khi game đang Pause
        if (currentAmmo <= 0 || isReloading || currentHealth <= 0 || isClimbing) return; // Không cho bắn khi đang leo dây

        currentAmmo--;
        noUseTimer = 0f;
        UpdateAmmoUI();

        anim.SetTrigger("Shoot");

        if (AudioManager.Instance != null && shootSound != null)
        {
            AudioManager.Instance.PlaySFX2D(shootSound, randomPitch: true);
        }

        // Gửi đạn đi với sát thương cơ bản (raw damage), sát thương chi tiết sẽ được tính toán lúc va chạm dựa trên thuộc tính của kẻ địch.
        float rawBulletDamage = stats.BulletDamage;

        Vector2 shootDirection = firePoint.right;
        
        // Tái sử dụng đạn từ Pool thay vì tạo mới để quản lý bộ nhớ RAM/CPU tối ưu
        GameObject projectile = SimpleObjectPooler.Instance.GetPooledObject("PlayerBullet");
        if (projectile != null)
        {
            projectile.transform.position = firePoint.position;
            projectile.transform.rotation = firePoint.rotation;
            projectile.SetActive(true);

            Projectile projScript = projectile.GetComponent<Projectile>();
            if (projScript != null)
            {
                projScript.Setup(rawBulletDamage, shootDirection);
            }
        }

        float baselineDamage = stats.CalculateAttackDamage(rawBulletDamage, false, false).damage;
        OnPlayerAttack?.Invoke("Gun", baselineDamage);

        if (currentAmmo == 0)
            StartReload();
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (Time.timeScale == 0f) return; // Không thực hiện khi game đang Pause
        if (currentHealth <= 0) return;
        PerformInteraction();
    }

    #endregion

    #region Health, Reload & Interaction Logic

    // Hàm thiết lập máu trực tiếp (dùng để khôi phục máu khi qua màn chơi mới)
    public void SetCurrentHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0f, maxHealth);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdatePlayerHealth(currentHealth, maxHealth);
        }
    }

    public void ChangeHealth(int amount)
    {
        if (currentHealth <= 0) return;

        // Nếu nhận sát thương (amount < 0)
        if (amount < 0)
        {
            // Kiểm tra miễn nhiễm sát thương (fluster_escape)
            if (immunityTimer > 0f)
            {
                Debug.Log("[Player Physics] Sát thương bị phớt lờ do đang trong trạng thái miễn nhiễm!");
                return;
            }

            int damageTaken = -amount;

            // resistance: Giảm sát thương nhận vào bằng 2 * S (không nhỏ hơn 1)
            int sResistance = stats.GetItemStack("resistance");
            if (sResistance > 0)
            {
                damageTaken = Mathf.Max(1, damageTaken - 2 * sResistance);
            }

            // fairy_queen: Giáp ảo hấp thụ sát thương trước
            if (shield > 0f)
            {
                if (shield >= damageTaken)
                {
                    shield -= damageTaken;
                    damageTaken = 0;
                    Debug.Log($"[Player Stats] Giáp ảo hấp thụ toàn bộ sát thương! Giáp còn lại: {shield}");
                }
                else
                {
                    damageTaken -= (int)shield;
                    Debug.Log($"[Player Stats] Giáp ảo bị phá hủy! HP chịu {damageTaken} sát thương dư thừa.");
                    shield = 0f;
                }
            }

            amount = -damageTaken;

            // Nhận sát thương -> reset bộ đếm thời gian của Fairy Queen
            if (amount < 0)
            {
                noDamageTimer = 0f;
                shieldApplied = false;

                if (AudioManager.Instance != null && hurtSound != null && Time.time >= nextHurtSoundTime)
                {
                    AudioManager.Instance.PlaySFX2D(hurtSound, randomPitch: true);
                    nextHurtSoundTime = Time.time + 0.35f; // Giới hạn tối thiểu phát cách nhau 0.35 giây
                }
            }
        }

        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        Debug.Log($"HP: {currentHealth}/{maxHealth}");

        // Cập nhật giao diện thanh máu ngay sau khi máu thay đổi
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdatePlayerHealth(currentHealth, maxHealth);
        }

        // ephemeral: Máu dưới 25% phá hủy toàn bộ cộng dồn của item này
        if (currentHealth > 0 && currentHealth < maxHealth * 0.25f)
        {
            PlayerInventory inv = GetComponent<PlayerInventory>();
            if (inv != null)
            {
                int sEphemeral = inv.GetItemStack("ephemeral");
                if (sEphemeral > 0)
                {
                    inv.ResetItemStack("ephemeral");
                    Debug.Log("[Item Effect] HP dưới 25%! Toàn bộ cộng dồn của 'ephemeral' đã bị hủy!");
                }
            }
        }

        if (currentHealth <= 0)
        {
            // rebirth_story: Tiêu hao 1 stack để hồi sinh ngay lập tức
            int sRebirth = stats.GetItemStack("rebirth_story");
            if (sRebirth > 0)
            {
                PlayerInventory inv = GetComponent<PlayerInventory>();
                if (inv != null)
                {
                    inv.RemoveItemStackOne("rebirth_story");
                    currentHealth = maxHealth;
                    Debug.Log("[Rebirth Story] Player đã hồi sinh hoàn toàn! Mất 1 stack.");

                    // Cập nhật giao diện thanh máu sau khi hồi sinh
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.UpdatePlayerHealth(currentHealth, maxHealth);
                    }
                    return; // Tránh cái chết
                }
            }

            PlayerDeath();
        }
    }

    private void HandleHealthRegen()
    {
        if (currentHealth <= 0 || currentHealth >= maxHealth) return;

        regenTimer += Time.deltaTime;
        if (regenTimer >= 1f)
        {
            currentHealth = Mathf.Clamp(currentHealth + stats.RegenRate, 0f, maxHealth);
            regenTimer = 0f;

            // Cập nhật giao diện thanh máu khi hồi phục máu theo thời gian
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdatePlayerHealth(currentHealth, maxHealth);
            }
        }
    }

    private void CheckForInteractables()
    {
        if (currentHealth <= 0) return;

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, interactRadius, interactableLayer);
        IInteractable foundInteractable = null;

        foreach (Collider2D hitCollider in hitColliders)
        {
            IInteractable interactable = hitCollider.GetComponent<IInteractable>();
            if (interactable == null)
            {
                interactable = hitCollider.GetComponentInParent<IInteractable>();
            }

            if (interactable != null)
            {
                foundInteractable = interactable;
                break; // Tìm thấy đối tượng đầu tiên có thể tương tác
            }
        }

        if (foundInteractable != null)
        {
            string prompt = foundInteractable.GetInteractPrompt();
            if (!string.IsNullOrEmpty(prompt))
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowInteractPrompt(prompt);
                }
            }
            else
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.HideInteractPrompt();
                }
            }
        }
        else
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.HideInteractPrompt();
            }
        }
    }

    private void PerformInteraction()
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, interactRadius, interactableLayer);
        foreach (Collider2D hitCollider in hitColliders)
        {
            IInteractable interactable = hitCollider.GetComponent<IInteractable>();
            if (interactable == null)
            {
                interactable = hitCollider.GetComponentInParent<IInteractable>();
            }

            if (interactable != null)
            {
                interactable.Interact();
                break; // Chỉ tương tác với một đối tượng tìm thấy đầu tiên
            }
        }
    }

    private void HandleReloadLogic()
    {
        if (isReloading)
        {
            reloadTimer += Time.deltaTime;
            if (reloadTimer >= reloadDuration)
            {
                currentAmmo = maxAmmo;
                isReloading = false;
                Debug.Log("Nạp đạn hoàn tất!");
                UpdateAmmoUI();

                if (AudioManager.Instance != null && reloadFinishSound != null)
                {
                    AudioManager.Instance.PlaySFX2D(reloadFinishSound);
                }
            }
            return;
        }

        if (currentAmmo < maxAmmo)
        {
            noUseTimer += Time.deltaTime;
            if (noUseTimer >= noUseDuration)
            {
                StartReload();
            }
        }
    }

    private void StartReload()
    {
        isReloading = true;
        reloadTimer = 0f;
        Debug.Log("Đang nạp đạn...");
        UpdateAmmoUI();

        if (AudioManager.Instance != null && reloadStartSound != null)
        {
            AudioManager.Instance.PlaySFX2D(reloadStartSound);
        }
    }

    #endregion

    #region Player Death & Defeat Logic

    private void PlayerDeath()
    {
        // Phát âm thanh khi chết của người chơi
        if (AudioManager.Instance != null && deathSound != null)
        {
            AudioManager.Instance.PlaySFX2D(deathSound);
        }

        // 1. Vô hiệu hóa phím bấm điều khiển ngay lập tức
        moveAction.Disable();
        jumpAction.Disable();
        slashAction.Disable();
        shootAction.Disable();
        interactAction.Disable();

        // Triệt tiêu tốc độ ngang X để rơi thẳng đứng xuống
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.playerDeathCount++;
        }

        // 2. Kiểm tra nếu đang ở mặt đất thì chết ngay
        if (isGrounded)
        {
            ExecuteDeath();
        }
        else
        {
            waitingForDeathLanding = true;
            Debug.Log("Phi hành gia chết trên không! Đang rơi tự do xuống đất...");
        }
    }

    private void ExecuteDeath()
    {
        waitingForDeathLanding = false;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic; 
        GetComponent<Collider2D>().enabled = false;

        anim.SetTrigger("Die");
        Debug.Log("Phi hành gia đã hy sinh! GAME OVER.");

        // Ẩn gợi ý tương tác khi chết
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideInteractPrompt();
        }

        // Tắt hiệu ứng Tuyết rơi khi chết
        if (activeGroundSnowEffect != null)
        {
            Destroy(activeGroundSnowEffect);
            activeGroundSnowEffect = null;
        }

        StartCoroutine(ShowLoseScreenRoutine());
    }

    private System.Collections.IEnumerator ShowLoseScreenRoutine()
    {
        yield return new WaitForSeconds(2.0f); // Chờ 2 giây để chạy xong hiệu ứng hoạt ảnh chết
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowLoseScreen();
        }
    }

    #endregion

    #region Physics, Movement & Trigger Events

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.Rotate(0f, 180f, 0f);
    }

    // --- VA CHẠM CẢM ỨNG VÙNG DÂY LEO ---
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & climbableLayer) != 0)
        {
            isNearLadder = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & climbableLayer) != 0)
        {
            isNearLadder = false;
           
            isClimbing = false;
        }
    }

    #endregion

    #region Roguelike Item Effects

    private void HandleFairyQueenShield()
    {
        int sFairyQueen = stats.GetItemStack("fairy_queen");
        if (sFairyQueen <= 0)
        {
            shield = 0f;
            return;
        }

        noDamageTimer += Time.deltaTime;
        if (noDamageTimer >= 3f && !shieldApplied)
        {
            float maxShield = (0.08f * sFairyQueen) * maxHealth;
            shield = maxShield;
            shieldApplied = true;
            Debug.Log($"[Item Effect] Kích hoạt Giáp ảo Fairy Queen: +{shield} Giáp!");
        }
    }

    private void HandleGroundSnow()
    {
        int sGroundSnow = stats.GetItemStack("ground_snow");

        // 1. Quản lý hiển thị hiệu ứng hình ảnh (Visual Effect) đi theo người chơi
        if (sGroundSnow > 0)
        {
            if (activeGroundSnowEffect == null && groundSnowEffectPrefab != null)
            {
                // Sinh ra hiệu ứng ở World Space (không làm con của Player) để tránh bị méo/dẹt hình khi Player quay mặt (Flip) hoặc chạy hoạt ảnh
                activeGroundSnowEffect = Instantiate(groundSnowEffectPrefab, transform.position, Quaternion.identity);
            }

            // Cập nhật vị trí của hiệu ứng luôn bám sát theo Player mỗi khung hình
            if (activeGroundSnowEffect != null)
            {
                activeGroundSnowEffect.transform.position = transform.position;
            }
        }
        else
        {
            if (activeGroundSnowEffect != null)
            {
                Destroy(activeGroundSnowEffect);
                activeGroundSnowEffect = null;
            }
            return; // Nếu không có item thì dừng xử lý sát thương
        }

        // 2. Xử lý gây sát thương xung quanh theo thời gian (mỗi 2 giây)
        groundSnowTimer += Time.deltaTime;
        if (groundSnowTimer >= 2f)
        {
            groundSnowTimer = 0f;
            float playerHeight = 1.8f;
            Collider2D playerCol = GetComponent<Collider2D>();
            if (playerCol != null) playerHeight = playerCol.bounds.size.y;

            float radius = 2f * playerHeight;
            // Sát thương bằng 60% sát thương của súng (đạn) nhân với số lượng stack
            float damage = (0.6f * sGroundSnow) * stats.BulletDamage;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, enemyLayer);
            foreach (Collider2D hit in hits)
            {
                EnemyController enemy = hit.GetComponent<EnemyController>();
                if (enemy == null) enemy = hit.GetComponentInParent<EnemyController>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                    Debug.Log($"[Ground Snow] Gây {damage} sát thương lên {hit.gameObject.name} trong bán kính quét.");
                }
            }
        }
    }

    private void HandleFlusterEscape()
    {
        if (immunityTimer > 0f)
        {
            immunityTimer -= Time.deltaTime;
        }

        if (flusterEscapeCooldownTimer > 0f)
        {
            flusterEscapeCooldownTimer -= Time.deltaTime;
        }

        int sFluster = stats.GetItemStack("fluster_escape");
        if (sFluster > 0 && currentHealth > 0 && currentHealth < maxHealth * 0.25f && flusterEscapeCooldownTimer <= 0f)
        {
            float missingHP = maxHealth - currentHealth;
            float explosionDamage = 7.0f * missingHP;
            
            immunityTimer = 4f; // Miễn nhiễm 4s
            
            // Cooldown theo GDD: base / (2 + n) với n là số stack (sFluster) (base mặc định 90s)
            flusterEscapeCooldownTimer = flusterEscapeBaseCooldown / (2f + sFluster);
            
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 3f, enemyLayer);
            foreach (Collider2D hit in hits)
            {
                EnemyController enemy = hit.GetComponent<EnemyController>();
                if (enemy == null) enemy = hit.GetComponentInParent<EnemyController>();
                if (enemy != null)
                {
                    enemy.TakeDamage(explosionDamage);
                }
            }

            // Kích hoạt các hiệu ứng hình ảnh, âm thanh và sự kiện UnityEvent
            TriggerFlusterEscapeEffects();

            Debug.Log($"[Fluster Escape] HP < 25%! Kích hoạt miễn nhiễm 4s và kích nổ gây {explosionDamage} sát thương diện rộng. Cooldown tiếp theo: {flusterEscapeCooldownTimer:F1}s");
        }
    }

    private void TriggerFlusterEscapeEffects()
    {
        // 1. Sinh hiệu ứng hạt (Prefab Particle System / Visual Effect) tại vị trí người chơi
        if (flusterEscapeEffectPrefab != null)
        {
            Instantiate(flusterEscapeEffectPrefab, transform.position, Quaternion.identity);
        }

        // 2. Phát âm thanh kích hoạt hiệu ứng
        if (flusterEscapeSound != null)
        {
            AudioSource.PlayClipAtPoint(flusterEscapeSound, transform.position);
        }

        // 3. Kích hoạt sự kiện UnityEvent (cho phép gán thêm rung màn hình, lóe màn hình từ Editor)
        if (onFlusterEscapeTriggered != null)
        {
            onFlusterEscapeTriggered.Invoke();
        }
    }

    #endregion

    #region Debug Gizmos

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
        if (groundCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(groundCheck.position, 0.3f);
        }
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }

    #endregion

    #region Audio & Footsteps

    private void HandleCsharpFootsteps()
    {
        if (isGrounded && Mathf.Abs(moveInputX) > 0.1f && !isClimbing && currentHealth > 0)
        {
            footstepTimer += Time.deltaTime;
            if (footstepTimer >= footstepCooldown)
            {
                footstepTimer = 0f;
                PlayFootstepSound();
            }
        }
        else
        {
            footstepTimer = footstepCooldown;
        }
    }

    public void PlayFootstepSound()
    {
        if (AudioManager.Instance != null && footstepSounds != null && footstepSounds.Length > 0 && currentHealth > 0)
        {
            AudioClip clip = footstepSounds[UnityEngine.Random.Range(0, footstepSounds.Length)];
            AudioManager.Instance.PlaySFX3DThrottled("Player_Footstep", clip, transform.position, 0.15f, volumeScale: 0.8f, randomPitch: true);
        }
    }

    #endregion
}