using UnityEngine;

public class PatrolSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Transform[] spawnPoints;
    public GameObject npcPrefab;
    public int initialPoolSize = 5;
    public float spawnDelay = 0f;

    [Header("Patrol Path (Scene)")]
    [Tooltip("씬에 배치된 웨이포인트들")]
    public Transform[] patrolPoints;

    private void Start()
    {
        // 풀 미리 채워 놓고...
        NPCPoolManager.Instance.Preload(npcPrefab, initialPoolSize);
        Invoke(nameof(SpawnNPC), spawnDelay);
    }

    private void SpawnNPC()
    {
        int idx = Random.Range(0, spawnPoints.Length);
        Transform sp = spawnPoints[idx];

        GameObject npc = NPCPoolManager.Instance.Acquire(npcPrefab, sp.position, sp.rotation);
        Debug.Log($"[PatrolSpawner] Spawned NPC at {sp.position}");

        // **여기서** 씬의 patrolPoints를 런타임으로 할당
        var agent = npc.GetComponent<NavPatrolAgentWithReturn>();
        if (agent != null)
            agent.Initialize(sp, npcPrefab, patrolPoints);
    }
}
