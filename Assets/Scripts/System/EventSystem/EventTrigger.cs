using UnityEngine;
using System.Collections.Generic;

public class EventTrigger : MonoBehaviour
{
    public GameEventAsset eventData;
    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered && !eventData.repeatable) return;
        if (!other.CompareTag("Player")) return;

        // ✅ 선행 이벤트 조건 미달이면 아예 무시
        if (!ArePrerequisitesMet()) return;

        if (AreConditionsMet())
        {
            ExecuteActions(eventData.actions);
            triggered = true;
        }
        else if (eventData.fallbackActions != null && eventData.fallbackActions.Count > 0)
        {
            ExecuteActions(eventData.fallbackActions);
        }
    }

    private bool ArePrerequisitesMet()
    {
        foreach (string requiredEventId in eventData.requiredPreviousEvents)
        {
            if (!GameEventProgress.Instance.IsCompleted(requiredEventId))
            {
                Debug.Log($"[EventTrigger] '{eventData.eventId}' 실행 차단: 선행 이벤트 '{requiredEventId}' 미완료");
                return false;
            }
        }
        return true;
    }

    private bool AreConditionsMet()
    {
        foreach (var cond in eventData.conditions)
        {
            if (cond is IEventCondition c && !c.IsMet())
                return false;
        }
        return true;
    }

    private void ExecuteActions(List<ScriptableObject> actionList)
    {
        foreach (var act in actionList)
        {
            if (act is IEventAction a)
                a.Execute();
        }
    }
}
