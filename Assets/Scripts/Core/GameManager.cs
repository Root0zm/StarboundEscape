using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SavedItem
{
    public string id;
    public int count;
}

[System.Serializable]
public class SavedPlayerData
{
    public float health;
    public int gold;
    public List<SavedItem> items = new List<SavedItem>();
    public bool hasSavedData;
}

public class GameManager : MonoBehaviour
{

    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<GameManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    _instance = go.AddComponent<GameManager>();
                    DontDestroyOnLoad(go);
                    Debug.Log("[GameManager] Tự động khởi tạo GameManager mới trong Scene!");
                }
            }
            return _instance;
        }
    }

    [Header("End Game Settings")]
    public int totalEndGameBosses = 1; // Số lượng Boss cuối màn mặc định ban đầu là 1

    [Header("Saved Player Data")]
    public SavedPlayerData savedData = new SavedPlayerData { hasSavedData = false };

    [Header("Dynamic Difficulty Scaling")]
    public float gameDifficultyCoeff = 0.3f; // Hệ số độ khó D
    public float difficultyTimer = 0f;       // Bộ đếm thời gian chơi thực tế (giây)

    public int CurrentLevel => levelsCompletedCount + 1; // Màn chơi hiện tại (bắt đầu từ 1)

    public float GetDifficultyProgress()
    {
        float timeMin = difficultyTimer / 60f;
        if (timeMin < 12f)
        {
            return (difficultyTimer % 180f) / 180f * 100f;
        }
        else
        {
            return (difficultyTimer % 60f) / 60f * 100f;
        }
    }

    public string GetDifficultyLevelName()
    {
        float timeMin = difficultyTimer / 60f;
        if (timeMin < 3f) return "EASY";
        if (timeMin < 6f) return "MEDIUM";
        if (timeMin < 9f) return "HARD";
        if (timeMin < 12f) return "NIGHTMARE";
        return "LUNATIC";
    }

    [Header("Game Manager Audio Config")]
    public AudioClip levelNormalBGM;         // Nhạc nền mặc định (nếu không cấu hình danh sách nhạc màn)
    public List<AudioClip> levelBGMs;        // Danh sách nhạc nền tương ứng cho từng màn (Màn 1 = phần tử 0, Màn 2 = phần tử 1,...)
    public AudioClip bossFightBGM;
    public AudioClip victorySound;
    public AudioClip defeatSound;

    [Header("Statistics Tracking")]
    public float gameStartTime;
    public int enemiesKilledCount = 0;
    public int bossesKilledCount = 0;
    public int playerDeathCount = 0;
    public int totalGoldCollected = 0;
    public int totalItemsCollected = 0;
    public int levelsCompletedCount = 0;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (_instance != this) return;

        ResetStatistics();

        // Kiểm tra nếu đang ở Scene Menu thì không tự động phát nhạc màn chơi
        string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        int activeSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        if (activeSceneIndex == 0 || activeSceneName.ToLower().Contains("menu"))
        {
            return;
        }

        // Tự động phát nhạc nền màn chơi lần đầu tiên khi GameManager vừa được khởi tạo ở cảnh chơi game
        if (AudioManager.Instance != null)
        {
            AudioClip clipToPlay = GetCurrentLevelBGM();
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null && clipToPlay != null)
            {
                AudioManager.Instance.PlayBGM(clipToPlay);
            }
        }
    }

    private void Update()
    {
        if (Time.timeScale > 0f)
        {
            difficultyTimer += Time.deltaTime;
            UpdateDifficulty();
        }
    }

    private void UpdateDifficulty()
    {
        float timeMin = difficultyTimer / 60f;
        if (timeMin < 3f)
        {
            gameDifficultyCoeff = 0.3f;
        }
        else if (timeMin < 6f)
        {
            gameDifficultyCoeff = 0.5f;
        }
        else if (timeMin < 9f)
        {
            gameDifficultyCoeff = 0.7f;
        }
        else if (timeMin < 12f)
        {
            gameDifficultyCoeff = 1.0f;
        }
        else
        {
            // Tăng 0.1 mỗi 1 phút sau khi đạt Lunatic (12 phút)
            gameDifficultyCoeff = 1.0f + 0.1f * (timeMin - 12f);
        }
    }

    public void ResetStatistics()
    {
        gameStartTime = Time.time;
        enemiesKilledCount = 0;
        bossesKilledCount = 0;
        playerDeathCount = 0;
        totalGoldCollected = 0;
        totalItemsCollected = 0;
        levelsCompletedCount = 0;
        totalEndGameBosses = 1;
        savedData = new SavedPlayerData { hasSavedData = false };
        difficultyTimer = 0f;
        gameDifficultyCoeff = 0.3f;

    }

    private void OnEnable()
    {
        if (_instance == null || _instance == this)
        {
            // Đăng ký sự kiện nạp Scene thành công
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    private void OnDisable()
    {
        // Hủy đăng ký sự kiện tránh rò rỉ bộ nhớ
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Kiểm tra nếu Scene vừa tải là Scene Menu thì không phát nhạc màn chơi chính
        if (scene.buildIndex == 0 || scene.name.ToLower().Contains("menu"))
        {
            return;
        }

        // Tự động tìm người chơi trong Scene mới để phục hồi dữ liệu
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            RestorePlayerData(playerObj);
        }

        // Tự động phát nhạc nền màn chơi khi nạp cảnh mới
        if (AudioManager.Instance != null)
        {
            AudioClip clipToPlay = GetCurrentLevelBGM();
            if (clipToPlay != null)
            {
                AudioManager.Instance.PlayBGM(clipToPlay);
            }
        }
    }

    /// <summary>
    /// Lấy bản nhạc nền tương ứng với màn chơi hiện tại dựa trên số màn đã hoàn thành.
    /// </summary>
    private AudioClip GetCurrentLevelBGM()
    {
        // levelsCompletedCount: màn 1 = 0, màn 2 = 1, màn 3 = 2,...
        int index = levelsCompletedCount;
        if (levelBGMs != null && index >= 0 && index < levelBGMs.Count && levelBGMs[index] != null)
        {
            return levelBGMs[index];
        }
        return levelNormalBGM; // Fallback về nhạc mặc định nếu không tìm thấy trong list
    }

    /// <summary>
    /// Hàm tăng số lượng Boss xuất hiện cuối màn lên 1 (Được gọi bởi Đền Gresha)
    /// </summary>
    public void IncreaseEndGameBossCount()
    {
        totalEndGameBosses++;
        Debug.Log($"[GameManager] Số lượng Boss cuối màn tăng lên! Tổng số Boss hiện tại: {totalEndGameBosses}");
    }

    /// <summary>
    /// Lưu thông tin trạng thái của người chơi trước khi chuyển màn (Hỗ trợ đa lớp bằng Reflection)
    /// </summary>
    public void SavePlayerData(GameObject playerObj)
    {
        if (playerObj == null)
        {
            return;
        }

        savedData.items = new List<SavedItem>();
        var allComponents = playerObj.GetComponents<Component>();

        // 1. Lưu các vật phẩm bằng Reflection (hỗ trợ PlayerInventory và PlayerInventoryMap2)
        foreach (var comp in allComponents)
        {
            if (comp == null) continue;
            var method = comp.GetType().GetMethod("GetOwnedItems");
            if (method != null)
            {
                var owned = method.Invoke(comp, null) as Dictionary<string, int>;
                if (owned != null)
                {
                    foreach (var pair in owned)
                    {
                        if (pair.Value > 0)
                        {
                            savedData.items.Add(new SavedItem { id = pair.Key, count = pair.Value });
                        }
                    }
                }
                break;
            }
        }

        // 2. Lưu lượng máu bằng Reflection (hỗ trợ PlayerController, PlayerControllerMap2, PlayerStats)
        float playerHealth = 100f;
        bool healthFound = false;
        foreach (var comp in allComponents)
        {
            if (comp == null) continue;
            var type = comp.GetType();
            
            var prop = type.GetProperty("Health") ?? type.GetProperty("health") ?? type.GetProperty("CurrentHealth") ?? type.GetProperty("Hp");
            if (prop != null)
            {
                var val = prop.GetValue(comp);
                if (val is float f) { playerHealth = f; healthFound = true; break; }
                if (val is int i) { playerHealth = i; healthFound = true; break; }
            }
            
            var field = type.GetField("currentHealth") ?? type.GetField("health") ?? type.GetField("hp");
            if (field != null)
            {
                var val = field.GetValue(comp);
                if (val is float f) { playerHealth = f; healthFound = true; break; }
                if (val is int i) { playerHealth = i; healthFound = true; break; }
            }
        }
        
        if (healthFound)
        {
            savedData.health = playerHealth;
        }

        // 3. Reset tiền vàng về 0 khi qua màn chơi mới để cân bằng game
        foreach (var comp in allComponents)
        {
            if (comp == null) continue;
            var type = comp.GetType();
            
            var prop = type.GetProperty("gold") ?? type.GetProperty("goldCount") ?? type.GetProperty("Money") ?? type.GetProperty("money");
            if (prop != null && prop.CanWrite)
            {
                try { prop.SetValue(comp, 0); } catch {}
            }
            
            var field = type.GetField("gold") ?? type.GetField("goldCount") ?? type.GetField("money");
            if (field != null)
            {
                try { field.SetValue(comp, 0); } catch {}
            }
        }
        savedData.gold = 0;

        savedData.hasSavedData = true;

        Debug.Log($"[GameManager] Đã lưu dữ liệu người chơi qua màn: HP={savedData.health}, Vàng (Reset)={savedData.gold}, Số loại items={savedData.items.Count}");
    }

    /// <summary>
    /// Khôi phục trạng thái máu, vàng và vật phẩm của người chơi khi nạp màn chơi mới (Hỗ trợ đa lớp bằng Reflection)
    /// </summary>
    public void RestorePlayerData(GameObject playerObj)
    {
        if (playerObj == null || !savedData.hasSavedData) return;

        var allComponents = playerObj.GetComponents<Component>();

        // 2. Phục hồi tiền vàng (đã reset về 0)
        foreach (var comp in allComponents)
        {
            if (comp == null) continue;
            var type = comp.GetType();
            
            var prop = type.GetProperty("gold") ?? type.GetProperty("goldCount") ?? type.GetProperty("Money") ?? type.GetProperty("money");
            if (prop != null && prop.CanWrite)
            {
                try { prop.SetValue(comp, savedData.gold); } catch {}
            }
            
            var field = type.GetField("gold") ?? type.GetField("goldCount") ?? type.GetField("money");
            if (field != null)
            {
                try { field.SetValue(comp, savedData.gold); } catch {}
            }
        }
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdatePlayerGold(savedData.gold);
        }

        // 3. Phục hồi lượng máu (Hồi đầy máu khi chuyển sang màn chơi mới)
        foreach (var comp in allComponents)
        {
            if (comp == null) continue;
            var type = comp.GetType();
            
            float maxH = 100f;
            bool maxFound = false;
            
            var maxProp = type.GetProperty("MaxHealth") ?? type.GetProperty("maxHealth") ?? type.GetProperty("MaxHp") ?? type.GetProperty("maxHp");
            if (maxProp != null)
            {
                var val = maxProp.GetValue(comp);
                if (val is float f) { maxH = f; maxFound = true; }
                if (val is int i) { maxH = i; maxFound = true; }
            }
            
            var maxField = type.GetField("maxHealth") ?? type.GetField("MaxHealth") ?? type.GetField("maxHp") ?? type.GetField("MaxHp");
            if (maxField != null)
            {
                var val = maxField.GetValue(comp);
                if (val is float f) { maxH = f; maxFound = true; }
                if (val is int i) { maxH = i; maxFound = true; }
            }
            
            if (maxFound)
            {
                var setHealthMethod = type.GetMethod("SetCurrentHealth");
                if (setHealthMethod != null)
                {
                    try { setHealthMethod.Invoke(comp, new object[] { maxH }); break; } catch {}
                }
                
                var changeHealthMethod = type.GetMethod("ChangeHealth");
                if (changeHealthMethod != null)
                {
                    float currH = maxH;
                    var hpProp = type.GetProperty("Health") ?? type.GetProperty("health") ?? type.GetProperty("CurrentHealth");
                    if (hpProp != null)
                    {
                        var val = hpProp.GetValue(comp);
                        if (val is float f) currH = f;
                        if (val is int i) currH = i;
                    }
                    else
                    {
                        var hpField = type.GetField("currentHealth") ?? type.GetField("health");
                        if (hpField != null)
                        {
                            var val = hpField.GetValue(comp);
                            if (val is float f) currH = f;
                            if (val is int i) currH = i;
                        }
                    }
                    
                    float diff = maxH - currH;
                    if (diff > 0)
                    {
                        try
                        {
                            var paramType = changeHealthMethod.GetParameters()[0].ParameterType;
                            if (paramType == typeof(int))
                            {
                                changeHealthMethod.Invoke(comp, new object[] { Mathf.RoundToInt(diff) });
                            }
                            else
                            {
                                changeHealthMethod.Invoke(comp, new object[] { diff });
                            }
                            break;
                        }
                        catch {}
                    }
                }
            }
        }

        // 4. Khôi phục số lượng Boss của Gresha về 1 khi chuyển sang màn chơi mới
        totalEndGameBosses = 1;
        Debug.Log($"[GameManager] Đã khôi phục dữ liệu người chơi thành công (Hồi đầy máu, Reset tiền về 0) và reset Boss của đền Gresha về 1!");
    }

    /// <summary>
    /// Chuyển dịch sang màn chơi tiếp theo dựa vào thứ tự Build Settings của Unity
    /// </summary>
    public void LoadNextLevel()
    {
        int nextSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex + 1;
        if (nextSceneIndex < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
        {
            Debug.Log($"[GameManager] Đang tải màn chơi tiếp theo (Build Index: {nextSceneIndex})...");
            levelsCompletedCount++;
            difficultyTimer += 90f; // Cộng thêm 1p30s khi qua màn chơi mới để tăng độ khó
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            levelsCompletedCount++;
            Debug.Log("[GameManager] Đã đạt tới màn chơi cuối cùng! Hiển thị màn hình Chiến Thắng.");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowWinScreen();
            }
        }
    }
}
