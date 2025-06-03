using UnityEngine;

/// <summary>
/// NPCBase를 상속받아 시간별 스케줄과 애니메이션 기능을 제공하는 NPC 클래스입니다.
/// TimeManager와 연동하여 시간에 따라 자동으로 위치를 이동하고 행동을 변경합니다.
/// </summary>
[RequireComponent(typeof(NPCScheduleManager))]
[RequireComponent(typeof(NPCAnimationController))]
public class ScheduledNPC : NPCBase
{
    private const string LOG_PREFIX = "[ScheduledNPC]";

    [Header("Schedule Integration")]
    [Tooltip("이 NPC가 사용할 일정 스케줄입니다.")]
    public NPCSchedule npcSchedule;
    [Tooltip("게임 시작 시 스케줄을 자동으로 시작할지 여부입니다.")]
    public bool autoStartSchedule = true;
    [Tooltip("스케줄과 일반 대화를 동시에 허용할지 여부입니다.")]
    public bool allowInteractionDuringSchedule = true;

    [Header("Animation Integration")]
    [Tooltip("Animator Controller가 할당된 경우 자동으로 애니메이션을 제어합니다.")]
    public bool autoControlAnimation = true;
    [Tooltip("이동 중에도 대화를 허용할지 여부입니다.")]
    public bool allowDialogueDuringMovement = false;

    [Header("Schedule Override")]
    [Tooltip("특별한 상황에서 스케줄을 일시 중단할지 여부입니다.")]
    public bool canOverrideSchedule = true;
    [Tooltip("스케줄 재개까지의 지연 시간입니다.")]
    public float scheduleResumeDelay = 2f;

    // 컴포넌트 참조
    private NPCScheduleManager scheduleManager;
    private NPCAnimationController animationController;
    
    // 상태 관리
    private bool isScheduleOverridden = false;
    private bool wasScheduleEnabledBeforeOverride = false;

    protected override void Awake()
    {
        base.Awake(); // NPCBase.Awake() 호출
        
        // 필수 컴포넌트 가져오기
        scheduleManager = GetComponent<NPCScheduleManager>();
        animationController = GetComponent<NPCAnimationController>();
        
        // 컴포넌트 유효성 검사
        if (scheduleManager == null)
        {
            Debug.LogError($"{LOG_PREFIX} ({NPCName}) NPCScheduleManager 컴포넌트가 필요합니다!");
        }
        
        if (animationController == null)
        {
            Debug.LogError($"{LOG_PREFIX} ({NPCName}) NPCAnimationController 컴포넌트가 필요합니다!");
        }
        
        Debug.Log($"{LOG_PREFIX} ({NPCName}) ScheduledNPC 초기화 완료");
    }

    private void Start()
    {
        // 스케줄 설정
        if (scheduleManager != null && npcSchedule != null)
        {
            scheduleManager.SetSchedule(npcSchedule);
            scheduleManager.SetScheduleEnabled(autoStartSchedule);
            
            Debug.Log($"{LOG_PREFIX} ({NPCName}) 스케줄 '{npcSchedule.scheduleName}' 설정 완료 (자동 시작: {autoStartSchedule})");
        }
        
        // 애니메이션 설정
        if (animationController != null)
        {
            animationController.SetEnabled(autoControlAnimation);
            
            Debug.Log($"{LOG_PREFIX} ({NPCName}) 애니메이션 컨트롤러 설정 완료 (자동 제어: {autoControlAnimation})");
        }
    }

    public override void Interact()
    {
        Debug.Log($"{LOG_PREFIX} ({NPCName}) 상호작용 시도");
        
        // 스케줄 실행 중 상호작용 제한 체크
        if (!CanInteractNow())
        {
            Debug.Log($"{LOG_PREFIX} ({NPCName}) 현재 상호작용이 제한됩니다.");
            
            // 제한된 상황에 대한 안내 메시지 (선택사항)
            if (dialogueData != null)
            {
                // 바쁨 상태 대화가 있다면 재생
                var busyDialogue = new System.Collections.Generic.List<DialogueLine>
                {
                    new DialogueLine { speaker = NPCName, text = "죄송해요, 지금은 좀 바쁘네요." }
                };
                DialogueManager.Instance.PlayGeneralDialogue(busyDialogue);
            }
            return;
        }
        
        // 스케줄 일시 중단 (설정된 경우)
        if (canOverrideSchedule && scheduleManager != null)
        {
            OverrideScheduleTemporarily();
        }
        
        // 애니메이션을 대화 상태로 변경
        if (animationController != null && autoControlAnimation)
        {
            animationController.SetBehavior(NPCBehaviorType.Talking);
        }
        
        // 부모 클래스의 상호작용 실행
        base.Interact();
    }

    /// <summary>
    /// 현재 상호작용이 가능한지 확인
    /// </summary>
    private bool CanInteractNow()
    {
        if (!allowInteractionDuringSchedule && scheduleManager != null)
        {
            var status = scheduleManager.GetScheduleStatus();
            
            // 이동 중일 때 대화 제한
            if (!allowDialogueDuringMovement && status.isMoving)
            {
                Debug.Log($"{LOG_PREFIX} ({NPCName}) 이동 중이므로 대화 불가");
                return false;
            }
            
            // 특정 행동 중일 때 대화 제한
            if (IsRestrictedBehavior(status.currentBehavior))
            {
                Debug.Log($"{LOG_PREFIX} ({NPCName}) 현재 행동({status.currentBehavior}) 중 대화 불가");
                return false;
            }
        }
        
        return true;
    }

    /// <summary>
    /// 대화가 제한되는 행동인지 확인
    /// </summary>
    private bool IsRestrictedBehavior(NPCBehaviorType behavior)
    {
        return behavior switch
        {
            NPCBehaviorType.Sleeping => true,
            NPCBehaviorType.Working => true, // 설정에 따라 조정 가능
            _ => false
        };
    }

    /// <summary>
    /// 스케줄을 일시적으로 중단
    /// </summary>
    private void OverrideScheduleTemporarily()
    {
        if (scheduleManager == null || isScheduleOverridden) return;
        
        var status = scheduleManager.GetScheduleStatus();
        wasScheduleEnabledBeforeOverride = status.isEnabled;
        
        if (wasScheduleEnabledBeforeOverride)
        {
            scheduleManager.SetScheduleEnabled(false);
            isScheduleOverridden = true;
            
            Debug.Log($"{LOG_PREFIX} ({NPCName}) 스케줄 일시 중단됨 (상호작용으로 인해)");
            
            // 일정 시간 후 스케줄 재개
            Invoke(nameof(ResumeSchedule), scheduleResumeDelay);
        }
    }

    /// <summary>
    /// 스케줄 재개
    /// </summary>
    private void ResumeSchedule()
    {
        if (!isScheduleOverridden || scheduleManager == null) return;
        
        scheduleManager.SetScheduleEnabled(wasScheduleEnabledBeforeOverride);
        isScheduleOverridden = false;
        
        Debug.Log($"{LOG_PREFIX} ({NPCName}) 스케줄 재개됨");
        
        // 애니메이션을 원래 행동으로 복원
        if (animationController != null && autoControlAnimation)
        {
            var status = scheduleManager.GetScheduleStatus();
            animationController.SetBehavior(status.currentBehavior);
        }
    }

    /// <summary>
    /// 새로운 스케줄 설정
    /// </summary>
    public void SetNPCSchedule(NPCSchedule newSchedule)
    {
        npcSchedule = newSchedule;
        
        if (scheduleManager != null)
        {
            scheduleManager.SetSchedule(newSchedule);
            Debug.Log($"{LOG_PREFIX} ({NPCName}) 새 스케줄 '{newSchedule?.scheduleName}' 적용됨");
        }
    }

    /// <summary>
    /// 스케줄 시스템 활성화/비활성화
    /// </summary>
    public void SetScheduleEnabled(bool enabled)
    {
        if (scheduleManager != null)
        {
            scheduleManager.SetScheduleEnabled(enabled);
            Debug.Log($"{LOG_PREFIX} ({NPCName}) 스케줄 시스템 {(enabled ? "활성화" : "비활성화")}");
        }
    }

    /// <summary>
    /// 애니메이션 시스템 활성화/비활성화
    /// </summary>
    public void SetAnimationEnabled(bool enabled)
    {
        if (animationController != null)
        {
            animationController.SetEnabled(enabled);
            autoControlAnimation = enabled;
            Debug.Log($"{LOG_PREFIX} ({NPCName}) 애니메이션 시스템 {(enabled ? "활성화" : "비활성화")}");
        }
    }

    /// <summary>
    /// 특정 행동으로 강제 변경 (스케줄 무시)
    /// </summary>
    public void ForceBehavior(NPCBehaviorType behavior, float duration = 0f)
    {
        if (animationController != null)
        {
            animationController.SetBehavior(behavior);
        }
        
        // 스케줄 일시 중단
        if (duration > 0f && scheduleManager != null)
        {
            OverrideScheduleTemporarily();
            CancelInvoke(nameof(ResumeSchedule));
            Invoke(nameof(ResumeSchedule), duration);
        }
        
        Debug.Log($"{LOG_PREFIX} ({NPCName}) 강제 행동 변경: {behavior} (지속시간: {duration:F1}초)");
    }

    /// <summary>
    /// 현재 NPC 상태 정보 반환
    /// </summary>
    public NPCStatusInfo GetNPCStatus()
    {
        var scheduleStatus = scheduleManager?.GetScheduleStatus() ?? default;
        var animationInfo = animationController?.GetCurrentAnimationInfo() ?? default;
        
        return new NPCStatusInfo
        {
            npcName = NPCName,
            isDialogueable = isDialogueable,
            scheduleStatus = scheduleStatus,
            animationInfo = animationInfo,
            isScheduleOverridden = isScheduleOverridden,
            canInteract = CanInteractNow()
        };
    }

    /// <summary>
    /// 디버그용 NPC 상태 정보 출력
    /// </summary>
    [ContextMenu("Debug NPC Status")]
    public void DebugNPCStatus()
    {
        var status = GetNPCStatus();
        
        Debug.Log($"{LOG_PREFIX} ({NPCName}) NPC Status:\n" +
                 $"- Name: {status.npcName}\n" +
                 $"- Dialogueable: {status.isDialogueable}\n" +
                 $"- Can Interact: {status.canInteract}\n" +
                 $"- Schedule Overridden: {status.isScheduleOverridden}\n" +
                 $"- Schedule: {status.scheduleStatus.scheduleName} ({status.scheduleStatus.currentTime})\n" +
                 $"- Location: {status.scheduleStatus.currentLocation}\n" +
                 $"- Behavior: {status.scheduleStatus.currentBehavior}\n" +
                 $"- Animation: {status.animationInfo.currentState}\n" +
                 $"- Moving: {status.scheduleStatus.isMoving}");
    }

    /// <summary>
    /// 특정 위치로 즉시 이동 (스케줄 무시)
    /// </summary>
    public void TeleportTo(Vector3 position)
    {
        transform.position = position;
        
        // NavMeshAgent 위치 동기화
        if (agent != null && agent.isOnNavMesh)
        {
            agent.Warp(position);
        }
        
        Debug.Log($"{LOG_PREFIX} ({NPCName}) 텔레포트: {position}");
    }

    /// <summary>
    /// 특정 위치로 스케줄과 관계없이 이동
    /// </summary>
    public void MoveToPosition(Vector3 position, float speed = 0f)
    {
        if (agent != null)
        {
            if (speed > 0f)
            {
                agent.speed = speed;
            }
            
            agent.SetDestination(position);
            
            // 애니메이션 설정
            if (animationController != null && autoControlAnimation)
            {
                animationController.SetMoving(true, agent.speed);
                animationController.SetBehavior(NPCBehaviorType.Walking);
            }
            
            Debug.Log($"{LOG_PREFIX} ({NPCName}) 수동 이동 시작: {position}");
        }
    }
}

/// <summary>
/// NPC 상태 정보 구조체
/// </summary>
[System.Serializable]
public struct NPCStatusInfo
{
    public string npcName;
    public bool isDialogueable;
    public NPCScheduleStatus scheduleStatus;
    public AnimationInfo animationInfo;
    public bool isScheduleOverridden;
    public bool canInteract;
} 