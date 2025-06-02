using UnityEngine;
using System.Collections;

public class PatrolSpawner : MonoBehaviour
{
    private const string LOG_PREFIX = "[PatrolSpawner]";

    [Header("Spawn Settings")]
    public Transform[] spawnPoints;

    [Header("즉시 생성 NPC 설정")]
    public GameObject[] instantSpawnPrefabs;
    public int[] instantSpawnCounts;

    [Header("반복 생성 NPC 설정")]
    public GameObject[] repeatSpawnPrefabs;
    public float[] repeatSpawnIntervals;
    public int[] repeatSpawnCounts;
    public bool[] isInfiniteSpawn;  // true일 경우 무한 스폰

    [Header("시간차 생성 NPC 설정")]
    public GameObject[] delayedSpawnPrefabs;
    public float[] spawnDelays;
    public int[] delayedSpawnCounts;

    [Header("Patrol Path (Scene)")]
    [Tooltip("씬에 배치된 웨이포인트들")]
    public Transform[] patrolPoints;

    private void Start()
    {
        ValidateComponents();
        InitializeNPCPools();
        StartSpawning();
    }

    private void ValidateComponents()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError($"{LOG_PREFIX} ({gameObject.name}) 스폰 포인트가 설정되지 않았습니다.");
            return;
        }

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogWarning($"{LOG_PREFIX} ({gameObject.name}) 순찰 경로가 설정되지 않았습니다.");
        }
    }

    private void InitializeNPCPools()
    {
        // 즉시 생성 NPC 풀 초기화
        if (instantSpawnPrefabs != null)
        {
            for (int i = 0; i < instantSpawnPrefabs.Length; i++)
            {
                if (instantSpawnPrefabs[i] != null)
                {
                    NPCPoolManager.Instance.Preload(instantSpawnPrefabs[i], instantSpawnCounts[i]);
                }
            }
        }

        // 반복 생성 NPC 풀 초기화
        if (repeatSpawnPrefabs != null)
        {
            for (int i = 0; i < repeatSpawnPrefabs.Length; i++)
            {
                if (repeatSpawnPrefabs[i] != null)
                {
                    NPCPoolManager.Instance.Preload(repeatSpawnPrefabs[i], repeatSpawnCounts[i]);
                }
            }
        }

        // 시간차 생성 NPC 풀 초기화
        if (delayedSpawnPrefabs != null)
        {
            for (int i = 0; i < delayedSpawnPrefabs.Length; i++)
            {
                if (delayedSpawnPrefabs[i] != null)
                {
                    NPCPoolManager.Instance.Preload(delayedSpawnPrefabs[i], delayedSpawnCounts[i]);
                }
            }
        }
    }

    private void StartSpawning()
    {
        // 즉시 생성 NPC 스폰
        if (instantSpawnPrefabs != null)
        {
            for (int i = 0; i < instantSpawnPrefabs.Length; i++)
            {
                for (int j = 0; j < instantSpawnCounts[i]; j++)
                {
                    SpawnNPC(instantSpawnPrefabs[i]);
                }
            }
        }

        // 반복 생성 NPC 스폰
        if (repeatSpawnPrefabs != null)
        {
            for (int i = 0; i < repeatSpawnPrefabs.Length; i++)
            {
                StartCoroutine(SpawnNPCRepeatedly(repeatSpawnPrefabs[i], repeatSpawnIntervals[i], repeatSpawnCounts[i], isInfiniteSpawn[i]));
            }
        }

        // 시간차 생성 NPC 스폰
        if (delayedSpawnPrefabs != null)
        {
            for (int i = 0; i < delayedSpawnPrefabs.Length; i++)
            {
                StartCoroutine(SpawnNPCWithDelay(delayedSpawnPrefabs[i], spawnDelays[i], delayedSpawnCounts[i]));
            }
        }
    }

    private System.Collections.IEnumerator SpawnNPCRepeatedly(GameObject prefab, float interval, int count, bool infinite)
    {
        int spawned = 0;
        while (infinite || spawned < count)
        {
            SpawnNPC(prefab);
            spawned++;
            yield return new WaitForSeconds(interval);
        }
    }

    private System.Collections.IEnumerator SpawnNPCWithDelay(GameObject prefab, float delay, int count)
    {
        yield return new WaitForSeconds(delay);
        for (int i = 0; i < count; i++)
        {
            SpawnNPC(prefab);
        }
    }

    private void SpawnNPC(GameObject prefab)
    {
        if (NPCPoolManager.Instance == null || prefab == null || spawnPoints.Length == 0)
        {
            Debug.LogError($"{LOG_PREFIX} ({gameObject.name}) SpawnNPC: 필수 컴포넌트가 없습니다.");
            return;
        }

        int idx = Random.Range(0, spawnPoints.Length);
        Transform sp = spawnPoints[idx];

        if (sp == null)
        {
            Debug.LogError($"{LOG_PREFIX} ({gameObject.name}) SpawnNPC: 선택된 스폰 포인트(spawnPoints[{idx}])가 null입니다.");
            return;
        }

        GameObject npc = NPCPoolManager.Instance.Acquire(prefab, sp.position, sp.rotation);
        if (npc == null)
        {
            Debug.LogError($"{LOG_PREFIX} ({gameObject.name}) SpawnNPC: NPCPoolManager에서 NPC를 가져오지 못했습니다.");
            return;
        }

        Debug.Log($"{LOG_PREFIX} ({gameObject.name}) NPC '{npc.name}' 스폰됨 at {sp.position}");

        var agent = npc.GetComponent<NavPatrolAgentWithReturn>();
        if (agent != null)
        {
            agent.Initialize(sp, prefab, patrolPoints);
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) NPC '{npc.name}'에 NavPatrolAgentWithReturn 초기화됨.");
        }
        else
        {
            Debug.LogWarning($"{LOG_PREFIX} ({gameObject.name}) NPC '{npc.name}'에 NavPatrolAgentWithReturn 컴포넌트가 없습니다.");
        }
    }
}
