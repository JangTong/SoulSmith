using UnityEngine;

public class Customer : MonoBehaviour, IInteractable
{
    [Header("Request Data")]
    public CustomerRequestSO request;

    public TradeZone tradeZone;  // 자신이 속한 거래 구역

    // InteractiveObject 인터페이스
    public void Interact()
    {
        if (tradeZone != null)
            tradeZone.OpenDialogue();
    }
}