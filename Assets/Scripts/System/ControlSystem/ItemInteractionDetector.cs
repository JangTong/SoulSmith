// 파일명: ItemInteractionDetector.cs
using UnityEngine;
using TMPro;

public class ItemInteractionDetector : MonoBehaviour
{
    public static ItemInteractionDetector Instance { get; private set; }

    [Header("Settings")]
    public float detectDistance = 4f;

    [Header("UI")]
    public TextMeshProUGUI itemNameText;

    private Transform playerCamera;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[ItemID] 중복 인스턴스 감지, 새로운 인스턴스 파괴");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        playerCamera = Camera.main.transform;
    }

    void Update()
    {
        UpdateDetectionUI();
    }

    private void UpdateDetectionUI()
    {
        if (TryPerformRaycast(out var hit) && hit.transform.CompareTag("Items"))
        {
            var comp = hit.transform.GetComponent<ItemComponent>();
            if (comp != null && !string.IsNullOrEmpty(comp.itemName))
            {
                itemNameText.text = comp.itemName;
                itemNameText.enabled = true;
                return;
            }
        }
        itemNameText.enabled = false;
    }

    public bool TryPerformRaycast(out RaycastHit hit)
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out hit, detectDistance))
        {
            // 손에 들었거나 장착된 아이템은 제외
            if (hit.transform.IsChildOf(playerCamera))
            {
                hit = default;
                return false;
            }
            return true;
        }
        hit = default;
        return false;
    }

    public void TryInteract()
    {
        if (TryPerformRaycast(out var hit))
        {
            var inter = hit.collider.GetComponent<IInteractable>();
            if (inter != null)
            {
                Debug.Log($"[ItemID] Interacting with '{hit.collider.name}'");
                inter.Interact();
            }
            else
            {
                Debug.Log("[ItemID] IInteractable component not found");
            }
        }
        else
        {
            Debug.Log("[ItemID] Raycast hit nothing or hit a held item");
        }
    }
}