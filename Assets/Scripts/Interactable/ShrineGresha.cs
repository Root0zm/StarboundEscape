using UnityEngine;

public class ShrineGresha : MonoBehaviour, IInteractable
{
    [Header("Yêu cầu Hiến Tế")]
    [SerializeField] private string requiredItemID = "parallel_cross"; // ID vật phẩm bắt buộc để hiến tế

    [Header("Audio Config")]
    [SerializeField] private AudioClip sacrificeSound;

    private Animator anim;

    private void Start()
    {
        // Tự động lấy linh kiện Animator trên đền
        anim = GetComponent<Animator>();
    }

    /// <summary>
    /// Thực thi từ interface IInteractable. Được gọi khi Player tương tác với Đền Gresha.
    /// </summary>
    public void Interact()
    {
        // Dò tìm Player trong Scene
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("[Đền Gresha] Không tìm thấy đối tượng Player trong Scene!");
            return;
        }

        // Truy cập vào thành phần PlayerInventory của người chơi
        PlayerInventory playerInventory = playerObj.GetComponent<PlayerInventory>();
        if (playerInventory == null)
        {
            playerInventory = playerObj.GetComponentInParent<PlayerInventory>();
        }

        if (playerInventory == null)
        {
            Debug.LogError("[Đền Gresha] Không tìm thấy component PlayerInventory trên Player!");
            return;
        }

        // Rà soát kho đồ của người chơi
        int currentStack = playerInventory.GetItemStack(requiredItemID);

        // TRƯỜNG HỢP 1: Player không có vật phẩm hiến tế
        if (currentStack <= 0)
        {
            Debug.LogWarning("Bạn không có vật phẩm Parallel Cross để hiến tế tại Đền Gresha!");
            return; // Thoát hàm, không làm gì cả
        }

        // TRƯỜNG HỢP 2: Hợp lệ (Player có vật phẩm)
        // 1. Trừ đi đúng 1 lượng cộng dồn của vật phẩm trong kho đồ
        playerInventory.RemoveItemStackOne(requiredItemID);

        // 2. Kích hoạt hoạt ảnh thành công trên Đền
        if (anim != null)
        {
            anim.SetTrigger("InteractSuccess");
        }

        if (AudioManager.Instance != null && sacrificeSound != null)
        {
            AudioManager.Instance.PlaySFX3D(sacrificeSound, transform.position);
        }

        // 3. Gửi lệnh sang GameManager để tăng số lượng Boss cuối màn lên 1
        if (GameManager.Instance != null)
        {
            GameManager.Instance.IncreaseEndGameBossCount();
        }
        else
        {
            Debug.LogError("[Đền Gresha] Không tìm thấy GameManager trong màn chơi!");
        }

        // 4. In thông báo thành công ra Console
        Debug.Log("Hiến tế thành công! Sức mạnh của Gresha đã làm tăng số lượng Boss cuối màn!");
    }

    /// <summary>
    /// Trả về chuỗi văn bản hướng dẫn tương tác hiển thị trên UI
    /// </summary>
    public string GetInteractPrompt()
    {
        return " [E] Hiến tế Parallel Cross";
    }
}
