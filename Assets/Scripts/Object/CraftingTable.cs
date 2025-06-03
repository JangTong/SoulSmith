using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class CraftingTable : MonoBehaviour
{
    [Header("References")]
    public Transform snapPoint;
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
            Debug.LogError("❌ Main Camera를 찾을 수 없습니다. 'MainCamera' 태그를 확인하세요.");
        }
    }

    private void SwitchToTableCamera()
    {
        if (cameraCraftingViewPoint == null)
        {
            Debug.LogError("카메라 작업대 위치가 설정되지 않았습니다!");
            return;
        }
        PlayerController.Instance.cam.MoveTo(cameraCraftingViewPoint, cameraMoveDuration); // 카메라 이동
    }

    private void SwitchToMainCamera()
    {
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
            Debug.LogWarning("❌ Blade가 없는 상태에서 부품을 추가할 수 없습니다!");
            return;
        }

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
            Debug.LogWarning("❌ Blade 부품이 없으므로 초기화합니다.");
            currentBlade = null;
        }

if (currentBlade == null && item.partsType == PartsType.Blade && item.canCombine)
{
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
    currentPart = item;

    currentPart.transform.SetParent(currentBlade.transform, worldPositionStays: true); // 💡 위치 보존

    Vector3 targetLocalPosition = new Vector3(currentPart.transform.localPosition.x, 0, currentPart.transform.localPosition.z);
    currentPart.transform
        .DOLocalMove(targetLocalPosition, 0.2f)
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
    }
    
    // 파츠 타격 횟수 초기화
    partHitCounts[currentPart.transform] = 0;

    isEditing = true;
}
        else return;

        SwitchToTableCamera();
    }
    
    // Raycast 기반 망치 타격 처리 함수 - Hammer.cs에서 호출됨
    public void HandleHammerHit(Transform hitTransform, Vector3 hitPoint)
    {
        Debug.Log($"[CraftingTable] HandleHammerHit 호출됨: {hitTransform.name}");
        
        if (currentBlade == null)
        {
            Debug.LogWarning("[CraftingTable] currentBlade가 NULL입니다!");
            return;
        }
        
        // 타격된 파츠가 현재 블레이드의 자식인지 확인
        if (!hitTransform.IsChildOf(currentBlade.transform) && hitTransform != currentBlade.transform)
        {
            Debug.LogWarning($"[CraftingTable] {hitTransform.name}은(는) currentBlade의 자식이 아닙니다!");
            return;
        }
        
        // 파츠 컴포넌트 확인
        ItemComponent partItem = hitTransform.GetComponent<ItemComponent>();
        if (partItem == null)
        {
            Debug.LogWarning($"[CraftingTable] {hitTransform.name}에 ItemComponent가 없습니다!");
            return;
        }
        
        // 블레이드는 타격 대상이 아님
        if (partItem.partsType == PartsType.Blade)
        {
            Debug.LogWarning("[CraftingTable] 블레이드는 타격 대상이 아닙니다!");
            return;
        }
        
        // 이미 합성된 파츠는 무시
        if (partItem.isPolished)
        {
            Debug.Log($"[CraftingTable] 이미 합성된 파츠입니다: {partItem.name}");
            return;
        }
        
        // 타격 횟수 증가
        if (!partHitCounts.ContainsKey(hitTransform))
        {
            partHitCounts[hitTransform] = 0;
        }
        
        partHitCounts[hitTransform]++;
        int currentHits = partHitCounts[hitTransform];
        
        Debug.Log($"[CraftingTable] 파츠 '{partItem.name}' 타격됨 ({currentHits}/{requiredHitsPerPart})");
        
        // 필요한 타격 횟수에 도달하면 파츠 합성
        if (currentHits >= requiredHitsPerPart)
        {
            Debug.Log($"[CraftingTable] 파츠 '{partItem.name}' 단조 완료!");
            
            // 파츠 합성
            CombineSinglePart(partItem);
        }
    }
    
    // 단일 파츠 합성
    private void CombineSinglePart(ItemComponent part)
    {
        if (currentBlade == null || part == null) return;
        
        // 블레이드에 파츠 스탯 추가
        currentBlade.AddStatsFrom(part);
        
        // 파츠의 Rigidbody 제거 (콜라이더는 유지)
        if (part.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            Destroy(rb);
        }
        
        // 합성 완료 표시
        part.isPolished = true;
        part.canCombine = false;
        
        Debug.Log($"[CraftingTable] 파츠 '{part.name}'가 '{currentBlade.name}'에 합성되었습니다.");
    }

    private void Update()
    {
        if (!isEditing || (currentBlade == null && currentPart == null)) return;

        float moveX = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;
        float moveZ = -Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;

        if (currentPart != null)
        {
            Vector3 newPos = currentPart.transform.localPosition + new Vector3(moveX, 0, moveZ);
            float clampedX = Mathf.Clamp(newPos.x, -0.4f, 0.4f);
            float clampedZ = Mathf.Clamp(newPos.z, -1f, 1f);
            currentPart.transform.localPosition = new Vector3(clampedX, 0, clampedZ);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            FinalizeAttachment();
        }
        
        // ESC 키로 작업 취소
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelEditing();
        }
    }
    
    // 작업 취소
    private void CancelEditing()
    {
        if (!isEditing) return;
        
        Debug.Log("작업 취소");
        isEditing = false;
        SwitchToMainCamera();
    }

    private void FinalizeAttachment()
    {
        if (currentBlade == null)
        {
            Debug.LogError("❌ currentBlade가 NULL입니다! 블레이드를 먼저 배치하세요.");
            return;
        }

        if (currentPart == null)
        {
            Debug.LogError("❌ currentPart가 NULL입니다! 부품을 먼저 추가하세요.");
            return;
        }

        currentPart.transform.SetParent(currentBlade.transform);
        currentPart = null;
        isEditing = false;

        SwitchToMainCamera();
    }
}