using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Player Prefabs")]
    [SerializeField] private GameObject astronautPrefab;
    [SerializeField] private GameObject banditPrefab;

    [Header("Save Settings")]
    [SerializeField] private string selectedCharacterKey = "SelectedCharacter";

    private void Awake()
    {
        // 1. Lấy nhân vật đã chọn từ PlayerPrefs (mặc định 0 - Astronaut)
        int choice = PlayerPrefs.GetInt(selectedCharacterKey, 0);
        GameObject selectedPrefab = choice == 1 ? banditPrefab : astronautPrefab;

        if (selectedPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] Chưa gán prefab nhân vật trong Spawner!");
            return;
        }

        // 2. Instantiate nhân vật tại vị trí của Spawner
        GameObject spawnedPlayer = Instantiate(selectedPrefab, transform.position, Quaternion.identity);
        spawnedPlayer.name = "Player"; // Đổi tên để các script khác dễ tìm kiếm trong game
        spawnedPlayer.tag = "Player"; // Đảm bảo tag luôn là Player để các hệ thống khác nhận diện


        // 3. Tự động liên kết camera Cinemachine bắt nét nhân vật vừa spawn thông qua Reflection
        // Phương pháp này đảm bảo an toàn 100% không bị lỗi biên dịch cho cả Cinemachine v2 lẫn v3
        Component cinemachineCam = null;
        var allComponents = Object.FindObjectsByType<Component>(FindObjectsInactive.Exclude);
        foreach (var comp in allComponents)
        {
            if (comp != null)
            {
                string typeName = comp.GetType().FullName;
                if (typeName == "Unity.Cinemachine.CinemachineCamera" || 
                    typeName == "Cinemachine.CinemachineVirtualCamera" || 
                    typeName.Contains("CinemachineCamera") || 
                    typeName.Contains("CinemachineVirtualCamera"))
                {
                    cinemachineCam = comp;
                    break;
                }
            }
        }

        if (cinemachineCam != null)
        {
            var propFollow = cinemachineCam.GetType().GetProperty("Follow");
            var propLookAt = cinemachineCam.GetType().GetProperty("LookAt");
            
            if (propFollow != null) propFollow.SetValue(cinemachineCam, spawnedPlayer.transform);
            if (propLookAt != null) propLookAt.SetValue(cinemachineCam, spawnedPlayer.transform);
            
            Debug.Log($"[PlayerSpawner] Tự động kết nối camera '{cinemachineCam.gameObject.name}' ({cinemachineCam.GetType().Name}) theo dõi '{spawnedPlayer.name}' thành công!");
        }
        else
        {
            Debug.LogWarning("[PlayerSpawner] Không tìm thấy camera Cinemachine nào trong Scene!");
        }
    }
}
