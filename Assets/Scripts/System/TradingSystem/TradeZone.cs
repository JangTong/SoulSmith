// TradeZone.cs
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TradeZone : MonoBehaviour
{
    private const string LOG_PREFIX = "[TradeZone]";

    [Header("SaleSlot & Filter")]
    public Transform saleSlot;
    public float slotRadius = 0.5f;

    // 3) OverlapSphereNonAlloc용 버퍼
    private readonly Collider[] overlapResults = new Collider[16];

    private CustomerNPC currentInteractingCustomer; // 현재 상호작용 중인 고객 (OpenDialogue 호출 시 설정)
    private ItemComponent placedItem;
    private EnchantComponent placedEnchant;

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out CustomerNPC cust))
        {
            // currentCustomer = cust; // OnTriggerEnter에서 currentCustomer를 설정하지 않음.
            // CustomerNPC가 TradeZone에 들어왔다는 사실만 인지하고, 실제 상호작용은 Interact()를 통해 시작됨.
            // cust.tradeZone = this; // CustomerNPC가 직접 자신의 tradeZone을 설정하도록 유도 (필요하다면)
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) CustomerNPC '{cust.NPCName}' 진입.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out CustomerNPC cust))
        {
            // if (cust == currentCustomer) currentCustomer = null; // 여기서 currentCustomer를 null로 만들면 안됨.
            // cust.tradeZone = null;
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) CustomerNPC '{cust.NPCName}' 퇴장.");
            if (cust == currentInteractingCustomer) {
                // 만약 상호작용 중이던 고객이 나가면, 상호작용 상태를 초기화 할 수 있음 (선택적)
                // currentInteractingCustomer = null; 
                // Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 상호작용 중이던 고객 '{cust.NPCName}' 퇴장. currentInteractingCustomer 초기화.");
            }
        }
    }

    // CustomerNPC를 인자로 받아 현재 상호작용 고객으로 설정
    public void OpenDialogue(CustomerNPC customer)
    {
        if (customer == null)
        {
            Debug.LogError($"{LOG_PREFIX} ({gameObject.name}) OpenDialogue 호출 시 customer가 null입니다.");
            return;
        }
        currentInteractingCustomer = customer; // 현재 상호작용하는 고객으로 설정
        Debug.Log($"{LOG_PREFIX} ({gameObject.name}) CustomerNPC '{customer.NPCName}'와 거래 대화 시작.");

        var req = currentInteractingCustomer.request;
        if (req == null)
        {
            Debug.LogError($"{LOG_PREFIX} ({gameObject.name}) CustomerNPC '{currentInteractingCustomer.NPCName}'의 request가 null입니다.");
            // 필요하다면 기본 대화 실행 또는 오류 처리
            DialogueManager.Instance.PlayGeneralDialogue(new System.Collections.Generic.List<DialogueLine>() { new DialogueLine { speaker = "System", text = "거래 정보를 불러올 수 없습니다." } });
            return;
        }

        if (req.useInlineMainDialogue)
            DialogueManager.Instance.PlayTradeDialogue(req.inlineMainDialogue, OnSellPressed);
        else
            DialogueManager.Instance.PlayTradeDialogue(req.referenceMainDialogue, OnSellPressed);
    }

    private void OnSellPressed()
    {
        if (currentInteractingCustomer == null)
        {
            Debug.LogError($"{LOG_PREFIX} ({gameObject.name}) OnSellPressed 호출 시 currentInteractingCustomer가 null입니다. 거래 처리 불가.");
            OnAfterResult(); // 고객 정보가 없으면 바로 결과 처리로 넘어가서 정리
            return;
        }
        Debug.Log($"{LOG_PREFIX} ({gameObject.name}) OnSellPressed for '{currentInteractingCustomer.NPCName}'. 아이템 평가 시작.");

        int count = Physics.OverlapSphereNonAlloc(saleSlot.position, slotRadius, overlapResults);
        placedItem = null;
        for (int i = 0; i < count; i++)
        {
            var hit = overlapResults[i];
            if (!hit.CompareTag("Items")) continue;
            // GetComponentInParent 대신 Getcomponent (루트 오브젝트에 ItemComponent가 있다고 가정)
            // 만약 자식 오브젝트에도 ItemComponent가 있을 수 있다면 GetComponentsInParent가 맞음
            placedItem = hit.GetComponent<ItemComponent>(); 
            if (placedItem != null) break;
        }

        var req = currentInteractingCustomer.request;
        bool isTradeSuccessful = false; // 거래 성공 여부

        if (placedItem != null)
        {
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 평가 대상 아이템: '{placedItem.itemName}'");
            placedEnchant = placedItem.GetComponent<EnchantComponent>();
            float evaluationScore = req.Evaluate(placedItem, placedEnchant, out isTradeSuccessful);
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 아이템 평가 완료. 성공 여부: {isTradeSuccessful}, 평가 점수: {evaluationScore:F2}");

            // 평가 점수에 따른 가격 조정 (기본 가격의 ±50%까지 변동)
            int basePrice = placedItem.sellPrice;
            float priceMultiplier = Mathf.Lerp(0.5f, 1.5f, evaluationScore);
            int adjustedPrice = Mathf.RoundToInt(basePrice * priceMultiplier);
            
            if (isTradeSuccessful)
            {
                Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 거래 성공. 보상 처리 및 성공 대화 재생.");
                // 보상 지급 (평가 점수가 반영된 가격으로 골드 추가)
                GameManager.Instance.AddGold(adjustedPrice);
                Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 아이템 '{placedItem.itemName}'의 기본 가격 {basePrice}G에서 평가 점수 {evaluationScore:F2}에 따라 조정된 {adjustedPrice}G를 지급했습니다.");
                Destroy(placedItem.gameObject); // 아이템 제거
                Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 아이템 '{placedItem.itemName}' 제거 및 보상 지급 완료.");

                if (req.useInlineSuccessDialogue)
                    DialogueManager.Instance.PlayGeneralDialogue(req.inlineSuccessDialogue, OnAfterResult);
                else
                    DialogueManager.Instance.PlayGeneralDialogue(req.referenceSuccessDialogue, OnAfterResult);
            }
            else
            {
                Debug.Log($"{LOG_PREFIX} ({gameObject.name}) 거래 실패. 실패 대화 재생.");
                if (req.useInlineFailureDialogue)
                    DialogueManager.Instance.PlayGeneralDialogue(req.inlineFailureDialogue, OnAfterResult);
                else
                    DialogueManager.Instance.PlayGeneralDialogue(req.referenceFailureDialogue, OnAfterResult);
            }
        }
        else
        {
            Debug.LogWarning($"{LOG_PREFIX} ({gameObject.name}) 판매 슬롯에 아이템이 없습니다. 거래 실패 처리.");
            isTradeSuccessful = false; // 아이템이 없으면 거래 실패

            if (req.useInlineFailureDialogue)
                DialogueManager.Instance.PlayGeneralDialogue(req.inlineFailureDialogue, OnAfterResult);
            else
                DialogueManager.Instance.PlayGeneralDialogue(req.referenceFailureDialogue, OnAfterResult);
        }
    }

    private void OnAfterResult()
    {
        Debug.Log($"{LOG_PREFIX} ({gameObject.name}) OnAfterResult 호출됨. 거래 결과 처리 및 상태 초기화.");
        if (currentInteractingCustomer != null)
        {
            currentInteractingCustomer.MarkTradeAttempted(); // 거래 시도 완료 알림
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) CustomerNPC '{currentInteractingCustomer.NPCName}'에게 MarkTradeAttempted 호출.");
        }
        else
        {
            Debug.LogWarning($"{LOG_PREFIX} ({gameObject.name}) OnAfterResult: currentInteractingCustomer가 null이어서 MarkTradeAttempted 호출 불가.");
        }

        // 상태 초기화
        currentInteractingCustomer = null;
        placedItem = null;
        placedEnchant = null;
        Debug.Log($"{LOG_PREFIX} ({gameObject.name}) TradeZone 상태 초기화 완료.");
    }
}