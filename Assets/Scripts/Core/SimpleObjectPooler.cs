using System.Collections.Generic;
using UnityEngine;

public class SimpleObjectPooler : MonoBehaviour
{
    private static SimpleObjectPooler _instance;
    public static SimpleObjectPooler Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<SimpleObjectPooler>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SimpleObjectPooler");
                    _instance = go.AddComponent<SimpleObjectPooler>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [System.Serializable]
    public struct Pool
    {
        public string tag;          // Định danh pool, ví dụ: "PlayerBullet", "HitParticle"
        public GameObject prefab;   // Prefab đối tượng cần pool
        public int size;            // Số lượng khởi tạo mặc định ban đầu
    }

    [Header("Pool Configurations")]
    [SerializeField] private List<Pool> pools = new List<Pool>();

    // Dictionary chứa danh sách các hàng đợi (Queue) đối tượng nhàn rỗi
    private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePools();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void InitializePools()
    {
        poolDictionary.Clear();

        foreach (Pool pool in pools)
        {
            if (pool.prefab == null || string.IsNullOrEmpty(pool.tag)) continue;

            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                DontDestroyOnLoad(obj); // Hoặc làm con của Pooler để gọn Hierachy
                obj.transform.SetParent(transform);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
            Debug.Log($"[ObjectPooler] Khởi tạo thành công pool '{pool.tag}' với quy mô {pool.size} đối tượng.");
        }
    }

    /// <summary>
    /// Lấy một đối tượng nhàn rỗi từ Pool dựa trên Tag định danh.
    /// </summary>
    public GameObject GetPooledObject(string tag)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"[ObjectPooler] Không tìm thấy pool nào có tag '{tag}'!");
            return null;
        }

        Queue<GameObject> queue = poolDictionary[tag];

        // Lấy đối tượng ở đầu hàng đợi
        if (queue.Count > 0)
        {
            GameObject obj = queue.Dequeue();

            // Nếu đối tượng đang hoạt động ngoài scene (tức là pool bị thiếu), ta tự động nhân bản thêm
            if (obj != null && obj.activeSelf)
            {
                // Đưa trả lại đối tượng cũ vào cuối hàng đợi để xoay vòng
                queue.Enqueue(obj);

                // Khởi tạo thêm 1 đối tượng mới để bổ sung quy mô (Auto-Expand)
                GameObject newObj = Instantiate(obj); // Sao chép từ đối tượng gốc
                newObj.SetActive(false);
                newObj.transform.SetParent(transform);
                
                // Trả về đối tượng mới
                return newObj;
            }

            // Đưa lại đối tượng vào cuối hàng đợi để tái sử dụng ở các lượt sau
            queue.Enqueue(obj);
            return obj;
        }

        return null;
    }

    /// <summary>
    /// Khởi tạo động một Pool mới từ Code mà không cần cấu hình trong Inspector.
    /// </summary>
    public void CreatePool(string tag, GameObject prefab, int size)
    {
        if (prefab == null || string.IsNullOrEmpty(tag)) return;

        // Nếu pool này đã được tạo trước đó rồi thì bỏ qua
        if (poolDictionary.ContainsKey(tag)) return;

        Queue<GameObject> objectPool = new Queue<GameObject>();

        for (int i = 0; i < size; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            DontDestroyOnLoad(obj);
            obj.transform.SetParent(transform);
            objectPool.Enqueue(obj);
        }

        poolDictionary.Add(tag, objectPool);
        Debug.Log($"[ObjectPooler] Khởi tạo động thành công pool '{tag}' với quy mô {size} đối tượng.");
    }
}
