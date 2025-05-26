using UnityEngine;

// 이벤트 완료 체크 함수
[CreateAssetMenu(menuName = "GameEvent/Action/Mark Event Complete")]
public class MarkEventCompleteAction : ScriptableObject, IEventAction
{
    public string eventId;

    public void Execute()
    {
        if (string.IsNullOrEmpty(eventId))
        {
            Debug.LogWarning("[MarkEventCompleteAction] eventId가 비어 있습니다.");
            return;
        }

        GameEventProgress.Instance.MarkComplete(eventId);
    }
}