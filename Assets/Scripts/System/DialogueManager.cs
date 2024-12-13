#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance; // 싱글톤 인스턴스

    public TextMeshProUGUI dialogueText;     // 대사를 표시할 텍스트
    public GameObject dialoguePanel;        // 대화창 패널
    public Button nextButton;               // "다음" 버튼

    public DialogueData currentDialogue;   // 현재 대화 데이터
    private int currentIndex = 0;           // 현재 대사 인덱스

    private void Awake()
    {
        // 싱글톤 구현
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // 중복 인스턴스 제거
        }
        
        // 버튼에 NextDialogue 함수 연결
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(NextDialogue);
        }

        CloseDialogue(); // 초기에는 대화창을 비활성화
    }

    public void LoadAndStartDialogue(string dialogueName)
    {
        currentDialogue = LoadDialogueByName(dialogueName);

        if (currentDialogue != null)
        {
            Debug.Log($"'{dialogueName}' 대화 데이터 로드 성공");
            StartDialogue();
        }
        else
        {
            Debug.LogWarning($"'{dialogueName}' 대화 데이터를 로드하지 못했습니다.");
        }
    }

    /// 대화 시작
    public void StartDialogue()
    {
        if (currentDialogue != null && currentDialogue.dialogues.Length > 0)
        {
            Debug.Log($"대화 시작: 총 {currentDialogue.dialogues.Length}개의 대화");
            currentIndex = 0; // 대화 인덱스 초기화
            dialoguePanel.SetActive(true); // 대화창 활성화
            ShowDialogue(); // 첫 번째 대사 표시

            PlayerController.Instance.ToggleUI(true);
            GameManager.Instance.ToggleTime(true);
        }
        else
        {
            Debug.LogWarning("대화 데이터를 로드했지만 내용이 비어있습니다.");
        }
    }

    /// 다음 대사 표시
    public void NextDialogue()
    {
        currentIndex++;

        if (currentIndex < currentDialogue.dialogues.Length)
        {
            ShowDialogue(); // 다음 대사 표시
        }
        else
        {
            CloseDialogue(); // 대화 종료
        }
    }

    /// 현재 대사 표시
    private void ShowDialogue()
    {
        if (currentDialogue != null && currentIndex < currentDialogue.dialogues.Length)
        {
            dialogueText.text = currentDialogue.dialogues[currentIndex];
        }
    }

    /// 대화 종료
    public void CloseDialogue()
    {
        dialoguePanel.SetActive(false); // 대화창 비활성화
        PlayerController.Instance.ToggleUI(false);
        currentDialogue = null;
        GameManager.Instance.ToggleTime(false);
    }

    /// 파일명으로 대화 데이터 로드
    private DialogueData LoadDialogueByName(string fileName)
    {
        // "DialogueData/파일명" 형식으로 경로 지정
        DialogueData dialogueData = Resources.Load<DialogueData>($"DialogueData/{fileName}");

        if (dialogueData == null)
        {
            Debug.LogWarning($"'{fileName}' 이름의 DialogueData를 'Resources/DialogueData/'에서 찾을 수 없습니다.");
        }

        return dialogueData;
    }
}
