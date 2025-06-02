using UnityEngine;

// 이벤트 완료 체크 함수
[CreateAssetMenu(menuName = "GameEvent/Action/Mark Event Complete")]
public class MarkEventCompleteAction : ScriptableObject, IEventAction
{
    private const string LOG_PREFIX = "[MarkEventCompleteAction]";
    public string eventId;

    public void Execute()
    {
        if (string.IsNullOrEmpty(eventId))
        {
            Debug.LogWarning($"{LOG_PREFIX} eventId가 비어 있습니다.");
            return;
        }

        Debug.Log($"{LOG_PREFIX} 이벤트 '{eventId}' 완료 표시 시작");
        GameEventProgress.Instance.MarkComplete(eventId);
        Debug.Log($"{LOG_PREFIX} 이벤트 '{eventId}' 완료 표시 완료");
    }
}