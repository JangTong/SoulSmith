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

        // 선행 이벤트 체크
        foreach (string req in eventData.requiredPreviousEvents)
        {
            if (!GameEventProgress.Instance.IsCompleted(req))
            {
                Debug.Log($"[EventTrigger] '{eventData.eventId}' blocked: prerequisite '{req}' not completed");
                return;
            }
        }

        // 단일 Execute 호출로 내부에서 fallback 포함 처리
        EventService.Instance.Execute(eventData);
        triggered = true;
    }
}