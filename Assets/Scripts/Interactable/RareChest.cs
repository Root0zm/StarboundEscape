using UnityEngine;

public class RareChest : MonoBehaviour, IInteractable
{
    [Header("Giá Mở Rương (Gacha Chest Cost)")]
    [SerializeField] private int baseOpenCost = 50; // Giá mở rương mặc định ban đầu

    public int CurrentOpenCost
    {
        get
        {
            if (GameManager.Instance != null)
            {
                float d = GameManager.Instance.gameDifficultyCoeff;
                int level = GameManager.Instance.CurrentLevel;
                return Mathf.RoundToInt(level * ((1f + d) / 2f) * baseOpenCost);
            }
            return baseOpenCost;
        }
    }

    [Header("Item Pools (Bể Gacha Vật Phẩm)")]
    [SerializeField] private ItemData[] rareItemPool;   // 70% tỷ lệ rơi trong RareChest
    [SerializeField] private ItemData[] epicItemPool;   // 30% tỷ lệ rơi trong RareChest
    [SerializeField] private GameObject itemDropPrefab; // Prefab Placeholder chung duy nhất

    [Header("Physics Settings (Bắn Vật Phẩm)")]
    [SerializeField] private float upwardForce = 6f; // Lực bắn lên trên
    [SerializeField] private float horizontalForceRange = 2f; // Phạm vi lực đẩy ngang (trái/phải)

    [Header("Audio Config")]
    [SerializeField] private AudioClip openSound;

    private bool isOpened = false; // Rương chỉ mở được duy nhất 1 lần

    void Start()
    {
    }

    /// <summary>
    /// Thực thi từ interface IInteractable. Được gọi khi Player đứng gần và nhấn phím tương tác.
    /// </summary>
    public void Interact()
    {
        if (isOpened) return;

        // Dò tìm thông số của Player trong Scene
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("[Gacha Chest] Không tìm thấy đối tượng Player trong Scene!");
            return;
        }

        PlayerStats playerStats = playerObj.GetComponent<PlayerStats>();
        if (playerStats == null)
        {
            playerStats = playerObj.GetComponentInParent<PlayerStats>();
        }

        if (playerStats == null)
        {
            Debug.LogError("[Gacha Chest] Không tìm thấy component PlayerStats trên Player!");
            return;
        }

        // Kiểm tra tiền của Player
        if (playerStats.Money < CurrentOpenCost)
        {
            Debug.Log($"[Gacha Chest] Không đủ tiền mở rương! Yêu cầu: {CurrentOpenCost} vàng. Bạn hiện có: {playerStats.Money} vàng.");
            return;
        }

        // Đủ tiền -> tiến hành mở rương
        if (playerStats.SpendMoney(CurrentOpenCost))
        {
            isOpened = true;

            // Kích hoạt hoạt ảnh mở rương trong Animator
            Animator anim = GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetTrigger("Open");
            }

            // Phát âm thanh mở rương
            if (AudioManager.Instance != null && openSound != null)
            {
                AudioManager.Instance.PlaySFX3D(openSound, transform.position);
            }

            // Sinh vật phẩm ngẫu nhiên
            SpawnGachaItem();
        }
    }

    /// <summary>
    /// Thuật toán quay số Gacha và nổ lực vật lý để bắn vật phẩm văng ra ngoài rương
    /// </summary>
    private void SpawnGachaItem()
    {
        int roll = Random.Range(1, 101); // Quay ngẫu nhiên từ 1 đến 100
        ItemData selectedItemData = null;

        // Tỷ lệ drop trong RareChest: Hiếm (70%) | Siêu hiếm (30%)
        if (roll <= 70)
        {
            if (rareItemPool != null && rareItemPool.Length > 0)
            {
                selectedItemData = rareItemPool[Random.Range(0, rareItemPool.Length)];
            }
        }
        else // roll > 70
        {
            if (epicItemPool != null && epicItemPool.Length > 0)
            {
                selectedItemData = epicItemPool[Random.Range(0, epicItemPool.Length)];
            }
        }

        if (selectedItemData != null && itemDropPrefab != null)
        {
            // Sinh ra vật thể Placeholder tại vị trí của Rương
            GameObject droppedObj = Instantiate(itemDropPrefab, transform.position, Quaternion.identity);

            // Bơm dữ liệu vật phẩm trúng thưởng sang script ItemDrop để tự đổi Sprite
            ItemDrop itemDropScript = droppedObj.GetComponent<ItemDrop>();
            if (itemDropScript == null) itemDropScript = droppedObj.GetComponentInChildren<ItemDrop>();
            if (itemDropScript != null)
            {
                itemDropScript.SetupItem(selectedItemData);
            }

            // Bắn vật phẩm bay ra bằng lực vật lý 2D
            Rigidbody2D rb = droppedObj.GetComponent<Rigidbody2D>();
            if (rb == null) rb = droppedObj.GetComponentInChildren<Rigidbody2D>();

            if (rb != null)
            {
                float forceX = Random.Range(-horizontalForceRange, horizontalForceRange);
                float forceY = Random.Range(upwardForce * 0.9f, upwardForce * 1.1f);

                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.linearVelocity = Vector2.zero; // Reset vận tốc trước đó
                rb.AddForce(new Vector2(forceX, forceY), ForceMode2D.Impulse);

                Debug.Log($"[Gacha Chest] Đã mở thành công! Sinh ra vật phẩm '{selectedItemData.itemName}' và phóng lên với lực vector ({forceX:F1}, {forceY:F1}).");
            }
            else
            {
                Debug.LogWarning($"[Gacha Chest] Vật phẩm sinh ra '{selectedItemData.itemName}' không có Rigidbody2D để tác dụng lực vật lý!");
            }
        }
        else
        {
            Debug.LogError("[Gacha Chest] Không thể chọn được vật phẩm hoặc chưa gán Item Drop Prefab!");
        }
    }

    /// <summary>
    /// Trả về chuỗi văn bản hướng dẫn tương tác hiển thị trên UI
    /// </summary>
    public string GetInteractPrompt()
    {
        if (isOpened) return "";
        return " [E] Mở Rương (" + CurrentOpenCost + " Vàng)";
    }
}
