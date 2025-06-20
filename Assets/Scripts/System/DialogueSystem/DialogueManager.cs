using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Events;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    // Debug
    private const string LOG_PREFIX = "[DialogueManager]";

    // 현재 대화 진행 상태
    private DialogueData currentDialogue;
    private int currentIndex;
    private UnityAction onDialogueComplete;
    private bool isTradeMode;
    private UnityAction onSellCallback;
    
    // 연계 대화 상태 관리
    private bool isChainDialogue = false;

    // AssetReference 로드용 임시 인스턴스
    private DialogueData tempDialogueData;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log($"{LOG_PREFIX} Awake: Instance assigned");
        }
        else
        {
            Debug.Log($"{LOG_PREFIX} Awake: Duplicate instance detected, destroying game object");
            Destroy(gameObject);
            return;
        }
    }

    // 1) ScriptableObject 기반 일반 대화 (Next 버튼만)
    public void PlayGeneralDialogue(DialogueData data, UnityAction onComplete = null)
    {
        Debug.Log($"{LOG_PREFIX} PlayGeneralDialogue called");
        SetupDialogue(data, onComplete, tradeMode: false, onSell: null);
    }

    // 2) ScriptableObject 기반 거래 대화 (마지막에 Sell 버튼 추가)
    public void PlayTradeDialogue(DialogueData data, UnityAction onSell)
    {
        Debug.Log($"{LOG_PREFIX} PlayTradeDialogue called");
        SetupDialogue(data, onComplete: null, tradeMode: true, onSell);
    }

    // 3) AssetReference 기반 로드 후 재생 (옵션: 일반/거래)
    public void LoadAndPlayDialogue(
        AssetReference dialogueAsset,
        UnityAction onComplete = null,
        bool tradeMode = false,
        UnityAction onSellCallback = null)
    {
        Debug.Log($"{LOG_PREFIX} LoadAndPlayDialogue called: {dialogueAsset.RuntimeKey}, tradeMode={tradeMode}");
        _ = StartDialogue(dialogueAsset, onComplete, tradeMode, onSellCallback);
    }

    private async Task StartDialogue(
        AssetReference dialogueAsset,
        UnityAction onComplete,
        bool tradeMode,
        UnityAction onSell)
    {
        Debug.Log($"{LOG_PREFIX} StartDialogue: Loading asset {dialogueAsset.RuntimeKey}");
        var handle = dialogueAsset.LoadAssetAsync<DialogueData>();
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log($"{LOG_PREFIX} StartDialogue: Loaded successfully");
            tempDialogueData = Instantiate(handle.Result);
            tempDialogueData.CleanupEmptyLines();
            SetupDialogue(tempDialogueData, onComplete, tradeMode, onSell);
        }
        else
        {
            Debug.LogWarning($"{LOG_PREFIX} StartDialogue: Failed to load '{dialogueAsset.RuntimeKey}'");
            onComplete?.Invoke();
        }
    }

    // 4) List<DialogueLine> 기반 일반 대화
    public void PlayGeneralDialogue(List<DialogueLine> lines, UnityAction onComplete = null)
    {
        Debug.Log($"{LOG_PREFIX} PlayGeneralDialogue(List) called with {lines?.Count ?? 0} lines");
        if (tempDialogueData == null)
            tempDialogueData = ScriptableObject.CreateInstance<DialogueData>();
        tempDialogueData.lines = lines;
        SetupDialogue(tempDialogueData, onComplete, tradeMode: false, onSell: null);
    }
    
    // 연계 대화 전용 메서드
    public void PlayChainDialogue(List<DialogueLine> lines, UnityAction onComplete = null)
    {
        Debug.Log($"{LOG_PREFIX} PlayChainDialogue(List) called with {lines?.Count ?? 0} lines - UI Lock 유지");
        isChainDialogue = true;
        if (tempDialogueData == null)
            tempDialogueData = ScriptableObject.CreateInstance<DialogueData>();
        tempDialogueData.lines = lines;
        SetupDialogue(tempDialogueData, onComplete, tradeMode: false, onSell: null);
    }

    // 5) List<DialogueLine> 기반 거래 대화
    public void PlayTradeDialogue(List<DialogueLine> lines, UnityAction onSell)
    {
        Debug.Log($"{LOG_PREFIX} PlayTradeDialogue(List) called with {lines?.Count ?? 0} lines");
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

        // --- 중복 구독 방지 & tradeMode 분기 ---
        DialogueUIController.OnNextClicked -= HandleNext;
        DialogueUIController.OnNextClicked += HandleNext;

        DialogueUIController.OnSellClicked -= HandleSell;
        if (tradeMode)
            DialogueUIController.OnSellClicked += HandleSell;

        // 게임 UI 토글 - 연계 대화든 일반 대화든 UI Lock 보장
        if (!isChainDialogue)
        {
            ToggleGameUI(enableUI: true, stopTime: false);
            Debug.Log($"{LOG_PREFIX} SetupDialogue: 새로운 대화 - UI Lock 적용");
        }
        else
        {
            // 연계 대화에서도 UI Lock 상태 확실히 보장
            ToggleGameUI(enableUI: true, stopTime: false);
            Debug.Log($"{LOG_PREFIX} SetupDialogue: 연계 대화 - UI Lock 강제 적용");
        }

        // 첫 줄 출력
        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        var line = currentDialogue.lines[currentIndex];
        bool isLast = (currentIndex == currentDialogue.lines.Count - 1);

        Debug.Log($"{LOG_PREFIX} ShowCurrentLine: Index={currentIndex}, Speaker={line.speaker}, IsLast={isLast}");
        UIManager.Instance.ShowDialogue(
            speaker: line.speaker,
            text: line.text,
            portrait: line.portrait,
            showSell: isTradeMode && isLast
        );

        Debug.Log($"{LOG_PREFIX} {line.speaker}: {line.text}");
    }

    private void HandleNext()
    {
        Debug.Log($"{LOG_PREFIX} HandleNext");
        AdvanceDialogue();
    }

    private void HandleSell()
    {
        Debug.Log($"{LOG_PREFIX} HandleSell");
        DialogueUIController.OnSellClicked -= HandleSell;
        OnSellButton();

        // 원래 거래 대화 중이었을 때만 다음으로 넘어감
        // (PlayGeneralDialogue 호출로 isTradeMode가 false로 바뀐 뒤에는 건너뛰기)
        if (isTradeMode)
            AdvanceDialogue();
    }

    private void AdvanceDialogue()
    {
        currentIndex++;
        Debug.Log($"{LOG_PREFIX} AdvanceDialogue: Moved to Index={currentIndex}");
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
            Debug.Log($"{LOG_PREFIX} OnSellButton: Invoking sell callback");
            onSellCallback?.Invoke();
        }
    }

    private void EndDialogue()
    {
        Debug.Log($"{LOG_PREFIX} EndDialogue: Dialogue ended");
        
        // onDialogueComplete 호출하여 연계 대화 확인
        var tempOnComplete = onDialogueComplete;
        
        // 상태 초기화 (onDialogueComplete가 연계 대화를 시작할 수 있으므로 먼저 초기화)
        onDialogueComplete = null;
        isTradeMode = false;
        onSellCallback = null;
        
        // 안전 차원에서 남은 이벤트 구독 해제
        DialogueUIController.OnNextClicked -= HandleNext;
        DialogueUIController.OnSellClicked -= HandleSell;
        
        // 연계 대화 상태 확인
        bool wasChainDialogue = isChainDialogue;
        isChainDialogue = false; // 다음 대화를 위해 초기화
        
        // 콜백 실행 (연계 대화가 시작될 수 있음)
        tempOnComplete?.Invoke();
        
        // 연계 대화가 새로 시작되었는지 확인
        bool newChainStarted = isChainDialogue;
        
        Debug.Log($"{LOG_PREFIX} EndDialogue: wasChain={wasChainDialogue}, newChainStarted={newChainStarted}");
        
        // 연계 대화가 시작되지 않았다면 완전 정리
        if (!newChainStarted)
        {
            UIManager.Instance.HideDialogue();
            ToggleGameUI(enableUI: false, stopTime: false);
            PlayerController.Instance.cam.ResetToDefault(0.5f, unlockUI: true);
            Debug.Log($"{LOG_PREFIX} EndDialogue: 연계 대화 없음 - 대화창 숨기기 및 UI Lock 해제");
        }
        else
        {
            // 연계 대화가 있어도 Focus는 해제 (UI 상호작용을 위해)
            UIManager.Instance.SetFocusActive(false);
            
            // 테스트: 연계 대화에서도 FadeOut 실행해보기
            Debug.Log($"{LOG_PREFIX} EndDialogue: 연계 대화 - FadeOut 테스트 실행");
            
            Debug.Log($"{LOG_PREFIX} EndDialogue: 연계 대화 시작됨 - 대화창 및 UI Lock 유지 (Focus 해제)");
        }
    }

    /// <summary>
    /// 연계 대화 시작 알림 (DialogueEventTrigger에서 호출)
    /// </summary>
    public void StartChainDialogue()
    {
        isChainDialogue = true;
        Debug.Log($"{LOG_PREFIX} StartChainDialogue: 연계 대화 시작됨");
    }

    // 게임 플레이 중 UI 토글 및 시간 정지/재개
    private void ToggleGameUI(bool enableUI, bool stopTime)
    {
        Debug.Log($"{LOG_PREFIX} ToggleGameUI – UIActive:{enableUI}, StopTime:{stopTime}");
        PlayerController.Instance.ToggleUI(enableUI);
        
        // stopTime이 true일 때만 시간 관련 처리
        if (stopTime)
        {
            TimeManager.Instance.ToggleTime(true);
        }
        // stopTime이 false일 때는 시간을 건드리지 않음 (사용자가 수동으로 정지한 시간 유지)
        
        UIManager.Instance.SetFocusActive(!enableUI);
    }
}
