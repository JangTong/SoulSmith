using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NPCBase))]
public class NPCScheduleManager : MonoBehaviour
{
    private const string LOG_PREFIX = "[NPCScheduleManager]";

    [Header("Schedule Settings")]
    public NPCSchedule schedule; // 할당할 스케줄
    public bool enableSchedule = true; // 스케줄 활성화 여부
    public bool debugSchedule = true; // 디버그 로그 출력 여부
    
    [Header("Movement Settings")]
    public float updateInterval = 1f; // 스케줄 체크 간격 (초)
    public float arrivalTolerance = 1f; // 도착 판정 거리
    public bool smoothMovement = true; // 부드러운 이동 여부
    
    [Header("Animation Integration")]
    public bool useAnimationController = true; // 애니메이션 컨트롤러 사용 여부
    
    [Header("Current Status")]
    [SerializeField] private string currentScheduleTime = "";
    [SerializeField] private string currentLocation = "";
    [SerializeField] private NPCBehaviorType currentBehavior = NPCBehaviorType.Idle;
    [SerializeField] private bool isExecutingSchedule = false;
    [SerializeField] private bool pauseSchedule = false; // 스케줄 일시 정지 여부
    
    // 컴포넌트 참조
    private NPCBase npcBase;
    private NPCAnimationController animationController;
    private NavMeshAgent navAgent;
    
    // 스케줄 관리
    private ScheduleEntry currentEntry;
    private ScheduleEntry nextEntry;
    private bool hasArrivedAtDestination = false;
    private float lastUpdateTime = 0f;
    private Coroutine currentScheduleCoroutine;
    private Coroutine lookAroundCoroutine;
    
    // 이동 상태
    private Vector3 targetPosition;
    private bool isMovingToTarget = false;

    private void Awake()
    {
        // 필수 컴포넌트 가져오기
        npcBase = GetComponent<NPCBase>();
        navAgent = GetComponent<NavMeshAgent>();
        
        // 선택적 컴포넌트
        if (useAnimationController)
        {
            animationController = GetComponent<NPCAnimationController>();
            if (animationController == null)
            {
                Debug.LogWarning($"{LOG_PREFIX} ({gameObject.name}) NPCAnimationController가 없습니다. 애니메이션 비활성화됩니다.");
                useAnimationController = false;
            }
        }
        
        if (debugSchedule)
        {
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 스케줄 매니저 초기화 완료");
        }
    }

    private void Start()
    {
        // TimeManager 이벤트 구독
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayStarted += OnDayStarted;
            TimeManager.Instance.OnTimerUpdated += OnTimeUpdated;
        }
        
        // 초기 스케줄 시작
        if (enableSchedule && schedule != null)
        {
            StartScheduleSystem();
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayStarted -= OnDayStarted;
            TimeManager.Instance.OnTimerUpdated -= OnTimeUpdated;
        }
        
        // 실행 중인 코루틴 정리
        StopAllScheduleCoroutines();
    }

    private void OnDayStarted()
    {
        if (enableSchedule && schedule != null)
        {
            if (debugSchedule)
            {
                Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 새로운 날 시작 - 스케줄 초기화");
            }
            
            StartScheduleSystem();
        }
    }

    private void OnTimeUpdated(float gameTimer)
    {
        if (!enableSchedule || schedule == null || TimeManager.Instance == null) return;
        
        // 스케줄이 일시 정지된 경우 업데이트 건너뛰기
        if (pauseSchedule)
        {
            if (debugSchedule)
            {
                Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 스케줄 일시 정지 중 - 시간 업데이트 건너뛰기");
            }
            return;
        }
        
        // 업데이트 간격 체크
        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;
        
        // 현재 시간에 맞는 스케줄 엔트리 가져오기
        int currentHour = TimeManager.Instance.hours;
        int currentMinute = TimeManager.Instance.minutes;
        ScheduleEntry newEntry = schedule.GetCurrentScheduleEntry(currentHour, currentMinute);
        
        // 스케줄이 변경되었는지 확인
        if (newEntry != currentEntry)
        {
            ExecuteScheduleEntry(newEntry);
        }
        
        // 현재 상태 업데이트
        UpdateCurrentStatus(currentHour, currentMinute);
    }

    private void StartScheduleSystem()
    {
        if (TimeManager.Instance == null)
        {
            Debug.LogError($"{LOG_PREFIX} ({gameObject.name}) TimeManager를 찾을 수 없습니다!");
            return;
        }
        
        // 현재 시간에 맞는 스케줄 실행
        int currentHour = TimeManager.Instance.hours;
        int currentMinute = TimeManager.Instance.minutes;
        ScheduleEntry initialEntry = schedule.GetCurrentScheduleEntry(currentHour, currentMinute);
        
        if (debugSchedule)
        {
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 스케줄 시스템 시작 - 현재 시간: {currentHour:00}:{currentMinute:00}");
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 스케줄 정보:\n{schedule.GetScheduleDebugInfo()}");
        }
        
        ExecuteScheduleEntry(initialEntry);
    }

    private void ExecuteScheduleEntry(ScheduleEntry entry)
    {
        if (entry == null)
        {
            if (debugSchedule)
            {
                Debug.LogWarning($"{LOG_PREFIX} ({gameObject.name}) 실행할 스케줄 엔트리가 없습니다.");
            }
            return;
        }
        
        // 이전 스케줄 정리
        StopAllScheduleCoroutines();
        
        currentEntry = entry;
        hasArrivedAtDestination = false;
        
        if (debugSchedule)
        {
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 스케줄 실행: {entry.GetTimeString()} - {entry.locationName} ({entry.behaviorType})");
        }
        
        // 스케줄 실행 코루틴 시작
        currentScheduleCoroutine = StartCoroutine(ExecuteScheduleCoroutine(entry));
    }

    private IEnumerator ExecuteScheduleCoroutine(ScheduleEntry entry)
    {
        isExecutingSchedule = true;
        
        // 1. 목적지로 이동 (새로운 위치 시스템 적용)
        if (entry.HasValidTarget())
        {
            yield return StartCoroutine(MoveToTarget(entry));
        }
        
        // 2. 행동 설정
        SetBehavior(entry.behaviorType);
        
        // 3. 애니메이션 설정
        if (useAnimationController && animationController != null)
        {
            animationController.SetBehavior(entry.behaviorType);
            if (!string.IsNullOrEmpty(entry.animationState))
            {
                animationController.SetAnimationState(entry.animationState);
            }
        }
        
        // 4. 특별 행동 (둘러보기)
        if (entry.shouldLookAround)
        {
            lookAroundCoroutine = StartCoroutine(LookAroundRoutine(entry.lookAroundInterval));
        }
        
        // 5. 대기 시간
        if (entry.waitTimeBeforeNext > 0f)
        {
            yield return new WaitForSeconds(entry.waitTimeBeforeNext);
        }
        
        isExecutingSchedule = false;
        
        if (debugSchedule)
        {
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 스케줄 엔트리 실행 완료: {entry.GetLocationDisplayName()}");
        }
    }

    private IEnumerator MoveToTarget(ScheduleEntry entry)
    {
        // 유효한 목적지가 있는지 확인
        if (!entry.HasValidTarget())
        {
            if (debugSchedule)
            {
                Debug.LogWarning($"{LOG_PREFIX} ({gameObject.name}) 유효한 목적지가 없습니다: {entry.GetLocationDisplayName()}");
            }
            yield break;
        }
        
        if (navAgent == null) yield break;
        
        targetPosition = entry.GetTargetPosition();
        
        // 이미 목적지 근처에 있는지 확인
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        if (distanceToTarget <= arrivalTolerance)
        {
            if (debugSchedule)
            {
                Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 이미 목적지 근처에 있음: {entry.GetLocationDisplayName()} (거리: {distanceToTarget:F2}m) - 이동 건너뛰기");
            }
            
            // 애니메이션만 설정하고 이동은 건너뛰기
            if (useAnimationController && animationController != null)
            {
                animationController.SetMoving(false);
            }
            yield break;
        }
        
        isMovingToTarget = true;
        
        // NavMeshAgent 설정
        navAgent.speed = entry.moveSpeed;
        navAgent.stoppingDistance = entry.stoppingDistance;
        navAgent.isStopped = false; // 이동 시작 전 정지 해제
        
        // 부드러운 이동을 위한 설정
        navAgent.autoBraking = false; // 목적지 근처에서 급격한 감속 방지
        navAgent.acceleration = Mathf.Min(navAgent.acceleration, 4f); // 가속도 제한
        
        // 애니메이션 설정 (이동 시작)
        if (useAnimationController && animationController != null)
        {
            animationController.SetMoving(true, entry.moveSpeed);
            animationController.SetBehavior(NPCBehaviorType.Walking);
        }
        
        // 목적지 설정
        if (navAgent.isOnNavMesh)
        {
            navAgent.SetDestination(targetPosition);
            
            if (debugSchedule)
            {
                Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 이동 시작: {entry.GetLocationDisplayName()} (거리: {Vector3.Distance(transform.position, targetPosition):F2}m)");
            }
        }
        else
        {
            Debug.LogWarning($"{LOG_PREFIX} ({gameObject.name}) NavMesh 위에 있지 않습니다. 이동을 건너뜁니다.");
            isMovingToTarget = false;
            yield break;
        }
        
        // 도착할 때까지 대기
        while (isMovingToTarget)
        {
            // 스케줄이 일시 정지된 경우 대기
            if (pauseSchedule)
            {
                yield return null;
                continue;
            }
            
            if (navAgent.pathPending)
            {
                yield return null;
                continue;
            }
            
            float remainingDistance = navAgent.remainingDistance;
            
            // 도착 체크
            if (remainingDistance <= arrivalTolerance)
            {
                hasArrivedAtDestination = true;
                isMovingToTarget = false;
                
                // NavMeshAgent 완전 정지
                navAgent.isStopped = true;
                navAgent.ResetPath();
                navAgent.velocity = Vector3.zero; // 관성으로 인한 이동 방지
                navAgent.autoBraking = true; // 자동 브레이킹 복구
                
                // 애니메이션 설정 (이동 종료)
                if (useAnimationController && animationController != null)
                {
                    animationController.SetMoving(false);
                }
                
                if (debugSchedule)
                {
                    Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 목적지 도착: {entry.GetLocationDisplayName()} - NavAgent 정지됨");
                }
                
                break;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator LookAroundRoutine(float interval)
    {
        while (isExecutingSchedule && currentEntry != null && currentEntry.shouldLookAround)
        {
            yield return new WaitForSeconds(interval);
            
            if (useAnimationController && animationController != null)
            {
                animationController.PlayLookAround();
            }
            else
            {
                // 애니메이션 컨트롤러가 없으면 간단히 회전
                StartCoroutine(SimpleLookAround());
            }
            
            if (debugSchedule)
            {
                Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 둘러보기 실행");
            }
        }
    }

    private IEnumerator SimpleLookAround()
    {
        Quaternion originalRotation = transform.rotation;
        
        // 좌측으로 45도 회전
        Quaternion leftRotation = originalRotation * Quaternion.Euler(0, -45, 0);
        yield return StartCoroutine(RotateTowards(leftRotation, 1f));
        
        // 우측으로 90도 회전
        Quaternion rightRotation = originalRotation * Quaternion.Euler(0, 45, 0);
        yield return StartCoroutine(RotateTowards(rightRotation, 1f));
        
        // 원래 방향으로 복귀
        yield return StartCoroutine(RotateTowards(originalRotation, 0.5f));
    }

    private IEnumerator RotateTowards(Quaternion targetRotation, float duration)
    {
        Quaternion startRotation = transform.rotation;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }
        
        transform.rotation = targetRotation;
    }

    private void SetBehavior(NPCBehaviorType behaviorType)
    {
        currentBehavior = behaviorType;
        
        // 추가 행동 로직을 여기에 구현할 수 있습니다
        // 예: 특정 행동에 따른 상호작용 활성화/비활성화 등
        
        if (debugSchedule)
        {
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 행동 변경: {behaviorType}");
        }
    }

    private void UpdateCurrentStatus(int hour, int minute)
    {
        currentScheduleTime = $"{hour:00}:{minute:00}";
        currentLocation = currentEntry?.GetLocationDisplayName() ?? "Unknown";
        
        // 다음 스케줄 정보 업데이트
        if (schedule != null)
        {
            nextEntry = schedule.GetNextScheduleEntry(hour, minute);
        }
    }

    private void StopAllScheduleCoroutines()
    {
        if (currentScheduleCoroutine != null)
        {
            StopCoroutine(currentScheduleCoroutine);
            currentScheduleCoroutine = null;
        }
        
        if (lookAroundCoroutine != null)
        {
            StopCoroutine(lookAroundCoroutine);
            lookAroundCoroutine = null;
        }
        
        isExecutingSchedule = false;
        isMovingToTarget = false;
    }

    /// <summary>
    /// 스케줄 변경
    /// </summary>
    public void SetSchedule(NPCSchedule newSchedule)
    {
        schedule = newSchedule;
        
        if (enableSchedule && newSchedule != null)
        {
            StartScheduleSystem();
            
            if (debugSchedule)
            {
                Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 스케줄 변경: {newSchedule.scheduleName}");
            }
        }
    }

    /// <summary>
    /// 스케줄 시스템 활성화/비활성화
    /// </summary>
    public void SetScheduleEnabled(bool enabled)
    {
        enableSchedule = enabled;
        
        if (!enabled)
        {
            StopAllScheduleCoroutines();
        }
        else if (schedule != null)
        {
            StartScheduleSystem();
        }
        
        if (debugSchedule)
        {
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 스케줄 시스템 {(enabled ? "활성화" : "비활성화")}");
        }
    }

    /// <summary>
    /// 현재 스케줄 상태 정보 반환
    /// </summary>
    public NPCScheduleStatus GetScheduleStatus()
    {
        return new NPCScheduleStatus
        {
            isEnabled = enableSchedule,
            currentTime = currentScheduleTime,
            currentLocation = currentLocation,
            currentBehavior = currentBehavior,
            isExecuting = isExecutingSchedule,
            isMoving = isMovingToTarget,
            hasArrived = hasArrivedAtDestination,
            scheduleName = schedule?.scheduleName ?? "None"
        };
    }

    /// <summary>
    /// 디버그용 스케줄 정보 출력
    /// </summary>
    [ContextMenu("Debug Schedule Status")]
    public void DebugScheduleStatus()
    {
        var status = GetScheduleStatus();
        string nextInfo = nextEntry != null ? $"{nextEntry.GetTimeString()} - {nextEntry.locationName}" : "None";
        
        Debug.Log($"{LOG_PREFIX} ({gameObject.name}) Schedule Status:\n" +
                 $"- Enabled: {status.isEnabled}\n" +
                 $"- Schedule: {status.scheduleName}\n" +
                 $"- Time: {status.currentTime}\n" +
                 $"- Location: {status.currentLocation}\n" +
                 $"- Behavior: {status.currentBehavior}\n" +
                 $"- Executing: {status.isExecuting}\n" +
                 $"- Moving: {status.isMoving}\n" +
                 $"- Arrived: {status.hasArrived}\n" +
                 $"- Next: {nextInfo}");
    }

    /// <summary>
    /// 스케줄 일시 정지 (대화 등의 상호작용 중)
    /// </summary>
    public void PauseSchedule()
    {
        if (pauseSchedule) return;
        
        pauseSchedule = true;
        
        // 진행 중인 이동 정지
        if (isMovingToTarget && navAgent != null)
        {
            navAgent.isStopped = true;
        }
        
        // 실행 중인 코루틴 일시 정지는 하지 않고, UpdateWalkingState에서 체크하도록 함
        
        if (debugSchedule)
        {
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 스케줄 일시 정지 (대화 중)");
        }
    }

    /// <summary>
    /// 스케줄 재개 (상호작용 종료 후)
    /// </summary>
    public void ResumeSchedule()
    {
        if (!pauseSchedule) return;
        
        pauseSchedule = false;
        
        // 이동이 진행 중이었다면 재개
        if (isMovingToTarget && navAgent != null)
        {
            navAgent.isStopped = false;
        }
        
        // 대화 중에 시간이 흘렀을 수 있으므로 현재 시간에 맞는 스케줄로 즉시 업데이트
        if (enableSchedule && schedule != null && TimeManager.Instance != null)
        {
            int currentHour = TimeManager.Instance.hours;
            int currentMinute = TimeManager.Instance.minutes;
            ScheduleEntry newEntry = schedule.GetCurrentScheduleEntry(currentHour, currentMinute);
            
            // 현재 스케줄과 다르면 새로운 스케줄 실행
            if (newEntry != currentEntry)
            {
                if (debugSchedule)
                {
                    Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 스케줄 재개 시 시간 변경 감지 - 새로운 스케줄 실행: {newEntry?.GetTimeString()} - {newEntry?.locationName}");
                }
                ExecuteScheduleEntry(newEntry);
            }
        }
        
        if (debugSchedule)
        {
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 스케줄 재개 (대화 종료)");
        }
    }
}

/// <summary>
/// 스케줄 상태 정보 구조체
/// </summary>
[System.Serializable]
public struct NPCScheduleStatus
{
    public bool isEnabled;
    public string currentTime;
    public string currentLocation;
    public NPCBehaviorType currentBehavior;
    public bool isExecuting;
    public bool isMoving;
    public bool hasArrived;
    public string scheduleName;
} 