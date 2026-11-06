using UnityEngine;
using UnityEngine.SceneManagement;
#if UI_TOOLKIT_ACTIVE // Chỉ dùng nếu bạn kích hoạt UI Toolkit, mặc định code dưới sẽ chạy bằng UI Toolkit của Unity
#endif
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Toolkit Reference")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private string playButtonName = "Play_Btn";
    [SerializeField] private string quitButtonName = "Quit_Btn";

    [Header("Scene Transition Settings")]
    [Tooltip("Tên của scene màn chơi đầu tiên để load khi bấm Play")]
    [SerializeField] private string playSceneName = "CharacterSelectScene";

    [Header("Audio Config (Optional)")]
    [SerializeField] private AudioClip menuBGM;
    [SerializeField] private AudioClip clickSound;

    private Button playButton;
    private Button quitButton;

    private void OnEnable()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument != null)
        {
            VisualElement root = uiDocument.rootVisualElement;

            // Dò tìm các nút bấm theo tên ID thiết lập trong file UXML
            playButton = root.Q<Button>(playButtonName);
            quitButton = root.Q<Button>(quitButtonName);

            // Đăng ký sự kiện Click
            if (playButton != null) playButton.clicked += StartGame;
            if (quitButton != null) quitButton.clicked += QuitGame;
        }
    }

    private void OnDisable()
    {
        // Hủy đăng ký để tránh rò rỉ bộ nhớ
        if (playButton != null) playButton.clicked -= StartGame;
        if (quitButton != null) quitButton.clicked -= QuitGame;
    }

    private void Start()
    {
        // Phát nhạc nền Menu nếu có cấu hình
        if (AudioManager.Instance != null && menuBGM != null)
        {
            AudioManager.Instance.PlayBGM(menuBGM, fadeDuration: 1.0f);
        }
    }

    private void StartGame()
    {
        PlayClickSFX();

        // Đảm bảo thời gian chạy bình thường khi bắt đầu chơi
        Time.timeScale = 1f;

        // Reset lại các thông số thống kê của người chơi nếu GameManager đã tồn tại
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetStatistics();
        }

        // Tải màn chơi đầu tiên
        Debug.Log("[MainMenu] Đang khởi chạy trò chơi...");
        if (!string.IsNullOrEmpty(playSceneName))
        {
            SceneManager.LoadScene(playSceneName);
        }
        else
        {
            SceneManager.LoadScene(1); // Fallback
        }
    }

    private void QuitGame()
    {
        PlayClickSFX();
        Debug.Log("[MainMenu] Đang thoát game...");
        
        Application.Quit();

#if UNITY_EDITOR
        // Thoát chế độ Play Mode nếu chạy thử trong Editor
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void PlayClickSFX()
    {
        if (AudioManager.Instance != null && clickSound != null)
        {
            AudioManager.Instance.PlaySFX2D(clickSound);
        }
    }
}
