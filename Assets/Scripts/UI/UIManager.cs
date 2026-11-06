using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(UIDocument))]
public class UIManager : MonoBehaviour
{
    // 1. CẤU TRÚC SINGLETON:
    // Biến static công khai để truy cập UIManager từ bất kỳ nơi nào (ví dụ: PlayerController, PlayerStats, EnemyController, TeleportPortal)
    public static UIManager Instance { get; private set; }

    // Các biến private dùng để lưu trữ tham chiếu tới các thẻ VisualElement sau khi tìm kiếm
    private VisualElement healthBarFill; // Thẻ 'Hp' dùng để làm đầy thanh máu của người chơi
    private Label healthText;             // Thẻ 'Hp_Text' dùng để hiển thị text chỉ số máu người chơi
    private Label goldText;               // Thẻ 'Gold_Text' dùng để hiển thị số tiền xu của người chơi
    
    // Thẻ dùng cho gợi ý tương tác (Interaction Prompt)
    private VisualElement interactPopup; // Thẻ chi tiết 'Interact_Popup' (khung chứa popup)
    private Label interactText;          // Thẻ con 'Interact_Text' (Label hiển thị chữ)

    // Thẻ dùng cho thanh máu Boss gộp (Shared Boss Health Bar)
    private VisualElement bossHPBg;      // Thẻ nền chứa 'BossHP_Bg'
    private VisualElement bossHPFill;    // Thẻ lõi màu đỏ co dãn 'BossHP_Fill'
    private Label bossHealthText;        // Thẻ văn bản hiển thị lượng máu gộp 'Boss_Health_Text'

    // Thẻ dùng cho thông báo nhặt đồ (Item Pickup Notification)
    private VisualElement pickupPanel;   // Thẻ khung chứa ngoài cùng 'Pickup_Panel'
    private VisualElement pickupIcon;    // Thẻ ô chứa ảnh vật phẩm 'Pickup_Icon'
    private Label pickupName;            // Thẻ Label hiển thị Tên 'Pickup_Name'
    private Label pickupDescription;     // Thẻ Label hiển thị Mô tả 'Pickup_Description'

    // Thẻ dùng cho hiển thị phần trăm sạc của Cổng Dịch Chuyển (Teleport Progress)
    private VisualElement teleportPanel; // Thẻ khung chứa ngoài 'Teleport_Panel'
    private Label teleportText;          // Thẻ văn bản hiển thị % 'Teleport_Text'
    private VisualElement teleportFill;  // Thẻ lõi co dãn tiến trình 'Teleport_Fill' (tùy chọn thanh tiến trình)

    [Header("Inventory HUD Config")]
    [SerializeField] private float defaultSlotSize = 60f;      // Kích thước mặc định của ô vật phẩm (Tăng lên từ 48f)
    [SerializeField] private float slotMargin = 4f;            // Khoảng cách bên ngoài (margin) của mỗi ô
    [SerializeField] private float maxInventoryWidth = 750f;   // Chiều rộng tối đa của Panel kho đồ trước khi co giãn (Tăng lên từ 600f)
    private VisualElement inventoryPanel;                      // Thẻ khung chứa 'Inventory_Panel'
    private Label ammoText;                                    // Thẻ 'Ammo_Text' hiển thị lượng đạn

    [Header("Difficulty UI Toolkit Elements")]
    private VisualElement difficultyPanel;
    private Label difficultyLevelText;
    private VisualElement difficultyBarFill;

    [Header("Win/Lose Panels UI")]
    private VisualElement winPanel;
    private VisualElement losePanel;
    private VisualElement healthBarBg;
    private VisualElement goldContainer;
    private VisualElement ammoContainer;

    // Pause Panel Elements
    private VisualElement pausePanel;
    private Button pauseResumeBtn;
    private Button pauseQuitBtn;
    private bool isPaused = false;

    // Win Panel Elements
    private Label winStatTime;
    private Label winStatEnemies;
    private Label winStatBosses;
    private Label winStatDeaths;
    private Label winStatGold;
    private Label winStatItems;
    private Label winStatLevels;
    private VisualElement winInventoryContainer;
    private Button winRestartBtn;
    private Button winQuitBtn;

    // Lose Panel Elements
    private Label loseStatTime;
    private Label loseStatEnemies;
    private Label loseStatBosses;
    private Label loseStatDeaths;
    private Label loseStatGold;
    private Label loseStatItems;
    private Label loseStatLevels;
    private VisualElement loseInventoryContainer;
    private Button loseRestartBtn;
    private Button loseQuitBtn;

    [Header("Global VFX Prefabs")]
    [SerializeField] private GameObject defaultHitParticlePrefab;      // Hiệu ứng đánh trúng quái vật mặc định
    [SerializeField] private GameObject abyssNovaHitParticlePrefab;   // Hiệu ứng nổ rộng của Abyss Nova

    // Danh sách lưu trữ các thực thể Boss đang tham chiến
    private List<EnemyController> activeBosses = new List<EnemyController>();
    // Tổng lượng máu tối đa ban đầu của tất cả các Boss cộng lại
    private float totalBossMaxHP = 0f;

    // Struct dùng để lưu thông tin vật phẩm trong hàng đợi thông báo
    private struct PickupInfo
    {
        public Sprite sprite;
        public string name;
        public string description;
    }
    // Hàng đợi lưu trữ các thông báo nhặt đồ cần hiển thị lần lượt
    private Queue<PickupInfo> pickupQueue = new Queue<PickupInfo>();
    // Quản lý luồng chạy hiển thị thông báo
    private Coroutine pickupCoroutine;

    private void Awake()
    {
        // Khởi tạo mô hình Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Đảm bảo UIManager không bị hủy khi chuyển Scene
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Tự động nâng cấp kích thước nếu đang dùng giá trị mặc định cũ (để tránh bị override bởi Scene serialization)
        if (defaultSlotSize == 48f) defaultSlotSize = 60f;
        if (maxInventoryWidth == 600f) maxInventoryWidth = 750f;
    }

    private void OnEnable()
    {
        RebindUIElements();
        // Đăng ký sự kiện nạp Scene để tự động hủy Pause và nạp lại UI
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void RebindUIElements()
    {
        // 2. KHỞI TẠO VÀ RÀ SOÁT TÌM THẺ TRONG UI TOOLKIT:
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            if (root != null)
            {
                // Tìm kiếm các thẻ định danh của Player:
                healthBarFill = root.Q<VisualElement>("Hp");
                healthText = root.Q<Label>("Hp_Text");
                goldText = root.Q<Label>("Gold_Text");

                // Tìm kiếm các thẻ của Popup tương tác:
                interactPopup = root.Q<VisualElement>("Interact_Popup");
                interactText = root.Q<Label>("Interact_Text");

                // Tìm kiếm các thẻ của Thanh máu Boss:
                bossHPBg = root.Q<VisualElement>("BossHP_Bg");
                bossHPFill = root.Q<VisualElement>("BossHP_Fill");
                bossHealthText = root.Q<Label>("Boss_Health_Text");

                // Tìm kiếm các thẻ của Thông báo nhặt đồ:
                pickupPanel = root.Q<VisualElement>("Pickup_Panel");
                pickupIcon = root.Q<VisualElement>("Pickup_Icon");
                pickupName = root.Q<Label>("Pickup_Name");
                pickupDescription = root.Q<Label>("Pickup_Description");

                // Tìm kiếm các thẻ tiến trình cổng Dịch Chuyển:
                teleportPanel = root.Q<VisualElement>("Teleport_Panel");
                teleportText = root.Q<Label>("Teleport_Text");
                teleportFill = root.Q<VisualElement>("Teleport_Fill");

                // Tìm kiếm thẻ kho đồ (Inventory Panel)
                inventoryPanel = root.Q<VisualElement>("Inventory_Panel");

                // Tìm kiếm thẻ hiển thị đạn
                ammoText = root.Q<Label>("Ammo_Text");

                // Tìm kiếm các thẻ độ khó (Difficulty UI)
                difficultyPanel = root.Q<VisualElement>("Difficulty_Panel");
                difficultyLevelText = root.Q<Label>("Difficulty_Level_Text");
                difficultyBarFill = root.Q<VisualElement>("Difficulty_Bar_Fill");

                // Tìm kiếm các thẻ Win/Lose Panel
                winPanel = root.Q<VisualElement>("Win_Panel");
                losePanel = root.Q<VisualElement>("Lose_Panel");
                healthBarBg = root.Q<VisualElement>("HealthBar_Bg");
                goldContainer = root.Q<VisualElement>("Gold_Container");
                ammoContainer = root.Q<VisualElement>("Ammo_Container");

                winStatTime = root.Q<Label>("Win_Stat_Time");
                winStatEnemies = root.Q<Label>("Win_Stat_Enemies");
                winStatBosses = root.Q<Label>("Win_Stat_Bosses");
                winStatDeaths = root.Q<Label>("Win_Stat_Deaths");
                winStatGold = root.Q<Label>("Win_Stat_Gold");
                winStatItems = root.Q<Label>("Win_Stat_Items");
                winStatLevels = root.Q<Label>("Win_Stat_Levels");
                winInventoryContainer = root.Q<VisualElement>("Win_Inventory_Container");
                winRestartBtn = root.Q<Button>("Win_Restart_Btn");
                winQuitBtn = root.Q<Button>("Win_Quit_Btn");

                loseStatTime = root.Q<Label>("Lose_Stat_Time");
                loseStatEnemies = root.Q<Label>("Lose_Stat_Enemies");
                loseStatBosses = root.Q<Label>("Lose_Stat_Bosses");
                loseStatDeaths = root.Q<Label>("Lose_Stat_Deaths");
                loseStatGold = root.Q<Label>("Lose_Stat_Gold");
                loseStatItems = root.Q<Label>("Lose_Stat_Items");
                loseStatLevels = root.Q<Label>("Lose_Stat_Levels");
                loseInventoryContainer = root.Q<VisualElement>("Lose_Inventory_Container");
                loseRestartBtn = root.Q<Button>("Lose_Restart_Btn");
                loseQuitBtn = root.Q<Button>("Lose_Quit_Btn");

                // Tìm kiếm Pause Panel
                pausePanel = root.Q<VisualElement>("Pause_Panel");
                pauseResumeBtn = root.Q<Button>("Pause_Resume_Btn");
                pauseQuitBtn = root.Q<Button>("Pause_Quit_Btn");

                // Đăng ký sự kiện Click (Hủy đăng ký trước để tránh trùng lặp)
                if (winRestartBtn != null) { winRestartBtn.clicked -= RestartGame; winRestartBtn.clicked += RestartGame; }
                if (winQuitBtn != null) { winQuitBtn.clicked -= QuitGame; winQuitBtn.clicked += QuitGame; }
                if (loseRestartBtn != null) { loseRestartBtn.clicked -= RestartGame; loseRestartBtn.clicked += RestartGame; }
                if (loseQuitBtn != null) { loseQuitBtn.clicked -= QuitGame; loseQuitBtn.clicked += QuitGame; }
                if (pauseResumeBtn != null) { pauseResumeBtn.clicked -= ResumeGame; pauseResumeBtn.clicked += ResumeGame; }
                if (pauseQuitBtn != null) { pauseQuitBtn.clicked -= QuitGame; pauseQuitBtn.clicked += QuitGame; }

                // Cảnh báo gỡ lỗi nếu thiếu thẻ trong UXML file
                if (healthBarFill == null) Debug.LogWarning("[UIManager] Không tìm thấy VisualElement 'Hp'!");
                if (healthText == null) Debug.LogWarning("[UIManager] Không tìm thấy Label 'Hp_Text'!");
                if (goldText == null) Debug.LogWarning("[UIManager] Không tìm thấy Label 'Gold_Text'!");
                if (interactPopup == null) Debug.LogWarning("[UIManager] Không tìm thấy VisualElement 'Interact_Popup'!");
                if (interactText == null) Debug.LogWarning("[UIManager] Không tìm thấy Label 'Interact_Text'!");
                if (bossHPBg == null) Debug.LogWarning("[UIManager] Không tìm thấy VisualElement 'BossHP_Bg'!");
                if (bossHPFill == null) Debug.LogWarning("[UIManager] Không tìm thấy VisualElement 'BossHP_Fill'!");
                if (bossHealthText == null) Debug.LogWarning("[UIManager] Không tìm thấy Label 'Boss_Health_Text'!");
                if (pickupPanel == null) Debug.LogWarning("[UIManager] Không tìm thấy VisualElement 'Pickup_Panel'!");
                if (pickupIcon == null) Debug.LogWarning("[UIManager] Không tìm thấy VisualElement 'Pickup_Icon'!");
                if (pickupName == null) Debug.LogWarning("[UIManager] Không tìm thấy Label 'Pickup_Name'!");
                if (pickupDescription == null) Debug.LogWarning("[UIManager] Không tìm thấy Label 'Pickup_Description'!");
                if (teleportPanel == null) Debug.LogWarning("[UIManager] Không tìm thấy VisualElement 'Teleport_Panel'!");
                if (teleportText == null) Debug.LogWarning("[UIManager] Không tìm thấy Label 'Teleport_Text'!");
                if (inventoryPanel == null) Debug.LogWarning("[UIManager] Không tìm thấy VisualElement 'Inventory_Panel'!");
                if (ammoText == null) Debug.LogWarning("[UIManager] Không tìm thấy Label 'Ammo_Text'!");
                if (difficultyPanel == null) Debug.LogWarning("[UIManager] (Tùy chọn) Không tìm thấy VisualElement 'Difficulty_Panel'!");
                if (difficultyLevelText == null) Debug.LogWarning("[UIManager] (Tùy chọn) Không tìm thấy Label 'Difficulty_Level_Text'!");
                if (difficultyBarFill == null) Debug.LogWarning("[UIManager] (Tùy chọn) Không tìm thấy VisualElement 'Difficulty_Bar_Fill'!");
            }
        }
    }

    // 3. HÀM CẬP NHẬT THANH MÁU PLAYER (PUBLIC):
    public void UpdatePlayerHealth(float currentHealth, float maxHealth)
    {
        if (healthBarFill == null || healthBarFill.panel == null || healthText == null || healthText.panel == null)
        {
            RebindUIElements();
        }
        if (maxHealth <= 0f || healthBarFill == null || healthText == null) return;

        float healthPercentage = Mathf.Clamp((currentHealth / maxHealth) * 100f, 0f, 100f);
        healthBarFill.style.width = new Length(healthPercentage, LengthUnit.Percent);

        int displayHp = Mathf.Max(0, Mathf.RoundToInt(currentHealth));
        int displayMaxHp = Mathf.RoundToInt(maxHealth);
        healthText.text = $"{displayHp} / {displayMaxHp}";
    }

    // 4. HÀM CẬP NHẬT TIỀN XU PLAYER (PUBLIC):
    public void UpdatePlayerGold(int currentGold)
    {
        if (goldText == null || goldText.panel == null)
        {
            RebindUIElements();
        }
        if (goldText == null) return;
        goldText.text = currentGold.ToString("N0");
    }

    // HÀM CẬP NHẬT SỐ LƯỢNG ĐẠN PLAYER (PUBLIC):
    public void UpdatePlayerAmmo(int currentAmmo, int maxAmmo, bool isReloading)
    {
        if (ammoText == null || ammoText.panel == null)
        {
            RebindUIElements();
        }
        if (ammoText == null) return;

        if (isReloading)
        {
            ammoText.text = "RELOADING!";
            ammoText.style.color = new StyleColor(new Color(1f, 0.75f, 0f)); // Màu vàng cam cho dễ nhận biết
        }
        else
        {
            ammoText.text = $"{currentAmmo} / {maxAmmo}";
            ammoText.style.color = new StyleColor(Color.white); // Trả về màu trắng mặc định
        }
    }

    // 5. HÀM HIỂN THỊ GỢI Ý TƯƠNG TÁC (PUBLIC):
    public void ShowInteractPrompt(string message)
    {
        if (interactPopup == null || interactText == null) return;
        interactText.text = message;
        interactPopup.style.opacity = 1f;
    }

    // 6. HÀM ẨN GỢI Ý TƯƠNG TÁC (PUBLIC):
    public void HideInteractPrompt()
    {
        if (interactPopup == null) return;
        interactPopup.style.opacity = 0f;
    }

    // =================================================================
    // 7. CÁC HÀM QUẢN LÝ THANH MÁU BOSS GỘP (SHARED BOSS HEALTH BAR)
    // =================================================================

    // Hàm bắt đầu trận đấu Boss: Đăng ký danh sách Boss và hiển thị thanh máu gộp lên màn hình
    public void StartBossFight(List<EnemyController> bosses)
    {
        if (bosses == null || bosses.Count == 0 || bossHPBg == null || bossHPFill == null || bossHealthText == null) return;

        // Làm rỗng danh sách cũ và nạp danh sách Boss mới
        activeBosses.Clear();
        activeBosses.AddRange(bosses);

        // Đếm tổng MaxHP của tất cả Boss tham gia trận đấu
        totalBossMaxHP = 0f;
        foreach (var boss in activeBosses)
        {
            if (boss != null)
            {
                totalBossMaxHP += boss.MaxHealth;
            }
        }

        // Đặt hiển thị ban đầu: Đầy 100% máu
        bossHealthText.text = $"{totalBossMaxHP} / {totalBossMaxHP}";
        bossHPFill.style.width = new Length(100f, LengthUnit.Percent);

        // Làm hiện khung thanh máu của Boss (Đặt opacity về 1 hoặc display về Flex)
        bossHPBg.style.opacity = 1f;
    }

    // Hàm cập nhật lượng máu gộp động: Cộng dồn máu hiện tại của các Boss còn sống và vẽ lại thanh UI
    public void UpdateBossHealthUI()
    {
        // Kiểm tra an toàn
        if (bossHPBg == null || bossHPFill == null || bossHealthText == null) return;

        float currentTotalHP = 0f;

        // Duyệt qua danh sách để cộng dồn lượng máu thực tế của các Boss đang còn sống
        foreach (var boss in activeBosses)
        {
            if (boss != null && !boss.IsDead && boss.CurrentHealth > 0f)
            {
                currentTotalHP += boss.CurrentHealth;
            }
        }

        // Kiểm tra an toàn: Nếu tổng MaxHP của Boss bằng 0 hoặc nhỏ hơn, tự động ẩn thanh máu
        if (totalBossMaxHP <= 0f)
        {
            HideBossHealthUI();
            return;
        }

        // Tính toán tỷ lệ phần trăm lượng máu gộp còn lại
        float percentage = (currentTotalHP / totalBossMaxHP) * 100f;
        percentage = Mathf.Clamp(percentage, 0f, 100f);

        // Thay đổi chiều rộng (width) của VisualElement màu đỏ theo tỷ lệ phần trăm
        bossHPFill.style.width = new Length(percentage, LengthUnit.Percent);

        // Cập nhật text hiển thị số lượng máu gộp chính xác đè lên thanh máu
        int displayHP = Mathf.Max(0, Mathf.RoundToInt(currentTotalHP));
        int displayMaxHP = Mathf.RoundToInt(totalBossMaxHP);
        bossHealthText.text = $"{displayHP} / {displayMaxHP}";

        // Nếu toàn bộ Boss bị tiêu diệt (tổng lượng máu gộp <= 0), ẩn thanh máu khỏi màn hình
        if (currentTotalHP <= 0f)
        {
            HideBossHealthUI();
        }
    }

    // Hàm ẩn thanh máu Boss và dọn dẹp danh sách theo dõi
    public void HideBossHealthUI()
    {
        if (bossHPBg != null)
        {
            bossHPBg.style.opacity = 0f; // Ẩn hoàn toàn khung chứa
        }

        activeBosses.Clear();
        totalBossMaxHP = 0f;
    }

    // =================================================================
    // 8. CÁC HÀM HIỂN THỊ THÔNG BÁO NHẶT VẬT PHẨM (PICKUP NOTIFICATION)
    // =================================================================

    // Hàm hiển thị thông báo nhặt được vật phẩm (Được gọi từ PlayerInventory)
    public void ShowPickupNotification(Sprite itemSprite, string itemName, string itemDesc)
    {
        if (pickupPanel == null || pickupIcon == null || pickupName == null || pickupDescription == null) return;

        pickupQueue.Enqueue(new PickupInfo { sprite = itemSprite, name = itemName, description = itemDesc });

        if (pickupCoroutine == null)
        {
            pickupCoroutine = StartCoroutine(PickupNotificationQueueRoutine());
        }
    }

    private IEnumerator PickupNotificationQueueRoutine()
    {
        while (pickupQueue.Count > 0)
        {
            PickupInfo info = pickupQueue.Dequeue();

            pickupIcon.style.backgroundImage = new StyleBackground(info.sprite);
            pickupName.text = info.name;
            pickupDescription.text = info.description;

            pickupPanel.style.opacity = 1f;

            yield return new WaitForSeconds(2.5f);

            pickupPanel.style.opacity = 0f;

            yield return new WaitForSeconds(0.3f);
        }

        pickupCoroutine = null;
    }

    // =================================================================
    // 9. CÁC HÀM HIỂN THỊ TIẾN TRÌNH SẠC CỔNG DỊCH CHUYỂN (TELEPORT UI)
    // =================================================================

    // Hàm làm hiển thị khung tiến trình sạc cổng dịch chuyển
    public void ShowTeleportUI()
    {
        if (teleportPanel == null) return;
        teleportPanel.style.opacity = 1f; // Hiện khung sạc cổng
    }

    // Hàm cập nhật % và thanh co dãn sạc cổng dịch chuyển (progress từ 0 đến 100)
    public void UpdateTeleportProgress(float progress)
    {
        if (teleportPanel == null || teleportText == null) return;

        // Giới hạn phần trăm trong khoảng từ 0% đến 100%
        float clampedProgress = Mathf.Clamp(progress, 0f, 100f);

        // Hiển thị phần trăm dạng văn bản (Ví dụ: "Đang sạc cổng: 45%")
        teleportText.text = $"Sạc cổng dịch chuyển: {Mathf.RoundToInt(clampedProgress)}%";

        // Nếu có thiết lập thanh fill tiến trình, co dãn nó theo phần trăm
        if (teleportFill != null)
        {
            teleportFill.style.width = new Length(clampedProgress, LengthUnit.Percent);
        }
    }

    // Hàm ẩn tiến trình sạc cổng dịch chuyển khi hoàn tất
    public void HideTeleportUI()
    {
        if (teleportPanel == null) return;
        teleportPanel.style.opacity = 0f; // Ẩn khung sạc cổng
    }

    // =================================================================
    // 10. CÁC HÀM QUẢN LÝ GIAO DIỆN KHO ĐỒ (INVENTORY HUD UI)
    // =================================================================

    // Hàm cập nhật kho đồ hiển thị (tự động co giãn theo số lượng vật phẩm)
    public void UpdateInventoryUI(PlayerInventory inventory)
    {
        if (inventoryPanel == null || inventoryPanel.panel == null)
        {
            RebindUIElements();
        }
        if (inventoryPanel == null || inventoryPanel.panel == null || inventory == null)
        {
            // Nếu panel chưa sẵn sàng (đang nạp scene), thử lại vào khung hình kế tiếp
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(RetryUpdateInventoryUI(inventory));
            }
            return;
        }

        // Xóa sạch các ô cũ
        inventoryPanel.Clear();

        // Lấy danh sách các vật phẩm người chơi sở hữu
        var owned = inventory.GetOwnedItems();
        List<KeyValuePair<string, int>> activeItems = new List<KeyValuePair<string, int>>();

        foreach (var pair in owned)
        {
            if (pair.Value > 0)
            {
                activeItems.Add(pair);
            }
        }

        int itemCount = activeItems.Count;
        if (itemCount == 0) return;

        // Tính toán kích thước co dãn động của mỗi ô (slot)
        float totalItemWidth = defaultSlotSize + (slotMargin * 2f);
        float size = defaultSlotSize;

        // Nếu tổng kích thước các item vượt quá maxInventoryWidth, thu nhỏ kích thước
        if (itemCount * totalItemWidth > maxInventoryWidth)
        {
            float availableWidthPerItem = maxInventoryWidth / itemCount;
            size = Mathf.Max(24f, availableWidthPerItem - (slotMargin * 2f));
        }

        foreach (var pair in activeItems)
        {
            string itemId = pair.Key;
            int count = pair.Value;

            ItemData itemData = inventory.GetItemData(itemId);
            if (itemData == null || itemData.itemIcon == null)
            {
                continue; // Bỏ qua nếu thiếu dữ liệu ScriptableObject hoặc Sprite
            }

            // 1. Khởi tạo ô chứa vật phẩm (Slot)
            VisualElement slot = new VisualElement();
            slot.style.width = size;
            slot.style.height = size;
            slot.style.marginLeft = slotMargin;
            slot.style.marginRight = slotMargin;
            slot.style.marginTop = slotMargin;
            slot.style.marginBottom = slotMargin;

            // Thiết lập phong cách hiển thị giống ô nhặt đồ
            slot.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.44f));
            slot.style.borderLeftColor = new StyleColor(new Color(1f, 1f, 1f, 0.15f));
            slot.style.borderRightColor = new StyleColor(new Color(1f, 1f, 1f, 0.15f));
            slot.style.borderTopColor = new StyleColor(new Color(1f, 1f, 1f, 0.15f));
            slot.style.borderBottomColor = new StyleColor(new Color(1f, 1f, 1f, 0.15f));
            slot.style.borderLeftWidth = 1f;
            slot.style.borderRightWidth = 1f;
            slot.style.borderTopWidth = 1f;
            slot.style.borderBottomWidth = 1f;
            slot.style.borderTopLeftRadius = 4f;
            slot.style.borderTopRightRadius = 4f;
            slot.style.borderBottomLeftRadius = 4f;
            slot.style.borderBottomRightRadius = 4f;
            slot.style.overflow = Overflow.Hidden;
            slot.style.position = Position.Relative;

            slot.AddToClassList("inventory-slot");

            // 2. Tạo VisualElement chứa Icon
            VisualElement icon = new VisualElement();
            icon.style.position = Position.Absolute;
            icon.style.width = new Length(100f, LengthUnit.Percent);
            icon.style.height = new Length(100f, LengthUnit.Percent);
            icon.style.backgroundImage = new StyleBackground(itemData.itemIcon);

            icon.AddToClassList("inventory-slot-icon");
            slot.Add(icon);

            // 3. Nếu số lượng lớn hơn 1, hiển thị nhãn số lượng nhỏ ở góc dưới phải
            if (count > 1)
            {
                // Tạo một badge nhỏ có nền tối bao quanh chữ số để tăng độ tương phản, dễ đọc trên mọi nền item
                VisualElement badge = new VisualElement();
                badge.style.position = Position.Absolute;
                badge.style.bottom = 2f;
                badge.style.right = 2f;
                badge.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.75f)); // Nền đen mờ 75%
                badge.style.borderTopLeftRadius = 4f;
                badge.style.borderTopRightRadius = 4f;
                badge.style.borderBottomLeftRadius = 4f;
                badge.style.borderBottomRightRadius = 4f;
                badge.style.paddingLeft = 4f;
                badge.style.paddingRight = 4f;
                badge.style.paddingTop = 1f;
                badge.style.paddingBottom = 1f;

                // Thêm viền mỏng sáng màu nhẹ để nổi bật
                badge.style.borderLeftColor = new StyleColor(new Color(1f, 1f, 1f, 0.15f));
                badge.style.borderRightColor = new StyleColor(new Color(1f, 1f, 1f, 0.15f));
                badge.style.borderTopColor = new StyleColor(new Color(1f, 1f, 1f, 0.15f));
                badge.style.borderBottomColor = new StyleColor(new Color(1f, 1f, 1f, 0.15f));
                badge.style.borderLeftWidth = 0.5f;
                badge.style.borderRightWidth = 0.5f;
                badge.style.borderTopWidth = 0.5f;
                badge.style.borderBottomWidth = 0.5f;

                Label countLabel = new Label();
                countLabel.text = count.ToString();
                countLabel.style.marginBottom = 0f;
                countLabel.style.marginTop = 0f;
                countLabel.style.marginLeft = 0f;
                countLabel.style.marginRight = 0f;
                countLabel.style.paddingBottom = 0f;
                countLabel.style.paddingTop = 0f;
                countLabel.style.paddingLeft = 0f;
                countLabel.style.paddingRight = 0f;

                // Đồng bộ font chữ với Gold Text
                if (goldText != null)
                {
                    countLabel.style.unityFontDefinition = goldText.style.unityFontDefinition;
                }

                // Cỡ chữ tỉ lệ với kích cỡ ô
                float fontSize = Mathf.Max(11f, size * 0.32f);
                countLabel.style.fontSize = fontSize;
                countLabel.style.color = new StyleColor(new Color(1f, 0.86f, 0.39f)); // Màu vàng nhạt giống coin
                countLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);

                countLabel.AddToClassList("inventory-slot-count");
                badge.Add(countLabel);
                slot.Add(badge);
            }

            inventoryPanel.Add(slot);
        }
    }

    private IEnumerator RetryUpdateInventoryUI(PlayerInventory inventory)
    {
        yield return null; // Đợi 1 frame để UI Toolkit panel được gắn vào canvas
        if (inventoryPanel == null || inventoryPanel.panel == null)
        {
            RebindUIElements();
        }
        if (inventoryPanel != null && inventoryPanel.panel != null && inventory != null)
        {
            UpdateInventoryUI(inventory);
        }
    }

    // Hàm sinh hiệu ứng hạt khi đánh trúng quái vật
    public void SpawnHitParticle(Vector3 position, bool hasAbyssNova)
    {
        if (hasAbyssNova && abyssNovaHitParticlePrefab != null)
        {
            Instantiate(abyssNovaHitParticlePrefab, position, Quaternion.identity);
        }
        else if (defaultHitParticlePrefab != null)
        {
            Instantiate(defaultHitParticlePrefab, position, Quaternion.identity);
        }
    }

    // =================================================================
    // 11. CÁC HÀM HIỂN THỊ MÀN HÌNH THẮNG / THUA (WIN/LOSE SCREEN)
    // =================================================================

    private void OnDisable()
    {
        if (winRestartBtn != null) winRestartBtn.clicked -= RestartGame;
        if (winQuitBtn != null) winQuitBtn.clicked -= QuitGame;
        if (loseRestartBtn != null) loseRestartBtn.clicked -= RestartGame;
        if (loseQuitBtn != null) loseQuitBtn.clicked -= QuitGame;
        if (pauseResumeBtn != null) pauseResumeBtn.clicked -= ResumeGame;
        if (pauseQuitBtn != null) pauseQuitBtn.clicked -= QuitGame;

        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void SetPlayingHUDVisibility(bool visible)
    {
        DisplayStyle style = visible ? DisplayStyle.Flex : DisplayStyle.None;

        if (healthBarBg != null) healthBarBg.style.display = style;
        if (goldContainer != null) goldContainer.style.display = style;
        if (ammoContainer != null) ammoContainer.style.display = style;
        if (inventoryPanel != null) inventoryPanel.style.display = style;
        if (difficultyPanel != null) difficultyPanel.style.display = style;
    }

    private void PopulateEndGameInventory(VisualElement container, PlayerInventory inventory)
    {
        if (container == null || inventory == null) return;
        container.Clear();

        // Đảm bảo container xếp các ô theo hàng ngang và tự xuống dòng nếu quá dài
        container.style.flexDirection = FlexDirection.Row;
        container.style.flexWrap = Wrap.Wrap;
        container.style.alignItems = Align.FlexStart;      // Căn chỉnh các ô lên phía trên cùng
        container.style.alignContent = Align.FlexStart; // Giữ các hàng xếp tuần tự từ trên xuống
        container.style.justifyContent = Justify.FlexStart;

        float endGameSlotSize = 72f; // Làm cho ô vật phẩm to lên ở màn hình thắng/thua (gốc là 64f)

        var owned = inventory.GetOwnedItems();
        List<KeyValuePair<string, int>> activeItems = new List<KeyValuePair<string, int>>();

        foreach (var pair in owned)
        {
            if (pair.Value > 0)
            {
                activeItems.Add(pair);
            }
        }

        foreach (var pair in activeItems)
        {
            string itemId = pair.Key;
            int count = pair.Value;

            ItemData itemData = inventory.GetItemData(itemId);
            if (itemData == null || itemData.itemIcon == null)
            {
                continue;
            }

            VisualElement slot = new VisualElement();
            slot.style.width = endGameSlotSize;
            slot.style.height = endGameSlotSize;
            slot.style.marginLeft = slotMargin;
            slot.style.marginRight = slotMargin;
            slot.style.marginTop = slotMargin;
            slot.style.marginBottom = slotMargin;

            slot.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.44f));
            slot.style.borderLeftColor = new StyleColor(new Color(1f, 1f, 1f, 0.15f));
            slot.style.borderRightColor = new StyleColor(new Color(1f, 1f, 1f, 0.15f));
            slot.style.borderTopColor = new StyleColor(new Color(1f, 1f, 1f, 0.15f));
            slot.style.borderBottomColor = new StyleColor(new Color(1f, 1f, 1f, 0.15f));
            slot.style.borderLeftWidth = 1f;
            slot.style.borderRightWidth = 1f;
            slot.style.borderTopWidth = 1f;
            slot.style.borderBottomWidth = 1f;
            slot.style.borderTopLeftRadius = 4f;
            slot.style.borderTopRightRadius = 4f;
            slot.style.borderBottomLeftRadius = 4f;
            slot.style.borderBottomRightRadius = 4f;
            slot.style.overflow = Overflow.Hidden;
            slot.style.position = Position.Relative;

            VisualElement icon = new VisualElement();
            icon.style.position = Position.Absolute;
            icon.style.width = new Length(100f, LengthUnit.Percent);
            icon.style.height = new Length(100f, LengthUnit.Percent);
            icon.style.backgroundImage = new StyleBackground(itemData.itemIcon);
            slot.Add(icon);

            if (count > 1)
            {
                // Tạo một badge nhỏ có nền tối bao quanh chữ số để tăng độ tương phản, dễ đọc trên mọi nền item
                VisualElement badge = new VisualElement();
                badge.style.position = Position.Absolute;
                badge.style.bottom = 2f;
                badge.style.right = 2f;
                badge.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.75f)); // Nền đen mờ 75%
                badge.style.borderTopLeftRadius = 4f;
                badge.style.borderTopRightRadius = 4f;
                badge.style.borderBottomLeftRadius = 4f;
                badge.style.borderBottomRightRadius = 4f;
                badge.style.paddingLeft = 4f;
                badge.style.paddingRight = 4f;
                badge.style.paddingTop = 1f;
                badge.style.paddingBottom = 1f;

                // Thêm viền mỏng sáng màu nhẹ để nổi bật
                badge.style.borderLeftColor = new StyleColor(new Color(1f, 1f, 1f, 0.15f));
                badge.style.borderRightColor = new StyleColor(new Color(1f, 1f, 1f, 0.15f));
                badge.style.borderTopColor = new StyleColor(new Color(1f, 1f, 1f, 0.15f));
                badge.style.borderBottomColor = new StyleColor(new Color(1f, 1f, 1f, 0.15f));
                badge.style.borderLeftWidth = 0.5f;
                badge.style.borderRightWidth = 0.5f;
                badge.style.borderTopWidth = 0.5f;
                badge.style.borderBottomWidth = 0.5f;

                Label countLabel = new Label();
                countLabel.text = count.ToString();
                countLabel.style.marginBottom = 0f;
                countLabel.style.marginTop = 0f;
                countLabel.style.marginLeft = 0f;
                countLabel.style.marginRight = 0f;
                countLabel.style.paddingBottom = 0f;
                countLabel.style.paddingTop = 0f;
                countLabel.style.paddingLeft = 0f;
                countLabel.style.paddingRight = 0f;

                if (goldText != null)
                {
                    countLabel.style.unityFontDefinition = goldText.style.unityFontDefinition;
                }

                float fontSize = Mathf.Max(12f, endGameSlotSize * 0.32f);
                countLabel.style.fontSize = fontSize;
                countLabel.style.color = new StyleColor(new Color(1f, 0.86f, 0.39f));
                countLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);

                badge.Add(countLabel);
                slot.Add(badge);
            }

            container.Add(slot);
        }
    }

    public void ShowWinScreen()
    {
        if (winPanel == null) return;

        // Dừng nhạc nền và phát nhạc chiến thắng (Victory SFX)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM(0.5f);
            if (GameManager.Instance != null && GameManager.Instance.victorySound != null)
            {
                // Giảm âm lượng nhạc chiến thắng xuống 0.4f để tránh quá to
                AudioManager.Instance.PlaySFX2D(GameManager.Instance.victorySound, 0.4f);
            }
        }

        SetPlayingHUDVisibility(false);
        winPanel.style.display = DisplayStyle.Flex;

        if (GameManager.Instance != null)
        {
            float playTime = Time.time - GameManager.Instance.gameStartTime;
            int minutes = Mathf.FloorToInt(playTime / 60f);
            int seconds = Mathf.FloorToInt(playTime % 60f);
            string formattedTime = string.Format("{0:00}:{1:00}", minutes, seconds);

            if (winStatTime != null)
                winStatTime.text = $"<color=#FFFFFF>Thời gian chơi: </color><color=#FFCC00>{formattedTime}</color>";
            
            if (winStatEnemies != null)
                winStatEnemies.text = $"<color=#FFFFFF>Số quái vật đã hạ gục: </color><color=#FFCC00>{GameManager.Instance.enemiesKilledCount}</color>";
            
            if (winStatBosses != null)
                winStatBosses.text = $"<color=#FFFFFF>Số boss bị hạ gục: </color><color=#FFCC00>{GameManager.Instance.bossesKilledCount}</color>";
            
            if (winStatDeaths != null)
                winStatDeaths.text = $"<color=#FFFFFF>Số lần chết: </color><color=#FFCC00>{GameManager.Instance.playerDeathCount}</color>";
            
            if (winStatGold != null)
                winStatGold.text = $"<color=#FFFFFF>Tổng số tiền đã nhặt được: </color><color=#FFCC00>{GameManager.Instance.totalGoldCollected:N0}</color>";
            
            if (winStatItems != null)
                winStatItems.text = $"<color=#FFFFFF>Số vật phẩm đã nhặt: </color><color=#FFCC00>{GameManager.Instance.totalItemsCollected}</color>";
            
            if (winStatLevels != null)
                winStatLevels.text = $"<color=#FFFFFF>Số màn chơi đã hoàn thành: </color><color=#FFCC00>{GameManager.Instance.levelsCompletedCount}</color>";
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            PlayerInventory inventory = playerObj.GetComponent<PlayerInventory>();
            if (inventory != null && winInventoryContainer != null)
            {
                PopulateEndGameInventory(winInventoryContainer, inventory);
            }
        }

        Time.timeScale = 0f;
    }

    public void ShowLoseScreen()
    {
        if (losePanel == null) return;

        // Dừng nhạc nền và phát nhạc thất bại (Defeat SFX)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM(0.5f);
            if (GameManager.Instance != null && GameManager.Instance.defeatSound != null)
            {
                // Giảm âm lượng nhạc thất bại xuống 0.4f để tránh quá to
                AudioManager.Instance.PlaySFX2D(GameManager.Instance.defeatSound, 0.4f);
            }
        }

        SetPlayingHUDVisibility(false);
        losePanel.style.display = DisplayStyle.Flex;

        if (GameManager.Instance != null)
        {
            float playTime = Time.time - GameManager.Instance.gameStartTime;
            int minutes = Mathf.FloorToInt(playTime / 60f);
            int seconds = Mathf.FloorToInt(playTime % 60f);
            string formattedTime = string.Format("{0:00}:{1:00}", minutes, seconds);

            if (loseStatTime != null)
                loseStatTime.text = $"<color=#FFFFFF>Thời gian chơi: </color><color=#FFCC00>{formattedTime}</color>";
            
            if (loseStatEnemies != null)
                loseStatEnemies.text = $"<color=#FFFFFF>Số quái vật đã hạ gục: </color><color=#FFCC00>{GameManager.Instance.enemiesKilledCount}</color>";
            
            if (loseStatBosses != null)
                loseStatBosses.text = $"<color=#FFFFFF>Số boss bị hạ gục: </color><color=#FFCC00>{GameManager.Instance.bossesKilledCount}</color>";
            
            if (loseStatDeaths != null)
                loseStatDeaths.text = $"<color=#FFFFFF>Số lần chết: </color><color=#FFCC00>{GameManager.Instance.playerDeathCount}</color>";
            
            if (loseStatGold != null)
                loseStatGold.text = $"<color=#FFFFFF>Tổng số tiền đã nhặt được: </color><color=#FFCC00>{GameManager.Instance.totalGoldCollected:N0}</color>";
            
            if (loseStatItems != null)
                loseStatItems.text = $"<color=#FFFFFF>Số vật phẩm đã nhặt: </color><color=#FFCC00>{GameManager.Instance.totalItemsCollected}</color>";
            
            if (loseStatLevels != null)
                loseStatLevels.text = $"<color=#FFFFFF>Số màn chơi đã hoàn thành: </color><color=#FFCC00>{GameManager.Instance.levelsCompletedCount}</color>";
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            PlayerInventory inventory = playerObj.GetComponent<PlayerInventory>();
            if (inventory != null && loseInventoryContainer != null)
            {
                PopulateEndGameInventory(loseInventoryContainer, inventory);
            }
        }

        Time.timeScale = 0f;
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        isPaused = false;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopSFX2D();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetStatistics();
        }

        SetPlayingHUDVisibility(true);

        if (winPanel != null) winPanel.style.display = DisplayStyle.None;
        if (losePanel != null) losePanel.style.display = DisplayStyle.None;
        if (pausePanel != null) pausePanel.style.display = DisplayStyle.None;

        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    private void QuitGame()
    {
        Debug.Log("[UIManager] Đang quay lại Menu chính...");
        
        Time.timeScale = 1f;
        isPaused = false;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopSFX2D();
        }

        if (winPanel != null) winPanel.style.display = DisplayStyle.None;
        if (losePanel != null) losePanel.style.display = DisplayStyle.None;
        if (pausePanel != null) pausePanel.style.display = DisplayStyle.None;

        HideBossHealthUI();

        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }

    private void Update()
    {
        UpdateDifficultyUI();
        HandlePauseInput();
    }

    private void HandlePauseInput()
    {
        bool escapePressed = false;
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            escapePressed = true;
        }
#else
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            escapePressed = true;
        }
#endif

        if (escapePressed)
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.style.display = DisplayStyle.None;
        SetPlayingHUDVisibility(true);
    }

    public void PauseGame()
    {
        // Không cho phép pause khi đã thắng hoặc thua
        if ((winPanel != null && winPanel.style.display == DisplayStyle.Flex) || 
            (losePanel != null && losePanel.style.display == DisplayStyle.Flex))
        {
            return;
        }

        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.style.display = DisplayStyle.Flex;
        SetPlayingHUDVisibility(false);
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        RebindUIElements();
        ResumeGame();

        string name = scene.name;
        if (name == "Menu" || name == "CharacterSelectScene" || name == "SplashScreen")
        {
            SetPlayingHUDVisibility(false);
        }
    }

    private void UpdateDifficultyUI()
    {
        if (GameManager.Instance == null) return;

        // Tự động bỏ qua nếu giao diện chưa được bạn thiết kế/gán ID
        if (difficultyPanel == null || difficultyLevelText == null || difficultyBarFill == null) return;

        string levelName = GameManager.Instance.GetDifficultyLevelName();
        float progress = GameManager.Instance.GetDifficultyProgress();

        // Cập nhật thanh tiến trình chạy đầy dần
        difficultyBarFill.style.width = new Length(progress, LengthUnit.Percent);

        // Thay đổi màu sắc chữ và thanh fill dựa trên từng cấp độ khó
        if (levelName == "EASY")
        {
            difficultyLevelText.text = "<color=#2ECC71>EASY</color>";
            difficultyBarFill.style.backgroundColor = new StyleColor(new Color(0.18f, 0.8f, 0.44f)); // Màu xanh lá #2ecc71
        }
        else if (levelName == "MEDIUM")
        {
            difficultyLevelText.text = "<color=#F1C40F>MEDIUM</color>";
            difficultyBarFill.style.backgroundColor = new StyleColor(new Color(0.95f, 0.77f, 0.06f)); // Màu vàng #f1c40f
        }
        else if (levelName == "HARD")
        {
            // Màu đỏ đậm (Dark Red) để phân biệt rõ với thanh máu đỏ tươi
            difficultyLevelText.text = "<color=#990000>HARD</color>";
            difficultyBarFill.style.backgroundColor = new StyleColor(new Color(0.6f, 0f, 0f)); // #990000
        }
        else if (levelName == "NIGHTMARE")
        {
            difficultyLevelText.text = "<color=#1F3A60>NIGHTMARE</color>";
            difficultyBarFill.style.backgroundColor = new StyleColor(new Color(0.12f, 0.23f, 0.38f)); // Xanh dương đậm #1f3a60
        }
        else if (levelName == "LUNATIC")
        {
            // Rainbow text cho LUNATIC (Mỗi chữ một màu sắc in hoa rực rỡ)
            difficultyLevelText.text = "<color=#FF0000>L</color><color=#FF7F00>U</color><color=#FFFF00>N</color><color=#00FF00>A</color><color=#00FFFF>T</color><color=#0080FF>I</color><color=#D800FF>C</color>";

            // Rainbow fill bar: Hoạt ảnh thay đổi màu sắc liên tục trong Lunatic
            float hue = (Time.unscaledTime * 0.25f) % 1f;
            Color rainbowColor = Color.HSVToRGB(hue, 0.85f, 0.85f);
            difficultyBarFill.style.backgroundColor = new StyleColor(rainbowColor);
        }
    }
}
