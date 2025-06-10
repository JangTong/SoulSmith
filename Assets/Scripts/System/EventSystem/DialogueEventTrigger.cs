using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 통합 대화 이벤트 트리거 - 간단하게도, 복잡하게도 사용 가능!
/// </summary>
public class DialogueEventTrigger : MonoBehaviour
{
    // Debug
    private const string LOG_PREFIX = "[DialogueEventTrigger]";
    
    // 상수
    private const int INVALID_LINE_INDEX = -1;
    private const string DEFAULT_PLAYER_TAG = "Player";
    private const string INLINE_DIALOGUE_NAME = "인라인 대화";
    [Header("대화 설정")]
    [Tooltip("인라인 대화를 사용할지 여부")]
    public bool useInlineDialogue = false;
    
    [Tooltip("ScriptableObject 대화 데이터 (useInlineDialogue가 false일 때 사용)")]
    public DialogueData dialogueData;
    
    [Tooltip("인라인 대화 리스트 (useInlineDialogue가 true일 때 사용)")]
    public List<DialogueLine> inlineDialogue = new List<DialogueLine>();
    
    [Header("트리거 설정")]
    public bool triggerOnStart = false;
    public bool triggerOnCollision = true;
    public bool triggerOnKeyPress = false;
    [Tooltip("대화를 실행할 키 (triggerOnKeyPress가 true일 때 사용)")]
    public KeyCode triggerKey = KeyCode.E;
    public string playerTag = DEFAULT_PLAYER_TAG;
    public bool oneTimeOnly = true;
    
    [Tooltip("대화 완료 후 이 GameObject를 삭제할지 여부")]
    public bool destroyAfterComplete = false;
    
    [Header("연계 대화 설정")]
    [Tooltip("연계 대화 사용 여부")]
    public bool useChainDialogue = false;
    
    [Tooltip("연계할 다음 DialogueEventTrigger")]
    public DialogueEventTrigger nextDialogueTrigger;
    
    [Tooltip("연계 대화 시작 전 대기 시간")]
    public float chainDelay = 0.3f;
    
    [Header("기본 이벤트")]
    [Tooltip("대화 시작할 때")]
    public UnityEvent onDialogueStart;
    
    [Tooltip("대화 끝날 때 (연계 대화 시작 전)")]
    public UnityEvent onDialogueComplete;
    
    [Header("대사별 이벤트 (선택사항)")]
    [Tooltip("비어있으면 기본 모드, 설정하면 대사별 세밀 제어 모드")]
    public List<DialogueLineEvent> lineEvents = new List<DialogueLineEvent>();
    
    private bool hasTriggered = false;
    private int currentLineIndex = INVALID_LINE_INDEX; // 현재 진행 중인 대사 인덱스
    private bool isProcessingLineEvents = false; // 대사별 이벤트 처리 중인지 여부

    [System.Serializable]
    public class DialogueLineEvent
    {
        [Tooltip("몇 번째 대사에서 실행할까? (0부터 시작)")]
        public int lineIndex;
        
        [Tooltip("이 대사에서 실행할 이벤트")]
        public UnityEvent onLineShown;
    }

    private void Start()
    {
        if (triggerOnStart)
        {
            // 한 프레임 지연하여 모든 시스템이 초기화된 후 실행
            StartCoroutine(DelayedTrigger());
        }
    }

    private void Update()
    {
        if (triggerOnKeyPress && Input.GetKeyDown(triggerKey))
        {
            TriggerDialogue();
        }
    }

    private System.Collections.IEnumerator DelayedTrigger()
    {
        // 모든 시스템이 Start()를 완료할 때까지 대기
        yield return new WaitForEndOfFrame();
        
        Debug.Log($"{LOG_PREFIX} {name}: 시스템 초기화 대기 완료. 대화 시작.");
        TriggerDialogue();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnCollision && other.CompareTag(playerTag))
        {
            TriggerDialogue();
        }
    }

    [ContextMenu("대화 실행")]
    public void TriggerDialogue()
    {
        if (oneTimeOnly && hasTriggered)
        {
            Debug.Log($"{LOG_PREFIX} {name}: 이미 실행됨 (1회만 실행)");
            return;
        }

        // 대화 데이터 유효성 검사
        if (useInlineDialogue)
        {
            if (inlineDialogue == null || inlineDialogue.Count == 0)
            {
                Debug.LogWarning($"{LOG_PREFIX} {name}: 인라인 대화가 비어있습니다!");
                return;
            }
        }
        else
        {
            if (dialogueData == null)
            {
                Debug.LogWarning($"{LOG_PREFIX} {name}: DialogueData가 없습니다!");
                return;
            }
        }

        hasTriggered = true;
        onDialogueStart?.Invoke();

        // 대사별 이벤트가 있으면 고급 모드로 처리
        if (HasLineEvents())
        {
            StartAdvancedDialogue();
        }
        else
        {
            StartSimpleDialogue();
        }

        string dialogueName = useInlineDialogue ? INLINE_DIALOGUE_NAME : dialogueData.name;
        string triggerType = GetTriggerTypeString();
        Debug.Log($"{LOG_PREFIX} {name}: 대화 실행 - {dialogueName} ({triggerType} 트리거, {(HasLineEvents() ? "고급" : "간단")} 모드)");
    }

    private bool HasLineEvents()
    {
        return lineEvents != null && lineEvents.Count > 0;
    }

    /// <summary>
    /// 트리거 타입을 문자열로 반환 (가독성을 위해 별도 메서드로 분리)
    /// </summary>
    private string GetTriggerTypeString()
    {
        if (triggerOnStart)
            return "시작";
        
        if (triggerOnCollision)
            return "충돌";
        
        if (triggerOnKeyPress)
            return $"키({triggerKey})";
        
        return "수동";
    }

    // 간단 모드: 기본 대화만 실행
    private void StartSimpleDialogue()
    {
        // 연계 대화가 있는 경우 첫 번째 대화도 연계 대화로 시작
        if (useChainDialogue && nextDialogueTrigger != null)
        {
            Debug.Log($"{LOG_PREFIX} {name}: 연계 대화 체인 시작 - 첫 번째 대화도 체인으로 처리");
            if (useInlineDialogue)
            {
                DialogueManager.Instance.PlayChainDialogue(inlineDialogue, CompleteDialogue);
            }
            else
            {
                var lines = dialogueData.lines;
                DialogueManager.Instance.PlayChainDialogue(lines, CompleteDialogue);
            }
        }
        else
        {
            // 일반 대화
            if (useInlineDialogue)
            {
                DialogueManager.Instance.PlayGeneralDialogue(inlineDialogue, CompleteDialogue);
            }
            else
            {
                DialogueManager.Instance.PlayGeneralDialogue(dialogueData, CompleteDialogue);
            }
        }
    }

    // 고급 모드 대화 시작 (대사별 이벤트 처리)
    private void StartAdvancedDialogue()
    {
        Debug.Log($"{LOG_PREFIX} {name}: 고급 모드 대화 시작 (Line Events 처리)");
        
        isProcessingLineEvents = true;
        currentLineIndex = INVALID_LINE_INDEX;
        
        // 이벤트 구독
        DialogueUIController.OnNextClicked += OnNextLineForLineEvents;
        
        if (useInlineDialogue)
        {
            DialogueManager.Instance.PlayGeneralDialogue(inlineDialogue, CompleteAdvancedDialogue);
        }
        else
        {
            // 대화 로드
            DialogueManager.Instance.PlayGeneralDialogue(dialogueData, CompleteAdvancedDialogue);
        }
        
        // 첫 번째 라인(인덱스 0) 이벤트 즉시 실행
        currentLineIndex = 0;
        ExecuteLineEvent(0);
        Debug.Log($"{LOG_PREFIX} {name}: 첫 번째 라인 이벤트 실행 완료");
    }

    // 고급 모드에서 Next 버튼이 눌렸을 때 호출
    private void OnNextLineForLineEvents()
    {
        if (!isProcessingLineEvents) return;
        
        currentLineIndex++;
        Debug.Log($"{LOG_PREFIX} {name}: 다음 대사로 이동. 현재 인덱스: {currentLineIndex}");
        
        // 대사 범위 내에서만 이벤트 실행
        int totalLines = useInlineDialogue ? inlineDialogue.Count : dialogueData.lines.Count;
        if (currentLineIndex < totalLines)
        {
            ExecuteLineEvent(currentLineIndex);
        }
    }

    // 지정된 라인 인덱스의 이벤트를 실행
    private void ExecuteLineEvent(int lineIndex)
    {
        foreach (var lineEvent in lineEvents)
        {
            if (lineEvent.lineIndex == lineIndex)
            {
                Debug.Log($"{LOG_PREFIX} {name}: 라인 {lineIndex} 이벤트 실행");
                lineEvent.onLineShown?.Invoke();
            }
        }
    }

    // 간단 모드 대화 완료 콜백
    private void CompleteDialogue()
    {
        onDialogueComplete?.Invoke();
        Debug.Log($"{LOG_PREFIX} {name}: 간단 모드 대화 완료");
        
        // 연계 대화 처리
        if (useChainDialogue && nextDialogueTrigger != null)
        {
            Debug.Log($"{LOG_PREFIX} {name}: 연계 대화 시작 예약 - 대상: {nextDialogueTrigger.name}, 지연: {chainDelay}초");
            StartCoroutine(ExecuteChainDialogue());
            return; // 연계 대화가 있으면 여기서 종료 (삭제나 추가 처리는 연계 완료 후)
        }
        
        // 연계 대화가 없으면 기본 완료 처리
        FinalizeDialogue();
    }

    // 고급 모드 대화 완료 콜백
    private void CompleteAdvancedDialogue()
    {
        isProcessingLineEvents = false;
        
        // 이벤트 구독 해제
        DialogueUIController.OnNextClicked -= OnNextLineForLineEvents;
        
        onDialogueComplete?.Invoke();
        Debug.Log($"{LOG_PREFIX} {name}: 고급 모드 대화 완료");
        
        // 연계 대화 처리
        if (useChainDialogue && nextDialogueTrigger != null)
        {
            Debug.Log($"{LOG_PREFIX} {name}: 연계 대화 시작 예약 (고급 모드) - 대상: {nextDialogueTrigger.name}, 지연: {chainDelay}초");
            StartCoroutine(ExecuteChainDialogue());
            return; // 연계 대화가 있으면 여기서 종료
        }
        
        // 연계 대화가 없으면 기본 완료 처리
        FinalizeDialogue();
    }
    
    /// <summary>
    /// 연계 대화 실행 코루틴
    /// </summary>
    private IEnumerator ExecuteChainDialogue()
    {
        // DialogueManager에 연계 대화 시작을 미리 알림
        DialogueManager.Instance.StartChainDialogue();
        
        Debug.Log($"{LOG_PREFIX} {name}: 연계 대화 실행 - {nextDialogueTrigger.name}");
        
        // 연계 대화 시작 전 대기
        yield return new WaitForSeconds(chainDelay);
        
        // 연계 대화 실행 (UI Lock 유지를 위해 PlayChainDialogue 사용)
        nextDialogueTrigger.TriggerChainDialogue();
    }
    
    /// <summary>
    /// 대화 최종 완료 처리 (삭제 등)
    /// </summary>
    private void FinalizeDialogue()
    {
        Debug.Log($"{LOG_PREFIX} {name}: 대화 최종 완료 처리");
        
        // 삭제 옵션이 활성화되어 있으면 GameObject 삭제
        if (destroyAfterComplete)
        {
            Debug.Log($"{LOG_PREFIX} {name}: 대화 완료 후 GameObject 삭제");
            Destroy(gameObject);
        }
    }

    // 안전을 위한 정리 메서드
    private void OnDestroy()
    {
        DialogueUIController.OnNextClicked -= OnNextLineForLineEvents;
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
        isProcessingLineEvents = false;
        currentLineIndex = INVALID_LINE_INDEX;
    }
    

    
    /// <summary>
    /// 연속 대화 실행 (oneTimeOnly 무시 - 연속 대화용)
    /// </summary>
    [ContextMenu("연속 대화 실행")]
    public void TriggerDialogueIgnoreOnce()
    {
        Debug.Log($"{LOG_PREFIX} {name}: 연속 대화 실행 (oneTimeOnly 무시)");
        
        ExecuteDialogue();
    }
    
    /// <summary>
    /// 연계 대화 실행 (UI Lock 유지)
    /// </summary>
    public void TriggerChainDialogue()
    {
        Debug.Log($"{LOG_PREFIX} {name}: 연계 대화 실행 (UI Lock 유지)");
        
        ExecuteDialogue(isChainDialogue: true);
    }
    
    /// <summary>
    /// 게임 시간 일시정지 (TimeScale은 건드리지 않음)
    /// TimeManager, DayNightSystem, HUD 시간만 멈춤
    /// </summary>
    [ContextMenu("게임 시간 정지")]
    public void PauseGameTime()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.PauseGameTimeOnly();
            Debug.Log($"{LOG_PREFIX} {name}: 게임 시간 정지 요청 완료");
        }
        else
        {
            Debug.LogWarning($"{LOG_PREFIX} {name}: TimeManager.Instance가 null입니다!");
        }
    }
    
    /// <summary>
    /// 게임 시간 재개
    /// </summary>
    [ContextMenu("게임 시간 재개")]
    public void ResumeGameTime()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.ResumeGameTimeOnly();
            Debug.Log($"{LOG_PREFIX} {name}: 게임 시간 재개 요청 완료");
        }
        else
        {
            Debug.LogWarning($"{LOG_PREFIX} {name}: TimeManager.Instance가 null입니다!");
        }
    }
    
    /// <summary>
    /// 게임 시간 토글 (정지 ↔ 재개)
    /// </summary>
    [ContextMenu("게임 시간 토글")]
    public void ToggleGameTime()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.ToggleGameTimeOnly();
            Debug.Log($"{LOG_PREFIX} {name}: 게임 시간 토글 요청 완료");
        }
        else
        {
            Debug.LogWarning($"{LOG_PREFIX} {name}: TimeManager.Instance가 null입니다!");
        }
    }
    
    /// <summary>
    /// 실제 대화 실행 로직
    /// </summary>
    private void ExecuteDialogue(bool isChainDialogue = false)
    {
        // 대화 데이터 유효성 검사
        if (useInlineDialogue)
        {
            if (inlineDialogue == null || inlineDialogue.Count == 0)
            {
                Debug.LogWarning($"{LOG_PREFIX} {name}: 인라인 대화가 비어있습니다!");
                FinalizeDialogue(); // 실패 시에도 최종 처리
                return;
            }
        }
        else
        {
            if (dialogueData == null)
            {
                Debug.LogWarning($"{LOG_PREFIX} {name}: DialogueData가 없습니다!");
                FinalizeDialogue(); // 실패 시에도 최종 처리
                return;
            }
        }

        // oneTimeOnly를 무시하고 실행
        onDialogueStart?.Invoke();

        // 연계 대화인지에 따라 다른 메서드 호출
        if (isChainDialogue)
        {
            StartChainDialogue();
        }
        else
        {
            // 대사별 이벤트가 있으면 고급 모드로 처리
            if (HasLineEvents())
            {
                StartAdvancedDialogue();
            }
            else
            {
                StartSimpleDialogue();
            }
        }

        string dialogueName = useInlineDialogue ? INLINE_DIALOGUE_NAME : dialogueData.name;
        string dialogueType = isChainDialogue ? "연계 대화" : "일반 대화";
        Debug.Log($"{LOG_PREFIX} {name}: {dialogueType} 실행 완료 - {dialogueName}");
    }
    
    /// <summary>
    /// 연계 대화 시작 (UI Lock 유지)
    /// </summary>
    private void StartChainDialogue()
    {
        Debug.Log($"{LOG_PREFIX} {name}: 연계 대화 시작 - UI Lock 유지");
        
        // 대사별 이벤트가 있으면 고급 모드로 처리
        if (HasLineEvents())
        {
            Debug.Log($"{LOG_PREFIX} {name}: 연계 대화 - 고급 모드 (Line Events 있음)");
            isProcessingLineEvents = true;
            currentLineIndex = INVALID_LINE_INDEX;
            
            // 이벤트 구독
            DialogueUIController.OnNextClicked += OnNextLineForLineEvents;
            
            if (useInlineDialogue)
            {
                DialogueManager.Instance.PlayChainDialogue(inlineDialogue, CompleteAdvancedDialogue);
            }
            else
            {
                var lines = dialogueData.lines;
                DialogueManager.Instance.PlayChainDialogue(lines, CompleteAdvancedDialogue);
            }
            
            // 첫 번째 라인(인덱스 0) 이벤트 즉시 실행
            currentLineIndex = 0;
            ExecuteLineEvent(0);
            Debug.Log($"{LOG_PREFIX} {name}: 연계 대화 - 첫 번째 라인 이벤트 실행 완료");
        }
        else
        {
            Debug.Log($"{LOG_PREFIX} {name}: 연계 대화 - 간단 모드 (Line Events 없음)");
            if (useInlineDialogue)
            {
                DialogueManager.Instance.PlayChainDialogue(inlineDialogue, CompleteDialogue);
            }
            else
            {
                var lines = dialogueData.lines;
                DialogueManager.Instance.PlayChainDialogue(lines, CompleteDialogue);
            }
        }
    }
    

    
    private void OnValidate()
    {
        // 연계 대화 설정 검증
        if (useChainDialogue && nextDialogueTrigger == null)
        {
            Debug.LogWarning($"{LOG_PREFIX} {name}: 연계 대화가 활성화되어 있지만 nextDialogueTrigger가 설정되지 않았습니다!");
        }
        
        // 자기 자신을 연계 대화로 설정하는 것 방지
        if (useChainDialogue && nextDialogueTrigger == this)
        {
            Debug.LogError($"{LOG_PREFIX} {name}: 자기 자신을 연계 대화로 설정할 수 없습니다!");
            nextDialogueTrigger = null;
        }
    }
} 