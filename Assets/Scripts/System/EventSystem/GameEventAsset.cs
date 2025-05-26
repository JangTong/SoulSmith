using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GameEvent/Event Asset")]
public class GameEventAsset : ScriptableObject
{
    [Header("기본 설정")]
    public string eventId;
    public bool repeatable = false;

    [Header("선행 이벤트 요구")]
    [Tooltip("이 중 하나라도 완료되지 않으면 이 이벤트는 무시됨 (fallback도 실행되지 않음)")]
    public List<string> requiredPreviousEvents = new List<string>();

    [Header("조건 (모두 만족해야 실행됨)")]
    public List<ScriptableObject> conditions; // IEventCondition 구현체

    [Header("조건 만족 시 실행할 액션")]
    public List<ScriptableObject> actions;    // IEventAction 구현체

    [Header("조건 불만족 시 실행할 fallback 액션")]
    public List<ScriptableObject> fallbackActions; // IEventAction 구현체
}
