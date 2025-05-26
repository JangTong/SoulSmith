// EventService.cs
using UnityEngine;

public class EventService : MonoBehaviour, IEventService
{
    public static IEventService Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[EventService] Initialized");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Execute(GameEventAsset evt)
    {
        if (evt == null)
        {
            Debug.LogError("[EventService] Execute called with null asset");
            return;
        }

        // 반복 실행 불가 & 이미 완료된 이벤트 스킵
        if (!evt.repeatable && GameEventProgress.Instance.IsCompleted(evt.eventId))
        {
            Debug.Log($"[EventService] Event '{evt.eventId}' already completed and not repeatable");
            return;
        }

        // 선행 이벤트 체크
        foreach (var req in evt.requiredPreviousEvents)
        {
            if (!GameEventProgress.Instance.IsCompleted(req))
            {
                Debug.Log($"[EventService] Prerequisite '{req}' not met for event '{evt.eventId}'");
                return;
            }
        }

        // 조건 검사
        bool allMet = true;
        foreach (var condObj in evt.conditions)
        {
            if (condObj is IEventCondition cond && !cond.IsMet())
            {
                allMet = false;
                break;
            }
        }

        // fallback을 포함해 하나의 메서드에서 처리
        var listToExecute = allMet ? evt.actions : evt.fallbackActions;
        if (listToExecute != null)
        {
            foreach (var actionObj in listToExecute)
            {
                if (actionObj is IEventAction action)
                {
                    action.Execute();
                    Debug.Log($"[EventService] Executed {(allMet ? string.Empty : "fallback ")}action '{actionObj.name}' for event '{evt.eventId}'");
                }
            }
        }

        // 진행 기록
        GameEventProgress.Instance.MarkComplete(evt.eventId);
    }
}