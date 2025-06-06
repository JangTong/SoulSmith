using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(ItemInteractionController))]
public class HammerSummoner : MonoBehaviour
{
    [Header("Settings")]
    // 회전 횟수 대신 초당 회전 속도(deg/sec)
    [SerializeField] private float rotationSpeed = 360f; 
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
        if (Input.GetKeyDown(summonKey) && !isSummoning && !PlayerController.Instance.IsUIActive())
            TrySummon();
    }

    /// <summary>
    /// 외부에서 망치 소환을 호출할 수 있는 public 메서드
    /// </summary>
    public void Summon()
    {
        TrySummon();
    }

    private void TrySummon()
    {
        var ctrl = ItemInteractionController.Instance;
        if (ctrl == null) return;

        // 이미 다른 아이템이 잡혀 있으면 중단
        if (ctrl.heldItemRight != null && ctrl.heldItemRight != hammerInstance)
        {
            Debug.LogWarning("[HammerSummoner] 이미 손에 다른 아이템이 있습니다.");
            return;
        }

        isSummoning = true;

        if (hammerInstance == null)
        {
            // 최초 생성: 랜덤 위치에서
            hammerInstance = Instantiate(
                hammerPrefab, 
                GetRandomSpawnPosition(ctrl.playerCamera.position), 
                Quaternion.identity
            );
            PrepareHammer(hammerInstance);
        }
        else
        {
            // 이미 존재하는 망치: 콜라이더/물리 리셋
            if (hammerInstance.TryGetComponent<Collider>(out var col)) col.enabled = false;
            if (hammerInstance.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
            Debug.Log($"[HammerSummoner] 기존 망치 소환 준비 완료 at {hammerInstance.transform.position}");
        }

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
        hammer.transform.rotation = Quaternion.identity;
        if (hammer.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
        Debug.Log($"[HammerSummoner] 망치 준비 완료 at {hammer.transform.position}");
    }

    private void FlyToHand(ItemInteractionController ctrl)
    {
        Vector3 target = ctrl.playerCamera.TransformPoint(handLocalPos);

        // 1) 위치 이동
        var moveTween = hammerInstance.transform
            .DOMove(target, flightDuration)
            .SetEase(Ease.InOutQuad);

        // 2) 로컬 Y축 기준 회전 속도 제어
        float totalAngle = rotationSpeed * flightDuration;
        var rotateTween = hammerInstance.transform
            .DOLocalRotate(
                new Vector3(0f, totalAngle, 0f),
                flightDuration,
                RotateMode.FastBeyond360
            )
            .SetEase(Ease.Linear);

        Debug.Log($"[HammerSummoner] Flying to hand with LOCAL Y-rotation: duration={flightDuration}s, rotationSpeed={rotationSpeed}°/s (totalAngle={totalAngle}°)");

        // 3) 이동 완료 시 회전 트윈 중지 후 장착
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

        var camCtrl = PlayerController.Instance.cam;
        ctrl.heldItemRight = hammerInstance;
        ctrl.currentState = ItemInteractionController.State.Equipped;
        isSummoning = false;

        Debug.Log("[HammerSummoner] 망치 장착 완료");
        if (camCtrl != null)
        {
            camCtrl.ShakeCamera(0.15f, 0.05f);
            Debug.Log("[HammerSummoner] 카메라 흔들기 트리거");
        }
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
