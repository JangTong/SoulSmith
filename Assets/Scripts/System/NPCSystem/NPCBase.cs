using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// 모든 NPC의 공통 기능을 제공하는 기본 클래스
/// Interact은 대화 가능 여부에 따라 처리하며,
/// 거래 등 다른 행동은 서브클래스에서 오버라이드합니다.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public abstract class NPCBase : MonoBehaviour, IInteractable
{
    [Header("NPC Info")]
    [Tooltip("NPC의 식별 이름")] public string NPCName;

    [Header("Dialogue Settings")]
    [Tooltip("일반 대화 가능 여부")] public bool isDialogueable = false;
    [Tooltip("재생할 대화 데이터")]
    public DialogueData dialogueData;
    [Tooltip("대화 완료 후 실행할 콜백 (선택)")]
    public UnityAction onDialogueComplete;

    protected NavMeshAgent agent;
    protected Transform homePoint;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        homePoint = transform;
        Debug.Log($"[NPCBase] '{NPCName}' Awake - Dialogueable: {isDialogueable}");
    }

    /// <summary>
    /// NPC 초기화 (스폰 시 호출)
    /// </summary>
    public virtual void Initialize(string name, Transform home)
    {
        NPCName = name;
        homePoint = home;
        transform.SetPositionAndRotation(home.position, home.rotation);
        Debug.Log($"[NPCBase] Initialized '{NPCName}' at {home.position}");
    }

    /// <summary>
    /// 플레이어가 상호작용 시 호출되는 함수
    /// </summary>
    public virtual void Interact()
    {
        if (isDialogueable)
        {
            if (dialogueData != null)
            {
                Debug.Log($"[NPCBase] '{NPCName}' start dialogue");
                DialogueManager.Instance.PlayGeneralDialogue(dialogueData, onDialogueComplete);
            }
            else
            {
                Debug.LogWarning($"[NPCBase] '{NPCName}' has no DialogueData.");
            }
        }
        else
        {
            Debug.Log($"[NPCBase] '{NPCName}' has no interaction defined.");
        }
    }

    /// <summary>
    /// 목적지로 이동
    /// </summary>
    public virtual void MoveTo(Vector3 target, float stoppingDistance = 0.5f)
    {
        agent.stoppingDistance = stoppingDistance;
        agent.SetDestination(target);
        Debug.Log($"[NPCBase] '{NPCName}' MoveTo → {target}");
    }

    /// <summary>
    /// 홈 포인트(스폰 지점)으로 복귀
    /// </summary>
    public virtual void ReturnHome()
    {
        MoveTo(homePoint.position);
        Debug.Log($"[NPCBase] '{NPCName}' Returning home → {homePoint.position}");
    }
}