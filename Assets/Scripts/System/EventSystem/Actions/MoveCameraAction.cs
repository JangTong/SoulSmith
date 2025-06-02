using UnityEngine;

// 카메라 위치 + 회전값을 직접 입력 가능한 SO
[CreateAssetMenu(menuName = "GameEvent/Action/Move Camera To Position & Rotation")]
public class MoveCameraAction : ScriptableObject, IEventAction
{
    private const string LOG_PREFIX = "[MoveCameraAction]";

    [Tooltip("카메라가 이동할 월드 좌표")]
    public Vector3 targetPosition;

    [Tooltip("카메라가 회전할 월드 오일러 각도")]
    public Vector3 targetEulerAngles;

    [Tooltip("이동·회전 완료 시간 (초)")]
    public float duration = 0.5f;

    public void Execute()
    {
        if (PlayerController.Instance == null || PlayerController.Instance.cam == null)
        {
            Debug.LogError($"{LOG_PREFIX} PlayerController 또는 카메라가 없습니다.");
            return;
        }

        Debug.Log($"{LOG_PREFIX} 카메라 이동 시작 - 위치: {targetPosition}, 회전: {targetEulerAngles}, 시간: {duration}초");

        // Vector3 → Quaternion 변환
        Quaternion targetRotation = Quaternion.Euler(targetEulerAngles);

        // PlayerController에 새로 만든 메서드 호출
        PlayerController.Instance.cam.MoveTo(
            targetPosition,
            targetRotation,
            duration
        );

        Debug.Log($"{LOG_PREFIX} 카메라 이동 요청 완료");
    }
}
