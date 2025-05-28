using UnityEngine;

public class CustomerNPC : NPCBase
{
    [Header("Customer Settings")]
    [Tooltip("이 고객의 요청 데이터(SO)")]
    public CustomerRequestSO request;
    [Tooltip("고객이 속할 TradeZone")]
    public TradeZone tradeZone;

    /// <summary>
    /// 거래 대화 실행
    /// </summary>
    public override void Interact()
    {
        Debug.Log($"[CustomerNPC] '{NPCName}' start trade");
        if (tradeZone != null)
        {
            tradeZone.OpenDialogue();
            Debug.Log($"[CustomerNPC] '{NPCName}' opened trade dialogue");
        }
        else
        {
            Debug.LogWarning($"[CustomerNPC] '{NPCName}' has no TradeZone assigned.");
        }
    }
}