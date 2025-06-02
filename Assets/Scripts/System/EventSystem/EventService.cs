// EventService.cs
using UnityEngine;

public class EventService : MonoBehaviour, IEventService
{
    private const string LOG_PREFIX = "[EventService]";
    public static IEventService Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log($"{LOG_PREFIX} 초기화 완료");
        }
        else
        {
            Debug.LogWarning($"{LOG_PREFIX} 중복 인스턴스 감지 - 제거됨");
            Destroy(gameObject);
        }
    }

    public void Execute(GameEventAsset evt)
    {
        if (evt == null)
        {
            Debug.LogError($"{LOG_PREFIX} null 이벤트 에셋으로 Execute 호출됨");
            return;
        }

        Debug.Log($"{LOG_PREFIX} 이벤트 '{evt.eventId}' 실행 시작");

        // 반복 실행 불가 & 이미 완료된 이벤트 스킵
        if (!evt.repeatable && GameEventProgress.Instance.IsCompleted(evt.eventId))
        {
            Debug.Log($"{LOG_PREFIX} 이벤트 '{evt.eventId}' 이미 완료됨 (반복 불가)");
            return;
        }

        // 선행 이벤트 체크
        foreach (var req in evt.requiredPreviousEvents)
        {
            if (!GameEventProgress.Instance.IsCompleted(req))
            {
                Debug.Log($"{LOG_PREFIX} 이벤트 '{evt.eventId}' 실행 실패: 선행 이벤트 '{req}' 미완료");
                return;
            }
        }

        // 조건 검사
        bool allMet = true;
        foreach (var condObj in evt.conditions)
        {
            if (condObj is IEventCondition cond)
            {
                bool isMet = cond.IsMet();
                Debug.Log($"{LOG_PREFIX} 조건 '{condObj.name}' 검사 결과: {isMet}");
                if (!isMet)
                {
                    allMet = false;
                    break;
                }
            }
        }

        Debug.Log($"{LOG_PREFIX} 모든 조건 {(allMet ? "만족" : "불만족")}");

        // fallback을 포함해 하나의 메서드에서 처리
        var listToExecute = allMet ? evt.actions : evt.fallbackActions;
        if (listToExecute != null)
        {
            Debug.Log($"{LOG_PREFIX} {(allMet ? "일반" : "fallback")} 액션 {listToExecute.Count}개 실행 시작");
            foreach (var actionObj in listToExecute)
            {
                if (actionObj is IEventAction action)
                {
                    Debug.Log($"{LOG_PREFIX} 액션 '{actionObj.name}' 실행 시작");
                    action.Execute();
                    Debug.Log($"{LOG_PREFIX} 액션 '{actionObj.name}' 실행 완료");
                }
            }
        }

        // 진행 기록
        GameEventProgress.Instance.MarkComplete(evt.eventId);
        Debug.Log($"{LOG_PREFIX} 이벤트 '{evt.eventId}' 실행 완료");
    }
}