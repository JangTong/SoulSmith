using UnityEngine;

/// <summary>
/// ScheduledNPC를 상속받아 시간별 스케줄과 고객 거래 기능을 모두 제공하는 NPC 클래스입니다.
/// 스케줄에 따라 이동하면서 TradeZone과 상호작용하여 거래를 시도할 수 있습니다.
/// </summary>
public class CustomerNPC : ScheduledNPC
{
    private const string LOG_PREFIX = "[CustomerNPC]";

    [Header("Customer Settings")] 
    [Tooltip("이 고객 NPC가 가지는 요청 데이터입니다. ScriptableObject 형태로 관리됩니다.")]
    public CustomerRequestSO request; 
    [Tooltip("현재 고객 NPC가 상호작용 중인 TradeZone입니다. 주로 자동으로 감지됩니다.")]
    public TradeZone tradeZone;

    [Header("Customer Schedule Integration")]
    [Tooltip("거래 중일 때 스케줄을 일시 중단할지 여부입니다.")]
    public bool pauseScheduleDuringTrade = true;
    [Tooltip("거래 완료 후 다음 스케줄로 이동할지 여부입니다.")]
    public bool proceedToNextScheduleAfterTrade = true;
    [Tooltip("거래 시도 후 대기 시간입니다.")]
    public float postTradeWaitTime = 3f;

    private bool hasTradeAttempted = false; 
    private bool isInTrade = false; // 거래 중 상태 추가

    protected override void Awake()
    {
        base.Awake(); // ScheduledNPC.Awake() 호출 (NPCBase.Awake()도 포함)
        Debug.Log($"{LOG_PREFIX} ({NPCName}) CustomerNPC 초기화 완료. 요청: {request?.name}, 초기 TradeZone: {tradeZone?.name ?? "Null"}");
    }

    public override void Interact() 
    {
        Debug.Log($"{LOG_PREFIX} ({NPCName}) CustomerNPC Interact 시도.");

        // 거래 요청이 있는 경우 거래 우선 처리
        if (ShouldProcessTrade())
        {
            ProcessTradeInteraction();
            return;
        }
        
        // 거래 요청이 없거나 이미 완료된 경우 일반 상호작용 처리
        base.Interact(); // ScheduledNPC의 Interact 호출
    }

    /// <summary>
    /// 거래를 처리해야 하는지 확인
    /// </summary>
    private bool ShouldProcessTrade()
    {
        // 요청이 없으면 거래 불가
        if (request == null) return false;
        
        // 반복 불가능하고 이미 거래를 시도한 경우 거래 완료 대화만 처리
        if (!request.isRepeatable && hasTradeAttempted) return true;
        
        // 일반적인 거래 시도 조건
        return true;
    }

    /// <summary>
    /// 거래 상호작용 처리
    /// </summary>
    private void ProcessTradeInteraction()
    {
        // 반복 불가능한 거래이며 이미 거래를 시도한 경우
        if (!request.isRepeatable && hasTradeAttempted)
        {
            Debug.Log($"{LOG_PREFIX} ({NPCName}) 반복 불가능한 거래이며 이미 거래 시도됨. 거래 후 대화 시작.");
            PlayPostTradeDialogue();
            return;
        }
        
        // 일반적인 거래 시도 또는 반복 가능한 거래
        if (tradeZone != null)
        {
            Debug.Log($"{LOG_PREFIX} ({NPCName}) TradeZone({tradeZone.gameObject.name})을 통해 거래 대화 시작.");
            
            // 거래 중 스케줄 일시 중단
            if (pauseScheduleDuringTrade)
            {
                StartTrade();
            }
            
            tradeZone.OpenDialogue(this); 
        }
        else
        {
            Debug.LogWarning($"{LOG_PREFIX} ({NPCName}) 할당된 TradeZone이 없습니다. 거래를 시작할 수 없습니다.");
            
            // 거래 불가 안내 메시지
            var noTradeDialogue = new System.Collections.Generic.List<DialogueLine>
            {
                new DialogueLine { speaker = NPCName, text = "죄송해요, 지금은 거래할 수 없네요." }
            };
            DialogueManager.Instance.PlayGeneralDialogue(noTradeDialogue);
        }
    }

    /// <summary>
    /// 거래 후 대화 재생
    /// </summary>
    private void PlayPostTradeDialogue()
    {
        if (request.useInlinePostTradeDialogue && request.inlinePostTradeDialogue != null && request.inlinePostTradeDialogue.Count > 0)
        {
            DialogueManager.Instance.PlayGeneralDialogue(request.inlinePostTradeDialogue);
        }
        else if (!request.useInlinePostTradeDialogue && request.referencePostTradeDialogue != null)
        {
            DialogueManager.Instance.PlayGeneralDialogue(request.referencePostTradeDialogue);
        }
        else
        {
            Debug.LogWarning($"{LOG_PREFIX} ({NPCName}) 거래 후 대화가 설정되지 않았습니다. 일반 상호작용으로 처리.");
            base.Interact(); // ScheduledNPC의 일반 상호작용
        }
    }

    /// <summary>
    /// 거래 시작 - 스케줄 일시 중단
    /// </summary>
    private void StartTrade()
    {
        if (isInTrade) return;
        
        isInTrade = true;
        SetScheduleEnabled(false); // ScheduledNPC의 메서드 사용
        
        Debug.Log($"{LOG_PREFIX} ({NPCName}) 거래 시작 - 스케줄 일시 중단");
    }

    /// <summary>
    /// 거래 완료 - 스케줄 재개
    /// </summary>
    private void CompleteTrade()
    {
        if (!isInTrade) return;
        
        isInTrade = false;
        
        if (proceedToNextScheduleAfterTrade)
        {
            // 잠시 대기 후 스케줄 재개
            Invoke(nameof(ResumeScheduleAfterTrade), postTradeWaitTime);
        }
        
        Debug.Log($"{LOG_PREFIX} ({NPCName}) 거래 완료");
    }

    /// <summary>
    /// 거래 후 스케줄 재개
    /// </summary>
    private void ResumeScheduleAfterTrade()
    {
        SetScheduleEnabled(true); // ScheduledNPC의 메서드 사용
        Debug.Log($"{LOG_PREFIX} ({NPCName}) 거래 후 스케줄 재개");
    }

    /// <summary>
    /// 거래 시도가 완료되었음을 표시합니다. TradeZone에서 호출됩니다.
    /// </summary>
    public void MarkTradeAttempted()
    {
        hasTradeAttempted = true;
        CompleteTrade(); // 거래 완료 처리
        Debug.Log($"{LOG_PREFIX} ({NPCName}) MarkTradeAttempted: 거래 시도 완료로 표시됨 (isRepeatable: {request?.isRepeatable}).");
    }

    /// <summary>
    /// 고객 거래 상태 정보 가져오기
    /// </summary>
    public CustomerTradeInfo GetCustomerTradeInfo()
    {
        return new CustomerTradeInfo
        {
            request = this.request,
            tradeZone = this.tradeZone,
            hasTradeAttempted = this.hasTradeAttempted,
            isInTrade = this.isInTrade,
            canRepeatTrade = this.request?.isRepeatable ?? false
        };
    }

    /// <summary>
    /// 새로운 거래 요청 설정
    /// </summary>
    public void SetCustomerRequest(CustomerRequestSO newRequest)
    {
        request = newRequest;
        hasTradeAttempted = false; // 새 요청이면 거래 시도 초기화
        Debug.Log($"{LOG_PREFIX} ({NPCName}) 새로운 거래 요청 설정: {newRequest?.name}");
    }

    /// <summary>
    /// TradeZone 수동 설정
    /// </summary>
    public void SetTradeZone(TradeZone zone)
    {
        tradeZone = zone;
        Debug.Log($"{LOG_PREFIX} ({NPCName}) TradeZone 수동 설정: {zone?.name}");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<TradeZone>(out TradeZone enteredZone))
        {
            if (this.tradeZone == null) 
            {
                this.tradeZone = enteredZone;
                Debug.Log($"{LOG_PREFIX} ({NPCName}) TradeZone '{enteredZone.gameObject.name}'에 진입하여 할당됨.");
            }
            else if (this.tradeZone != enteredZone)
            {
                Debug.LogWarning($"{LOG_PREFIX} ({NPCName}) 이미 TradeZone '{this.tradeZone.gameObject.name}'에 있는 상태에서 다른 TradeZone '{enteredZone.gameObject.name}'에 진입. 기존 할당 유지.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.TryGetComponent<TradeZone>(out TradeZone exitedZone))
        {
            if (this.tradeZone == exitedZone) 
            {
                this.tradeZone = null;
                Debug.Log($"{LOG_PREFIX} ({NPCName}) TradeZone '{exitedZone.gameObject.name}'에서 벗어남. TradeZone 참조 해제.");
            }
        }
    }

    /// <summary>
    /// 디버그용 고객 NPC 상태 출력
    /// </summary>
    [ContextMenu("Debug Customer Status")]
    public void DebugCustomerStatus()
    {
        base.DebugNPCStatus(); // ScheduledNPC의 디버그 정보 출력
        
        var tradeInfo = GetCustomerTradeInfo();
        Debug.Log($"{LOG_PREFIX} ({NPCName}) Customer Trade Status:\n" +
                 $"- Request: {tradeInfo.request?.name ?? "None"}\n" +
                 $"- TradeZone: {tradeInfo.tradeZone?.name ?? "None"}\n" +
                 $"- Has Trade Attempted: {tradeInfo.hasTradeAttempted}\n" +
                 $"- Is In Trade: {tradeInfo.isInTrade}\n" +
                 $"- Can Repeat Trade: {tradeInfo.canRepeatTrade}");
    }
}

/// <summary>
/// 고객 거래 상태 정보 구조체
/// </summary>
[System.Serializable]
public struct CustomerTradeInfo
{
    public CustomerRequestSO request;
    public TradeZone tradeZone;
    public bool hasTradeAttempted;
    public bool isInTrade;
    public bool canRepeatTrade;
}