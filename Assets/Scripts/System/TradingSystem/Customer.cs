using UnityEngine;

public class Customer : InteractiveObject
{
    [Header("Request Data")]
    public CustomerRequestSO request;

    public TradeZone tradeZone;  // 자신이 속한 거래 구역

    // InteractiveObject 인터페이스
    override public void Interaction()
    {
        if (tradeZone != null)
            tradeZone.OpenDialogue();
    }
}