using UnityEngine;

// Dialogue 재생 함수
[CreateAssetMenu(menuName = "GameEvent/Action/Play Dialogue")]
public class PlayDialogueAction : ScriptableObject, IEventAction
{
    private const string LOG_PREFIX = "[PlayDialogueAction]";
    public DialogueData dialogue;

    public void Execute()
    {
        if (dialogue == null)
        {
            Debug.LogWarning($"{LOG_PREFIX} 대화 데이터가 비어 있습니다.");
            return;
        }

        Debug.Log($"{LOG_PREFIX} 대화 재생 시작: '{dialogue.name}'");
        DialogueManager.Instance.PlayGeneralDialogue(dialogue);
        Debug.Log($"{LOG_PREFIX} 대화 재생 요청 완료");
    }
}