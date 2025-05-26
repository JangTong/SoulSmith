using System.Collections.Generic;
using UnityEngine;

public class GameEventProgress : MonoBehaviour
{
    public static GameEventProgress Instance { get; private set; }

    private HashSet<string> completedEvents = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject); // 씬 전환 시 유지
    }

    public void MarkComplete(string eventId)
    {
        if (!completedEvents.Contains(eventId))
        {
            completedEvents.Add(eventId);
            Debug.Log($"[GameEventProgress] 이벤트 완료: {eventId}");
        }
    }

    public bool IsCompleted(string eventId)
    {
        return completedEvents.Contains(eventId);
    }

    // 선택: 디버그용
    public void ResetProgress()
    {
        completedEvents.Clear();
        Debug.Log("[GameEventProgress] 모든 이벤트 진행 상태 초기화됨");
    }
}
