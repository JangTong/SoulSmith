using UnityEngine;
using DG.Tweening;

public class Door : InteractiveObject
{
    public Transform doorHinge; // 문이 회전할 기준
    public float openAngle = 90f; // 열리는 각도
    public float duration = 0.5f; // 애니메이션 시간
    private bool isOpen = false;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    private void Start()
    {
        if (doorHinge == null) doorHinge = transform;

        closedRotation = doorHinge.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0, openAngle, 0);
    }

    public override void Interaction()
    {
        isOpen = !isOpen;

        doorHinge.DOLocalRotateQuaternion(isOpen ? openRotation : closedRotation, duration)
                 .SetEase(Ease.InOutQuad);

        Debug.Log(isOpen ? "🚪 문 열림" : "🚪 문 닫힘");
    }
}
