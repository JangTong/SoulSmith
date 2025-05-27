using UnityEngine;

public class Bellows : MonoBehaviour, IInteractable
{
    private Forge forge;
    [Header("Animation")]
    [Tooltip("풀무 애니메이터 컴포넌트")]
    public Animator animator;   // ★ 추가

    private static readonly int PlayFlapHash = Animator.StringToHash("PlayFlap");

    private void Start()
    {
        forge = GetComponentInParent<Forge>();
        if (forge == null)
            Debug.LogError("[Bellows] 부모에 Forge가 없습니다!");

        if (animator == null)
            Debug.LogError("[Bellows] Animator가 할당되지 않았습니다!");
    }

    public void Interact()
    {
        if (forge == null)
        {
            Debug.LogWarning("[Bellows] Forge 참조가 없음, 작동 안함");
            return;
        }

        Debug.Log("[Bellows] 상호작용: 애니메이션 재생 시도");
        
        // 1) 애니메이션 트리거
        animator.SetTrigger(PlayFlapHash);
        Debug.Log("[Bellows] 애니메이터에 PlayFlap 트리거 전송");

        // 2) 사운드 재생
        string[] soundNames = { "Bellow_1", "Bellow_2" };
        int idx = Random.Range(0, soundNames.Length);
        SoundManager.Instance.PlaySoundAtPosition(soundNames[idx], transform.position);
        Debug.Log($"[Bellows] 사운드 재생: {soundNames[idx]}");

        // 3) 대장간 작동
        forge.StartForging();
        Debug.Log("[Bellows] Forge.StartForging 호출");
    }
}
