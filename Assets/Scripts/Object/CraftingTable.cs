using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class CraftingTable : MonoBehaviour
{
    // 디버그 로그 접두어
    private const string LOG_PREFIX = "[CraftingTable]";

    [Header("References")]
    public Transform snapPoint;
    public Transform partsHolder; // 파츠를 임시로 보관할 Transform
    private Camera mainCamera;

    [Header("Camera Positions")]
    public Transform cameraCraftingViewPoint;
    public float cameraMoveDuration = 0.5f;

    [Header("Settings")]
    public float moveSpeed;
    public ItemComponent currentBlade;
    public ItemComponent currentPart;
    private bool isEditing = false;
    
    [Header("Forging Settings")]
    public int requiredHitsPerPart = 3; // 각 파츠당 필요한 타격 횟수
    
    // 파츠별 타격 횟수 추적
    private Dictionary<Transform, int> partHitCounts = new Dictionary<Transform, int>();

    private void Awake()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError($"{LOG_PREFIX} Awake: Main Camera를 찾을 수 없습니다. 'MainCamera' 태그를 확인하세요.");
        }
        
        // partsHolder가 없으면 생성
        if (partsHolder == null)
        {
            partsHolder = new GameObject("PartsHolder").transform;
            partsHolder.SetParent(transform);
            partsHolder.localPosition = Vector3.zero;
            Debug.Log($"{LOG_PREFIX} Awake: PartsHolder 생성됨");
        }
    }

    private void SwitchToTableCamera()
    {
        if (cameraCraftingViewPoint == null)
        {
            Debug.LogError($"{LOG_PREFIX} SwitchToTableCamera: 카메라 작업대 위치가 설정되지 않았습니다!");
            return;
        }
        Debug.Log($"{LOG_PREFIX} SwitchToTableCamera: 카메라를 작업대 시점으로 이동 (duration={cameraMoveDuration}s)");
        PlayerController.Instance.cam.MoveTo(cameraCraftingViewPoint, cameraMoveDuration); // 카메라 이동
    }

    private void SwitchToMainCamera()
    {
        Debug.Log($"{LOG_PREFIX} SwitchToMainCamera: 카메라를 기본 시점으로 복귀 (duration={cameraMoveDuration}s)");
        PlayerController.Instance.cam.ResetToDefault(cameraMoveDuration); // 로컬 기준 복귀
    }

    private void OnTriggerEnter(Collider other)
    {
        // HammerHead는 이제 Raycast 기반으로만 처리 (무시)
        if (other.gameObject.name == "HammerHead")
        {
            return;
        }

        // 2) 플레이어가 들고 있거나 장착한 아이템(UI/Detector에서 camera 자식으로 붙임)은 무시
        var ctrl = ItemInteractionController.Instance;
        if (ctrl != null && other.transform.IsChildOf(ctrl.playerCamera))
            return;

        // 3) 나머지 아이템 인식 기존 로직
        ItemComponent item = other.GetComponent<ItemComponent>();
        if (item == null || item.partsType == PartsType.None || item.canCombine == false)
            return;

        if (currentBlade == null && item.partsType != PartsType.Blade)
        {
            Debug.LogWarning($"{LOG_PREFIX} OnTriggerEnter: Blade가 없는 상태에서 부품을 추가할 수 없습니다!");
            return;
        }

        Debug.Log($"{LOG_PREFIX} OnTriggerEnter: 아이템 '{item.name}' 감지됨 (파츠 타입: {item.partsType})");
        AttachItem(item);
    }
    
    private void AttachItem(ItemComponent item)
    {
        bool hasBlade = false;
        foreach (Transform child in this.transform)
        {
            if (child.TryGetComponent<ItemComponent>(out ItemComponent part) && part.partsType == PartsType.Blade)
            {
                hasBlade = true;
                break;
            }
        }

        if (!hasBlade)
        {
            Debug.LogWarning($"{LOG_PREFIX} AttachItem: Blade 부품이 없으므로 초기화합니다.");
            currentBlade = null;
        }

if (currentBlade == null && item.partsType == PartsType.Blade && item.canCombine)
{
    Debug.Log($"{LOG_PREFIX} AttachItem: 블레이드 '{item.name}' 장착");
    currentBlade = item;

    currentBlade.transform.SetParent(snapPoint, worldPositionStays: true); // 💡 위치 보존

    currentBlade.transform
        .DOLocalMove(Vector3.zero, 0.2f)
        .SetEase(Ease.OutSine);
    currentBlade.transform
        .DOLocalRotate(Vector3.zero, 0.2f)
        .SetEase(Ease.OutSine);

    // Rigidbody 설정은 그대로
    if (!currentBlade.TryGetComponent<Rigidbody>(out Rigidbody bladeRb))
    {
        bladeRb = currentBlade.gameObject.AddComponent<Rigidbody>();
        bladeRb.mass = 5f;
        bladeRb.interpolation = RigidbodyInterpolation.Interpolate;
        bladeRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }
    bladeRb.isKinematic = true;

    currentPart = null;
    isEditing = true;
}
else if (currentBlade != null && item.partsType != PartsType.None && item.weaponType == currentBlade.weaponType && item.canCombine)
{
    Debug.Log($"{LOG_PREFIX} AttachItem: 파츠 '{item.name}' 배치 시작 (타입: {item.partsType})");
    currentPart = item;

    // 블레이드의 자식으로 바로 넣지 않고 partsHolder에 임시 배치
    currentPart.transform.SetParent(partsHolder, worldPositionStays: true);
    Debug.Log($"{LOG_PREFIX} AttachItem: 파츠가 partsHolder의 자식으로 설정됨");

    // 블레이드 주변에 배치하기 위해 블레이드 기준 위치 계산
    Vector3 bladePosition = currentBlade.transform.position;
    Vector3 targetPosition = bladePosition + new Vector3(0, 0, 0.2f); // 블레이드 위에 약간 떠 있게 배치
    
    currentPart.transform
        .DOMove(targetPosition, 0.2f)
        .SetEase(Ease.OutSine);
    currentPart.transform
        .DOLocalRotate(Vector3.zero, 0.2f)
        .SetEase(Ease.OutSine);

    if (currentPart.partsType != PartsType.Blade && currentPart.TryGetComponent<Rigidbody>(out Rigidbody partRb))
    {
        partRb.isKinematic = true;
    }

    Collider partCollider = currentPart.GetComponent<Collider>();
    Collider bladeCollider = currentBlade.GetComponent<Collider>();
    if (partCollider != null && bladeCollider != null)
    {
        Physics.IgnoreCollision(partCollider, bladeCollider, true);
        Debug.Log($"{LOG_PREFIX} AttachItem: 파츠-블레이드 콜라이더 충돌 무시됨");
    }
    
    // 파츠 타격 횟수 초기화
    partHitCounts[currentPart.transform] = 0;

    isEditing = true;
    Debug.Log($"{LOG_PREFIX} AttachItem: 파츠 배치 완료, 이동 가능 상태");
}
        else return;

        SwitchToTableCamera();
    }
    
    // Raycast 기반 망치 타격 처리 함수 - Hammer.cs에서 호출됨
    public void HandleHammerHit(Transform hitTransform, Vector3 hitPoint)
    {
        Debug.Log($"{LOG_PREFIX} HandleHammerHit: 타격됨 - {hitTransform.name}, 위치={hitPoint}");
        
        if (currentBlade == null)
        {
            Debug.LogWarning($"{LOG_PREFIX} HandleHammerHit: currentBlade가 NULL입니다!");
            return;
        }
        
        // 파츠 컴포넌트 확인
        ItemComponent partItem = hitTransform.GetComponent<ItemComponent>();
        if (partItem == null)
        {
            Debug.LogWarning($"{LOG_PREFIX} HandleHammerHit: {hitTransform.name}에 ItemComponent가 없습니다!");
            return;
        }
        
        // 블레이드는 타격 대상이 아님
        if (partItem.partsType == PartsType.Blade)
        {
            Debug.LogWarning($"{LOG_PREFIX} HandleHammerHit: 블레이드는 타격 대상이 아닙니다!");
            return;
        }
        
        // 이미 합성된 파츠는 무시
        if (partItem.isPolished)
        {
            Debug.Log($"{LOG_PREFIX} HandleHammerHit: 이미 합성된 파츠입니다: {partItem.name}");
            return;
        }
        
        // 현재 파츠가 블레이드에 적합한지 확인
        if (partItem.weaponType != currentBlade.weaponType)
        {
            Debug.LogWarning($"{LOG_PREFIX} HandleHammerHit: {partItem.name}은(는) {currentBlade.name}에 적합하지 않은 무기 타입입니다!");
            return;
        }
        
        // 타격 횟수 증가
        if (!partHitCounts.ContainsKey(hitTransform))
        {
            partHitCounts[hitTransform] = 0;
        }
        
        partHitCounts[hitTransform]++;
        int currentHits = partHitCounts[hitTransform];
        
        Debug.Log($"{LOG_PREFIX} HandleHammerHit: 파츠 '{partItem.name}' 타격됨 ({currentHits}/{requiredHitsPerPart})");
        
        // 필요한 타격 횟수에 도달하면 파츠 합성
        if (currentHits >= requiredHitsPerPart)
        {
            Debug.Log($"{LOG_PREFIX} HandleHammerHit: 파츠 '{partItem.name}' 단조 완료!");
            
            // 파츠 합성
            CombineSinglePart(partItem);
        }
    }
    
    // 단일 파츠 합성
    private void CombineSinglePart(ItemComponent part)
    {
        if (currentBlade == null || part == null) return;
        
        Debug.Log($"{LOG_PREFIX} CombineSinglePart: 파츠 '{part.name}' 합성 시작, 현재 위치: {part.transform.position}");
        
        // 월드 위치와 회전 저장 (이후 복원)
        Vector3 worldPosition = part.transform.position;
        Quaternion worldRotation = part.transform.rotation;
        
        // 블레이드에 파츠 스탯 추가
        currentBlade.AddStatsFrom(part);
        
        // 파츠를 블레이드의 자식으로 이동 (합성 완료된 파츠만 블레이드에 부착)
        part.transform.SetParent(currentBlade.transform);
        Debug.Log($"{LOG_PREFIX} CombineSinglePart: 파츠가 블레이드의 자식으로 설정됨");
        
        // 파츠 위치와 회전을 유지하기 위해 DOTween 사용
        part.transform.DOMove(worldPosition, 0.2f).SetEase(Ease.OutQuad);
        part.transform.DORotateQuaternion(worldRotation, 0.2f).SetEase(Ease.OutQuad)
            .OnComplete(() => {
                // 애니메이션 완료 후 Y 위치를 0으로 조정
                Vector3 localPos = part.transform.localPosition;
                localPos.y = 0;
                part.transform.localPosition = localPos;
                Debug.Log($"{LOG_PREFIX} CombineSinglePart: 파츠 위치 조정 완료 - local={localPos}");
            });
        
        // 파츠의 Rigidbody 제거 (콜라이더는 유지)
        if (part.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            Destroy(rb);
            Debug.Log($"{LOG_PREFIX} CombineSinglePart: Rigidbody 제거됨");
        }
        
        // 합성 완료 표시
        part.isPolished = true;
        part.canCombine = false;
        
        Debug.Log($"{LOG_PREFIX} CombineSinglePart: 파츠 '{part.name}'가 '{currentBlade.name}'에 합성되었습니다.");
    }

    private void Update()
    {
        if (!isEditing || (currentBlade == null && currentPart == null)) return;

        float moveX = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;
        float moveZ = -Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;

        // 이동 입력이 있을 때만 로그 출력
        if (currentPart != null && (Mathf.Abs(moveX) > 0.001f || Mathf.Abs(moveZ) > 0.001f))
        {
            // 이동 전 위치
            Vector3 oldPos = currentPart.transform.localPosition;
            
            Vector3 newPos = currentPart.transform.localPosition + new Vector3(moveX, 0, moveZ);
            float clampedX = Mathf.Clamp(newPos.x, -0.4f, 0.4f);
            float clampedZ = Mathf.Clamp(newPos.z, -1f, 1f);
            currentPart.transform.localPosition = new Vector3(clampedX, 0, clampedZ);
            
            Debug.Log($"{LOG_PREFIX} Update: 파츠 이동 - {oldPos} → {currentPart.transform.localPosition}, 입력={moveX:F3},{moveZ:F3}");
        }

        // 키 입력 처리
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log($"{LOG_PREFIX} Update: Space 키 감지됨 - 파츠 위치 확정");
            FinalizeAttachment();
        }
        
        // ESC 키로 작업 취소
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log($"{LOG_PREFIX} Update: ESC 키 감지됨 - 작업 취소");
            CancelEditing();
        }
    }
    
    // 작업 취소
    private void CancelEditing()
    {
        if (!isEditing) return;
        
        Debug.Log($"{LOG_PREFIX} CancelEditing: 작업 취소");
        isEditing = false;
        SwitchToMainCamera();
    }

    private void FinalizeAttachment()
    {
        if (currentBlade == null)
        {
            Debug.LogError($"{LOG_PREFIX} FinalizeAttachment: currentBlade가 NULL입니다! 블레이드를 먼저 배치하세요.");
            return;
        }

        if (currentPart == null)
        {
            Debug.LogError($"{LOG_PREFIX} FinalizeAttachment: currentPart가 NULL입니다! 부품을 먼저 추가하세요.");
            return;
        }

        // 위치 확정 기록
        Vector3 finalLocalPos = currentPart.transform.localPosition;
        Debug.Log($"{LOG_PREFIX} FinalizeAttachment: 파츠 위치 확정 - local={finalLocalPos}");
        
        currentPart.transform.SetParent(currentBlade.transform);
        Debug.Log($"{LOG_PREFIX} FinalizeAttachment: 파츠가 블레이드의 자식으로 설정됨");
        
        currentPart = null;
        isEditing = false;

        SwitchToMainCamera();
    }
}