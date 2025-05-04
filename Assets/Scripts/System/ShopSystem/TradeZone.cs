using UnityEngine;
using TMPro;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class TradeZone : MonoBehaviour
{
    [Header("tradeArea Collider")]
    public Collider tradeArea;            // IsTrigger 체크된 콜라이더 (Customer 감지용)

    [Header("SaleSlot & Filter")]
    public Transform saleSlot;            // 아이템 올려두는 위치
    public float slotRadius = 0.5f;
    public LayerMask itemLayer;

    [SerializeField]
    private Customer currentCustomer;
    private ItemComponent placedItem;
    private EnchantComponent placedEnchant;

    void OnTriggerEnter(Collider other)
    {
        var customer = other.GetComponent<Customer>();
        if (customer != null)
        {
            currentCustomer = customer;
            customer.tradeZone = this;
        }
    }

    void OnTriggerExit(Collider other)
    {
        var customer = other.GetComponent<Customer>();
        if (customer != null && customer == currentCustomer)
        {
            customer.tradeZone = null;
            currentCustomer = null;
        }
    }

    /// Customer.Interaction() → 호출됨
    public void OpenDialogue()
    {
        if (currentCustomer == null) return;

        // 요청 대사와, Sell 버튼을 눌렀을 때 실행할 콜백을 함께 넘깁니다.
        DialogueManager.Instance.ShowTradeDialogue(
            currentCustomer.request.dialogue,
            OnSellPressed
        );
    }

private void OnSellPressed()
    {
        // 대화창은 아직 열려 있는 상태
        // 거래 슬롯 검사
        Collider[] hits = Physics.OverlapSphere(saleSlot.position, slotRadius);
        placedItem = null;
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Items")) continue;
            var comps = hit.GetComponentsInParent<ItemComponent>();
            if (comps.Length > 0)
            {
                placedItem = comps[comps.Length - 1];
                break;
            }
        }
        if (placedItem == null)
        {
            // 아이템 없음 → 실패 대사
            DialogueManager.Instance.ShowTradeDialogue(
                currentCustomer.request.failureDialogue,
                OnAfterResult
            );
            return;
        }
        placedEnchant = placedItem.GetComponent<EnchantComponent>();

        // 평가
        bool ok;
        float score = currentCustomer.request.Evaluate(placedItem, placedEnchant, out ok);

        // 결과 대사
        var text = ok
            ? currentCustomer.request.successDialogue
            : currentCustomer.request.failureDialogue;

        // 골드 지급 & 파괴 (성공 시만)
        if (ok)
        {
            int bonus = Mathf.FloorToInt(score * placedItem.sellPrice);
            GameManager.Instance.AddGold(placedItem.sellPrice + bonus);
            Destroy(placedItem.gameObject);
        }

        // 결과 텍스트 보여 주고, Next 클릭 시 정리
        DialogueManager.Instance.ShowTradeDialogue(text, OnAfterResult);
    }
    private void OnAfterResult()
    {
        // 대화창 닫히고 시간/UI 복원은 ShowCustomDialogue 콜백에서 자동 처리
        // 고객 퇴장, 슬롯 초기화 등 후속 로직
        currentCustomer = null;
        placedItem = null;
        placedEnchant = null;
    }
}
