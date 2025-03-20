using UnityEngine;
using TMPro;
using DG.Tweening;
using Unity.VisualScripting;
using System.IO;

public class ItemPickup : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static ItemPickup Instance { get; private set; }

    public Transform playerCamera;             // 플레이어의 카메라 위치
    public TextMeshProUGUI itemNameText;       // 아이템 이름을 표시할 UI 텍스트
    public GameObject pickedItem = null;      // 현재 플레이어가 들고 있는 아이템 (GameObject 타입)
    public float rotationSpeed = 100f;         // 아이템 회전 속도 (마우스 스크롤로 제어)
    public float pickupDistance = 4f;          // 아이템을 줍는 최대 거리
    public bool canPickUp = true;              // 아이템 줍기 가능 여부
    public bool isEquipped = false;           // 아이템이 장착되었는지 여부
    public bool isSwinging = false;           // 아이템 휘두르기 상태

    private void Awake()
    {
        // Singleton 인스턴스 설정
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple ItemPickup instances found! Destroying the new one.");
            Destroy(gameObject); // 다른 인스턴스가 이미 존재하면 파괴
            return;
        }

        Instance = this; // 현재 인스턴스를 Singleton으로 설정
    }

    private void Start()
    {
        // 메인 카메라를 플레이어 카메라로 설정
        playerCamera = Camera.main.transform;
    }

    private void Update()
    {
        CheckPickedItem();
        CheckForItem();

        // 좌클릭 입력 처리
        if (Input.GetMouseButtonDown(0))
        {
            if (pickedItem == null && canPickUp) // 아이템 줍기
            {
                PickupItem();
            }
            else if (isEquipped && !isSwinging) // 장착 상태에서 휘두르기
            {
                SwingItem();
            }
            else if (!isSwinging) // 아이템 떨어뜨리기
            {
                DropItem();
            }

            DetectAndInteract(); // 오브젝트 상호작용
        }

        // 우클릭 입력 처리: 아이템 장착/해제
        if (pickedItem != null && Input.GetMouseButtonDown(1) && !isSwinging)
        {
            ToggleEquip();
        }

        // 들고 있는 아이템 회전 및 정렬
        if (pickedItem != null && !isEquipped && !isSwinging)
        {
            RotatePickedItem();
            LockItemRotation();
        }
    }

    private void CheckForItem()
    {
        // 카메라의 정면 방향으로 레이캐스트
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance) && hit.transform.CompareTag("Items"))
        {
            // 아이템 컴포넌트 가져오기
            ItemComponent itemComponent = hit.transform.GetComponent<ItemComponent>();

            if (itemComponent != null && itemComponent.itemName != null)
            {
                // 아이템 이름 표시
                itemNameText.text = itemComponent.itemName; // 아이템의 이름 출력
                itemNameText.enabled = true;
            }
            else
            {
                // 컴포넌트가 없으면 아이템 이름 숨김
                itemNameText.enabled = false;
            }
        }
        else
        {
            // 아이템 이름 숨김
            itemNameText.enabled = false;
        }
    }
    
    public void CheckPickedItem()
    {    
        // 손이 비어있으면 bool변수 초기화
        if (pickedItem == null || playerCamera.transform.childCount == 0)
        {
            pickedItem = null;
            canPickUp = true;
            isEquipped = false;
            isSwinging = false;
        }
    }

    private void DetectAndInteract()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance)) // 상호작용 거리 제한
        {
            InteractiveObject interactiveObject = hit.collider.GetComponent<InteractiveObject>();
            if (interactiveObject != null)
            {
                interactiveObject.Interaction(); // Interaction 호출
                Debug.Log($"상호작용: {hit.collider.name}");
            }
            else
            {
                Debug.Log("상호작용 가능한 오브젝트가 아닙니다.");
            }
        }
        else
        {
            Debug.Log("상호작용 가능한 오브젝트가 범위 내에 없습니다.");
        }
    }

    private void PickupItem()
    {
        // 레이캐스트로 아이템 확인
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance) && hit.transform.CompareTag("Items"))
        {
            pickedItem = hit.transform.gameObject; // 히트된 오브젝트를 pickedItem으로 설정
            pickedItem.transform.SetParent(playerCamera); // 아이템의 부모를 카메라로 설정
            pickedItem.transform.localPosition = new Vector3(0, 0, 1f); // 카메라 앞 위치로 이동
            pickedItem.transform.localRotation = Quaternion.identity; // 회전 초기화

            // Rigidbody가 있는 경우 물리 효과 비활성화
            if (pickedItem.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.isKinematic = true; // 물리 연산 비활성화
            }

            canPickUp = false; // 아이템을 이미 들고 있음
        }
    }

    private void DropItem()
    {
        if (pickedItem != null)
        {
            pickedItem.transform.SetParent(null); // 부모(카메라)에서 분리

            // Rigidbody가 있는 경우 물리 효과 활성화
            if (pickedItem.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.isKinematic = false; // 물리 연산 활성화
            }

            pickedItem = null; // pickedItem 초기화
            isEquipped = false; // 장착 상태 초기화
            canPickUp = true; // 다시 줍기 가능
        }
    }

    private void RotatePickedItem()
    {
        // 마우스 휠 입력을 받아 아이템 회전
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            pickedItem.transform.Rotate(Vector3.up, scrollInput * rotationSpeed, Space.Self); // Y축 기준으로 회전
        }
    }

    private void LockItemRotation()
    {
        // 아이템 회전을 특정 축으로 제한 (Y축만 유지)
        Vector3 eulerAngles = pickedItem.transform.rotation.eulerAngles;
        pickedItem.transform.rotation = Quaternion.Euler(0, eulerAngles.y, 0); // X, Z 축 회전 잠금
    }

    private void ToggleEquip()
    {
        ItemComponent pickedItemComp = pickedItem.GetComponent<ItemComponent>();
        if (isEquipped)
        {
            // 장착 해제: 원래 들고 있는 상태로 복귀
            pickedItem.transform.DOLocalMove(new Vector3(0, 0, 1f), 0.2f).SetEase(Ease.InOutQuad);
            pickedItem.transform.localRotation = Quaternion.identity; // 기본 회전
            isEquipped = false;
        }
        else
        {
            if(pickedItemComp.itemType != ItemType.Weapon)return;
            // 장착: 화면 우측 하단에 배치
            pickedItem.transform.DOLocalMove(new Vector3(0.5f, -0.5f, 0.5f), 0.2f).SetEase(Ease.InOutQuad);
            pickedItem.transform.DOLocalRotate(new Vector3(-45, 0, 90), 0.2f);
            isEquipped = true;
        }
    }
    
    private void SwingItem()
    {
        if (pickedItem == null || isSwinging) return; // 아이템이 없거나 이미 휘두르는 중이면 실행하지 않음

        isSwinging = true;

        // 시작 위치 및 회전 설정 (화면 기준 우측 상단 → 화면 중심)
        Vector3 originalPosition = pickedItem.transform.localPosition;
        Quaternion originalRotation = pickedItem.transform.localRotation;

        Vector3 startRotation = new Vector3(-45, 0, 90);
        Vector3 middleRotation = new Vector3(-100, 00, 90);
        Vector3 endRotation = new Vector3(-20, 0, 90);
    
        Vector3 middlePosition = new Vector3(0.5f, 0.5f, 0.5f);
        Vector3 endPosition = new Vector3(0, 0f, 1f);


        DG.Tweening.Sequence swingSequence = DOTween.Sequence();

        swingSequence.Append(pickedItem.transform.DOLocalRotate(middleRotation, 0.33f).SetEase(Ease.InOutQuad));
        swingSequence.Join(pickedItem.transform.DOLocalMove(middlePosition, 0.33f).SetEase(Ease.InOutQuad));

        swingSequence.Append(pickedItem.transform.DOLocalRotate(endRotation, 0.07f).SetEase(Ease.OutCubic));
        swingSequence.Join(pickedItem.transform.DOLocalMove(endPosition, 0.07f).SetEase(Ease.OutCubic));

        swingSequence.Append(pickedItem.transform.DOLocalRotate(startRotation, 0.2f).SetEase(Ease.InCubic));
        swingSequence.Join(pickedItem.transform.DOLocalMove(originalPosition, 0.2f).SetEase(Ease.InCubic));

        // 애니메이션 종료 후 상태 초기화
        swingSequence.OnComplete(() =>
        {
            pickedItem.transform.localPosition = originalPosition;
            pickedItem.transform.localRotation = originalRotation;
            isSwinging = false;
        });
    }
}