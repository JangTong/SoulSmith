using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class NPCAnimationController : MonoBehaviour
{
    private const string LOG_PREFIX = "[NPCAnimationController]";

    [Header("Animation Settings")]
    public bool enableAnimations = true;
    public float animationTransitionTime = 0.1f;
    
    [Header("Auto Walking Detection")]
    [Tooltip("NavMeshAgent의 이동을 자동으로 감지해서 Walking 애니메이션 재생")]
    public bool autoDetectWalking = true;
    [Tooltip("이동 감지를 위한 최소 속도 임계값")]
    public float walkingSpeedThreshold = 0.05f;
    [Tooltip("Walking 상태 업데이트 주기 (초)")]
    public float walkingUpdateInterval = 0.1f;
    
    [Header("Animation Parameters")]
    public string speedParameterName = "Speed";
    public string behaviorParameterName = "Behavior";
    public string isMovingParameterName = "IsMoving";
    public string triggerParameterName = "Trigger";
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    private Animator animator;
    private NavMeshAgent navAgent;
    private string currentAnimationState = "";
    private NPCBehaviorType currentBehavior = NPCBehaviorType.Idle;
    private NPCBehaviorType behaviorBeforeWalking = NPCBehaviorType.Idle; // Walking 이전 행동 저장
    private bool isMoving = false;
    private bool isAutoWalking = false; // 자동 Walking 상태인지 확인
    private float currentSpeed = 0f;
    
    // 애니메이션 상태 매핑
    private readonly System.Collections.Generic.Dictionary<NPCBehaviorType, string> behaviorToAnimation = 
        new System.Collections.Generic.Dictionary<NPCBehaviorType, string>()
        {
            { NPCBehaviorType.Idle, "Idle" },
            { NPCBehaviorType.Walking, "Walking" },
            { NPCBehaviorType.Working, "Working" },
            { NPCBehaviorType.Talking, "Talking" },
            { NPCBehaviorType.Sitting, "Sitting" },
            { NPCBehaviorType.Eating, "Eating" },
            { NPCBehaviorType.Shopping, "Shopping" },
            { NPCBehaviorType.Sleeping, "Sleeping" },
            { NPCBehaviorType.Reading, "Reading" },
            { NPCBehaviorType.Custom, "Custom" }
        };

    private void Awake()
    {
        animator = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();
        
        if (animator == null)
        {
            Debug.LogError($"{LOG_PREFIX} ({gameObject.name}) Animator 컴포넌트를 찾을 수 없습니다!");
            enableAnimations = false;
        }
        
        if (navAgent == null && autoDetectWalking)
        {
            Debug.LogWarning($"{LOG_PREFIX} ({gameObject.name}) NavMeshAgent가 없어서 자동 Walking 감지를 비활성화합니다.");
            autoDetectWalking = false;
        }
        
        Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 애니메이션 컨트롤러 초기화 완료 (자동 Walking: {autoDetectWalking})");
    }

    private void Start()
    {
        // 초기 애니메이션 상태 설정
        SetBehavior(NPCBehaviorType.Idle);
        
        // 자동 Walking 감지 시작
        if (autoDetectWalking && navAgent != null)
        {
            InvokeRepeating(nameof(UpdateWalkingState), 0f, walkingUpdateInterval);
        }
    }

    private void OnDisable()
    {
        // InvokeRepeating 정리
        CancelInvoke(nameof(UpdateWalkingState));
    }

    /// <summary>
    /// NavMeshAgent의 이동 상태를 확인해서 자동으로 Walking 애니메이션 처리
    /// </summary>
    private void UpdateWalkingState()
    {
        if (!enableAnimations || !autoDetectWalking || navAgent == null) return;

        bool shouldBeWalking = IsAgentMoving();
        
        // 현재 이동 중이고 Walking 상태가 아닌 경우
        if (shouldBeWalking && !isAutoWalking)
        {
            StartAutoWalking();
        }
        // 이동이 멈췄고 자동 Walking 상태인 경우
        else if (!shouldBeWalking && isAutoWalking)
        {
            StopAutoWalking();
        }
        
        // 이동 중이면 속도 업데이트
        if (shouldBeWalking)
        {
            UpdateMovementSpeed();
        }
    }

    /// <summary>
    /// NavMeshAgent가 실제로 이동 중인지 확인
    /// </summary>
    private bool IsAgentMoving()
    {
        if (navAgent == null || !navAgent.isOnNavMesh) return false;
        
        // 명시적으로 정지된 경우
        if (navAgent.isStopped) return false;
        
        // 경로가 없으면 이동 중이 아님 (관성으로 움직여도 무시)
        bool hasPath = navAgent.hasPath && !navAgent.pathPending;
        if (!hasPath)
        {
            if (showDebugLogs && navAgent.velocity.magnitude > walkingSpeedThreshold)
            {
                Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 경로 없음 - 관성 이동 무시: Velocity={navAgent.velocity.magnitude:F3}");
            }
            return false;
        }
        
        // 경로가 있고 실제로 움직이고 있으면 이동 중
        bool isActuallyMoving = navAgent.velocity.magnitude > walkingSpeedThreshold;
        
        if (showDebugLogs)
        {
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 이동 체크: HasPath={hasPath}, Velocity={navAgent.velocity.magnitude:F3}, Threshold={walkingSpeedThreshold}");
        }
        
        return hasPath && isActuallyMoving;
    }

    /// <summary>
    /// 자동 Walking 시작
    /// </summary>
    private void StartAutoWalking()
    {
        if (isAutoWalking) return;
        
        // 현재 행동이 Walking이 아니면 백업
        if (currentBehavior != NPCBehaviorType.Walking)
        {
            behaviorBeforeWalking = currentBehavior;
        }
        
        isAutoWalking = true;
        SetBehavior(NPCBehaviorType.Walking);
        
        if (showDebugLogs)
        {
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 자동 Walking 시작 (이전 행동: {behaviorBeforeWalking})");
        }
    }

    /// <summary>
    /// 자동 Walking 종료
    /// </summary>
    private void StopAutoWalking()
    {
        if (!isAutoWalking) return;
        
        if (showDebugLogs)
        {
            string agentInfo = navAgent != null ? 
                $"NavAgent - isStopped: {navAgent.isStopped}, hasPath: {navAgent.hasPath}, velocity: {navAgent.velocity.magnitude:F3}" : 
                "No NavAgent";
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 자동 Walking 종료 - {agentInfo}");
        }
        
        isAutoWalking = false;
        
        // NavMeshAgent가 여전히 이동 중이라면 정지
        if (navAgent != null && navAgent.isOnNavMesh && !navAgent.isStopped)
        {
            // 단, 스케줄 시스템에서 관리하는 이동이 아닌 경우에만 정지
            // (다른 시스템에서 제어하는 이동을 방해하지 않기 위해)
            if (navAgent.velocity.magnitude < walkingSpeedThreshold)
            {
                navAgent.isStopped = true;
                navAgent.ResetPath();
            }
        }
        
        // 이전 행동으로 복귀
        SetBehavior(behaviorBeforeWalking);
        
        // 이동 상태 해제
        SetMoving(false, 0f);
        
        if (showDebugLogs)
        {
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 자동 Walking 종료 완료 (복귀 행동: {behaviorBeforeWalking})");
        }
    }

    /// <summary>
    /// 이동 속도 업데이트
    /// </summary>
    private void UpdateMovementSpeed()
    {
        if (navAgent == null) return;
        
        float normalizedSpeed = navAgent.velocity.magnitude / navAgent.speed;
        SetMoving(true, normalizedSpeed);
    }

    /// <summary>
    /// 행동 타입에 따른 애니메이션 설정 (자동 Walking 고려)
    /// </summary>
    public void SetBehavior(NPCBehaviorType behaviorType)
    {
        if (!enableAnimations || animator == null) return;

        // 자동 Walking 중이고 Walking이 아닌 행동을 설정하려는 경우
        if (isAutoWalking && behaviorType != NPCBehaviorType.Walking)
        {
            // 이전 행동만 업데이트하고 실제 애니메이션은 변경하지 않음
            behaviorBeforeWalking = behaviorType;
            
            if (showDebugLogs)
            {
                Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 자동 Walking 중 - 행동 백업: {behaviorType}");
            }
            return;
        }

        if (currentBehavior == behaviorType) return;

        currentBehavior = behaviorType;
        
        // 1순위: Behavior 파라미터 기반 전환 (권장)
        if (HasParameter(behaviorParameterName))
        {
            animator.SetInteger(behaviorParameterName, (int)behaviorType);
            
            if (showDebugLogs)
            {
                Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 행동 변경 (파라미터): {behaviorType} -> Behavior = {(int)behaviorType}");
            }
        }
        // 2순위: 직접 애니메이션 상태 변경 (fallback)
        else if (behaviorToAnimation.TryGetValue(behaviorType, out string animationState))
        {
            SetAnimationState(animationState);
            
            if (showDebugLogs)
            {
                Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 행동 변경 (직접): {behaviorType} -> 애니메이션: {animationState}");
            }
        }
        else
        {
            Debug.LogWarning($"{LOG_PREFIX} ({gameObject.name}) 행동 타입 {behaviorType}에 해당하는 애니메이션을 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 강제로 특정 행동 설정 (자동 Walking 무시)
    /// </summary>
    public void ForceSetBehavior(NPCBehaviorType behaviorType)
    {
        if (isAutoWalking)
        {
            isAutoWalking = false;
            if (showDebugLogs)
            {
                Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 강제 행동 설정으로 자동 Walking 해제");
            }
        }
        
        SetBehavior(behaviorType);
    }

    /// <summary>
    /// 자동 Walking 감지 설정
    /// </summary>
    public void SetAutoWalkingEnabled(bool enabled)
    {
        if (autoDetectWalking == enabled) return;
        
        autoDetectWalking = enabled;
        
        if (enabled && navAgent != null)
        {
            InvokeRepeating(nameof(UpdateWalkingState), 0f, walkingUpdateInterval);
        }
        else
        {
            CancelInvoke(nameof(UpdateWalkingState));
            
            // 자동 Walking 중이었다면 해제
            if (isAutoWalking)
            {
                StopAutoWalking();
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 자동 Walking 감지: {enabled}");
        }
    }

    /// <summary>
    /// 직접 애니메이션 상태 설정
    /// </summary>
    public void SetAnimationState(string stateName)
    {
        if (!enableAnimations || animator == null || string.IsNullOrEmpty(stateName)) return;

        if (currentAnimationState == stateName) 
        {
            // 이미 같은 상태가 재생 중이면 다시 시작하지 않음 (루프 보호)
            return;
        }

        try
        {
            // 상태가 존재하는지 확인
            if (HasState(stateName))
            {
                // 현재 재생 중인 상태와 다른 경우에만 Play 호출
                AnimatorStateInfo currentStateInfo = animator.GetCurrentAnimatorStateInfo(0);
                int stateHash = Animator.StringToHash(stateName);
                
                if (!currentStateInfo.IsName(stateName) || !currentStateInfo.loop)
                {
                    animator.Play(stateName, 0, 0f);
                }
                
                currentAnimationState = stateName;
                
                if (showDebugLogs)
                {
                    Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 애니메이션 상태 변경: {stateName}");
                }
            }
            else
            {
                Debug.LogWarning($"{LOG_PREFIX} ({gameObject.name}) 애니메이션 상태 '{stateName}'을 찾을 수 없습니다.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"{LOG_PREFIX} ({gameObject.name}) 애니메이션 상태 설정 중 오류: {e.Message}");
        }
    }

    /// <summary>
    /// 이동 상태 설정
    /// </summary>
    public void SetMoving(bool moving, float speed = 1f)
    {
        if (!enableAnimations || animator == null) return;

        if (isMoving != moving || Mathf.Abs(currentSpeed - speed) > 0.01f)
        {
            isMoving = moving;
            currentSpeed = speed;

            // IsMoving 파라미터 설정
            if (HasParameter(isMovingParameterName))
            {
                animator.SetBool(isMovingParameterName, moving);
            }

            // Speed 파라미터 설정
            if (HasParameter(speedParameterName))
            {
                animator.SetFloat(speedParameterName, moving ? speed : 0f);
            }

            if (showDebugLogs)
            {
                Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 이동 상태: {moving}, 속도: {speed:F2}");
            }
        }
    }

    /// <summary>
    /// 트리거 애니메이션 실행
    /// </summary>
    public void TriggerAnimation(string triggerName)
    {
        if (!enableAnimations || animator == null || string.IsNullOrEmpty(triggerName)) return;

        if (HasParameter(triggerName))
        {
            animator.SetTrigger(triggerName);
            
            if (showDebugLogs)
            {
                Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 트리거 실행: {triggerName}");
            }
        }
        else
        {
            Debug.LogWarning($"{LOG_PREFIX} ({gameObject.name}) 트리거 파라미터 '{triggerName}'를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 특정 행동에 대한 룩 어라운드 애니메이션
    /// </summary>
    public void PlayLookAround()
    {
        if (!enableAnimations || animator == null) return;

        StartCoroutine(LookAroundCoroutine());
    }

    private IEnumerator LookAroundCoroutine()
    {
        string originalState = currentAnimationState;
        
        // 좌측으로 고개 돌리기
        SetAnimationState("LookLeft");
        yield return new WaitForSeconds(1f);
        
        // 우측으로 고개 돌리기
        SetAnimationState("LookRight");
        yield return new WaitForSeconds(1f);
        
        // 원래 상태로 복귀
        SetAnimationState(originalState);
        
        if (showDebugLogs)
        {
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 룩 어라운드 애니메이션 완료");
        }
    }

    /// <summary>
    /// 애니메이터에 특정 파라미터가 있는지 확인
    /// </summary>
    private bool HasParameter(string parameterName)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName)) return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == parameterName)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 애니메이터에 특정 상태가 있는지 확인
    /// </summary>
    private bool HasState(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) return false;

        try
        {
            return animator.HasState(0, Animator.StringToHash(stateName));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 현재 애니메이션 정보 반환
    /// </summary>
    public AnimationInfo GetCurrentAnimationInfo()
    {
        return new AnimationInfo
        {
            currentState = currentAnimationState,
            currentBehavior = currentBehavior,
            behaviorBeforeWalking = behaviorBeforeWalking,
            isMoving = isMoving,
            isAutoWalking = isAutoWalking,
            speed = currentSpeed,
            isEnabled = enableAnimations,
            autoDetectWalking = autoDetectWalking
        };
    }

    /// <summary>
    /// 애니메이션 시스템 활성화/비활성화
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        enableAnimations = enabled;
        if (animator != null)
        {
            animator.enabled = enabled;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 애니메이션 시스템 {(enabled ? "활성화" : "비활성화")}");
        }
    }

    /// <summary>
    /// 디버그용 애니메이션 정보 출력
    /// </summary>
    [ContextMenu("Debug Animation Info")]
    public void DebugAnimationInfo()
    {
        if (animator == null)
        {
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) Animator가 없습니다.");
            return;
        }

        var info = GetCurrentAnimationInfo();
        string navAgentInfo = navAgent != null ? 
            $"NavAgent Speed: {navAgent.velocity.magnitude:F2}, Has Path: {navAgent.hasPath}" : 
            "No NavMeshAgent";
            
        Debug.Log($"{LOG_PREFIX} ({gameObject.name}) Animation Info:\n" +
                 $"- State: {info.currentState}\n" +
                 $"- Behavior: {info.currentBehavior}\n" +
                 $"- Behavior Before Walking: {info.behaviorBeforeWalking}\n" +
                 $"- Moving: {info.isMoving} (Speed: {info.speed:F2})\n" +
                 $"- Auto Walking: {info.isAutoWalking}\n" +
                 $"- Auto Detect Walking: {info.autoDetectWalking}\n" +
                 $"- Enabled: {info.isEnabled}\n" +
                 $"- {navAgentInfo}");
    }
}

/// <summary>
/// 애니메이션 정보 구조체
/// </summary>
[System.Serializable]
public struct AnimationInfo
{
    public string currentState;
    public NPCBehaviorType currentBehavior;
    public NPCBehaviorType behaviorBeforeWalking;
    public bool isMoving;
    public bool isAutoWalking;
    public float speed;
    public bool isEnabled;
    public bool autoDetectWalking;
} 