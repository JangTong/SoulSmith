using System.Collections.Generic;
using UnityEngine;

public class NPCPoolManager : MonoBehaviour
{
    private const string LOG_PREFIX = "[NPCPoolManager]";
    private static NPCPoolManager _instance;
    public static NPCPoolManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var existingManager = FindObjectOfType<NPCPoolManager>();
                if (existingManager != null)
                {
                    _instance = existingManager;
                }
                else
                {
                    var go = new GameObject("NPCPoolManager");
                    _instance = go.AddComponent<NPCPoolManager>();
                }
                DontDestroyOnLoad(_instance.gameObject);
                Debug.Log($"{LOG_PREFIX} Instance 초기화 완료: {_instance.gameObject.name}");
            }
            return _instance;
        }
        private set => _instance = value;
    }

    // prefab별 풀 큐 관리
    private Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
    private bool isInitialized = false;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            isInitialized = true;
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 첫 번째 인스턴스로 초기화됨.");
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"{LOG_PREFIX} ({gameObject.name}) 중복 인스턴스 감지됨. 제거합니다. 기존: {_instance.gameObject.name}");
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        if (_instance == this && !isInitialized)
        {
            isInitialized = true;
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) OnEnable에서 초기화됨.");
        }
    }

    /// <summary>
    /// 특정 프리팹에 대해 풀을 미리 생성합니다.
    /// </summary>
    public void Preload(GameObject prefab, int count)
    {
        if (!isInitialized)
        {
            Debug.LogError($"{LOG_PREFIX} 초기화되지 않은 상태에서 Preload가 호출되었습니다.");
            return;
        }

        if (prefab == null)
        {
            Debug.LogError($"{LOG_PREFIX} Preload 시도 중 프리팹이 null입니다.");
            return;
        }

        if (!pools.ContainsKey(prefab))
        {
            pools[prefab] = new Queue<GameObject>();
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 프리팹 '{prefab.name}'에 대한 새 풀 생성.");
        }

        var queue = pools[prefab];
        for (int i = 0; i < count; i++)
        {
            var go = CreateNewInstance(prefab);
            go.SetActive(false);
            queue.Enqueue(go);
        }
        Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 프리팹 '{prefab.name}' {count}개 인스턴스 Preload 완료. 현재 풀 크기: {queue.Count}");
    }

    private GameObject CreateNewInstance(GameObject prefab)
    {
        var go = Instantiate(prefab);
        go.transform.SetParent(transform); // NPCPoolManager의 자식으로 생성
        return go;
    }

    /// <summary>
    /// 풀에서 객체를 꺼내 활성화하여 반환합니다.
    /// </summary>
    public GameObject Acquire(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!isInitialized)
        {
            Debug.LogError($"{LOG_PREFIX} 초기화되지 않은 상태에서 Acquire가 호출되었습니다.");
            return null;
        }

        if (prefab == null)
        {
            Debug.LogError($"{LOG_PREFIX} Acquire 시도 중 프리팹이 null입니다.");
            return null;
        }

        if (!pools.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            pools[prefab] = queue;
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 프리팹 '{prefab.name}'에 대한 풀이 없어 새로 생성.");
        }

        GameObject obj;
        if (queue.Count > 0)
        {
            obj = queue.Dequeue();
            if (obj == null)
            {
                Debug.LogWarning($"{LOG_PREFIX} ({gameObject.name}) 풀의 객체가 유효하지 않아 새로 생성합니다.");
                obj = CreateNewInstance(prefab);
            }
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 프리팹 '{prefab.name}' 인스턴스 재사용. 남은 풀 크기: {queue.Count}");
        }
        else
        {
            obj = CreateNewInstance(prefab);
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 프리팹 '{prefab.name}' 풀 비어있어 새로 생성.");
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.transform.SetParent(null); // 월드에 배치할 때는 부모 해제
        obj.SetActive(true);
        Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 프리팹 '{prefab.name}' 인스턴스 활성화. 위치: {position}");
        return obj;
    }

    /// <summary>
    /// 객체를 비활성화하고 풀로 반환합니다.
    /// </summary>
    public void Release(GameObject prefab, GameObject obj)
    {
        if (!isInitialized)
        {
            Debug.LogError($"{LOG_PREFIX} 초기화되지 않은 상태에서 Release가 호출되었습니다.");
            return;
        }

        if (prefab == null)
        {
            Debug.LogError($"{LOG_PREFIX} Release 시도 중 프리팹이 null입니다.");
            return;
        }
        if (obj == null)
        {
            Debug.LogError($"{LOG_PREFIX} Release 시도 중 객체가 null입니다.");
            return;
        }

        obj.SetActive(false);
        obj.transform.SetParent(transform); // 풀로 돌아올 때는 NPCPoolManager의 자식으로

        if (!pools.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            pools[prefab] = queue;
            Debug.LogWarning($"{LOG_PREFIX} ({gameObject.name}) 프리팹 '{prefab.name}'에 대한 풀이 없어 새로 생성.");
        }
        queue.Enqueue(obj);
        Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 프리팹 '{prefab.name}'의 인스턴스 반환됨. 현재 풀 크기: {queue.Count}");
    }
}
