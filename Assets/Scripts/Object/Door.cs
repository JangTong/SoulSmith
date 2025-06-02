using UnityEngine;
using DG.Tweening;

/// <summary>
/// 문틀에 부착되는 컴포넌트입니다.
/// 문틀의 forward 방향이 문 앞쪽을 향하도록 설정해주세요.
/// </summary>
public class Door : MonoBehaviour, IInteractable
{
    [Header("문 설정")]
    public GameObject doorObject;  // 실제 문 오브젝트
    public float openAngle = 90f;  // 열리는 각도
    public float duration = 0.5f;  // 애니메이션 시간

    private bool isOpen = false;
    private Quaternion closedRotation;

    private void Start()
    {
        if (doorObject == null)
        {
            Debug.LogError("문 오브젝트가 할당되지 않았습니다!");
            enabled = false;
            return;
        }

        closedRotation = doorObject.transform.localRotation;

        // 문에 클릭 감지 추가
        if (!doorObject.TryGetComponent<DoorClickHandler>(out var clickHandler))
        {
            clickHandler = doorObject.AddComponent<DoorClickHandler>();
            clickHandler.mainDoor = this;
        }
    }

    public void Interact()
    {
        if (doorObject == null) return;

        isOpen = !isOpen;
        
        // 플레이어 위치를 문의 로컬 좌표계로 변환
        Vector3 localPlayerPos = transform.InverseTransformPoint(PlayerController.Instance.transform.position);
        
        // 로컬 X좌표가 양수면 오른쪽, 음수면 왼쪽
        float angle = localPlayerPos.z > 0 ? -openAngle : openAngle;
        
        if (DOTween.IsTweening(doorObject.transform))
        {
            DOTween.Kill(doorObject.transform);
        }

        Quaternion targetRotation = isOpen ? 
            closedRotation * Quaternion.Euler(0, angle, 0) : 
            closedRotation;

        doorObject.transform.DOLocalRotateQuaternion(targetRotation, duration)
                          .SetEase(Ease.InOutQuad);
    }
}

/// <summary>
/// 문 오브젝트에 부착되어 클릭을 감지하고 문틀의 Door 컴포넌트에 전달합니다.
/// </summary>
public class DoorClickHandler : MonoBehaviour, IInteractable
{
    public Door mainDoor;

    public void Interact()
    {
        if (mainDoor != null)
        {
            mainDoor.Interact();
        }
    }
}
