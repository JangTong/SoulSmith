using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Events;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    // 현재 대화 진행 상태
    private DialogueData currentDialogue;
    private int currentIndex;
    private UnityAction onDialogueComplete;
    private bool isTradeMode;
    private UnityAction onSellCallback;

    // AssetReference 로드용 임시 인스턴스
    private DialogueData tempDialogueData;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // 1) ScriptableObject 기반 일반 대화 (Next 버튼만)
    public void PlayGeneralDialogue(DialogueData data, UnityAction onComplete = null)
        => SetupDialogue(data, onComplete, tradeMode: false, onSell: null);

    // 2) ScriptableObject 기반 거래 대화 (마지막에 Sell 버튼 추가)
    public void PlayTradeDialogue(DialogueData data, UnityAction onSell)
        => SetupDialogue(data, onComplete: null, tradeMode: true, onSell);

    // 3) AssetReference 기반 로드 후 재생 (옵션: 일반/거래)
    public void LoadAndPlayDialogue(
        AssetReference dialogueAsset,
        UnityAction onComplete = null,
        bool tradeMode = false,
        UnityAction onSellCallback = null)
    {
        _ = StartDialogue(dialogueAsset, onComplete, tradeMode, onSellCallback);
    }

    private async Task StartDialogue(
        AssetReference dialogueAsset,
        UnityAction onComplete,
        bool tradeMode,
        UnityAction onSell)
    {
        var handle = dialogueAsset.LoadAssetAsync<DialogueData>();
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            // 인스턴스 복제하여 수정 방지
            tempDialogueData = Instantiate(handle.Result);
            tempDialogueData.CleanupEmptyLines();
            SetupDialogue(tempDialogueData, onComplete, tradeMode, onSell);
        }
        else
        {
            Debug.LogWarning($"[DialogueManager] Failed to load '{dialogueAsset.RuntimeKey}'");
            onComplete?.Invoke();
        }
    }

    // 4) List<DialogueLine> 기반 일반 대화
    public void PlayGeneralDialogue(List<DialogueLine> lines, UnityAction onComplete = null)
    {
        if (tempDialogueData == null)
            tempDialogueData = ScriptableObject.CreateInstance<DialogueData>();
        tempDialogueData.lines = lines;
        SetupDialogue(tempDialogueData, onComplete, tradeMode: false, onSell: null);
    }

    // 5) List<DialogueLine> 기반 거래 대화
    public void PlayTradeDialogue(List<DialogueLine> lines, UnityAction onSell)
    {
        if (tempDialogueData == null)
            tempDialogueData = ScriptableObject.CreateInstance<DialogueData>();
        tempDialogueData.lines = lines;
        SetupDialogue(tempDialogueData, onComplete: null, tradeMode: true, onSell);
    }

    // 공통 세팅 메서드
    private void SetupDialogue(
        DialogueData data,
        UnityAction onComplete,
        bool tradeMode,
        UnityAction onSell)
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

        // 버튼 이벤트 구독
        DialogueUIController.OnNextClicked += HandleNext;
        DialogueUIController.OnSellClicked += HandleSell;

        // 게임 UI 토글 (일시정지)
        ToggleGameUI(enableUI: true, stopTime: false);

        // 첫 줄 출력
        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        var line  = currentDialogue.lines[currentIndex];
        bool isLast = (currentIndex == currentDialogue.lines.Count - 1);

        UIManager.Instance.ShowDialogue(
            speaker:  line.speaker,
            text:  line.text,
            portrait: line.portrait,
            showSell: isTradeMode && isLast
        );

        Debug.Log($"[Dialogue] {line.speaker}: {line.text}");

        if (line.eventToTrigger != null)
        {
            EventService.Instance.Execute(line.eventToTrigger);
            Debug.Log($"[Dialogue] Event triggered: {line.eventToTrigger.name}");
        }
    }

    private void HandleNext()
    {
        AdvanceDialogue();
    }

    private void HandleSell()
    {
        DialogueUIController.OnSellClicked -= HandleSell;
        OnSellButton();
        AdvanceDialogue();
    }

    private void AdvanceDialogue()
    {
        currentIndex++;
        if (currentIndex < currentDialogue.lines.Count)
        {
            ShowCurrentLine();
        }
        else
        {
            EndDialogue();
        }
    }

    private void OnSellButton()
    {
        if (isTradeMode)
        {
            onSellCallback?.Invoke();
            Debug.Log("[Dialogue] Sell callback invoked");
        }
    }

    private void EndDialogue()
    {
        UIManager.Instance.HideDialogue();
        ToggleGameUI(enableUI: false, stopTime: false);
        PlayerController.Instance.cam.ResetToDefault(0.5f);
        onDialogueComplete?.Invoke();

        // 상태 초기화
        onDialogueComplete = null;
        isTradeMode        = false;
        onSellCallback     = null;

        // 안전 차원에서 남은 이벤트 구독 해제
        DialogueUIController.OnNextClicked -= HandleNext;
        DialogueUIController.OnSellClicked -= HandleSell;
    }

    // 게임 플레이 중 UI 토글 및 시간 정지/재개
    private void ToggleGameUI(bool enableUI, bool stopTime)
    {
        PlayerController.Instance.ToggleUI(enableUI);
        GameManager.Instance.ToggleTime(stopTime);
        UIManager.Instance.SetFocusActive(!enableUI);

        Debug.Log($"[DialogueManager] ToggleGameUI – UIActive:{enableUI}, StopTime:{stopTime}");
    }
}
