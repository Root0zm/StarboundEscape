using UnityEngine;

public class ItemDrop : MonoBehaviour
{
    [Header("Dữ liệu Vật Phẩm")]
    [SerializeField] private ItemData itemData; // Biến private chứa dữ liệu và hiển thị trên Inspector của Prefab

    [Header("Cấu hình Nhặt Đồ")]
    [SerializeField] private float pickupDelay = 1.0f; // Thời gian chờ (giây) trước khi có thể nhặt đồ
    private float delayTimer;

    private SpriteRenderer spriteRenderer;

    // Getter công khai để lấy dữ liệu ItemData
    public ItemData ItemDataReference => itemData;

    private void Start()
    {
        // Khởi tạo bộ đếm thời gian chờ nhặt đồ
        delayTimer = pickupDelay;
    }

    private void Update()
    {
        // Giảm thời gian chờ theo thời gian thực
        if (delayTimer > 0f)
        {
            delayTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// Hàm thiết lập dữ liệu và tự động gán Sprite của vật phẩm ngoài Map
    /// </summary>
    public void SetupItem(ItemData data)
    {
        this.itemData = data;
        
        // Tự động lấy linh kiện và ép ảnh Icon của Item vào SpriteRenderer ngoài Map
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer != null && data != null)
        {
            spriteRenderer.sprite = data.itemIcon;
        }
    }

    /// <summary>
    /// Xử lý va chạm khi người chơi đi ngang qua để nhặt đồ (Khi vừa bước vào vùng va chạm)
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (delayTimer > 0f) return; // Chưa hết thời gian chờ -> Không cho nhặt
        TryPickup(collision);
    }

    /// <summary>
    /// Xử lý va chạm duy trì (Để phòng trường hợp Player đứng chờ sẵn tại vị trí rương)
    /// </summary>
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (delayTimer > 0f) return; // Chưa hết thời gian chờ -> Không cho nhặt
        TryPickup(collision);
    }

    /// <summary>
    /// Thực hiện logic nhặt đồ và nạp vào túi đồ của Player
    /// </summary>
    private void TryPickup(Collider2D collision)
    {
        // Kiểm tra đối tượng va chạm có tag là Player
        if (collision.CompareTag("Player"))
        {
            PlayerInventory inventory = collision.GetComponent<PlayerInventory>();
            if (inventory == null)
            {
                inventory = collision.GetComponentInParent<PlayerInventory>();
            }

            // Nếu tìm thấy PlayerInventory và dữ liệu vật phẩm hợp lệ
            if (inventory != null && itemData != null)
            {
                inventory.AddItem(itemData);
                Debug.Log($"[Túi Đồ] Đã nhặt thành công: {itemData.itemName}");
                
                // Xóa vật thể khỏi Map để tránh nhặt lại
                Destroy(gameObject);
            }
        }
    }
}
