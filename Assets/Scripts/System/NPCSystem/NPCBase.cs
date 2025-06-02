using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// 모든 NPC의 공통 기능을 제공하는 기본 클래스입니다.
/// 이 클래스는 NPC의 이름, 대화 가능 여부, 대화 데이터 등 기본적인 정보를 관리하며,
/// NavMeshAgent를 이용한 이동 기능을 제공합니다.
/// 플레이어와의 상호작용(Interact)은 대화 가능 여부에 따라 처리하며,
/// 거래 등 구체적인 상호작용은 이 클래스를 상속받는 서브클래스에서 오버라이드하여 구현합니다.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))] // NavMeshAgent 컴포넌트가 항상 존재하도록 강제합니다.
public abstract class NPCBase : MonoBehaviour, IInteractable // MonoBehaviour를 상속받고, IInteractable 인터페이스를 구현합니다.
{
    private const string LOG_PREFIX = "[NPCBase]";

    [Header("NPC Info")] // Inspector 창에서 구역을 나누어 표시하기 위한 어트리뷰트입니다.
    [Tooltip("NPC의 식별 이름입니다.")] // Inspector 창에서 변수에 대한 설명을 표시합니다.
    public string NPCName; // NPC의 이름을 저장하는 변수입니다.

    [Header("Dialogue Settings")] // 대화 관련 설정을 위한 구역입니다.
    [Tooltip("일반 대화 가능 여부를 설정합니다.")]
    public bool isDialogueable = false; // NPC와 일반 대화가 가능한지 여부를 나타냅니다.
    [Tooltip("NPC가 재생할 대화 데이터입니다.")]
    public DialogueData dialogueData; // NPC가 사용할 대화 데이터를 참조합니다.
    [Tooltip("대화 완료 후 실행될 콜백 함수입니다. (선택 사항)")]
    public UnityAction onDialogueComplete; // 대화가 끝난 후 실행될 UnityAction 이벤트입니다.

    protected NavMeshAgent agent; // NPC의 이동을 제어하는 NavMeshAgent 컴포넌트입니다. protected로 선언하여 하위 클래스에서 접근 가능합니다.
    protected Transform homePoint; // NPC의 초기 스폰 위치 또는 복귀 지점을 나타내는 Transform입니다.

    /// <summary>
    /// Awake는 스크립트 인스턴스가 로드될 때 호출됩니다.
    /// NavMeshAgent 컴포넌트를 가져오고, homePoint를 현재 Transform으로 초기화합니다.
    /// </summary>
    protected virtual void Awake() // 가상 메서드로 선언하여 하위 클래스에서 재정의할 수 있습니다.
    {
        agent = GetComponent<NavMeshAgent>(); // NavMeshAgent 컴포넌트를 가져와 agent 변수에 할당합니다.
        homePoint = transform; // 현재 게임 오브젝트의 Transform을 homePoint로 설정합니다.
        if (string.IsNullOrEmpty(NPCName))
        {
            NPCName = gameObject.name; // NPCName이 비어있으면 게임 오브젝트 이름으로 설정
            Debug.Log($"{LOG_PREFIX} ({gameObject.name}) NPCName이 설정되지 않아 게임 오브젝트 이름 '{NPCName}'으로 자동 설정됨.");
        }
        Debug.Log($"{LOG_PREFIX} ({NPCName}) Awake 완료. NavMeshAgent 할당, HomePoint: {homePoint.name}, Dialogueable: {isDialogueable}");
    }

    /// <summary>
    /// NPC를 초기화하는 메서드입니다. 주로 NPC가 스폰될 때 호출됩니다.
    /// NPC의 이름과 홈 포인트를 설정합니다.
    /// </summary>
    /// <param name="name">NPC에게 설정할 이름입니다.</param>
    /// <param name="home">NPC의 홈 포인트로 설정할 Transform입니다.</param>
    public virtual void Initialize(string name, Transform home)
    {
        NPCName = name; // 전달받은 이름으로 NPCName을 설정합니다.
        homePoint = home; // 전달받은 Transform으로 homePoint를 설정합니다.
        transform.SetPositionAndRotation(home.position, home.rotation); // NPC의 위치와 회전을 홈 포인트에 맞춥니다.
        Debug.Log($"{LOG_PREFIX} ({NPCName}) Initialize 완료. 이름: {NPCName}, 홈 포인트: {home.name}, 위치: {home.position}");
    }

    /// <summary>
    /// 플레이어가 NPC와 상호작용할 때 호출되는 메서드입니다.
    /// isDialogueable 플래그와 dialogueData 유무에 따라 대화를 시작하거나 경고 로그를 출력합니다.
    /// </summary>
    public virtual void Interact()
    {
        Debug.Log($"{LOG_PREFIX} ({NPCName}) Interact 시도.");
        if (isDialogueable) // 대화가 가능한 경우
        {
            if (dialogueData != null) // 대화 데이터가 할당되어 있는 경우
            {
                Debug.Log($"{LOG_PREFIX} ({NPCName}) 대화 데이터 ('{dialogueData.name}')를 사용하여 DialogueManager 통해 일반 대화 시작.");
                DialogueManager.Instance.PlayGeneralDialogue(dialogueData, onDialogueComplete);
            }
            else
            {
                Debug.LogWarning($"{LOG_PREFIX} ({NPCName}) 대화 가능(isDialogueable=true)하지만 dialogueData가 null입니다.");
            }
        }
        else
        {
            Debug.Log($"{LOG_PREFIX} ({NPCName}) 대화 불가능(isDialogueable=false) 상태입니다. 상호작용 정의되지 않음.");
        }
    }

    /// <summary>
    /// NPC를 지정된 목적지로 이동시키는 메서드입니다.
    /// </summary>
    /// <param name="target">이동할 목적지의 Vector3 위치입니다.</param>
    /// <param name="stoppingDistance">목적지로부터 얼마나 가까이 멈출지를 결정하는 거리입니다. 기본값은 0.5f입니다.</param>
    public virtual void MoveTo(Vector3 target, float stoppingDistance = 0.5f)
    {
        if (agent == null)
        {
            Debug.LogError($"{LOG_PREFIX} ({NPCName}) MoveTo 시도 중 NavMeshAgent가 null입니다.");
            return;
        }
        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning($"{LOG_PREFIX} ({NPCName}) MoveTo 시도 중 NavMeshAgent가 NavMesh 위에 있지 않습니다. 목적지: {target}");
            // return; // 경우에 따라 이동을 시도하지 않도록 할 수 있음
        }
        agent.stoppingDistance = stoppingDistance; // NavMeshAgent의 정지 거리를 설정합니다.
        agent.SetDestination(target); // NavMeshAgent의 목적지를 설정하여 이동을 시작합니다.
        Debug.Log($"{LOG_PREFIX} ({NPCName}) 목적지({target})로 이동 시작. 정지 거리: {stoppingDistance}");
    }

    /// <summary>
    /// NPC를 홈 포인트(스폰 지점 또는 지정된 복귀 지점)로 복귀시키는 메서드입니다.
    /// </summary>
    public virtual void ReturnHome()
    {
        if (homePoint == null)
        {
            Debug.LogError($"{LOG_PREFIX} ({NPCName}) ReturnHome 시도 중 homePoint가 null입니다.");
            return;
        }
        Debug.Log($"{LOG_PREFIX} ({NPCName}) 홈 포인트({homePoint.name}, {homePoint.position})로 복귀 시작.");
        MoveTo(homePoint.position); // homePoint의 위치로 이동합니다.
    }
}