using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SellingTable : MonoBehaviour
{
    public Transform fixedPosition; // Item을 고정할 자식 Transform 위치
    public GameObject objectOnTable = null;    // 테이블 위에 놓인 아이템
    public ItemComponent itemComponent;       // 현재 아이템의 ItemComponent
    public TextMeshProUGUI itemInfoText;      // 아이템 정보를 표시할 UI 텍스트

    private void Update()
    {
        if (objectOnTable != null && itemComponent != null)
        {
            DisplayItemInfo(); // 테이블 위 아이템 정보를 업데이트
        }
        else
        {
            ClearItemInfo(); // 테이블이 비어있을 경우 정보 초기화
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(objectOnTable == null)
        {
            // GameObject에서 ItemComponent 가져오기
            itemComponent = other.GetComponent<ItemComponent>();
            objectOnTable = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Trigger를 벗어난 오브젝트가 테이블의 아이템이라면 정보 초기화
        if (objectOnTable == other.gameObject)
        {
            objectOnTable = null;
            itemComponent = null; // 현재 아이템 정보 초기화
            ClearItemInfo(); // UI 정보 초기화
            Debug.Log($"{other.name}이(가) Table에서 제거되었습니다.");
        }
    }

    /// 테이블 위의 아이템 정보를 UI에 표시
    private void DisplayItemInfo()
    {
        if (itemComponent != null && itemInfoText != null)
        {
            // ItemComponent에서 필요한 정보를 가져와 출력
            itemInfoText.text =
                $"<b>Item Name:</b> {itemComponent.itemName}\n" +
                $"<b>Rarity:</b> {itemComponent.itemRarity}\n" +
                $"<b>Type:</b> {itemComponent.itemType}\n" +
                $"<b>Weight:</b> {itemComponent.weight:F2}\n" +
                $"<b>Attack:</b> {itemComponent.atkPower:F1}\n" +
                $"<b>Defense:</b> {itemComponent.defPower:F1}\n" +
                $"<b>Buy Price:</b> {itemComponent.buyPrice} coins\n" +
                $"<b>Sell Price:</b> {itemComponent.sellPrice} coins";
        }
    }

    private void ClearItemInfo()
    {
        if (itemInfoText != null)
        {
            itemInfoText.text = string.Empty;
        }
    }
}
