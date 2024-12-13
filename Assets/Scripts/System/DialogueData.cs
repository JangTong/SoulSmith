using UnityEngine;

[CreateAssetMenu(fileName = "DialogueData", menuName = "Dialogue/DialogueData")]
public class DialogueData : ScriptableObject
{
    [TextArea(3, 5)]
    public string[] dialogues; // 대사 목록
}
