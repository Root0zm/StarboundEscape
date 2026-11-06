using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    [Header("Cài đặt sát thương")]
    public int damageAmount = 10;

    [Header("Giao diện thông báo tại chỗ")]
    public GameObject trapCanvasObject; // Ô để kéo TrapCanvas vào
    public float displayDuration = 1.5f; // Thời gian hiện chữ sau khi dẫm trúng

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Gọi hàm trừ máu của Player (Đổi thành tên script của bạn nếu bị lỗi đỏ)
            PlayerController player = collision.GetComponent<PlayerController>();

            if (player != null)
            {
                player.ChangeHealth(-damageAmount); // Hoặc player.TakeDamage(damageAmount);

                // Hiện dòng chữ thông báo ngay tại bụi gai
                if (trapCanvasObject != null)
                {
                    CancelInvoke("HideMessage");
                    trapCanvasObject.SetActive(true);

                    // Sau đúng displayDuration giây thì tự động ẩn chữ đi
                    Invoke("HideMessage", displayDuration);
                }

                Debug.Log("Player dẫm bẫy, hiện chữ tại chỗ!");
            }
        }
    }

    // Hàm thực hiện ẩn chữ
    private void HideMessage()
    {
        if (trapCanvasObject != null)
        {
            trapCanvasObject.SetActive(false);
        }
    }
}