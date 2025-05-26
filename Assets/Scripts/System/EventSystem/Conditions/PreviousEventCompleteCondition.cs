using UnityEngine;

[CreateAssetMenu(menuName = "GameEvent/Condition/Previous Event Complete")]
public class PreviousEventCompleteCondition : ScriptableObject, IEventCondition
{
    public string requiredEventId;

    public bool IsMet()
    {
        if (string.IsNullOrEmpty(requiredEventId))
        {
            Debug.LogWarning("[PreviousEventCompleteCondition] requiredEventId가 비어 있습니다.");
            return false;
        }

        return GameEventProgress.Instance.IsCompleted(requiredEventId);
    }
}