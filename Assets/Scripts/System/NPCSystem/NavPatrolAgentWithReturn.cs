using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavPatrolAgentWithReturn : MonoBehaviour
{
    public Transform[] patrolPoints;
    public bool loopPatrol = false;
    public float returnTolerance = 0.5f;

    private NavMeshAgent agent;
    private int currentIndex = 0;
    private Transform homePoint;
    private GameObject prefabRef;  // 풀 매니저에 넘겨줄 레퍼런스
    private enum State { Patrolling, Returning }
    private State state = State.Patrolling;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        Debug.Log($"[Agent] Awake 호출 – patrolPoints:{patrolPoints?.Length}");
    }

    /// <summary>
    /// 스폰 시 초기화: homePoint와 prefab 레퍼런스 세팅
    /// </summary>
    public void Initialize(Transform home, GameObject prefab, Transform[] patrolPoints)
    {
        homePoint    = home;
        prefabRef    = prefab;
        this.patrolPoints = patrolPoints;  // 씬 웨이포인트 할당
        currentIndex = 0;
        state        = State.Patrolling;

        // 첫 목적지 설정
        if (patrolPoints.Length > 0)
            MoveTo(patrolPoints[0].position);

        Debug.Log($"[Agent] Initialized at {home.position} with {patrolPoints.Length} waypoints");
    }

    private void Update()
    {
        if (agent.pathPending) return;

        if (state == State.Patrolling && agent.remainingDistance <= agent.stoppingDistance)
            OnPatrolPointReached();
        else if (state == State.Returning && agent.remainingDistance <= returnTolerance)
            OnReturnHomeReached();
    }

    private void OnPatrolPointReached()
    {
        Debug.Log($"[Agent] Patrol point {currentIndex} reached");
        currentIndex++;
        if (currentIndex >= patrolPoints.Length)
        {
            if (loopPatrol)
                currentIndex = 0;
            else
                StartReturn();
        }
        if (state == State.Patrolling)
            MoveTo(patrolPoints[currentIndex].position);
    }

    private void StartReturn()
    {
        state = State.Returning;
        Debug.Log("[Agent] 순찰 완료 → 복귀 시작");
        MoveTo(homePoint.position);
    }

    private void OnReturnHomeReached()
    {
        Debug.Log("[Agent] Home reached, returning to pool");
        // 파괴 대신 풀에 반환
        NPCPoolManager.Instance.Release(prefabRef, gameObject);
    }

    private void MoveTo(Vector3 dest)
    {
        agent.SetDestination(dest);
        Debug.Log($"[Agent] [{state}] 목적지 설정: {dest}");
    }
}
