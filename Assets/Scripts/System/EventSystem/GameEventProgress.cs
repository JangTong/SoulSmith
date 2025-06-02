using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks and manages progress of game events.
/// Provides debug utilities for resetting and logging.
/// </summary>
public class GameEventProgress : MonoBehaviour
{
    private const string LOG_PREFIX = "[GameEventProgress]";
    public static GameEventProgress Instance { get; private set; }

    private readonly HashSet<string> completedEvents = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log($"{LOG_PREFIX} 인스턴스 초기화 완료");
        }
        else
        {
            Debug.LogWarning($"{LOG_PREFIX} 중복 인스턴스 감지 - 제거됨");
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Checks if the specified event has been completed.
    /// </summary>
    public bool IsCompleted(string eventId)
    {
        bool result = completedEvents.Contains(eventId);
        Debug.Log($"{LOG_PREFIX} 이벤트 '{eventId}' 완료 여부: {result}");
        return result;
    }

    /// <summary>
    /// Marks an event as completed and logs the action.
    /// </summary>
    public void MarkComplete(string eventId)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            Debug.LogWarning($"{LOG_PREFIX} 빈 eventId로 MarkComplete 호출됨");
            return;
        }

        if (completedEvents.Add(eventId))
        {
            Debug.Log($"{LOG_PREFIX} 이벤트 '{eventId}' 완료로 표시됨");
        }
        else
        {
            Debug.LogWarning($"{LOG_PREFIX} 이벤트 '{eventId}' 이미 완료 상태임");
        }
    }

    /// <summary>
    /// Clears all recorded event progress. Debug-only tool via context menu.
    /// </summary>
    [ContextMenu("Reset Events")]
    public void ResetProgress()
    {
        int count = completedEvents.Count;
        completedEvents.Clear();
        Debug.Log($"{LOG_PREFIX} 모든 이벤트 진행 상태 초기화 완료 (총 {count}개 이벤트)");
    }
}
