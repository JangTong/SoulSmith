using UnityEngine;

public class CraftingTable : MonoBehaviour
{
    [Header("References")]
    public Transform snapPoint;
    public Camera tableCamera;
    private Camera mainCamera;

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

        tableCamera.enabled = false;
        if (mainCamera != null)
        {
            mainCamera.enabled = true;
        }
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
            currentBlade.transform.SetParent(snapPoint);
            currentBlade.transform.localRotation = Quaternion.identity;
            currentBlade.transform.localPosition = Vector3.zero;

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
            currentPart.transform.SetParent(currentBlade.transform, true);
            currentPart.transform.localPosition = new Vector3(currentPart.transform.localPosition.x, 0, currentPart.transform.localPosition.z);
            currentPart.transform.localRotation = Quaternion.identity;

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

        PlayerController.Instance.ToggleUI(true);
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

        PlayerController.Instance.ToggleUI(false);
        SwitchToMainCamera();
    }

    private void SwitchToTableCamera()
    {
        if (mainCamera != null)
        {
            mainCamera.enabled = false;
        }
        tableCamera.enabled = true;
        ItemPickup.Instance.canPickUp = false;
    }

    private void SwitchToMainCamera()
    {
        if (mainCamera == null)
        {
            Debug.LogError("❌ Main Camera가 NULL입니다! 'MainCamera' 태그를 확인하세요.");
            return;
        }

        tableCamera.enabled = false;
        mainCamera.enabled = true;
        ItemPickup.Instance.canPickUp = true;
    }
}