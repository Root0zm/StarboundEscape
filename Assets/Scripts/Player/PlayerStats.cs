using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Base Stats")]
    public float baseMaxHealth = 100f; 
    public float baseMoveSpeed = 8f;
    public float baseJumpForce = 12f;
    public int baseMaxJumps = 1; 
    public float baseSwordDamage = 20f; 
    public float baseBulletDamage = 30f; 
    public float baseRegenRate = 1f; 

    // Các thuộc tính có nhân chỉ số từ cộng dồn vật phẩm (Inventory)
    public float MaxHealth => baseMaxHealth;
    
    // runway_drive (15% * S), stellar_flower (6.5% * S)
    public float MoveSpeed => baseMoveSpeed * (1f + 0.15f * GetItemStack("runway_drive") + 0.065f * GetItemStack("stellar_flower"));
    
    public float JumpForce => baseJumpForce;
    
    // flying_fantastica (+1 * S)
    public int MaxJumps => baseMaxJumps + GetItemStack("flying_fantastica");
    
    public float SwordDamage => baseSwordDamage;
    public float BulletDamage => baseBulletDamage;

    // Tốc độ đánh: little_nova (+12% * S), stellar_flower (+7% * S)
    public float AttackSpeedMultiplier => 1f + 0.12f * GetItemStack("little_nova") + 0.07f * GetItemStack("stellar_flower");

    // Lượng hồi máu mỗi giây: kiseki (+2 * S)
    public float RegenRate => baseRegenRate + 2f * GetItemStack("kiseki");

    public float CurrentHealth { get; private set; }
    private float regenTimer;

    [Header("Economy System")]
    public int gold = 50; // Biến gold công khai chứa số vàng hiện tại của Player

    public int Money => gold;

    private PlayerInventory inventory;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
    }

    public int GetItemStack(string id)
    {
        return inventory != null ? inventory.GetItemStack(id) : 0;
    }

    /// <summary>
    /// Hàm cập nhật chỉ số của Player. Vì các chỉ số được tính toán động,
    /// hàm này chủ yếu thực hiện logging và đồng bộ hóa trạng thái.
    /// </summary>
    public void UpdateStats()
    {
        Debug.Log($"[PlayerStats] Đã cập nhật chỉ số mới: MoveSpeed={MoveSpeed}, MaxJumps={MaxJumps}, AttackSpeed={AttackSpeedMultiplier}, Regen={RegenRate}");
    }

    public void AddMoney(int amount)
    {
        gold += amount;
        Debug.Log($"[Economy] Nhận được +{amount} vàng. Tiền hiện có: {gold}");

        if (amount > 0 && GameManager.Instance != null)
        {
            GameManager.Instance.totalGoldCollected += amount;
        }

        // Cập nhật giao diện tiền xu khi được cộng tiền
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdatePlayerGold(gold);
        }
    }

    public bool SpendMoney(int amount)
    {
        if (gold >= amount)
        {
            gold -= amount;
            Debug.Log($"[Economy] Tiêu tốn -{amount} vàng. Tiền hiện có: {gold}");

            // Cập nhật giao diện tiền xu khi bị trừ tiền
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdatePlayerGold(gold);
            }
            return true;
        }
        return false;
    }

    private void Start()
    {
        CurrentHealth = MaxHealth;

        // Cập nhật giao diện tiền xu khi bắt đầu game
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdatePlayerGold(gold);
        }
    }

    private void Update()
    {
        if (CurrentHealth < MaxHealth)
        {
            regenTimer += Time.deltaTime;
            if (regenTimer >= 1f)
            {
                // Sử dụng thuộc tính RegenRate có cộng dồn vật phẩm
                CurrentHealth = Mathf.Min(CurrentHealth + RegenRate, MaxHealth);
                regenTimer = 0f;
            }
        }
    }

    public void ChangeHp(float amount)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0f, MaxHealth);
        if (CurrentHealth <= 0f)
        {
            Debug.Log("Game Over!");
        }
    }

    // =================================================================
    // LOGIC TÍNH TOÁN SÁT THƯƠNG ĐÒN ĐÁNH & CHÍ MẠNG (DAMAGE & CRIT)
    // =================================================================

    /// <summary>
    /// Tính tỷ lệ chí mạng thực tế của Player
    /// </summary>
    public float GetCriticalChance()
    {
        float critChance = 0f;

        // pure_heart: Tăng tỷ lệ chí mạng thêm (5 + 2.5 * (S - 1))%
        int sPureHeart = GetItemStack("pure_heart");
        if (sPureHeart > 0)
        {
            critChance += 5f + 2.5f * (sPureHeart - 1);
        }

        // magic_sharp: Đòn đánh có tỷ lệ chí mạng tăng thêm 10% * S
        int sMagicSharp = GetItemStack("magic_sharp");
        if (sMagicSharp > 0)
        {
            critChance += 10f * sMagicSharp;
        }

        return critChance;
    }

    /// <summary>
    /// Tính toán sát thương đòn đánh sau khi cộng dồn các hiệu ứng của vật phẩm (Overload mặc định sử dụng baseSwordDamage)
    /// </summary>
    public (float damage, bool isCrit) CalculateAttackDamage(bool isEnemyFullHP, bool isBoss)
    {
        return CalculateAttackDamage(baseSwordDamage, isEnemyFullHP, isBoss);
    }

    /// <summary>
    /// Tính toán sát thương đòn đánh chi tiết với đầu vào rawDamage
    /// </summary>
    public (float damage, bool isCrit) CalculateAttackDamage(float rawDamage, bool isEnemyFullHP, bool isBoss)
    {
        bool isCrit = false;
        float finalDamage = rawDamage;

        // 1. Kiểm tra Chí mạng (Magic Sharp + Pure Heart)
        float critChance = GetCriticalChance();
        if (Random.Range(0f, 100f) < critChance)
        {
            isCrit = true;
            
            // Sát thương chí mạng mặc định x2 (magic_sharp nhân x2)
            float critMultiplier = 2f;

            // increase_beat: Nếu chí mạng, sát thương cộng thêm 100% * S
            int sIncreaseBeat = GetItemStack("increase_beat");
            critMultiplier += 1f * sIncreaseBeat;

            finalDamage *= critMultiplier;

            // pure_heart: Khi chí mạng, gọi hàm hồi 8 máu (+2 máu từ stack 2) cho Player
            int sPureHeart = GetItemStack("pure_heart");
            if (sPureHeart > 0)
            {
                PlayerController playerController = GetComponent<PlayerController>();
                if (playerController != null)
                {
                    int healAmount = 8 + 2 * (sPureHeart - 1);
                    playerController.ChangeHealth(healAmount);
                }
            }
        }

        // 2. ephemeral: Tăng sát thương thêm 20% * S
        int sEphemeral = GetItemStack("ephemeral");
        if (sEphemeral > 0)
        {
            finalDamage *= (1f + 0.20f * sEphemeral);
        }

        // 3. overflow: Nếu kẻ địch đầy máu, sát thương nhân thêm: 50% + 20% * (S - 1)
        int sOverflow = GetItemStack("overflow");
        if (sOverflow > 0 && isEnemyFullHP)
        {
            finalDamage *= (1.5f + 0.2f * (sOverflow - 1));
        }

        // 4. light_seeker: Nếu kẻ địch là Boss, sát thương nhân thêm: 40% * S
        int sLightSeeker = GetItemStack("light_seeker");
        if (sLightSeeker > 0 && isBoss)
        {
            finalDamage *= (1f + 0.4f * sLightSeeker);
        }

        // 5. beautiful_trick: Tăng sát thương tuyến tính dựa trên vàng (gold) của Player: Mathf.Min(40f, (gold / 700f) * 40f) %
        float goldBonusDamagePercent = Mathf.Min(40f, (gold / 700f) * 40f);
        finalDamage *= (1f + goldBonusDamagePercent / 100f);

        return (finalDamage, isCrit);
    }
}