using UnityEngine;
using DG.Tweening;

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
        PlayerController.Instance.MoveCameraToWorld(cameraCraftingViewPoint, cameraMoveDuration); // 카메라 이동
    }

    private void SwitchToMainCamera()
    {
        PlayerController.Instance.ResetCameraToLocalDefault(cameraMoveDuration); // 로컬 기준 복귀
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "HammerHead")
        {
            CombineParts();
            return;
        }

        ItemComponent item = other.GetComponent<ItemComponent>();
        if (item == null || item.partsType == PartsType.None || item.canCombine == false) return;

        if (ItemPickup.Instance != null && ItemPickup.Instance.pickedItem != null)
        {
            Debug.LogWarning("❌ 다른 아이템을 들고 있는 상태에서는 부품을 추가할 수 없습니다!");
            return;
        }

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

    isEditing = true;
}
        else return;

        SwitchToTableCamera();
    }

    private void CombineParts()
    {
        if (currentBlade == null)
        {
            Debug.LogError("❌ currentBlade가 NULL입니다! 조합할 수 없습니다.");
            return;
        }

        foreach (Transform child in currentBlade.transform)
        {
            if (child.TryGetComponent<ItemComponent>(out ItemComponent part))
            {
                currentBlade.AddStatsFrom(part);
                part.canCombine = false; // 조합 완료 처리
            }
            if (child.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                Destroy(rb);
            }
        }

        currentBlade.canCombine = false;

        Debug.Log("🔨 HammerHead 충돌 감지! 모든 부품이 하나로 합쳐지고 PartsType이 None으로 변경되었습니다.");
        currentBlade = null;
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