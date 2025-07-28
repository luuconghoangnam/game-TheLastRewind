using UnityEngine;
using System.Collections.Generic;

public class ObjectPoolingClone : MonoBehaviour
{
    public GameObject clonePrefab;
    public Transform[] spawnPoints;
    public int maxClones = 5;
    public int totalClonesToKill = 15;
    public GameObject boss2Object; // Kéo thả object Boss2 vào đây trong Inspector

    private List<GameObject> activeClones = new List<GameObject>();
    private int clonesKilled = 0;
    private bool canSpawnClones = false; // ===== THÊM: Flag để kiểm soát spawn timing =====
    private int nextSpawnPointIndex = 0; // ===== THÊM: Counter riêng cho spawn point =====

    // ===== THÊM MỚI: Event cho GameManager =====
    public System.Action OnCloneDefeatedEvent;
    public System.Action OnAllClonesKilled;

    void Start()
    {
        // ===== SỬA: Không spawn clone ngay, chờ GameManager kích hoạt =====
        // SpawnInitialClones(); // COMMENT DÒNG NÀY
        Debug.Log("ObjectPoolingClone initialized - waiting for boss to disappear before spawning clones");
    }

    // ===== THÊM: Method để GameManager gọi khi boss đã biến mất =====
    public void StartClonePhase()
    {
        canSpawnClones = true;
        nextSpawnPointIndex = 0; // Reset spawn point index
        SpawnInitialClones();
        Debug.Log("Clone phase started - spawning initial clones");
    }

    // ===== THÊM: Method để GameManager reset khi cần =====
    public void StopClonePhase()
    {
        canSpawnClones = false;
        DestroyAllClones();
        clonesKilled = 0;
        nextSpawnPointIndex = 0; // Reset spawn point index
        Debug.Log("Clone phase stopped");
    }

    void SpawnInitialClones()
    {
        // ===== THÊM: Chỉ spawn khi được phép =====
        if (!canSpawnClones) return;

        for (int i = 0; i < maxClones; i++)
        {
            SpawnCloneAt(spawnPoints[i % spawnPoints.Length]);
        }
        Debug.Log($"Spawned {maxClones} initial clones");
    }

    public void OnCloneDie(GameObject clone)
    {
        activeClones.Remove(clone);
        Destroy(clone);
        clonesKilled++;

        // ===== THÊM: Trigger event mỗi khi clone chết =====
        OnCloneDefeatedEvent?.Invoke();

        Debug.Log($"Clone died. Total killed: {clonesKilled}/{totalClonesToKill}");

        if (clonesKilled < totalClonesToKill)
        {
            // ===== SỬA: Chỉ spawn lại nếu được phép =====
            if (canSpawnClones)
            {
                // ===== SỬA: Sử dụng round-robin với counter riêng =====
                SpawnCloneAt(spawnPoints[nextSpawnPointIndex]);
                Debug.Log($"Spawned new clone at spawn point {nextSpawnPointIndex}");
                
                // Tăng index và wrap around
                nextSpawnPointIndex = (nextSpawnPointIndex + 1) % spawnPoints.Length;
            }
        }
        else
        {
            // ===== SỬA: Không tự động kích hoạt boss, để GameManager xử lý =====
            canSpawnClones = false; // Dừng spawn clone
            DestroyAllClones(); // Xóa tất cả clone còn lại
            
            Debug.Log("All required clones killed - notifying GameManager");
            OnAllClonesKilled?.Invoke();
        }
    }

    void SpawnCloneAt(Transform spawnPoint)
    {
        // ===== THÊM: Chỉ spawn khi được phép =====
        if (!canSpawnClones) return;

        GameObject clone = Instantiate(clonePrefab, spawnPoint.position, Quaternion.identity);
        activeClones.Add(clone);

        CloneController cloneController = clone.GetComponent<CloneController>();
        if (cloneController != null)
        {
            cloneController.pooling = this;
            cloneController.player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
    }

    public void DestroyAllClones()
    {
        foreach (var clone in activeClones)
        {
            if (clone != null)
                Destroy(clone);
        }
        activeClones.Clear();
        Debug.Log("All clones destroyed");
    }

    // ===== THÊM: Debug methods =====
    public int GetClonesKilled()
    {
        return clonesKilled;
    }

    public int GetActiveCloneCount()
    {
        return activeClones.Count;
    }

    public bool CanSpawnClones()
    {
        return canSpawnClones;
    }

    // ===== THÊM: Reset method cho debugging =====
    [ContextMenu("Reset Clone System")]
    public void ResetCloneSystem()
    {
        StopClonePhase();
        clonesKilled = 0;
        Debug.Log("Clone system reset");
    }
}