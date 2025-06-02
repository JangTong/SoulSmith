using UnityEngine;
using System.Collections.Generic;

public class EventTrigger : MonoBehaviour
{
    private const string LOG_PREFIX = "[EventTrigger]";
    public GameEventAsset eventData;
    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered && !eventData.repeatable)
        {
            Debug.Log($"{LOG_PREFIX} '{eventData.eventId}' 이미 트리거됨 (반복 불가)");
            return;
        }

        if (!other.CompareTag("Player"))
        {
            Debug.Log($"{LOG_PREFIX} Player가 아닌 오브젝트와 충돌: {other.name}");
            return;
        }

        Debug.Log($"{LOG_PREFIX} '{eventData.eventId}' 트리거 시작");

        // 선행 이벤트 체크
        foreach (string req in eventData.requiredPreviousEvents)
        {
            if (!GameEventProgress.Instance.IsCompleted(req))
            {
                Debug.Log($"{LOG_PREFIX} '{eventData.eventId}' 실행 차단: 선행 이벤트 '{req}' 미완료");
                return;
            }
        }

        // 단일 Execute 호출로 내부에서 fallback 포함 처리
        EventService.Instance.Execute(eventData);
        triggered = true;
        Debug.Log($"{LOG_PREFIX} '{eventData.eventId}' 트리거 완료");
    }
}