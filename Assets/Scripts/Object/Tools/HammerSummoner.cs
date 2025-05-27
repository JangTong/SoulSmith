using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(ItemInteractionController))]
public class HammerSummoner : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField, Range(1, 20)] private int spinCount = 3;  // 원하는 만큼 회전 횟수 설정
    [SerializeField] private KeyCode summonKey = KeyCode.H;
    [SerializeField] private GameObject hammerPrefab;
    [SerializeField, Range(1f, 10f)] private float spawnDistance = 5f;
    [SerializeField, Range(0.1f, 2f)] private float flightDuration = 0.5f;
    [SerializeField] private Vector3 handLocalPos = new Vector3(0.5f, -0.5f, 0.5f);
    [SerializeField] private Vector3 handLocalRot = new Vector3(-45, 0, 90);

    private GameObject hammerInstance;
    private bool isSummoning;

    private void Update()
    {
        if (Input.GetKeyDown(summonKey) && !isSummoning)
            TrySummon();
    }

    private void TrySummon()
    {
        var ctrl = ItemInteractionController.Instance;
        if (ctrl == null) return;

        // 손에 다른 아이템이 있으면 중단
        if (ctrl.heldItemRight != null && ctrl.heldItemRight != hammerInstance)
        {
            Debug.LogWarning("[HammerSummoner] 이미 손에 다른 아이템이 있습니다.");
            return;
        }

        isSummoning = true;

        if (hammerInstance == null)
        {
            // 1) 최초 생성: 랜덤 위치에서 날아오게
            hammerInstance = Instantiate(hammerPrefab, GetRandomSpawnPosition(ctrl.playerCamera.position), Quaternion.identity);
            PrepareHammer(hammerInstance);
        }
        else
        {
            // 2) 이미 존재하는 망치는 현재 위치에서 바로 날아오게
            //    (물리/콜라이더 초기화만 해두면 됨)
            if (hammerInstance.TryGetComponent<Collider>(out var col)) col.enabled = false;
            if (hammerInstance.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
            Debug.Log("[HammerSummoner] 기존 망치 소환 준비 완료 at " + hammerInstance.transform.position);
        }

        // 공통: 날아오는 트윈
        FlyToHand(ctrl);
    }

    private Vector3 GetRandomSpawnPosition(Vector3 origin)
    {
        Vector3 dir = Random.onUnitSphere;
        dir.y = Mathf.Abs(dir.y);
        return origin + dir.normalized * spawnDistance;
    }

    private void PrepareHammer(GameObject hammer)
    {
        // 이미 위치는 Instantiate 시 세팅됐으므로 회전만 리셋
        hammer.transform.rotation = Quaternion.identity;
        if (hammer.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
        Debug.Log("[HammerSummoner] 망치 준비 완료 at " + hammer.transform.position);
    }

    private void FlyToHand(ItemInteractionController ctrl)
    {
        Vector3 target = ctrl.playerCamera.TransformPoint(handLocalPos);

        // 1) 위치 이동 트윈
        var moveTween = hammerInstance.transform
            .DOMove(target, flightDuration)
            .SetEase(Ease.InOutQuad);

        // 2) 로컬 Y축 회전: 360° × spinCount
        var rotateTween = hammerInstance.transform
            .DOLocalRotate(new Vector3(0f, 360f * spinCount, 0f),
                        flightDuration,
                        RotateMode.FastBeyond360)
            .SetEase(Ease.Linear);

        // 3) 이동 완료 시 회전 멈추고 장착
        moveTween.OnComplete(() =>
        {
            rotateTween.Kill();
            AttachToHand(ctrl);
        });
    }

    private void AttachToHand(ItemInteractionController ctrl)
    {
        hammerInstance.transform.SetParent(ctrl.playerCamera);
        hammerInstance.transform
            .DOLocalMove(handLocalPos, 0.2f).SetEase(Ease.InOutQuad);
        hammerInstance.transform
            .DOLocalRotate(handLocalRot, 0.2f).SetEase(Ease.InOutQuad)
            .OnComplete(() => FinalizeAttach(ctrl));
    }

    private void FinalizeAttach(ItemInteractionController ctrl)
    {
        if (hammerInstance.TryGetComponent<Collider>(out var col)) col.enabled = true;
        if (hammerInstance.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;

        ctrl.heldItemRight = hammerInstance;
        ctrl.currentState = ItemInteractionController.State.Equipped;
        isSummoning = false;

        Debug.Log("[HammerSummoner] 망치 장착 완료");
    }

    public void DestroyHammer()
    {
        if (hammerInstance != null)
        {
            Destroy(hammerInstance);
            hammerInstance = null;
            Debug.Log("[HammerSummoner] 망치 제거됨");
        }
    }
}
