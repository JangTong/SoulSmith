using UnityEngine;

public class Tutorial : MonoBehaviour
{
    public string tutorialDialogueName = "TutorialDialogue"; // 튜토리얼 대화 파일명
    public bool isTutorialCompleted = false; // 튜토리얼 완료 여부

    private void Start()
    {
        StartTutorial(); // 게임 시작 시 튜토리얼 시작
    }

    /// 튜토리얼 시작
    private void StartTutorial()
    {
        if (!isTutorialCompleted)
        {
            DialogueManager.Instance.LoadAndStartDialogue(tutorialDialogueName);
        }
    }
}
