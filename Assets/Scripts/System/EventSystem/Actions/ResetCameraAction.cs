using UnityEngine;

[CreateAssetMenu(menuName = "GameEvent/Action/Reset Camera")]
public class ResetCameraAction : ScriptableObject, IEventAction
{
    private const string LOG_PREFIX = "[ResetCameraAction]";

    public void Execute()
    {
        if (PlayerController.Instance == null || PlayerController.Instance.cam == null)
        {
            Debug.LogError($"{LOG_PREFIX} PlayerController 또는 카메라가 없습니다.");
            return;
        }

        Debug.Log($"{LOG_PREFIX} 카메라 리셋 시작");
        PlayerController.Instance.cam.ResetToDefault(0.5f, true);
        Debug.Log($"{LOG_PREFIX} 카메라 리셋 요청 완료");
    }
}
