using UnityEngine;

/// <summary>
/// NPCBase를 상속받아 고객 NPC의 특정 행동을 정의하는 클래스입니다.
/// 고객은 요청(CustomerRequestSO)을 가지며, TradeZone과 상호작용하여 거래를 시도합니다.
/// </summary>
public class CustomerNPC : NPCBase
{
    private const string LOG_PREFIX = "[CustomerNPC]";

    [Header("Customer Settings")] // 고객 NPC 관련 설정을 위한 Inspector 구역입니다.
    [Tooltip("이 고객 NPC가 가지는 요청 데이터입니다. ScriptableObject 형태로 관리됩니다.")]
    public CustomerRequestSO request; // 고객의 요청 정보를 담고 있는 ScriptableObject 참조입니다.
    [Tooltip("현재 고객 NPC가 상호작용 중인 TradeZone입니다. 주로 자동으로 감지됩니다.")]
    public TradeZone tradeZone; 

    private bool hasTradeAttempted = false; // 거래 시도 여부를 나타냅니다 (성공/실패 무관).

    protected override void Awake()
    {
        base.Awake(); // NPCBase.Awake() 호출, 여기서 NPCName이 설정될 수 있음
        Debug.Log($"{LOG_PREFIX} ({NPCName}) Awake 완료. 요청: {request?.name}, 초기 TradeZone: {tradeZone?.name ?? "Null"}");
    }

    public override void Interact() // NPCBase의 Interact 메서드를 오버라이드합니다.
    {
        Debug.Log($"{LOG_PREFIX} ({NPCName}) Interact 시도.");

        if (request == null)
        {
            Debug.LogWarning($"{LOG_PREFIX} ({NPCName}) 요청(CustomerRequestSO)이 null입니다. NPCBase의 Interact 로직을 실행합니다.");
            base.Interact(); // 부모의 Interact (일반 대화 등) 실행
            return;
        }
        
        // 거래 반복 불가능하고 이미 거래를 시도한 경우
        if (!request.isRepeatable && hasTradeAttempted)
        {
            Debug.Log($"{LOG_PREFIX} ({NPCName}) 반복 불가능한 거래이며 이미 거래 시도됨. 거래 후 대화 시작.");
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
                base.Interact(); // 설정된 거래 후 대화가 없으면 기본 상호작용
            }
            return;
        }
        
        // 일반적인 거래 시도 또는 반복 가능한 거래
        if (tradeZone != null)
        {
            Debug.Log($"{LOG_PREFIX} ({NPCName}) 현재 할당된 TradeZone({tradeZone.gameObject.name})을 통해 거래 대화 시작.");
            tradeZone.OpenDialogue(this); 
        }
        else
        {
            Debug.LogWarning($"{LOG_PREFIX} ({NPCName}) 현재 할당된 TradeZone이 없습니다. 거래를 시작할 수 없습니다.");
            // 필요하다면 "거래할 장소를 찾을 수 없습니다" 등의 기본 안내 대화 출력
            // DialogueManager.Instance.PlayGeneralDialogue(new System.Collections.Generic.List<DialogueLine>() { new DialogueLine { speaker = NPCName, text = "거래할 장소를 아직 찾지 못했네." } });
        }
    }

    /// <summary>
    /// 거래 시도가 완료되었음을 표시합니다. TradeZone에서 호출됩니다.
    /// </summary>
    public void MarkTradeAttempted()
    {
        hasTradeAttempted = true;
        Debug.Log($"{LOG_PREFIX} ({NPCName}) MarkTradeAttempted: 거래 시도 완료로 표시됨 (isRepeatable: {request?.isRepeatable}).");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<TradeZone>(out TradeZone enteredZone))
        {
            if (this.tradeZone == null) // 아직 할당된 TradeZone이 없을 때만 새로 할당
            {
                this.tradeZone = enteredZone;
                Debug.Log($"{LOG_PREFIX} ({NPCName}) OnTriggerEnter: TradeZone '{enteredZone.gameObject.name}'에 진입하여 할당됨.");
            }
            else if (this.tradeZone != enteredZone)
            {
                // 이미 다른 TradeZone에 있는 상태에서 새로운 TradeZone에 진입한 경우,
                // 어떤 정책을 사용할지 결정해야 합니다. (예: 새로운 Zone으로 교체, 기존 Zone 유지 등)
                // 여기서는 기존 할당을 유지하고 경고만 출력합니다.
                Debug.LogWarning($"{LOG_PREFIX} ({NPCName}) OnTriggerEnter: 이미 TradeZone '{this.tradeZone.gameObject.name}'에 있는 상태에서 다른 TradeZone '{enteredZone.gameObject.name}'에 진입. 기존 할당 유지.");
            }
            // 이미 같은 TradeZone에 다시 진입한 경우는 무시 (tradeZone == enteredZone)
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.TryGetComponent<TradeZone>(out TradeZone exitedZone))
        {
            if (this.tradeZone == exitedZone) // 현재 할당된 TradeZone에서 벗어나는 경우에만 참조 해제
            {
                this.tradeZone = null;
                Debug.Log($"{LOG_PREFIX} ({NPCName}) OnTriggerExit: TradeZone '{exitedZone.gameObject.name}'에서 벗어남. TradeZone 참조 해제.");
            }
        }
    }
}