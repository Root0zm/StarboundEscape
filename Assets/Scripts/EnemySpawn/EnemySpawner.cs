using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Pool Config")]
    [SerializeField] private List<GameObject> enemyPool;  
    [Header("Spawn Rules (Theo GDD)")]
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int maxEnemiesAllowed = 20;  
    [Header("Dynamic Screen Spawn Settings")]
    [SerializeField] private float paddingOutsideCamera = 2f;  
    [Header("Spawning Boundary & Ground Check")]
    [SerializeField] private Collider2D mapBoundaryCollider; // Collider giới hạn bản đồ
    [SerializeField] private LayerMask groundLayer;           // Layer đất cứng để kiểm tra độ cao
    private float nextSpawnTime;
    private Camera mainCamera;

    [Header("Despawn Settings")]
    [SerializeField] private float maxDistanceToPlayer = 25f; // Khoảng cách tối đa cách người chơi
    private Dictionary<EnemyController, float> outOfRangeTimers = new Dictionary<EnemyController, float>();

    void Start()
    {
        mainCamera = Camera.main;
        // Tự động gán fallback nếu quên thiết lập layer trong Inspector
        if (groundLayer == 0)
        {
            groundLayer = LayerMask.GetMask("Ground");
        }
        StartCoroutine(DespawnFarEnemiesRoutine());
    }

    private IEnumerator DespawnFarEnemiesRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f); // Quét mỗi 0.5 giây để tránh giảm hiệu năng

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null) continue;

            Transform playerTransform = playerObj.transform;
            EnemyController[] allEnemies = FindObjectsByType<EnemyController>();

            // Danh sách các enemy hoạt động hiện tại để dọn dẹp dictionary
            HashSet<EnemyController> currentActiveEnemies = new HashSet<EnemyController>();

            foreach (EnemyController enemy in allEnemies)
            {
                if (enemy == null || enemy.IsDead || enemy.IsBoss()) continue;

                currentActiveEnemies.Add(enemy);
                float distance = Vector2.Distance(enemy.transform.position, playerTransform.position);

                if (distance > maxDistanceToPlayer)
                {
                    if (outOfRangeTimers.ContainsKey(enemy))
                    {
                        outOfRangeTimers[enemy] += 0.5f;
                        if (outOfRangeTimers[enemy] >= 2f)
                        {
                            Destroy(enemy.gameObject);
                        }
                    }
                    else
                    {
                        outOfRangeTimers[enemy] = 0.5f;
                    }
                }
                else
                {
                    outOfRangeTimers[enemy] = 0f;
                }
            }

            // Dọn dẹp các phần tử đã bị hủy khỏi game để tránh rò rỉ bộ nhớ
            List<EnemyController> keysToRemove = new List<EnemyController>();
            foreach (var key in outOfRangeTimers.Keys)
            {
                if (key == null || !currentActiveEnemies.Contains(key))
                {
                    keysToRemove.Add(key);
                }
            }
            foreach (var key in keysToRemove)
            {
                outOfRangeTimers.Remove(key);
            }
        }
    }

    void Update()
    {
        // Kiểm tra xem Cổng dịch chuyển đã sạc xong (IsCompleted == true) chưa
        TeleportPortal portal = FindAnyObjectByType<TeleportPortal>();
        if (portal != null && portal.IsCompleted)
        {
            return; // Dừng spawn thêm quái hoàn toàn khi sạc xong
        }

        if (Time.time >= nextSpawnTime)
        {
            if (GetCurrentEnemyCount() < maxEnemiesAllowed)
            {
                SpawnEnemyAtCameraEdge();
            }
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    private void SpawnEnemyAtCameraEdge()
    {
        if (enemyPool == null || enemyPool.Count == 0 || mainCamera == null) return;

        float cameraHeight = 2f * mainCamera.orthographicSize;
        float cameraWidth = cameraHeight * mainCamera.aspect;

        Vector3 cameraPosition = mainCamera.transform.position;

        float leftEdge = cameraPosition.x - (cameraWidth / 2f);
        float rightEdge = cameraPosition.x + (cameraWidth / 2f);
        float bottomEdge = cameraPosition.y - (cameraHeight / 2f);

        Vector3 proposedPosition = Vector3.zero;

        int randomSide = Random.Range(0, 2);

        if (randomSide == 0)
        {
            proposedPosition.x = leftEdge - paddingOutsideCamera;
            proposedPosition.y = Random.Range(bottomEdge, cameraPosition.y);
        }
        else
        {
            proposedPosition.x = rightEdge + paddingOutsideCamera;
            proposedPosition.y = Random.Range(bottomEdge, cameraPosition.y);
        }

        int randomEnemyIndex = Random.Range(0, enemyPool.Count);
        GameObject enemyPrefab = enemyPool[randomEnemyIndex];

        Vector3 safeSpawnPos = GetSafeSpawnPosition(proposedPosition, enemyPrefab);

        Instantiate(enemyPrefab, safeSpawnPos, Quaternion.identity);
    }

    private float GetSpawnOffsetY(GameObject prefab)
    {
        if (prefab == null) return 1.0f;

        Collider2D col = prefab.GetComponent<Collider2D>();
        if (col == null) col = prefab.GetComponentInChildren<Collider2D>();

        if (col != null)
        {
            float scaleY = Mathf.Abs(prefab.transform.localScale.y);
            if (col is BoxCollider2D box)
            {
                return (box.size.y * 0.5f - box.offset.y) * scaleY;
            }
            else if (col is CapsuleCollider2D capsule)
            {
                return (capsule.size.y * 0.5f - capsule.offset.y) * scaleY;
            }
            else if (col is CircleCollider2D circle)
            {
                return (circle.radius - circle.offset.y) * scaleY;
            }

            if (col.bounds.extents.y > 0f)
            {
                return col.bounds.extents.y;
            }
        }

        return 1.0f;
    }

    /*private Vector3 GetSafeSpawnPosition(Vector3 proposedPosition, GameObject enemyPrefab)
    {
        Vector3 safePosition = proposedPosition;

        // 1. Kiểm tra giới hạn map bằng mapBoundaryCollider
        if (mapBoundaryCollider != null)
        {
            // Nếu vị trí proposedPosition nằm ngoài vùng bao quanh (bounds) của Collider
            if (!mapBoundaryCollider.bounds.Contains(proposedPosition))
            {
                // Tìm điểm trên bề mặt giới hạn gần điểm đề xuất nhất
                Vector3 closestPoint = mapBoundaryCollider.ClosestPoint(proposedPosition);

                // Tính toán hướng từ điểm biên đó hướng vào tâm map dọc theo trục X
                float directionX = mapBoundaryCollider.bounds.center.x - closestPoint.x;
                float shiftDirectionX = directionX >= 0f ? 1f : -1f;

                // Tọa độ mới dịch chuyển vào trong map 1.5 mét theo trục X
                safePosition = new Vector3(closestPoint.x + shiftDirectionX * 1.5f, proposedPosition.y, proposedPosition.z);

                Debug.LogWarning($"[EnemySpawner] Vị trí đề xuất {proposedPosition} nằm ngoài biên bản đồ! Đã điều chỉnh lùi vào trong: {safePosition}");
            }
        }

        // 2. Tìm mặt đất hợp lệ gần với safePosition.y nhất
        // Xác định vị trí Y bắt đầu bắn tia check từ trên cao để đảm bảo không bị nằm trong lòng đất
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        float playerY = playerObj != null ? playerObj.transform.position.y : (mainCamera != null ? mainCamera.transform.position.y : safePosition.y);
        
        float startY = Mathf.Max(safePosition.y + 10f, playerY + 10f);
        if (mainCamera != null)
        {
            float cameraTop = mainCamera.transform.position.y + mainCamera.orthographicSize;
            startY = Mathf.Max(startY, cameraTop + 5f);
        }

        Vector2 rayStart = new Vector2(safePosition.x, startY);
        float rayLength = 35f; // Đảm bảo bắn xuống đủ sâu để quét qua toàn bộ nền đất
        
        RaycastHit2D[] hits = Physics2D.RaycastAll(rayStart, Vector2.down, rayLength, groundLayer);
        
        RaycastHit2D bestHit = default;
        float minDiffY = float.MaxValue;
        bool foundValidGround = false;

        foreach (var hit in hits)
        {
            // Bỏ qua nếu raycast bắt đầu từ bên trong collider (distance/fraction bằng 0)
            if (hit.collider == null || hit.fraction <= 0.0001f || hit.distance <= 0.0001f)
            {
                continue;
            }

            // Tính độ lệch Y giữa điểm va chạm và vị trí đề xuất ban đầu
            float diffY = Mathf.Abs(hit.point.y - proposedPosition.y);
            if (diffY < minDiffY)
            {
                minDiffY = diffY;
                bestHit = hit;
                foundValidGround = true;
            }
        }

        if (foundValidGround)
        {
            float offset = GetSpawnOffsetY(enemyPrefab);
            // Đặt quái ngay trên điểm va chạm cộng thêm một khoảng hở nhỏ 0.1m để tránh kẹt trong nền đất
            safePosition.y = bestHit.point.y + offset + 0.1f;
            Debug.Log($"[EnemySpawner] Phát hiện sàn đất cứng tại Y = {bestHit.point.y}. Đã đặt quái tại Y = {safePosition.y} (offset: {offset})");
        }
        else
        {
            // Fallback nếu không quét được bằng RaycastAll, thử dùng raycast thường từ safePosition.y + 2f
            RaycastHit2D fallbackHit = Physics2D.Raycast(new Vector2(safePosition.x, safePosition.y + 2f), Vector2.down, 15f, groundLayer);
            if (fallbackHit.collider != null)
            {
                float offset = GetSpawnOffsetY(enemyPrefab);
                safePosition.y = fallbackHit.point.y + offset + 0.1f;
                Debug.LogWarning($"[EnemySpawner] RaycastAll thất bại. Fallback phát hiện sàn đất tại Y = {fallbackHit.point.y}. Đặt quái tại Y = {safePosition.y}");
            }
            else
            {
                Debug.LogWarning($"[EnemySpawner] Không tìm thấy sàn đất dưới vị trí {safePosition.x}. Giữ nguyên tọa độ Y đề xuất.");
            }
        }

        return safePosition;
    }*/

    private Vector3 GetSafeSpawnPosition(Vector3 proposedPosition, GameObject enemyPrefab)
    {
        Vector3 safePosition = proposedPosition;

        // 1. Kiểm tra giới hạn bản đồ (Giữ nguyên của bạn)
        if (mapBoundaryCollider != null)
        {
            if (!mapBoundaryCollider.bounds.Contains(proposedPosition))
            {
                Vector3 closestPoint = mapBoundaryCollider.ClosestPoint(proposedPosition);
                float directionX = mapBoundaryCollider.bounds.center.x - closestPoint.x;
                float shiftDirectionX = directionX >= 0f ? 1f : -1f;
                safePosition = new Vector3(closestPoint.x + shiftDirectionX * 1.5f, proposedPosition.y, proposedPosition.z);
                Debug.LogWarning($"[EnemySpawner] Vị trí đề xuất {proposedPosition} nằm ngoài biên bản đồ! Đã điều chỉnh lùi vào trong: {safePosition}");
            }
        }

        // ====================================================================
        // ĐOẠN CODE THÊM MỚI: XỬ LÝ RIÊNG CHO QUÁI BAY (BEE ENEMY)
        // ====================================================================
        /*Rigidbody2D enemyRb = enemyPrefab.GetComponent<Rigidbody2D>();
        if (enemyRb != null && enemyRb.gravityScale == 0f)
        {
            // Bắn một tia Raycast xuống đất để kiểm tra xem vị trí ngẫu nhiên có bị thấp quá không
            Vector2 beeRayStart = new Vector2(safePosition.y, safePosition.y + 10f);
            RaycastHit2D groundCheck = Physics2D.Raycast(safePosition, Vector2.down, 15f, groundLayer);
            if (groundCheck.collider != null)
            {
                // Nếu khoảng cách từ vị trí sinh quái tới mặt đất dưới 2 mét (quá sát đất)
                if (safePosition.y - groundCheck.point.y < 2.0f)
                {
                    // Ép con bọ bay cao lên, cách mặt đất ngẫu nhiên từ 3 đến 5 mét
                    safePosition.y = groundCheck.point.y + Random.Range(3.0f, 5.0f);
                    Debug.Log($"[EnemySpawner] Phát hiện quái bay {enemyPrefab.name}. Đã đưa lên cao cách đất: {safePosition.y}");
                }
            }
            return safePosition; // Trả về tọa độ luôn, không chạy xuống đoạn code ép bám đất phía dưới nữa
        }*/

        Rigidbody2D enemyRb = enemyPrefab.GetComponent<Rigidbody2D>();
        if (enemyRb != null && enemyRb.gravityScale == 0f)
        {
            // BẬC THẦY ĐÒ ĐƯỜNG: Bắn tia từ trên trời cao hẳn (Trục Y của đỉnh Camera + 5 mét)
            // Để đảm bảo tia luôn nằm TRÊN mọi đường đi và bề mặt đất gạch
            float skyY = mainCamera.transform.position.y + (mainCamera.orthographicSize) + 5f;
            Vector2 skyRayStart = new Vector2(safePosition.x, skyY);

            // Bắn một tia dài hẳn 30 mét từ trên trời xuống dưới
            RaycastHit2D groundCheck = Physics2D.Raycast(skyRayStart, Vector2.down, 30f, groundLayer);

            if (groundCheck.collider != null)
            {
                // Khi đã tìm thấy BỀ MẶT đường đi thực tế (groundCheck.point.y)
                // Ta ép con bọ phải xuất hiện TRÊN bề mặt đó từ 3 đến 5 mét
                safePosition.y = groundCheck.point.y + Random.Range(3.0f, 5.0f);

                Debug.Log($"[EnemySpawner] Đã sửa vị trí Bee: Tìm thấy bề mặt đường đi tại {groundCheck.point.y}, đưa Bee lên cao hẳn: {safePosition.y}");
                return safePosition;
            }
        }
        // ====================================================================

        // 2. Kiểm tra mặt đất sàn nhà dành cho QUÁI ĐI BỘ (Giữ nguyên gốc của bạn)
        /*Vector2 rayStart = new Vector2(safePosition.x, safePosition.y + 2f);
        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, 15f, groundLayer);
        if (hit.collider != null)
        {
            float offset = GetSpawnOffsetY(enemyPrefab); //
            safePosition.y = hit.point.y + offset + 0.1f; //
            Debug.Log($"[EnemySpawner] Phát hiện sàn đất cứng tại Y = {hit.point.y}. Đã đặt quái tại Y = {safePosition.y} (offset: {offset})"); //
        }
        else
        {
            Debug.LogWarning($"[EnemySpawner] Không tìm thấy sàn đất dưới vị trí {safePosition.x}. Giữ nguyên tọa độ Y đề xuất."); //
        }*/
        float skyYWalk = mainCamera.transform.position.y + mainCamera.orthographicSize + 5f;
        Vector2 rayStart = new Vector2(safePosition.x, skyYWalk);

        // Bắn một tia dài hẳn 40 mét từ trên trời xuyên thẳng xuống dưới đất
        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, 40f, groundLayer);

        if (hit.collider != null)
        {
            // Tính toán khoảng dịch chuyển Y dựa trên chiều cao collider của quái đi bộ
            float offset = GetSpawnOffsetY(enemyPrefab); //

            // Đặt quái đứng ngay ngắn khít trên mặt sàn gạch vừa tìm thấy
            safePosition.y = hit.point.y + offset + 0.1f; //

            Debug.Log($"[EnemySpawner] Phát hiện sàn đất cứng tại Y = {hit.point.y} cho quái đi bộ. Đã đặt tại Y = {safePosition.y} (offset: {offset})"); //
        }
        else
        {
            // Dự phòng nếu không dò ra đất, giữ nguyên tọa độ đề xuất
            Debug.LogWarning($"[EnemySpawner] Không tìm thấy sàn đất dưới vị trí {safePosition.x} từ trên trời quét xuống cho quái đi bộ."); //
        }

        return safePosition;
    }

    private int GetCurrentEnemyCount()
    {
        // Sử dụng FindObjectsByType phiên bản mới để tránh cảnh báo lỗi thời
        EnemyController[] currentEnemies = FindObjectsByType<EnemyController>();
        int count = 0;
        foreach (var enemy in currentEnemies)
        {
            count++;
        }
        return count;
    }
}