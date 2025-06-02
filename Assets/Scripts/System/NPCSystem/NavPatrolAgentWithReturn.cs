using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavPatrolAgentWithReturn : MonoBehaviour
{
    private const string LOG_PREFIX = "[NavPatrolAgentWithReturn]";

    public Transform[] patrolPoints;
    public bool loopPatrol = false;
    public float returnTolerance = 0.5f;

    private NavMeshAgent agent;
    private int currentIndex = 0;
    private Transform homePoint;
    private GameObject prefabRef;
    private enum State { Patrolling, Returning }
    private State currentState = State.Patrolling;
    private string npcName = ""; // NPC 이름을 캐싱하기 위한 변수

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        npcName = gameObject.name; // Awake 시점에 gameObject.name을 캐싱
        Debug.Log($"{LOG_PREFIX} ({npcName}) Awake: NavMeshAgent 가져옴. 초기 순찰 지점 개수: {patrolPoints?.Length}");
    }

    /// <summary>
    /// 스폰 시 초기화: homePoint와 prefab 레퍼런스 세팅
    /// </summary>
    public void Initialize(Transform home, GameObject prefab, Transform[] newPatrolPoints)
    {
        homePoint = home;
        prefabRef = prefab;
        patrolPoints = newPatrolPoints;
        currentIndex = 0;
        currentState = State.Patrolling;
        // Initialize가 Awake 이후에 호출될 수 있으므로, npcName이 비어있으면 여기서도 설정
        if (string.IsNullOrEmpty(npcName)) npcName = gameObject.name; 

        string homePointName = homePoint ? homePoint.name : "null";
        string prefabName = prefabRef ? prefabRef.name : "null";
        int patrolPointsCount = patrolPoints != null ? patrolPoints.Length : 0;

        Debug.Log($"{LOG_PREFIX} ({npcName}) Initialize: 홈 포인트({homePointName}), 프리팹({prefabName}), 순찰 지점({patrolPointsCount}개) 설정 완료.");

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            if (patrolPoints[0] != null)
            {
                MoveTo(patrolPoints[0].position);
                Debug.Log($"{LOG_PREFIX} ({npcName}) 첫 번째 순찰 지점({patrolPoints[0].name})으로 이동 시작.");
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX} ({npcName}) 첫 번째 순찰 지점(patrolPoints[0])이 null입니다! 순찰을 시작할 수 없습니다.");
            }
        }
        else
        {
            Debug.LogWarning($"{LOG_PREFIX} ({npcName}) 순찰 지점이 설정되지 않았거나 비어있습니다. 순찰을 시작하지 않습니다.");
            // 필요하다면 여기서 복귀 로직을 바로 호출할 수 있습니다.
            // StartReturn();
        }
    }

    private void Update()
    {
        if (agent == null) 
        {
            Debug.LogError($"{LOG_PREFIX} ({npcName}) Update: NavMeshAgent가 null입니다!");
            return;
        }
        if (!agent.isOnNavMesh)
        {
            // isOnNavMesh는 false여도 에러는 아닐 수 있지만, 이동이 불가하므로 경고
            Debug.LogWarning($"{LOG_PREFIX} ({npcName}) Update: NavMesh 위에 있지 않습니다. 현재 위치: {transform.position}");
            return; 
        }
        if (agent.pathPending) 
        {
            return; 
        }

        if (currentState == State.Patrolling && agent.remainingDistance <= agent.stoppingDistance)
        {
            OnPatrolPointReached();
        }
        else if (currentState == State.Returning && agent.remainingDistance <= returnTolerance)
        {
            OnReturnHomeReached();
        }
    }

    private void OnPatrolPointReached()
    {
        if (patrolPoints == null || currentIndex < 0 || currentIndex >= patrolPoints.Length || patrolPoints[currentIndex] == null)
        {
            Debug.LogError($"{LOG_PREFIX} ({npcName}) OnPatrolPointReached: 순찰 지점 데이터(patrolPoints)가 유효하지 않거나 현재 인덱스({currentIndex})가 범위를 벗어났습니다. 복귀를 시도합니다.");
            StartReturn();
            return;
        }

        Debug.Log($"{LOG_PREFIX} ({npcName}) OnPatrolPointReached: 순찰 지점 {currentIndex} ({patrolPoints[currentIndex].name}) 도달.");
        currentIndex++;
        if (currentIndex >= patrolPoints.Length)
        {
            Debug.Log($"{LOG_PREFIX} ({npcName}) 모든 순찰 지점 완료.");
            if (loopPatrol)
            {
                currentIndex = 0;
                if (patrolPoints[currentIndex] != null)
                {
                    Debug.Log($"{LOG_PREFIX} ({npcName}) 순찰 루프 시작. 다음 지점: {patrolPoints[currentIndex].name}");
                    MoveTo(patrolPoints[currentIndex].position);
                }
                else
                {
                    Debug.LogError($"{LOG_PREFIX} ({npcName}) 순찰 루프 시작 지점(patrolPoints[0])이 null입니다. 복귀합니다.");
                    StartReturn();
                }
            }
            else
            {
                StartReturn();
            }
        }
        else // 다음 순찰 지점으로 이동
        {
            if (patrolPoints[currentIndex] != null)
            {
                Debug.Log($"{LOG_PREFIX} ({npcName}) 다음 순찰 지점({patrolPoints[currentIndex].name})으로 이동.");
                MoveTo(patrolPoints[currentIndex].position);
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX} ({npcName}) 다음 순찰 지점(patrolPoints[{currentIndex}])이 null입니다! 순찰을 중단하고 복귀합니다.");
                StartReturn();
            }
        }
    }

    private void StartReturn()
    {
        currentState = State.Returning;
        string homeName = homePoint ? homePoint.name : "null";
        Vector3 homePos = homePoint ? homePoint.position : Vector3.zero;
        Debug.Log($"{LOG_PREFIX} ({npcName}) StartReturn: 상태를 Returning으로 변경. 홈 포인트({homeName}, {homePos})로 복귀 시작.");
        if (homePoint != null)
        {
            MoveTo(homePoint.position);
        }
        else
        {
             Debug.LogError($"{LOG_PREFIX} ({npcName}) StartReturn: 홈 포인트가 null입니다! 복귀할 수 없습니다. 풀에 즉시 반환 시도.");
             if (NPCPoolManager.Instance != null && prefabRef != null)
             {
                NPCPoolManager.Instance.Release(prefabRef, gameObject);
             }
             else
             {
                Debug.LogError($"{LOG_PREFIX} ({npcName}) NPCPoolManager 또는 prefabRef가 null이어서 풀에 반환 불가. GameObject 파괴.");
                Destroy(gameObject);
             }
        }
    }

    private void OnReturnHomeReached()
    {
        string homeName = homePoint ? homePoint.name : "null";
        Debug.Log($"{LOG_PREFIX} ({npcName}) OnReturnHomeReached: 홈 포인트({homeName}) 도달. NPCPoolManager에 반환 시도.");
        if (NPCPoolManager.Instance != null && prefabRef != null)
        {
            NPCPoolManager.Instance.Release(prefabRef, gameObject);
        }
        else
        {
            Debug.LogError($"{LOG_PREFIX} ({npcName}) NPCPoolManager 또는 prefabRef가 null이어서 풀에 반환 불가. GameObject 파괴.");
            Destroy(gameObject);
        }
    }

    private void MoveTo(Vector3 destination)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(destination);
            Debug.Log($"{LOG_PREFIX} ({npcName}) MoveTo: 상태({currentState}), 목적지({destination}), 게임오브젝트({gameObject.name}) 이동 명령.");
        }
        else
        {
             if(agent == null) Debug.LogError($"{LOG_PREFIX} ({npcName}) MoveTo: NavMeshAgent가 null입니다. 목적지: {destination}");
             else Debug.LogError($"{LOG_PREFIX} ({npcName}) MoveTo: NavMeshAgent가 NavMesh 위에 없습니다. 목적지: {destination}");
        }
    }
}
