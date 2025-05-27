// 파일명: ItemInteractionController.cs
using System;
using UnityEngine;
using DG.Tweening;

public class ItemInteractionController : MonoBehaviour
{
    public static ItemInteractionController Instance { get; private set; }

    [Header("References")]
    public Transform playerCamera;               // 플레이어 시점 카메라

    [Header("Settings")]
    public float pickupDistance = 4f;            // 탐지 거리
    public float rotationSpeed = 100f;           // 휠 회전 속도

    public enum State { Idle, Holding, Equipped, Swinging }
    public State currentState = State.Idle;

    public enum Hand { Right, Left }
    public GameObject heldItemRight, heldItemLeft;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple ItemInteractionController instances! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;
    }

    public void OnPrimaryAction()
    {
        if (heldItemRight == null && currentState == State.Idle)
            TryPrimaryPickup();
        else if (currentState == State.Equipped)
        {
            SwingItem();
            TryCastSpells();
        }
        else if (currentState == State.Holding)
            DropHeldItem(Hand.Right);

        ItemInteractionDetector.Instance.TryInteract();
    }

    public void OnSecondaryAction(Hand hand)
    {
        if (hand == Hand.Left) ToggleLeftEquip();
        else ToggleRightEquip();
    }

    public void RotateHeldItem(float scrollDelta)
    {
        if (currentState != State.Holding || heldItemRight == null || Mathf.Abs(scrollDelta) < 0.01f)
            return;

        float targetY = heldItemRight.transform.rotation.eulerAngles.y + scrollDelta * rotationSpeed;
        heldItemRight.transform
            .DORotate(new Vector3(0, targetY, 0), 0.05f).SetEase(Ease.OutSine);

        LockHeldItemRotation();
    }

    private void LateUpdate()
    {
        if (currentState == State.Holding && heldItemRight != null)
            LockHeldItemRotation();
    }

    #region Core Logic

    private void TryPrimaryPickup()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        // 단일 Raycast: 맨 앞 콜라이더 하나만 검사
        if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance)
            && hit.transform.CompareTag("Items"))
        {
            // hit.transform으로 부모 오브젝트(GameObject) 획득
            PickupItem(hit.transform.gameObject);
        }
    }

    private void PickupItem(GameObject item)
    {
        heldItemRight = item;
        item.transform.SetParent(playerCamera);
        if (item.TryGetComponent<Rigidbody>(out var rb))
            rb.isKinematic = true;

        item.transform
            .DOLocalMove(new Vector3(0, 0, 1f), 0.3f).SetEase(Ease.OutBack);
        item.transform
            .DOLocalRotate(Vector3.zero, 0.3f).SetEase(Ease.OutSine);

        SetState(State.Holding);
        Debug.Log($"📦 Picked up: {item.name}");
    }

    private void DropHeldItem(Hand hand)
    {
        var item = (hand == Hand.Left) ? heldItemLeft : heldItemRight;
        if (item == null) return;

        item.transform.SetParent(null);
        if (item.TryGetComponent<Rigidbody>(out var rb))
            rb.isKinematic = false;

        if (hand == Hand.Right) heldItemRight = null;
        else heldItemLeft = null;

        SetState(State.Idle);
        Debug.Log($"🧺 Dropped: {item.name}");
    }

    private void ToggleRightEquip()
    {
        if (heldItemRight == null)
        {
            Debug.Log("[Equip] No item in right hand");
            return;
        }

        var comp = heldItemRight.GetComponent<ItemComponent>();
        var t = heldItemRight.transform;
        if (currentState == State.Equipped)
        {
            t.DOLocalMove(new Vector3(0, 0, 1f), 0.2f).SetEase(Ease.InOutQuad);
            t.localRotation = Quaternion.identity;
            SetState(State.Holding);
            Debug.Log("🗡️ Right hand unequipped");
        }
        else if (comp != null && comp.itemType == ItemType.Weapon)
        {
            t.DOLocalMove(new Vector3(0.5f, -0.5f, 0.5f), 0.2f).SetEase(Ease.InOutQuad);
            t.DOLocalRotate(new Vector3(-45, 0, 90), 0.2f).SetEase(Ease.InOutQuad);
            SetState(State.Equipped);
            Debug.Log("🗡️ Right hand equipped");
        }
    }

    private void ToggleLeftEquip()
    {
        if (heldItemLeft == null)
        {
            Ray ray = new Ray(playerCamera.position, playerCamera.forward);
            if (Physics.Raycast(ray, out var hit, pickupDistance))
            {
                var comp = hit.collider.GetComponentInParent<ItemComponent>();
                if (comp == null) return;
                if (comp.gameObject == heldItemRight) return;
                if (!comp.CompareTag("Items")) return;

                heldItemLeft = comp.gameObject;
                var t = heldItemLeft.transform;
                t.SetParent(playerCamera);
                t.DOLocalMove(new Vector3(-0.5f, -0.5f, 0.5f), 0.3f).SetEase(Ease.InOutQuad);
                t.DOLocalRotate(new Vector3(-45, 0, -90), 0.3f).SetEase(Ease.InOutQuad);
                if (heldItemLeft.TryGetComponent<Rigidbody>(out var rb))
                    rb.isKinematic = true;
                Debug.Log("🪔 Left hand item equipped");
            }
        }
        else
        {
            var t = heldItemLeft.transform;
            t.DOLocalMove(new Vector3(0, 0, 1f), 0.3f).SetEase(Ease.InOutQuad)
             .OnComplete(() =>
             {
                 t.SetParent(null);
                 if (heldItemLeft.TryGetComponent<Rigidbody>(out var rb))
                     rb.isKinematic = false;
                 Debug.Log("🪔 Left hand item dropped");
                 heldItemLeft = null;
             });
        }
    }

    private void SwingItem()
    {
        if (heldItemRight == null || currentState == State.Swinging) return;

        currentState = State.Swinging;
        var t = heldItemRight.transform;
        Vector3 originalPosition = t.localPosition;
        Quaternion originalRotation = t.localRotation;

        Vector3 startRotation = new Vector3(-45, 0, 90);
        Vector3 middleRotation = new Vector3(-100, 0, 90);
        Vector3 endRotation = new Vector3(-20, 0, 90);

        Vector3 middlePosition = new Vector3(0.5f, 0.5f, 0.5f);
        Vector3 endPosition = new Vector3(0, 0f, 1f);

        Sequence swingSequence = DOTween.Sequence();
        swingSequence.Append(t.DOLocalRotate(middleRotation, 0.33f).SetEase(Ease.InOutQuad));
        swingSequence.Join(t.DOLocalMove(middlePosition, 0.33f).SetEase(Ease.InOutQuad));
        swingSequence.Append(t.DOLocalRotate(endRotation, 0.07f).SetEase(Ease.OutCubic));
        swingSequence.Join(t.DOLocalMove(endPosition, 0.07f).SetEase(Ease.OutCubic));
        swingSequence.AppendCallback(() =>
        {
            var tool = heldItemRight.GetComponent<Tool>();
            if (tool != null) tool.Use();
        });
        swingSequence.Append(t.DOLocalRotate(startRotation, 0.2f).SetEase(Ease.InCubic));
        swingSequence.Join(t.DOLocalMove(originalPosition, 0.2f).SetEase(Ease.InCubic));
        swingSequence.OnComplete(() =>
        {
            t.localPosition = originalPosition;
            t.localRotation = originalRotation;
            SetState(State.Equipped);
        });
    }

    private void TryCastSpells()
    {
        if (heldItemRight == null) return;
        var enchant = heldItemRight.GetComponent<EnchantComponent>();
        if (enchant != null)
        {
            enchant.CastAllSpells(playerCamera);
            Debug.Log("✨ CastAllSpells called");
        }
    }
    #endregion

    #region Helpers
    private void LockHeldItemRotation()
    {
        if (heldItemRight == null) return;
        float y = heldItemRight.transform.rotation.eulerAngles.y;
        heldItemRight.transform.DORotate(new Vector3(0, y, 0), 0.15f).SetEase(Ease.OutQuad);
    }

    private void SetState(State newState)
    {
        Debug.Log($"[State] {currentState} → {newState}");
        currentState = newState;
    }
    #endregion
}
