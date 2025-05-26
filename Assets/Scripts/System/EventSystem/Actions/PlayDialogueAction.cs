using UnityEngine;

// Dialogue 재생 함수
[CreateAssetMenu(menuName = "GameEvent/Action/Play Dialogue")]
    public class PlayDialogueAction : ScriptableObject, IEventAction
    {
        public DialogueData dialogue;

        public void Execute()
        {
            DialogueManager.Instance.PlayGeneralDialogue(dialogue);
        }
    }