using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks and manages progress of game events.
/// Provides debug utilities for resetting and logging.
/// </summary>
public class GameEventProgress : MonoBehaviour
{
    public static GameEventProgress Instance { get; private set; }

    private readonly HashSet<string> completedEvents = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[GameEventProgress] Instance initialized");
        }
        else
        {
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
        Debug.Log($"[GameEventProgress] IsCompleted('{eventId}') => {result}");
        return result;
    }

    /// <summary>
    /// Marks an event as completed and logs the action.
    /// </summary>
    public void MarkComplete(string eventId)
    {
        if (completedEvents.Add(eventId))
        {
            Debug.Log($"[GameEventProgress] Event '{eventId}' marked complete.");
        }
        else
        {
            Debug.LogWarning($"[GameEventProgress] Event '{eventId}' was already marked complete.");
        }
    }

    /// <summary>
    /// Clears all recorded event progress. Debug-only tool via context menu.
    /// </summary>
    [ContextMenu("Reset Events")]
    public void ResetProgress()
    {
        completedEvents.Clear();
        Debug.Log("[GameEventProgress] All event progress has been reset.");
    }
}
