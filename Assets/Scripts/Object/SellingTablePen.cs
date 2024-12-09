using UnityEngine;

public class SellingTablePen : InteractiveObject
{
    private SellingTable sellingTable; // 부모 오브젝트에 연결된 Forge 스크립트 참조

    private void Start()
    {
        // 부모 오브젝트에서 Forge 컴포넌트 찾기
        sellingTable = GetComponentInParent<SellingTable>();

        if (sellingTable == null)
        {
            Debug.LogError("Blower의 부모 오브젝트에 Forge 컴포넌트가 없습니다!");
        }
    }

    public override void Interaction()
    {
        if (sellingTable != null)
        {
            Debug.Log("sellingTable과 상호작용 중: SellItem 실행");
            sellingTable.SellItem();
        }
        else
        {
            Debug.LogWarning("sellingTable 컴포넌트가 연결되지 않아 SellItem을 실행할 수 없습니다.");
        }
    }
}
