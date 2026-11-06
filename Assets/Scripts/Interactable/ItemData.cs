using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Roguelike/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Định danh vật phẩm (Trùng với ID hiệu ứng)")]
    public string itemID; // Dạng chuỗi viết liền không dấu, ví dụ: "little_nova"
    
    [Header("Tên vật phẩm hiển thị")]
    public string itemName;
    
    [Header("Mô tả vật phẩm")]
    [TextArea(3, 10)]
    public string description;
    
    [Header("Icon vật phẩm")]
    public Sprite itemIcon;

    // Phân loại phẩm cấp của vật phẩm
    public enum ItemRarity { White, Blue, Red, Special }
    
    [Header("Độ hiếm")]
    public ItemRarity rarity;

    [Header("Âm thanh nhặt đồ riêng biệt (Để trống sẽ dùng tiếng mặc định)")]
    public AudioClip pickupSound;
}
