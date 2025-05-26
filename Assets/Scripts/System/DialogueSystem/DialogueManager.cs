// DialogueManager.cs
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using TMPro;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq; 


public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI speakerNameText;
    public Image portraitImage;
    public Button nextButton;
    public Button sellButton;

    private DialogueData currentDialogue;
    private int currentIndex;
    private UnityAction onDialogueComplete;
    private bool isTradeMode;
    private UnityAction onSellCallback;

    // 4) 임시 SO 재사용용 인스턴스
    private DialogueData tempDialogueData;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        dialoguePanel.SetActive(false);
        sellButton.gameObject.SetActive(false);

        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(NextDialogue);

        // 임시 SO 한 번만 생성
        tempDialogueData = ScriptableObject.CreateInstance<DialogueData>();
    }

    /// <summary>
    /// 2) Addressables에서 불러온 DialogueData 재생 + 완료 시 해제
    /// </summary>
    public async void LoadAndStartDialogue(string key)
    {
        var handle = Addressables.LoadAssetAsync<DialogueData>($"DialogueData/{key}");
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            // 완료 콜백에 Release 처리
            PlayGeneralDialogue(handle.Result, () => Addressables.Release(handle));
        }
        else
        {
            Debug.LogWarning($"DialogueData '{key}' 로드 실패");
            Addressables.Release(handle);
        }
    }

    // 1) 기존 SO 에셋 기반 재생 (Next만)
    public void PlayGeneralDialogue(DialogueData data, UnityAction onComplete = null)
        => SetupDialogue(data, onComplete, tradeMode: false, onSell: null);

    // 1) 기존 SO 에셋 기반 거래 재생 (마지막 페이지만 Sell+Next)
    public void PlayTradeDialogue(DialogueData data, UnityAction onSell)
        => SetupDialogue(data, onComplete: null, tradeMode: true, onSell);

    // 4) 리스트 기반 재생 오버로드 (Next만)
    public void PlayGeneralDialogue(List<DialogueLine> lines, UnityAction onComplete = null)
    {
        tempDialogueData.lines = lines;
        SetupDialogue(tempDialogueData, onComplete, tradeMode: false, onSell: null);
    }

    // 4) 리스트 기반 재생 오버로드 (거래용)
    public void PlayTradeDialogue(List<DialogueLine> lines, UnityAction onSell)
    {
        tempDialogueData.lines = lines;
        SetupDialogue(tempDialogueData, onComplete: null, tradeMode: true, onSell);
    }

    private void SetupDialogue(DialogueData data, UnityAction onComplete, bool tradeMode, UnityAction onSell)
    {
        if (data == null || data.lines == null || data.lines.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        currentDialogue    = data;
        currentIndex       = 0;
        onDialogueComplete = onComplete;
        isTradeMode        = tradeMode;
        onSellCallback     = onSell;

        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(NextDialogue);

        sellButton.onClick.RemoveAllListeners();
        if (isTradeMode)
            sellButton.onClick.AddListener(() => onSellCallback?.Invoke());
        sellButton.gameObject.SetActive(false);

        dialoguePanel.SetActive(true);
        ToggleGameUI(true);
        ShowCurrentLine();
    }

    private void NextDialogue()
    {
        currentIndex++;
        if (currentDialogue != null && currentIndex < currentDialogue.lines.Count)
        {
            ShowCurrentLine();
        }
        else
        {
            CloseDialogue();
            onDialogueComplete?.Invoke();
            onDialogueComplete = null;
            isTradeMode        = false;
            onSellCallback     = null;
        }
    }

    public void CloseDialogue()
    {
        dialoguePanel.SetActive(false);
        ToggleGameUI(false);
        PlayerController.Instance.ResetCameraToLocalDefault();

        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(NextDialogue);

        sellButton.onClick.RemoveAllListeners();
        sellButton.gameObject.SetActive(false);

        currentDialogue    = null;
        onDialogueComplete = null;
    }

    private void ShowCurrentLine()
    {
        var line = currentDialogue.lines[currentIndex];
        dialogueText.text = line.text;
        speakerNameText.text = line.speaker ?? string.Empty;
        portraitImage.gameObject.SetActive(line.portrait != null);
        if (line.portrait != null)
            portraitImage.sprite = line.portrait;

        bool isLast = currentIndex == currentDialogue.lines.Count - 1;
        sellButton.gameObject.SetActive(isTradeMode && isLast);

        // GameEventAsset 실행 위임 (fallback 포함)
        if (line.eventToTrigger != null)
        {
            EventService.Instance.Execute(line.eventToTrigger);
        }
    }

    private void ToggleGameUI(bool enable)
    {
        PlayerController.Instance.ToggleUI(enable);
        //GameManager.Instance.ToggleTime(enable);
    }
}
