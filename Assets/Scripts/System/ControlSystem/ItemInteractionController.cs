using System;
using UnityEngine;
using DG.Tweening;

public class ItemInteractionController : MonoBehaviour
{
    public static ItemInteractionController Instance { get; private set; }

    [Header("References")]
    public Transform playerCamera;

    [Header("Settings")]
    public float pickupDistance = 4f;
    public float rotationSpeed = 100f;

    public enum State { Idle, Holding, Equipped, Swinging }
    public State currentState = State.Idle;

    public enum Hand { Right, Left }
    public GameObject heldItemRight, heldItemLeft;

    // 애니메이션 중 중복 입력 방지 플래그
    private bool isAnimating = false;

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
        if (isAnimating) return;

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
        if (isAnimating) return;

        if (hand == Hand.Left) ToggleLeftEquip();
        else ToggleRightEquip();
    }

    public void RotateHeldItem(float scrollDelta)
    {
        if (currentState != State.Holding || heldItemRight == null || Mathf.Abs(scrollDelta) < 0.01f || isAnimating)
            return;

        var t = heldItemRight.transform;
        if (DOTween.IsTweening(t)) DOTween.Kill(t);

        float targetY = t.rotation.eulerAngles.y + scrollDelta * rotationSpeed;
        t.DORotate(new Vector3(0, targetY, 0), 0.05f).SetEase(Ease.OutSine);
    }

    private void LateUpdate()
    {
        if (currentState == State.Holding && heldItemRight != null && !isAnimating)
            LockHeldItemRotation();
    }

    #region Core Logic

    private void TryPrimaryPickup()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance)
            && hit.transform.CompareTag("Items"))
        {
            PickupItem(hit.transform.gameObject);
        }
    }

    private void PickupItem(GameObject item)
    {
        if (DOTween.IsTweening(item.transform))
            DOTween.Kill(item.transform);

        heldItemRight = item;
        item.transform.SetParent(playerCamera);

        isAnimating = true;
        var seq = DOTween.Sequence();
        seq.Append(item.transform.DOLocalMove(new Vector3(0, 0, 1f), 0.3f).SetEase(Ease.OutBack));
        seq.Join(item.transform.DOLocalRotate(Vector3.zero, 0.3f).SetEase(Ease.OutSine));
        seq.OnComplete(() => { isAnimating = false; });

        if (item.TryGetComponent<Rigidbody>(out var rb))
            rb.isKinematic = true;

        SetState(State.Holding);
        Debug.Log($"📦 Picked up: {item.name}");
    }

    private void DropHeldItem(Hand hand)
    {
        var item = (hand == Hand.Left) ? heldItemLeft : heldItemRight;
        if (item == null) return;

        if (DOTween.IsTweening(item.transform))
            DOTween.Kill(item.transform);

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
        if (heldItemRight == null) return;

        var t = heldItemRight.transform;
        if (DOTween.IsTweening(t)) DOTween.Kill(t);

        isAnimating = true;
        var seq = DOTween.Sequence();

        var comp = heldItemRight.GetComponent<ItemComponent>();
        if (currentState == State.Equipped)
        {
            seq.Append(t.DOLocalMove(new Vector3(0, 0, 1f), 0.2f).SetEase(Ease.InOutQuad));
            seq.OnComplete(() =>
            {
                t.localRotation = Quaternion.identity;
                SetState(State.Holding);
                isAnimating = false;
                Debug.Log("🗡️ Right hand unequipped");
            });
        }
        else if (comp != null && comp.itemType == ItemType.Weapon)
        {
            seq.Append(t.DOLocalMove(new Vector3(0.5f, -0.5f, 0.5f), 0.2f).SetEase(Ease.InOutQuad));
            seq.Join(t.DOLocalRotate(new Vector3(-45, 0, 90), 0.2f).SetEase(Ease.InOutQuad));
            seq.OnComplete(() =>
            {
                SetState(State.Equipped);
                isAnimating = false;
                Debug.Log("🗡️ Right hand equipped");
            });
        }
        else
        {
            isAnimating = false;
        }
    }

    private void ToggleLeftEquip()
    {
        // 왼손 장착: 모든 Items 태그 객체 허용
        if (heldItemLeft == null)
        {
            Ray ray = new Ray(playerCamera.position, playerCamera.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance)
                && hit.transform.CompareTag("Items")
                && !hit.transform.IsChildOf(playerCamera))
            {
                GameObject item = hit.transform.gameObject;

                heldItemLeft = item;
                var t = heldItemLeft.transform;
                if (DOTween.IsTweening(t)) DOTween.Kill(t);

                t.SetParent(playerCamera);
                isAnimating = true;
                var seq = DOTween.Sequence();
                seq.Append(t.DOLocalMove(new Vector3(-0.5f, -0.5f, 0.5f), 0.3f).SetEase(Ease.InOutQuad));
                seq.Join(t.DOLocalRotate(new Vector3(-45, 0, -90), 0.3f).SetEase(Ease.InOutQuad));
                seq.OnComplete(() =>
                {
                    if (heldItemLeft.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
                    isAnimating = false;
                    Debug.Log("🪔 Left hand item equipped");
                });
            }
        }
        else
        {
            var t = heldItemLeft.transform;
            if (DOTween.IsTweening(t)) DOTween.Kill(t);

            isAnimating = true;
            var seq = DOTween.Sequence();
            seq.Append(t.DOLocalMove(new Vector3(0, 0, 1f), 0.3f).SetEase(Ease.InOutQuad));
            seq.OnComplete(() =>
            {
                t.SetParent(null);
                if (heldItemLeft.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = false;
                Debug.Log("🪔 Left hand item dropped");
                heldItemLeft = null;
                isAnimating = false;
            });
        }
    }

    private void SwingItem()
    {
        if (isAnimating || heldItemRight == null || currentState == State.Swinging) return;

        var t = heldItemRight.transform;
        if (DOTween.IsTweening(t)) DOTween.Kill(t);

        isAnimating = true;
        currentState = State.Swinging;

        Vector3 originalPos = t.localPosition;
        Quaternion originalRot = t.localRotation;

        Vector3 startRot = new Vector3(-45, 0, 90);
        Vector3 midRot = new Vector3(-100, 0, 90);
        Vector3 endRot = new Vector3(-20, 0, 90);

        Vector3 midPos = new Vector3(0.5f, 0.5f, 0.5f);
        Vector3 endPos = new Vector3(0, 0f, 1f);

        var seq = DOTween.Sequence();
        seq.Append(t.DOLocalRotate(midRot, 0.33f).SetEase(Ease.InOutQuad));
        seq.Join(t.DOLocalMove(midPos, 0.33f).SetEase(Ease.InOutQuad));
        seq.Append(t.DOLocalRotate(endRot, 0.07f).SetEase(Ease.OutCubic));
        seq.Join(t.DOLocalMove(endPos, 0.07f).SetEase(Ease.OutCubic));
        seq.AppendCallback(() =>
        {
            var tool = heldItemRight.GetComponent<Tool>();
            tool?.Use();
        });
        seq.Append(t.DOLocalRotate(startRot, 0.2f).SetEase(Ease.InCubic));
        seq.Join(t.DOLocalMove(originalPos, 0.2f).SetEase(Ease.InCubic));
        seq.OnComplete(() =>
        {
            t.localPosition = originalPos;
            t.localRotation = originalRot;
            SetState(State.Equipped);
            isAnimating = false;
        });
    }

    private void TryCastSpells()
    {
        if (heldItemRight == null) return;
        var enchant = heldItemRight.GetComponent<EnchantComponent>();
        enchant?.CastAllSpells(playerCamera);
        Debug.Log("✨ CastAllSpells called");
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
