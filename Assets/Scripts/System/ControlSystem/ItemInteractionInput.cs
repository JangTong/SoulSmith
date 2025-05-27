// 파일명: ItemInteractionInput.cs
using UnityEngine;

public class ItemInteractionInput : MonoBehaviour
{
    void Update()
    {
        if (PlayerController.Instance != null && PlayerController.Instance.IsUIActive()) return;

        var ii = ItemInteractionController.Instance;
        if (ii == null) return;

        // 왼손 장착 토글 (Q 키)
        if (Input.GetKeyDown(KeyCode.Q))
            ii.OnSecondaryAction(ItemInteractionController.Hand.Left);

        // 주 행동 (좌클릭)
        if (Input.GetMouseButtonDown(0))
            ii.OnPrimaryAction();

        // 오른손 장착 토글 (우클릭)
        if (Input.GetMouseButtonDown(1))
            ii.OnSecondaryAction(ItemInteractionController.Hand.Right);

        // 마우스 휠로 들고 있는 아이템 회전
        if (ii.currentState == ItemInteractionController.State.Holding)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0f)
                ii.RotateHeldItem(scroll);
        }
    }
}