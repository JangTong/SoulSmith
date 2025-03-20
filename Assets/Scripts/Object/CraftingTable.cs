using UnityEngine;

public class CraftingTable : MonoBehaviour
{
    [Header("References")]
    public Transform snapPoint;
    public Camera tableCamera;
    private Camera mainCamera; // Main Camera 캐싱

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

        // 🎯 게임 시작 시 테이블 카메라 비활성화, 메인 카메라 활성화
        tableCamera.enabled = false;
        if (mainCamera != null)
        {
            mainCamera.enabled = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // ✅ 이름이 "HammerHead"인 오브젝트가 충돌하면 모든 부품을 하나로 합침
        if (other.gameObject.name == "HammerHead")
        {
            CombineParts();
            return;
        }
        
        ItemComponent item = other.GetComponent<ItemComponent>();
        if (item == null || item.partsType == PartsType.None) return; // ✅ PartsType이 None이면 인식하지 않음
        
        // ✅ ItemPickup에 PickedItem이 없을 때만 아이템 인식
        if (ItemPickup.Instance != null && ItemPickup.Instance.pickedItem != null)
        {
            Debug.LogWarning("❌ 다른 아이템을 들고 있는 상태에서는 부품을 추가할 수 없습니다!");
            return;
        }

        // ✅ Blade가 없는 상태에서 부품을 감지하지 않도록 수정
        if (currentBlade == null && item.partsType != PartsType.Blade) 
        {
            Debug.LogWarning("❌ Blade가 없는 상태에서 부품을 추가할 수 없습니다!");
            return;
        }

        AttachItem(item);
    }

    private void AttachItem(ItemComponent item)
    {
        
        bool hasBlade = false; // Blade 존재 여부 확인용 변수

        foreach (Transform child in this.transform)
        {
            if (child.TryGetComponent<ItemComponent>(out ItemComponent part) && part.partsType == PartsType.Blade)
            {
                hasBlade = true;
                break; // Blade가 하나라도 있으면 루프 종료
            }
        }

        // Blade가 없으면 currentBlade와 currentPart 초기화
        if (!hasBlade)
        {
            Debug.LogWarning("❌ Blade 부품이 없으므로 초기화합니다.");
            currentBlade = null;
        }

        if (currentBlade == null && item.partsType == PartsType.Blade)
        {
            // Blade를 추가하는 경우
            currentBlade = item;
            currentBlade.transform.SetParent(snapPoint);
            
            currentBlade.transform.localRotation = Quaternion.identity; // 🎯 회전 초기화
            currentBlade.transform.localPosition = Vector3.zero;


            if (!currentBlade.TryGetComponent<Rigidbody>(out Rigidbody bladeRb))
            {
                bladeRb = currentBlade.gameObject.AddComponent<Rigidbody>();
                bladeRb.mass = 5f;
                bladeRb.interpolation = RigidbodyInterpolation.Interpolate;
                bladeRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
            bladeRb.isKinematic = true; // Blade를 고정하여 물리 연산 방지
            currentPart = null; // Blade를 고정한 후 추가 부품을 이동하도록 설정
            isEditing = true;
        }
        else if (currentBlade != null && item.partsType != PartsType.None && item.weaponType == currentBlade.weaponType)
        {
            // Blade가 존재하고 같은 WeaponType을 가진 부품 추가
            currentPart = item;
            currentPart.transform.SetParent(currentBlade.transform, true);
            currentPart.transform.localPosition = new Vector3(currentPart.transform.localPosition.x, 0, currentPart.transform.localPosition.z);
            currentPart.transform.localRotation = Quaternion.identity;

            // Blade가 아닌 경우에만 Rigidbody 제거
            if (currentPart.partsType != PartsType.Blade && currentPart.TryGetComponent<Rigidbody>(out Rigidbody partRb))
            {
                partRb.isKinematic = true;
            }

            // Compound Collider 활성화
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

        // ✅ Blade와 모든 자식의 PartsType을 None으로 변경하고 Rigidbody 제거
        currentBlade.partsType = PartsType.None;
        foreach (Transform child in currentBlade.transform)
        {
            if (child.TryGetComponent<ItemComponent>(out ItemComponent part))
            {
                part.partsType = PartsType.None;
                part.weaponType = WeaponType.None;
            }
            if (child.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                Destroy(rb);
            }
        }
        
        Debug.Log("🔨 HammerHead 충돌 감지! 모든 부품이 하나로 합쳐지고 PartsType이 None으로 변경되었습니다.");
        currentBlade = null; // 🎯 다음 Blade를 인식할 수 있도록 초기화
    }

    private void Update()
    {
        if (!isEditing || (currentBlade == null && currentPart == null)) return;

        float moveX = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;
        float moveZ = -Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;

        if (currentPart != null)
        {
            currentPart.transform.localPosition += new Vector3(moveX, 0, moveZ);
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

        currentBlade.atkPower += currentPart.atkPower;
        currentBlade.defPower += currentPart.defPower;

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
