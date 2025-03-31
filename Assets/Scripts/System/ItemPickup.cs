using UnityEngine;
using TMPro;
using DG.Tweening;
using Unity.VisualScripting;
using System.IO;
using System.Collections.Generic;

public class ItemPickup : MonoBehaviour
{
    public static ItemPickup Instance { get; private set; }

    public Transform playerCamera;
    public TextMeshProUGUI itemNameText;
    public GameObject pickedItem = null;
    public float rotationSpeed = 100f;
    public float pickupDistance = 4f;

    public ItemPickupState currentState = ItemPickupState.Idle;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple ItemPickup instances found! Destroying the new one.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        playerCamera = Camera.main.transform;
    }

    private void Update()
    {
        CheckPickedItem();
        HandleInput();
        CheckForItem();
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (pickedItem == null && currentState == ItemPickupState.Idle)
            {
                PickupItem();
            }
            else if (currentState == ItemPickupState.Equipped)
            {
                SwingItem();
                TryCastSpells();
            }
            else if (currentState == ItemPickupState.Holding)
            {
                DropItem();
            }

            DetectAndInteract();
        }

        if (pickedItem != null && Input.GetMouseButtonDown(1) && currentState != ItemPickupState.Swinging)
        {
            ToggleEquip();
        }

        if (pickedItem != null && currentState == ItemPickupState.Holding)
        {
            RotatePickedItem();
            LockItemRotation();
        }
    }

    private void TryCastSpells()
    {
        var enchant = pickedItem?.GetComponent<EnchantComponent>();
        if (enchant != null)
        {
            enchant.CastAllSpells(playerCamera.transform);
        }
    }

    private void CheckForItem()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance) && hit.transform.CompareTag("Items"))
        {
            ItemComponent itemComponent = hit.transform.GetComponent<ItemComponent>();

            if (itemComponent != null && itemComponent.itemName != null)
            {
                itemNameText.text = itemComponent.itemName;
                itemNameText.enabled = true;
            }
            else
            {
                itemNameText.enabled = false;
            }
        }
        else
        {
            itemNameText.enabled = false;
        }
    }

    public void CheckPickedItem()
    {
        if (pickedItem == null || playerCamera.transform.childCount == 0)
        {
            pickedItem = null;
            currentState = ItemPickupState.Idle;
        }
    }

    private void DetectAndInteract()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance))
        {
            InteractiveObject interactiveObject = hit.collider.GetComponent<InteractiveObject>();
            if (interactiveObject != null)
            {
                interactiveObject.Interaction();
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
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance) && hit.transform.CompareTag("Items"))
        {
            pickedItem = hit.transform.gameObject;
            pickedItem.transform.SetParent(playerCamera);
            pickedItem.transform.localPosition = new Vector3(0, 0, 1f);
            pickedItem.transform.localRotation = Quaternion.identity;

            if (pickedItem.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.isKinematic = true;
            }

            currentState = ItemPickupState.Holding;
            Debug.Log($"📦 아이템 획득: {pickedItem.name}");
        }
    }

    private void DropItem()
    {
        if (pickedItem != null)
        {
            pickedItem.transform.SetParent(null);

            if (pickedItem.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.isKinematic = false;
            }

            Debug.Log($"🧺 아이템 버림: {pickedItem.name}");
            pickedItem = null;
            currentState = ItemPickupState.Idle;
        }
    }

    private void RotatePickedItem()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            pickedItem.transform.Rotate(Vector3.up, scrollInput * rotationSpeed, Space.Self);
        }
    }

    private void LockItemRotation()
    {
        Vector3 eulerAngles = pickedItem.transform.rotation.eulerAngles;
        pickedItem.transform.rotation = Quaternion.Euler(0, eulerAngles.y, 0);
    }

    private void ToggleEquip()
    {
        ItemComponent pickedItemComp = pickedItem.GetComponent<ItemComponent>();
        if (currentState == ItemPickupState.Equipped)
        {
            pickedItem.transform.DOLocalMove(new Vector3(0, 0, 1f), 0.2f).SetEase(Ease.InOutQuad);
            pickedItem.transform.localRotation = Quaternion.identity;
            currentState = ItemPickupState.Holding;
            Debug.Log("🗡️ 아이템 해제됨");
        }
        else
        {
            if (pickedItemComp.itemType != ItemType.Weapon) return;
            pickedItem.transform.DOLocalMove(new Vector3(0.5f, -0.5f, 0.5f), 0.2f).SetEase(Ease.InOutQuad);
            pickedItem.transform.DOLocalRotate(new Vector3(-45, 0, 90), 0.2f);
            currentState = ItemPickupState.Equipped;
            Debug.Log("🗡️ 아이템 장착됨");
        }
    }

    private void SwingItem()
    {
        if (pickedItem == null || currentState == ItemPickupState.Swinging) return;

        currentState = ItemPickupState.Swinging;

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

        swingSequence.OnComplete(() =>
        {
            pickedItem.transform.localPosition = originalPosition;
            pickedItem.transform.localRotation = originalRotation;
            currentState = ItemPickupState.Equipped;
        });
    }
}

public enum ItemPickupState
{
    Idle,
    Holding,
    Equipped,
    Swinging
} 