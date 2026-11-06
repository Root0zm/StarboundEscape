using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using System.Collections;

public class SplashScreenController : MonoBehaviour
{
    [Header("Scene Transition Settings")]
    [Tooltip("Tên của scene tiếp theo cần tải (thường là scene Menu)")]
    [SerializeField] private string nextSceneName = "Menu";
    
    [Tooltip("Thời gian hiển thị màn hình giới thiệu (giây)")]
    [SerializeField] private float displayDuration = 3f;

    [Header("UI Toolkit (Recommended)")]
    [Tooltip("Kéo UIDocument của SplashScreen vào đây")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Canvas UI (Optional Fallback)")]
    [Tooltip("Kéo CanvasGroup của Logo/Nội dung vào đây nếu sử dụng UGUI truyền thống")]
    [SerializeField] private CanvasGroup logoCanvasGroup;

    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("Audio Settings (Optional)")]
    [Tooltip("Âm thanh/Nhạc hiệu phát khi hiện logo")]
    [SerializeField] private AudioClip introSound;

    private bool isTransitioning = false;
    private VisualElement rootElement;

    private void Start()
    {
        // Ẩn/Hiện con trỏ chuột trong màn hình intro
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;

        if (uiDocument != null)
        {
            rootElement = uiDocument.rootVisualElement;
        }

        // Bắt đầu chuỗi hiệu ứng Splash Screen
        StartCoroutine(SplashSequence());
    }

    private void Update()
    {
        // Nhấn phím bất kỳ hoặc click chuột/chạm màn hình bằng Input System để bỏ qua màn hình giới thiệu (Skip)
        bool skipTriggered = false;

        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            skipTriggered = true;
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            skipTriggered = true;
        }
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            skipTriggered = true;
        }

        if (!isTransitioning && skipTriggered)
        {
            StartCoroutine(TransitionToNextScene());
        }
    }

    private IEnumerator SplashSequence()
    {
        // Chuẩn bị trạng thái ban đầu của logo (ẩn đi)
        SetOpacity(0f);

        // Phát âm thanh intro nếu có
        if (introSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX2D(introSound);
        }

        // 1. Hiệu ứng Fade In
        if (fadeInDuration > 0)
        {
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                SetOpacity(Mathf.Clamp01(elapsed / fadeInDuration));
                yield return null;
            }
            SetOpacity(1f);
        }
        else
        {
            SetOpacity(1f);
        }

        // 2. Chờ hiển thị theo thời gian thiết lập
        yield return new WaitForSeconds(displayDuration);

        // 3. Hiệu ứng Fade Out và chuyển cảnh
        yield return StartCoroutine(TransitionToNextScene());
    }

    private IEnumerator TransitionToNextScene()
    {
        if (isTransitioning) yield break;
        isTransitioning = true;

        // Hiệu ứng Fade Out
        if (fadeOutDuration > 0)
        {
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                SetOpacity(Mathf.Clamp01(1f - (elapsed / fadeOutDuration)));
                yield return null;
            }
            SetOpacity(0f);
        }

        // Tải scene tiếp theo
        Debug.Log($"[SplashScreen] Đang chuyển sang scene: {nextSceneName}");
        SceneManager.LoadScene(nextSceneName);
    }

    private void SetOpacity(float alpha)
    {
        // Set opacity cho UI Toolkit
        if (rootElement != null)
        {
            rootElement.style.opacity = alpha;
        }

        // Set opacity cho UGUI CanvasGroup
        if (logoCanvasGroup != null)
        {
            logoCanvasGroup.alpha = alpha;
        }
    }
}
