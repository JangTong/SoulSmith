using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 통합 대화 이벤트 트리거 - 간단하게도, 복잡하게도 사용 가능!
/// </summary>
public class DialogueEventTrigger : MonoBehaviour
{
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
    public string playerTag = "Player";
    public bool oneTimeOnly = true;
    
    [Tooltip("대화 완료 후 이 GameObject를 삭제할지 여부")]
    public bool destroyAfterComplete = false;
    
    [Header("기본 이벤트")]
    [Tooltip("대화 시작할 때")]
    public UnityEvent onDialogueStart;
    
    [Tooltip("대화 끝날 때")]
    public UnityEvent onDialogueComplete;
    
    [Header("대사별 이벤트 (선택사항)")]
    [Tooltip("비어있으면 기본 모드, 설정하면 대사별 세밀 제어 모드")]
    public List<DialogueLineEvent> lineEvents = new List<DialogueLineEvent>();
    
    private bool hasTriggered = false;
    private int currentLineIndex = -1; // 현재 진행 중인 대사 인덱스
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
        
        Debug.Log($"[DialogueEventTrigger] {name}: 시스템 초기화 대기 완료. 대화 시작.");
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
            Debug.Log($"[DialogueEventTrigger] {name}: 이미 실행됨 (1회만 실행)");
            return;
        }

        // 대화 데이터 유효성 검사
        if (useInlineDialogue)
        {
            if (inlineDialogue == null || inlineDialogue.Count == 0)
            {
                Debug.LogWarning($"[DialogueEventTrigger] {name}: 인라인 대화가 비어있습니다!");
                return;
            }
        }
        else
        {
            if (dialogueData == null)
            {
                Debug.LogWarning($"[DialogueEventTrigger] {name}: DialogueData가 없습니다!");
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

        string dialogueName = useInlineDialogue ? "인라인 대화" : dialogueData.name;
        string triggerType = GetTriggerTypeString();
        Debug.Log($"[DialogueEventTrigger] {name}: 대화 실행 - {dialogueName} ({triggerType} 트리거, {(HasLineEvents() ? "고급" : "간단")} 모드)");
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
        // DialogueManager.PlayGeneralDialogue는 2개의 인자만 받음
        if (useInlineDialogue)
        {
            DialogueManager.Instance.PlayGeneralDialogue(inlineDialogue, CompleteDialogue);
        }
        else
        {
            DialogueManager.Instance.PlayGeneralDialogue(dialogueData, CompleteDialogue);
        }
    }

    // 고급 모드: 대사별 이벤트를 포함한 대화 처리
    private void StartAdvancedDialogue()
    {
        isProcessingLineEvents = true;
        currentLineIndex = -1;
        
        // DialogueUIController의 NextClicked 이벤트를 구독하여 대사별 이벤트 처리
        DialogueUIController.OnNextClicked -= OnNextLineForLineEvents;
        DialogueUIController.OnNextClicked += OnNextLineForLineEvents;
        
        // DialogueManager로 대화 시작 (완료 콜백은 별도 처리)
        if (useInlineDialogue)
        {
            DialogueManager.Instance.PlayGeneralDialogue(inlineDialogue, CompleteAdvancedDialogue);
        }
        else
        {
            DialogueManager.Instance.PlayGeneralDialogue(dialogueData, CompleteAdvancedDialogue);
        }
        
        // 첫 번째 대사 이벤트 실행 (대화 시작과 함께)
        currentLineIndex = 0;
        ExecuteLineEvent(currentLineIndex);
    }

    // 고급 모드에서 Next 버튼이 눌렸을 때 호출
    private void OnNextLineForLineEvents()
    {
        if (!isProcessingLineEvents) return;
        
        currentLineIndex++;
        Debug.Log($"[DialogueEventTrigger] {name}: 다음 대사로 이동. 현재 인덱스: {currentLineIndex}");
        
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
                Debug.Log($"[DialogueEventTrigger] {name}: 라인 {lineIndex} 이벤트 실행");
                lineEvent.onLineShown?.Invoke();
            }
        }
    }

    // 간단 모드 대화 완료 콜백
    private void CompleteDialogue()
    {
        onDialogueComplete?.Invoke();
        Debug.Log($"[DialogueEventTrigger] {name}: 간단 모드 대화 완료");
        
        // 삭제 옵션이 활성화되어 있으면 GameObject 삭제
        if (destroyAfterComplete)
        {
            Debug.Log($"[DialogueEventTrigger] {name}: 대화 완료 후 GameObject 삭제");
            Destroy(gameObject);
        }
    }

    // 고급 모드 대화 완료 콜백
    private void CompleteAdvancedDialogue()
    {
        isProcessingLineEvents = false;
        
        // 이벤트 구독 해제
        DialogueUIController.OnNextClicked -= OnNextLineForLineEvents;
        
        onDialogueComplete?.Invoke();
        Debug.Log($"[DialogueEventTrigger] {name}: 고급 모드 대화 완료");
        
        // 삭제 옵션이 활성화되어 있으면 GameObject 삭제
        if (destroyAfterComplete)
        {
            Debug.Log($"[DialogueEventTrigger] {name}: 대화 완료 후 GameObject 삭제");
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
        currentLineIndex = -1;
    }
} 