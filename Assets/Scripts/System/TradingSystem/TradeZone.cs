// TradeZone.cs
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TradeZone : MonoBehaviour
{
    [Header("SaleSlot & Filter")]
    public Transform saleSlot;
    public float slotRadius = 0.5f;

    // 3) OverlapSphereNonAlloc용 버퍼
    private readonly Collider[] overlapResults = new Collider[16];

    private CustomerNPC currentCustomer;
    private ItemComponent placedItem;
    private EnchantComponent placedEnchant;

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out CustomerNPC cust))
        {
            currentCustomer = cust;
            cust.tradeZone = this;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out CustomerNPC cust) && cust == currentCustomer)
        {
            cust.tradeZone = null;
            currentCustomer = null;
        }
    }

    public void OpenDialogue()
    {
        if (currentCustomer == null) return;
        var req = currentCustomer.request;

        if (req.useInlineMainDialogue)
            DialogueManager.Instance.PlayTradeDialogue(req.inlineMainDialogue, OnSellPressed);
        else
            DialogueManager.Instance.PlayTradeDialogue(req.referenceMainDialogue, OnSellPressed);
    }

    private void OnSellPressed()
    {
        // 3) NonAlloc 버퍼 사용
        int count = Physics.OverlapSphereNonAlloc(saleSlot.position, slotRadius, overlapResults);
        placedItem = null;
        for (int i = 0; i < count; i++)
        {
            var hit = overlapResults[i];
            if (!hit.CompareTag("Items")) continue;
            var comps = hit.GetComponentsInParent<ItemComponent>();
            if (comps.Length > 0)
            {
                placedItem = comps[^1];
                break;
            }
        }

        var req = currentCustomer.request;
        bool ok = false;
        float score = 0f;

        if (placedItem != null)
        {
            placedEnchant = placedItem.GetComponent<EnchantComponent>();
            // 1) Evaluate 반환값 사용
            score = req.Evaluate(placedItem, placedEnchant, out ok);
        }

        if (!ok)
        {
            // 실패 대화
            if (req.useInlineFailureDialogue)
                DialogueManager.Instance.PlayGeneralDialogue(req.inlineFailureDialogue, OnAfterResult);
            else
                DialogueManager.Instance.PlayGeneralDialogue(req.referenceFailureDialogue, OnAfterResult);
            return;
        }

        // 성공 보상 (score 반영)
        int bonus = Mathf.FloorToInt(placedItem.sellPrice * score);
        GameManager.Instance.AddGold(placedItem.sellPrice + bonus);
        Destroy(placedItem.gameObject);

        // 성공 결과 대화
        if (req.useInlineSuccessDialogue)
            DialogueManager.Instance.PlayGeneralDialogue(req.inlineSuccessDialogue, OnAfterResult);
        else
            DialogueManager.Instance.PlayGeneralDialogue(req.referenceSuccessDialogue, OnAfterResult);
    }

    private void OnAfterResult()
    {
        currentCustomer = null;
        placedItem      = null;
        placedEnchant   = null;
    }
}