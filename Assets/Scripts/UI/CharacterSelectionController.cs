using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class CharacterSelectionController : MonoBehaviour
{
    [Header("UI Toolkit References")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private string leftOptionName = "LeftOption";
    [SerializeField] private string rightOptionName = "RightOption";

    [Header("Scene Settings")]
    [Tooltip("Tên của scene màn chơi đầu tiên để load sau khi chọn nhân vật")]
    [SerializeField] private string firstLevelSceneName = "Map 1";

    [Header("Audio Config")]
    [SerializeField] private AudioClip clickSound;

    private VisualElement leftOption;
    private VisualElement rightOption;

    private void OnEnable()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument != null)
        {
            VisualElement root = uiDocument.rootVisualElement;

            leftOption = root.Q<VisualElement>(leftOptionName);
            rightOption = root.Q<VisualElement>(rightOptionName);

            if (leftOption != null)
            {
                // Đăng ký sự kiện Click
                leftOption.RegisterCallback<ClickEvent>(evt => ChooseAstronaut());
                
                // Đăng ký hiệu ứng Hover
                leftOption.RegisterCallback<MouseEnterEvent>(evt => OnHoverEnter(leftOption));
                leftOption.RegisterCallback<MouseLeaveEvent>(evt => OnHoverLeave(leftOption));
            }

            if (rightOption != null)
            {
                // Đăng ký sự kiện Click
                rightOption.RegisterCallback<ClickEvent>(evt => ChooseBandit());
                
                // Đăng ký hiệu ứng Hover
                rightOption.RegisterCallback<MouseEnterEvent>(evt => OnHoverEnter(rightOption));
                rightOption.RegisterCallback<MouseLeaveEvent>(evt => OnHoverLeave(rightOption));
            }
        }
    }

    private void OnDisable()
    {
        if (leftOption != null)
        {
            leftOption.UnregisterCallback<ClickEvent>(evt => ChooseAstronaut());
            leftOption.UnregisterCallback<MouseEnterEvent>(evt => OnHoverEnter(leftOption));
            leftOption.UnregisterCallback<MouseLeaveEvent>(evt => OnHoverLeave(leftOption));
        }

        if (rightOption != null)
        {
            rightOption.UnregisterCallback<ClickEvent>(evt => ChooseBandit());
            rightOption.UnregisterCallback<MouseEnterEvent>(evt => OnHoverEnter(rightOption));
            rightOption.UnregisterCallback<MouseLeaveEvent>(evt => OnHoverLeave(rightOption));
        }
    }

    private void ChooseAstronaut()
    {
        SelectCharacter(0);
    }

    private void ChooseBandit()
    {
        SelectCharacter(1);
    }

    private void SelectCharacter(int index)
    {
        PlayClickSFX();
        
        // Lưu lựa chọn (0: Phi hành gia, 1: Bandit)
        PlayerPrefs.SetInt("SelectedCharacter", index);
        PlayerPrefs.Save();

        Debug.Log($"[CharacterSelection] Đã chọn nhân vật {index} ({(index == 0 ? "Phi hành gia" : "Bandit")})");

        // Load scene chơi chính
        if (!string.IsNullOrEmpty(firstLevelSceneName))
        {
            SceneManager.LoadScene(firstLevelSceneName);
        }
        else
        {
            SceneManager.LoadScene(1); // Fallback
        }
    }

    private void OnHoverEnter(VisualElement elem)
    {
        // Phóng to nhẹ và làm nổi bật màu nền của vùng chọn
        elem.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(50));
        elem.style.scale = new Scale(new Vector3(1.02f, 1.02f, 1f));
        elem.style.backgroundColor = new StyleColor(new Color(0.15f, 0.12f, 0.25f, 0.85f)); // Làm sáng nhẹ nền
    }

    private void OnHoverLeave(VisualElement elem)
    {
        // Trả về kích thước và màu mặc định
        elem.style.scale = new Scale(new Vector3(1f, 1f, 1f));
        elem.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.75f)); // Trả về màu nền gốc (0.75 opacity)
    }

    private void PlayClickSFX()
    {
        if (AudioManager.Instance != null && clickSound != null)
        {
            AudioManager.Instance.PlaySFX2D(clickSound);
        }
    }
}
