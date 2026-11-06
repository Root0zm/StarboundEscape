using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class PlayerInventory : MonoBehaviour
{
    // Dictionary lưu trữ số lượng vật phẩm cộng dồn theo ID
    private Dictionary<string, int> ownedItems = new Dictionary<string, int>();

    [Header("Item Database")]
    [SerializeField] private List<ItemData> allItemsDatabase = new List<ItemData>();
    private Dictionary<string, ItemData> itemDatabaseDict = new Dictionary<string, ItemData>();

    [Header("Item Prefabs (Optional)")]
    [SerializeField] private GameObject flamePillarPrefab;  // Rising Nebula
    [SerializeField] private GameObject starPiecePrefab;    // Star Piece ngôi sao đuổi
    [SerializeField] private GameObject lightningPrefab;    // Lightning Drop tia sét

    [Header("Audio Config")]
    [SerializeField] private AudioClip defaultPickupSound;
    [SerializeField] private AudioClip lightningStrikeSound;
    [SerializeField] private AudioClip risingNebulaSound;

    // Thời gian chờ để kích hoạt Cột lửa tiếp theo (cooldown 6s)
    private float nextRisingNebulaTime = 0f;

    [HideInInspector] public bool isRestoring = false;

    private void Awake()
    {
        InitializeDatabase();
    }

    private void Start()
    {
        // 1. Phục hồi túi đồ trực tiếp từ GameManager.Instance.savedData trong bộ nhớ
        if (GameManager.Instance != null && GameManager.Instance.savedData.hasSavedData)
        {
            SavedPlayerData data = GameManager.Instance.savedData;
            if (data.items != null)
            {
                isRestoring = true;
                ownedItems.Clear();

                foreach (var savedItem in data.items)
                {
                    for (int i = 0; i < savedItem.count; i++)
                    {
                        AddItemDirectly(savedItem.id);
                    }
                }
                isRestoring = false;
                Debug.Log($"[PlayerInventory] Phục hồi thành công {data.items.Count} loại items từ GameManager.");
            }

            // Đặt lại cờ để tránh việc nạp lại trùng lặp sau này
            data.hasSavedData = false;
        }

        // 2. Cập nhật giao diện UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateInventoryUI(this);
        }
    }

    private void InitializeDatabase()
    {
        itemDatabaseDict.Clear();
        foreach (var item in allItemsDatabase)
        {
            if (item != null && !string.IsNullOrEmpty(item.itemID))
            {
                itemDatabaseDict[item.itemID] = item;
            }
        }
    }

    public Dictionary<string, int> GetOwnedItems()
    {
        return ownedItems;
    }

    public ItemData GetItemData(string id)
    {
        if (itemDatabaseDict.TryGetValue(id, out var data))
        {
            return data;
        }
        return null;
    }

    /// <summary>
    /// Lấy số lượng cộng dồn của một món đồ từ xa
    /// </summary>
    public int GetItemStack(string id)
    {
        return ownedItems.ContainsKey(id) ? ownedItems[id] : 0;
    }

    /// <summary>
    /// Thêm vật phẩm khi nhặt được đồ
    /// </summary>
    public void AddItem(ItemData item)
    {
        if (item == null || string.IsNullOrEmpty(item.itemID)) return;

        if (!itemDatabaseDict.ContainsKey(item.itemID))
        {
            itemDatabaseDict[item.itemID] = item;
        }

        if (!ownedItems.ContainsKey(item.itemID))
        {
            ownedItems.Add(item.itemID, 1);
        }
        else
        {
            ownedItems[item.itemID]++;
        }


        if (!isRestoring && GameManager.Instance != null)
        {
            GameManager.Instance.totalItemsCollected++;
        }

        // Gọi UIManager hiển thị thông báo nhặt được vật phẩm
        if (!isRestoring && UIManager.Instance != null)
        {
            UIManager.Instance.ShowPickupNotification(item.itemIcon, item.itemName, item.description);
        }

        // Phát âm thanh nhặt đồ
        if (!isRestoring && AudioManager.Instance != null)
        {
            AudioClip clipToPlay = item.pickupSound != null ? item.pickupSound : defaultPickupSound;
            AudioManager.Instance.PlaySFX2D(clipToPlay);
        }

        ApplyItemEffects(item.itemID);
    }

    /// <summary>
    /// Hàm hỗ trợ thêm trực tiếp vật phẩm bằng ID (Ví dụ: parallel_cross rơi từ quái)
    /// </summary>
    public void AddItemDirectly(string id)
    {
        if (string.IsNullOrEmpty(id)) return;

        if (!ownedItems.ContainsKey(id))
        {
            ownedItems.Add(id, 1);
        }
        else
        {
            ownedItems[id]++;
        }


        if (!isRestoring && GameManager.Instance != null)
        {
            GameManager.Instance.totalItemsCollected++;
        }

        // Tìm kiếm thông tin ItemData trong cơ sở dữ liệu để hiển thị thông báo UI và phát âm thanh
        ItemData itemData = GetItemData(id);
        if (itemData != null)
        {
            // Gọi UIManager hiển thị thông báo nhặt được vật phẩm
            if (!isRestoring && UIManager.Instance != null)
            {
                UIManager.Instance.ShowPickupNotification(itemData.itemIcon, itemData.itemName, itemData.description);
            }

            // Phát âm thanh nhặt đồ
            if (!isRestoring && AudioManager.Instance != null)
            {
                AudioClip clipToPlay = itemData.pickupSound != null ? itemData.pickupSound : defaultPickupSound;
                AudioManager.Instance.PlaySFX2D(clipToPlay);
            }
        }

        ApplyItemEffects(id);
    }

    /// <summary>
    /// Giảm 1 stack của món đồ trong túi (Ví dụ: Rebirth Story hồi sinh)
    /// </summary>
    public void RemoveItemStackOne(string id)
    {
        if (ownedItems.ContainsKey(id))
        {
            ownedItems[id]--;
            if (ownedItems[id] <= 0)
            {
                ownedItems.Remove(id);
            }
            ApplyItemEffects(id);
        }
    }

    /// <summary>
    /// Reset toàn bộ cộng dồn của một món đồ về 0 (Ví dụ: Ephemeral khi HP dưới 25%)
    /// </summary>
    public void ResetItemStack(string id)
    {
        if (ownedItems.ContainsKey(id))
        {
            ownedItems.Remove(id);
            ApplyItemEffects(id);
        }
    }

    /// <summary>
    /// Hàm cập nhật chỉ số hoặc thực thi hiệu ứng lập tức
    /// </summary>
    private void ApplyItemEffects(string id)
    {
        // Gửi thông báo cập nhật lại các chỉ số nội bộ trên PlayerStats nếu có
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.UpdateStats();

            // Kích hoạt kích thước, nhảy hoặc cập nhật logic giáp ảo (nếu cần)
            PlayerController pc = GetComponent<PlayerController>();
            if (pc != null)
            {
                // Đồng bộ chỉ số nhảy tối đa sang jumpsRemaining của PlayerController
                // (nếu số lần nhảy tối đa thay đổi)
                if (id == "flying_fantastica")
                {
                    // Lực kéo nhảy phụ thuộc vào flying_fantastica
                    Debug.Log($"[Inventory] Số lần nhảy tối đa đã cập nhật thành: {stats.MaxJumps}");
                }
            }
        }

        // Cập nhật lại giao diện kho đồ (Inventory HUD)
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateInventoryUI(this);
        }
    }

    // =================================================================
    // LOGIC TỶ LỆ KÍCH HOẠT ĐÒN ĐÁNH (ON HIT TRIGGERS)
    // =================================================================
    public void TriggerOnHitEffects(EnemyController enemy, float baseDamage)
    {
        if (enemy == null) return;

        // 1. cold_snap: Giảm tốc độ di chuyển của quái đi 60%
        int sColdSnap = GetItemStack("cold_snap");
        if (sColdSnap > 0)
        {
            float slowDuration = 2f + 2f * (sColdSnap - 1);
            enemy.ApplySlow(0.6f, slowDuration);
            Debug.Log($"[Item Effect] Kích hoạt Cold Snap: Làm chậm 60% trong {slowDuration}s lên {enemy.gameObject.name}");
        }

        // 2. star_piece: 10% tỷ lệ sinh ra ngôi sao đuổi
        int sStarPiece = GetItemStack("star_piece");
        if (sStarPiece > 0)
        {
            if (Random.Range(0f, 100f) < 10f)
            {
                float starDamage = (3f + 2f * (sStarPiece - 1)) * baseDamage;
                SpawnStarPiece(enemy, starDamage);
            }
        }

        // 3. lightning_drop: 10% tỷ lệ sét đánh từ trên trời
        int sLightning = GetItemStack("lightning_drop");
        if (sLightning > 0)
        {
            if (Random.Range(0f, 100f) < 10f)
            {
                float lightningDamage = (5f * sLightning) * baseDamage;
                SpawnLightning(enemy, lightningDamage);
            }
        }

        // 4. abyss_nova: Sát thương diện rộng lan quái xung quanh
        int sAbyssNova = GetItemStack("abyss_nova");
        if (sAbyssNova > 0)
        {
            float playerHeight = 1.8f;
            Collider2D playerCol = GetComponent<Collider2D>();
            if (playerCol != null) playerHeight = playerCol.bounds.size.y;

            float radius = 0.75f * playerHeight; // Đường kính = 1.5 * Height -> Bán kính = 0.75 * Height
            float extraDamage = 0.20f * sAbyssNova * baseDamage;

            Collider2D[] hits = Physics2D.OverlapCircleAll(enemy.transform.position, radius, LayerMask.GetMask("Enemy"));
            foreach (Collider2D hit in hits)
            {
                if (hit.gameObject != enemy.gameObject)
                {
                    EnemyController target = hit.GetComponent<EnemyController>();
                    if (target == null) target = hit.GetComponentInParent<EnemyController>();
                    if (target != null)
                    {
                        target.TakeDamage(extraDamage);

                        // Sinh hiệu ứng hạt trúng đòn thường cho các quái vật bị nổ lan xung quanh
                        if (UIManager.Instance != null)
                        {
                            UIManager.Instance.SpawnHitParticle(target.transform.position, false);
                        }
                    }
                }
            }
            
        }
    }

    private void SpawnStarPiece(EnemyController target, float damage)
    {
        if (starPiecePrefab != null)
        {
            GameObject star = Instantiate(starPiecePrefab, transform.position, Quaternion.identity);
            HomingProjectile script = star.GetComponent<HomingProjectile>();
            if (script == null) script = star.AddComponent<HomingProjectile>();
            script.Setup(target.transform, damage);
            Debug.Log($"[Star Piece] Kích hoạt! Sinh ra ngôi sao đuổi theo {target.gameObject.name} (Sát thương: {damage})");
        }
        else
        {
            // Fallback trực tiếp nếu chưa gán prefab
            target.TakeDamage(damage);
            Debug.Log($"[Star Piece Fallback] Không có prefab, gây trực tiếp {damage} sát thương lên {target.gameObject.name}");
        }
    }

    private void SpawnLightning(EnemyController target, float damage)
    {
        if (lightningPrefab != null)
        {
            Vector3 spawnPos = target.transform.position + Vector3.up * 4f;
            GameObject lightning = Instantiate(lightningPrefab, spawnPos, Quaternion.identity);
            LightningStrike script = lightning.GetComponent<LightningStrike>();
            if (script == null) script = lightning.AddComponent<LightningStrike>();
            script.Setup(target, damage);
            Debug.Log($"[Lightning Drop] Kích hoạt! Sét đánh xuống {target.gameObject.name} (Sát thương: {damage})");

            if (AudioManager.Instance != null && lightningStrikeSound != null)
            {
                AudioManager.Instance.PlaySFX3D(lightningStrikeSound, target.transform.position);
            }
        }
        else
        {
            // Fallback trực tiếp nếu chưa gán prefab
            target.TakeDamage(damage);
            Debug.Log($"[Lightning Drop Fallback] Không có prefab, sét đánh trực tiếp {damage} sát thương lên {target.gameObject.name}");
        }
    }

    // =================================================================
    // LOGIC KÍCH HOẠT CỘT LỬA KHI TIÊU DIỆT KẺ ĐỊCH (RISING NEBULA)
    // =================================================================
    public void TriggerRisingNebula(Vector3 position, float enemyMaxHP)
    {
        int sRisingNebula = GetItemStack("rising_nebula");
        if (sRisingNebula <= 0) return;

        // Kiểm tra xem đã hết thời gian cooldown 6 giây chưa
        if (Time.time < nextRisingNebulaTime) return;

        // Thiết lập thời gian chờ tiếp theo
        nextRisingNebulaTime = Time.time + 6f;

        PlayerStats stats = GetComponent<PlayerStats>();
        float playerDamage = stats != null ? stats.SwordDamage : 20f;
        float damagePerSecond = (0.5f * sRisingNebula) * playerDamage + 0.1f * enemyMaxHP;

        if (flamePillarPrefab != null)
        {
            GameObject pillar = Instantiate(flamePillarPrefab, position, Quaternion.identity);
            FlamePillar script = pillar.GetComponent<FlamePillar>();
            if (script == null) script = pillar.AddComponent<FlamePillar>();
            script.Setup(damagePerSecond, 4f);
            Debug.Log($"[Rising Nebula] Sinh ra cột lửa tại {position} gây {damagePerSecond} sát thương mỗi giây.");

            if (AudioManager.Instance != null && risingNebulaSound != null)
            {
                AudioManager.Instance.PlaySFX3D(risingNebulaSound, position);
            }
        }
        else
        {
            // Fallback sinh ra cột lửa giả lập
            GameObject pillar = new GameObject("FlamePillarFallback");
            pillar.transform.position = position;
            FlamePillar script = pillar.AddComponent<FlamePillar>();
            script.Setup(damagePerSecond, 4f);
            Debug.Log($"[Rising Nebula Fallback] Sinh ra cột lửa fallback tại {position} gây {damagePerSecond} sát thương/giây.");
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private string debugItemId = "parallel_cross";
    private bool showDebugMenu = false;

    private void Update()
    {
        // Nhấn phím F1 để đóng/mở bảng Menu Debug trong lúc chơi game (Sử dụng Input System mới)
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
        {
            showDebugMenu = !showDebugMenu;
        }
    }

    private void OnGUI()
    {
        if (!showDebugMenu) return;

        GUILayout.BeginArea(new Rect(10f, 10f, 250f, 160f), "Debug Inventory Helper", GUI.skin.box);
        GUILayout.Label("Nhập ID Vật phẩm để test:");
        debugItemId = GUILayout.TextField(debugItemId);

        if (GUILayout.Button("Thêm Vật Phẩm"))
        {
            AddItemDirectly(debugItemId);
        }

        // Các nút bấm nhanh cho các item thông dụng
        if (GUILayout.Button("Thêm Parallel Cross"))
        {
            AddItemDirectly("parallel_cross");
        }
        if (GUILayout.Button("Thêm Ground Snow"))
        {
            AddItemDirectly("ground_snow");
        }

        GUILayout.EndArea();
    }
#endif
}

// =================================================================
// LỚP HỖ TRỢ VẬT LÝ CHO CÁC HIỆU ỨNG VẬT PHẨM (HELPER CLASSES)
// =================================================================

public class HomingProjectile : MonoBehaviour
{
    private Transform target;
    private float damage;
    private float speed = 12f;
    private float rotationSpeed = 360f; // Tốc độ xoay của ngôi sao (360 độ/giây)

    public void Setup(Transform targetEnemy, float dmg)
    {
        this.target = targetEnemy;
        this.damage = dmg;
        Destroy(gameObject, 5f); // Tự hủy sau 5s đề phòng mất dấu
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Xoay tròn ngôi sao liên tục quanh trục Z trong không gian 2D
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        if (Vector2.Distance(transform.position, target.position) < 0.3f)
        {
            EnemyController enemy = target.GetComponent<EnemyController>();
            if (enemy == null) enemy = target.GetComponentInParent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
    }
}

public class LightningStrike : MonoBehaviour
{
    public void Setup(EnemyController target, float damage)
    {
        if (target != null)
        {
            target.TakeDamage(damage);
        }
        Destroy(gameObject, 0.5f); // Tự hủy nhanh sau khi làm hiệu ứng sét
    }
}

public class FlamePillar : MonoBehaviour
{
    private float damage;
    private float duration;
    private float tickTimer = 0f;

    public void Setup(float dps, float dur)
    {
        this.damage = dps;
        this.duration = dur;
    }

    void Start()
    {
        Destroy(gameObject, duration);
    }

    void Update()
    {
        tickTimer += Time.deltaTime;
        if (tickTimer >= 1f)
        {
            tickTimer = 0f;
            DealDamage();
        }
    }

    private void DealDamage()
    {
        // Quét hình tròn bán kính 1.5m gây sát thương diện rộng mỗi giây
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.5f, LayerMask.GetMask("Enemy"));
        foreach (Collider2D hit in hits)
        {
            EnemyController enemy = hit.GetComponent<EnemyController>();
            if (enemy == null) enemy = hit.GetComponentInParent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
    }
}
