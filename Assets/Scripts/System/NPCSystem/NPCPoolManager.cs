using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPC 전용 풀 매니저 (PoolManager → NPCPoolManager로 명칭 변경)
/// </summary>
public class NPCPoolManager : MonoBehaviour
{
    public static NPCPoolManager Instance { get; private set; }

    // prefab별 풀 큐 관리
    private Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// 특정 프리팹에 대해 풀을 미리 생성합니다.
    /// </summary>
    public void Preload(GameObject prefab, int count)
    {
        if (!pools.ContainsKey(prefab))
            pools[prefab] = new Queue<GameObject>();

        var queue = pools[prefab];
        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(prefab);
            go.SetActive(false);
            queue.Enqueue(go);
        }
        Debug.Log($"[NPCPoolManager] Preloaded {count} instances of {prefab.name}");
    }

    /// <summary>
    /// 풀에서 객체를 꺼내 활성화하여 반환합니다.
    /// </summary>
    public GameObject Acquire(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!pools.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            pools[prefab] = queue;
        }

        GameObject obj;
        if (queue.Count > 0)
        {
            obj = queue.Dequeue();
            Debug.Log($"[NPCPoolManager] Reusing instance of {prefab.name}, remaining pool: {queue.Count}");
        }
        else
        {
            obj = Instantiate(prefab);
            Debug.LogWarning($"[NPCPoolManager] Pool empty for {prefab.name}! Instantiating new one.");
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        return obj;
    }

    /// <summary>
    /// 객체를 비활성화하고 풀로 반환합니다.
    /// </summary>
    public void Release(GameObject prefab, GameObject obj)
    {
        obj.SetActive(false);
        if (!pools.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            pools[prefab] = queue;
        }
        queue.Enqueue(obj);
        Debug.Log($"[NPCPoolManager] Released instance of {prefab.name}, pool size: {queue.Count}");
    }
}
