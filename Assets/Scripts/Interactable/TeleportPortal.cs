using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Cần thiết nếu bạn dùng Text hoặc Slider để hiện thời gian
using TMPro;          // Dùng TextMeshPro cho chữ đẹp

public class TeleportPortal : MonoBehaviour, IInteractable  
{
    private enum PortalState { Idle, Charging, WaitingForBoss, Completed }
    [SerializeField] private PortalState currentState = PortalState.Idle;

    public bool IsCompleted => currentState == PortalState.Completed;

    [Header("Time Settings")]
    [SerializeField] private float chargeTime = 60f; // Thời gian sạc: 60 giây
    private float currentTime;

    [Header("Boss Spawn Settings")]
    [SerializeField] private GameObject bossPrefab;   // Kéo Prefab con Boss của MÀN NÀY vào đây
    [SerializeField] private Transform spawnPoint;    // Vị trí Boss sẽ xuất hiện
    [SerializeField] private float spawnAtSecond = 59f; // Sẽ gọi Boss xuất hiện khi đồng hồ đếm xuống mốc này
    [SerializeField] private Collider2D mapBoundaryCollider; // Collider giới hạn của Map
    [SerializeField] private LayerMask groundLayer;   // Layer đất cứng để kiểm tra độ cao khi spawn
    private List<GameObject> spawnedBosses = new List<GameObject>();
    private bool bossSpawned = false;

    [Header("Boss Reward Settings")]
    [SerializeField] private ItemData[] blueItemPool;   // Bể vật phẩm màu xanh để rơi khi Boss chết
    [SerializeField] private GameObject itemDropPrefab; // Prefab Placeholder dùng chung để rơi đồ

    [Header("UI References")]
    [SerializeField] private TMP_Text timerText;      // Kéo UI Text hiển thị số giây vào đây
    [SerializeField] private Slider chargeSlider;     // Kéo UI Slider thanh sạc vào đây (nếu có)

    [Header("Teleport Audio Config")]
    [SerializeField] private AudioClip activationSound;
    [SerializeField] private AudioClip portalCompletedSound;

    [Header("Animation Settings")]
    private Animator portalAnim;
    void Start()
    {
        portalAnim = GetComponent<Animator>();
        currentTime = chargeTime;
        UpdateUI();
        if (portalAnim != null) portalAnim.SetInteger("portalState", 0);
        if (chargeSlider != null)
        {
            chargeSlider.maxValue = chargeTime;
            chargeSlider.value = chargeTime;
        }
    }

    void Update()
    {
        // 2. LOGIC ĐẾM NGƯỢC KHI ĐANG SẠC
        if (currentState == PortalState.Charging)
        {
            currentTime -= Time.deltaTime;

            // Kiểm tra mốc thời gian để Spawn Boss
            if (!bossSpawned && currentTime <= spawnAtSecond)
            {
                SpawnBoss();
            }

            // Chặn thời gian lại ở mốc 1 giây
            if (currentTime <= 1f)
            {
                currentTime = 1f;
                currentState = PortalState.WaitingForBoss;
            }

            UpdateUI();

            // Cập nhật giao diện % sạc của cổng dịch chuyển
            if (UIManager.Instance != null)
            {
                float progress = (1f - (currentTime / chargeTime)) * 100f;
                UIManager.Instance.UpdateTeleportProgress(progress);
            }

            // Nếu boss đã xuất hiện và bị tiêu diệt trước khi sạc xong, chuyển nhạc nền về bình thường ngay lập tức
            if (bossSpawned && IsBossDead())
            {
                if (AudioManager.Instance != null && GameManager.Instance != null && GameManager.Instance.levelNormalBGM != null)
                {
                    AudioManager.Instance.PlayBGM(GameManager.Instance.levelNormalBGM);
                }
            }
        }

        // 3. LOGIC CHỜ BOSS BỊ TIÊU DIỆT TẠI MỐC 1 GIÂY
        if (currentState == PortalState.WaitingForBoss)
        {
            // Kiểm tra xem toàn bộ Boss đã chết hoặc bị xóa khỏi Map chưa
            if (spawnedBosses.Count == 0 || IsBossDead())
            {
                // Rơi vật phẩm màu xanh ngẫu nhiên từ Boss
                DropBossItem();

                currentState = PortalState.Completed;
                currentTime = 0f;
                UpdateUI();
                OnPortalFullyCharged();

                // Ẩn giao diện UI sạc cổng dịch chuyển
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.HideTeleportUI();
                }
            }
            else
            {
                // Giữ mức hiển thị % tương ứng với thời gian chờ
                if (UIManager.Instance != null)
                {
                    float progress = (1f - (currentTime / chargeTime)) * 100f;
                    UIManager.Instance.UpdateTeleportProgress(progress);
                }
            }
        }
    }

    public void Interact()
    {
        Debug.Log($"[TeleportPortal] Người chơi nhấn tương tác! Trạng thái hiện tại của cổng: {currentState}");
        if (currentState == PortalState.Idle)
        {
            StartCharging();
        }
        else if (currentState == PortalState.Completed)
        {
            TeleportToNextLevel();
        }
    }

    private void TeleportToNextLevel()
    {
        Debug.Log("[TeleportPortal] Bắt đầu kích hoạt quá trình chuyển sang màn tiếp theo...");
        if (GameManager.Instance != null)
        {
            // 1. Lưu dữ liệu người chơi hiện tại vào GameManager trước khi tải màn tiếp theo
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                GameManager.Instance.SavePlayerData(playerObj);
            }
            else
            {
                Debug.LogWarning("[TeleportPortal] Cảnh báo: Không tìm thấy đối tượng có Tag 'Player' để lưu dữ liệu!");
            }

            // 2. Chuyển cảnh sang màn chơi tiếp theo
            GameManager.Instance.LoadNextLevel();
        }
        else
        {
            Debug.LogError("[TeleportPortal] Lỗi nghiêm trọng: GameManager.Instance bằng NULL, không thể qua màn!");
        }
    }

    private void StartCharging()
    {
        currentState = PortalState.Charging;
        if (portalAnim != null) portalAnim.SetInteger("portalState", 1);
        Debug.Log("Cổng dịch chuyển bắt đầu sạc! Hãy chuẩn bị chiến đấu!");

        // Khởi chạy hiển thị tiến trình sạc trên UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowTeleportUI();
            UIManager.Instance.UpdateTeleportProgress(0f);
        }

        // Phát tiếng kích hoạt cổng
        if (AudioManager.Instance != null && activationSound != null)
        {
            AudioManager.Instance.PlaySFX3D(activationSound, transform.position);
        }
    }

    private void SpawnBoss()
    {
        bossSpawned = true;
        if (bossPrefab != null && spawnPoint != null)
        {
            int bossCount = 1;
            if (GameManager.Instance != null)
            {
                bossCount = GameManager.Instance.totalEndGameBosses;
            }

            List<EnemyController> bossControllers = new List<EnemyController>();

            for (int i = 0; i < bossCount; i++)
            {
                // Dịch chuyển ngang nhẹ 2.5m cho các con Boss tiếp theo để tránh chồng lấn vị trí
                float offsetX = i * 2.5f;
                Vector3 spawnPos = spawnPoint.position + new Vector3(offsetX, 0f, 0f);
                Vector3 safeSpawnPos = GetSafeSpawnPosition(spawnPos);

                GameObject bossObj = Instantiate(bossPrefab, safeSpawnPos, spawnPoint.rotation);
                spawnedBosses.Add(bossObj);

                EnemyController ec = bossObj.GetComponent<EnemyController>();
                if (ec == null) ec = bossObj.GetComponentInParent<EnemyController>();
                if (ec != null)
                {
                    bossControllers.Add(ec);
                }
            }

            Debug.Log($"[TeleportPortal] Cổng dịch chuyển đã spawn {bossCount} Boss!");

            // Bắt đầu hiển thị thanh máu gộp của Boss lên giao diện UI Toolkit
            if (UIManager.Instance != null && bossControllers.Count > 0)
            {
                UIManager.Instance.StartBossFight(bossControllers);
            }

            // Chuyển nhạc nền sang nhạc đánh Boss kịch tính
            if (AudioManager.Instance != null && GameManager.Instance != null && GameManager.Instance.bossFightBGM != null)
            {
                AudioManager.Instance.PlayBGM(GameManager.Instance.bossFightBGM);
            }
        }
    }

    private float GetSpawnOffsetY(GameObject prefab)
    {
        if (prefab == null) return 1.0f;

        Collider2D col = prefab.GetComponent<Collider2D>();
        if (col == null) col = prefab.GetComponentInChildren<Collider2D>();

        if (col != null)
        {
            float scaleY = Mathf.Abs(prefab.transform.localScale.y);
            if (col is BoxCollider2D box)
            {
                return (box.size.y * 0.5f - box.offset.y) * scaleY;
            }
            else if (col is CapsuleCollider2D capsule)
            {
                return (capsule.size.y * 0.5f - capsule.offset.y) * scaleY;
            }
            else if (col is CircleCollider2D circle)
            {
                return (circle.radius - circle.offset.y) * scaleY;
            }

            if (col.bounds.extents.y > 0f)
            {
                return col.bounds.extents.y;
            }
        }

        return 1.0f;
    }

    private Vector3 GetSafeSpawnPosition(Vector3 proposedPosition)
    {
        Vector3 safePosition = proposedPosition;

        // 1. Kiểm tra giới hạn map bằng mapBoundaryCollider
        if (mapBoundaryCollider != null)
        {
            // Nếu vị trí proposedPosition nằm ngoài vùng bao quanh (bounds) của Collider
            if (!mapBoundaryCollider.bounds.Contains(proposedPosition))
            {
                // Tìm điểm trên bề mặt giới hạn gần điểm đề xuất nhất
                Vector3 closestPoint = mapBoundaryCollider.ClosestPoint(proposedPosition);

                // Tính toán hướng từ điểm biên đó hướng vào tâm map dọc theo trục X
                float directionX = mapBoundaryCollider.bounds.center.x - closestPoint.x;
                float shiftDirectionX = directionX >= 0f ? 1f : -1f;

                // Tọa độ mới dịch chuyển vào trong map 1.5 mét theo trục X
                safePosition = new Vector3(closestPoint.x + shiftDirectionX * 1.5f, proposedPosition.y, proposedPosition.z);

                Debug.LogWarning($"[Spawning] Vị trí đề xuất {proposedPosition} nằm ngoài biên bản đồ! Đã điều chỉnh lùi vào trong: {safePosition}");
            }
        }

        // 2. Kiểm tra mặt đất sàn nhà (Bắn Raycast từ trên cao 5m xuống dưới 15m để tìm mặt đất chính xác)
        Vector2 rayStart = new Vector2(safePosition.x, safePosition.y + 5f);
        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, 15f, groundLayer);
        if (hit.collider != null)
        {
            // Đặt Y của Boss dựa trên chiều cao thực tế của collider
            float offset = GetSpawnOffsetY(bossPrefab);
            safePosition.y = hit.point.y + offset + 0.1f;
            Debug.Log($"[Spawning] Phát hiện sàn đất cứng tại Y = {hit.point.y}. Đã hạ Boss cách đất {offset + 0.1f}m (Y = {safePosition.y}) để rơi xuống tự nhiên.");
        }
        else
        {
            Debug.LogWarning($"[Spawning] Bắn tia Raycast từ Y={rayStart.y} xuống dưới nhưng không chạm đất sàn. Giữ nguyên tọa độ Y gốc.");
        }

        return safePosition;
    }

    private bool IsBossDead()
    {
        if (spawnedBosses.Count == 0) return true;

        foreach (GameObject bossObj in spawnedBosses)
        {
            if (bossObj != null)
            {
                // Tìm kiếm lớp cơ sở EnemyController ở root, con, hoặc cha
                EnemyController enemyScript = bossObj.GetComponent<EnemyController>();
                if (enemyScript == null) enemyScript = bossObj.GetComponentInChildren<EnemyController>();
                if (enemyScript == null) enemyScript = bossObj.GetComponentInParent<EnemyController>();

                if (enemyScript != null)
                {
                    if (enemyScript.IsBoss() && !enemyScript.IsDead)
                    {
                        return false; // Còn ít nhất một con Boss chưa chết
                    }
                }
                else
                {
                    // Tránh trường hợp bossObj vẫn tồn tại nhưng chưa nạp kịp script
                    return false;
                }
            }
        }
        return true; // Tất cả các con Boss đã bị tiêu diệt
    }

    /// <summary>
    /// Sinh vật phẩm phẩm cấp xanh rơi ra từ Boss khi bị tiêu diệt
    /// </summary>
    private void DropBossItem()
    {
        // Xác định vị trí rơi đồ (Mặc định rơi ở Cổng nếu không tìm thấy vị trí Boss)
        Vector3 dropPosition = transform.position;
        foreach (GameObject bossObj in spawnedBosses)
        {
            if (bossObj != null)
            {
                dropPosition = bossObj.transform.position;
                break;
            }
        }

        if (blueItemPool == null || blueItemPool.Length == 0)
        {
            Debug.LogWarning("[TeleportPortal] Bể vật phẩm màu xanh của Boss đang trống! Không thể rơi đồ.");
            return;
        }

        if (itemDropPrefab == null)
        {
            Debug.LogError("[TeleportPortal] Chưa gán itemDropPrefab trong Inspector!");
            return;
        }

        // Chọn ngẫu nhiên 1 ItemData màu xanh
        ItemData selectedItem = blueItemPool[Random.Range(0, blueItemPool.Length)];

        if (selectedItem != null)
        {
            // Sinh ra vật thể Placeholder
            GameObject droppedObj = Instantiate(itemDropPrefab, dropPosition, Quaternion.identity);

            // Bơm dữ liệu vật phẩm trúng thưởng sang script ItemDrop
            ItemDrop itemDropScript = droppedObj.GetComponent<ItemDrop>();
            if (itemDropScript == null) itemDropScript = droppedObj.GetComponentInChildren<ItemDrop>();
            if (itemDropScript != null)
            {
                itemDropScript.SetupItem(selectedItem);
            }

            // Tác dụng lực văng nhẹ cho vật phẩm bay lên
            Rigidbody2D rb = droppedObj.GetComponent<Rigidbody2D>();
            if (rb == null) rb = droppedObj.GetComponentInChildren<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.linearVelocity = Vector2.zero; // Reset vận tốc trước đó
                rb.AddForce(new Vector2(Random.Range(-1.5f, 1.5f), 5f), ForceMode2D.Impulse);
            }

            Debug.Log($"[TeleportPortal] Boss đã bị tiêu diệt! Rơi ra vật phẩm màu xanh: '{selectedItem.itemName}' tại vị trí {dropPosition}.");
        }
    }

    private void OnPortalFullyCharged()
    {
        if (portalAnim != null) portalAnim.SetInteger("portalState", 2);
        Debug.Log("CỔNG DỊCH CHUYỂN ĐÃ SẠC XONG XUÔI! BẠN CÓ THỂ QUA MÀN!");

        // Phát tiếng sạc hoàn tất cổng
        if (AudioManager.Instance != null && portalCompletedSound != null)
        {
            AudioManager.Instance.PlaySFX3D(portalCompletedSound, transform.position);
        }

        // Chuyển nhạc nền trở lại nhạc thường màn chơi
        if (AudioManager.Instance != null && GameManager.Instance != null && GameManager.Instance.levelNormalBGM != null)
        {
            AudioManager.Instance.PlayBGM(GameManager.Instance.levelNormalBGM);
        }
    }

    private void UpdateUI()
    {
        if (timerText != null)
        {
            // Hiển thị dạng số nguyên làm tròn lên (ví dụ: 89.4s sẽ hiện là 90)
            timerText.text = Mathf.CeilToInt(currentTime).ToString() + "s";
        }

        if (chargeSlider != null)
        {
            chargeSlider.value = currentTime;
        }
    }

    // --- PHẦN QUÉT VÙNG VA CHẠM ĐỂ HIỆN CHỮ "ẤN E ĐỂ SẠC" ---
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("Nhấn E để kích hoạt Cổng dịch chuyển");
        }
    }

    /// <summary>
    /// Trả về chuỗi văn bản hướng dẫn tương tác hiển thị trên UI
    /// </summary>
    public string GetInteractPrompt()
    {
        if (currentState == PortalState.Idle)
        {
            return " [E] Kích hoạt cổng Dịch Chuyển";
        }
        else if (currentState == PortalState.Completed)
        {
            return " [E] Qua màn chơi tiếp theo";
        }
        return "";
    }
}