// 파일명: ItemInteractionDetector.cs
using UnityEngine;

public class ItemInteractionDetector : MonoBehaviour
{
    private const string LOG_PREFIX = "[ItemInteractionDetector]";
    public static ItemInteractionDetector Instance { get; private set; }

    [Header("Settings")]
    public float detectDistance = 4f;

    private Transform playerCamera;
    private bool wasShowingItem = false;  // 이전 UI 표시 상태

    void Awake()
    {
        Debug.Log($"{LOG_PREFIX} Awake: initializing instance");
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"{LOG_PREFIX} Duplicate instance, destroying this");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log($"{LOG_PREFIX} Awake: Instance assigned");
    }

    void Start()
    {
        Debug.Log($"{LOG_PREFIX} Start: locating main camera");
        playerCamera = Camera.main != null ? Camera.main.transform : null;
        if (playerCamera != null)
            Debug.Log($"{LOG_PREFIX} Start: playerCamera set to {playerCamera.name}");
        else
            Debug.LogWarning($"{LOG_PREFIX} Start: playerCamera is null");
    }

    void Update()
    {
        UpdateDetectionUI();
    }

    private void UpdateDetectionUI()
    {
        // ① 현재 프레임 감지 상태 계산
        bool willShow = false;
        RaycastHit hit;
        ItemComponent comp = null;
        if (TryPerformRaycast(out hit) && hit.transform.CompareTag("Items"))
        {
            comp = hit.transform.GetComponent<ItemComponent>();
            willShow = comp != null && !string.IsNullOrEmpty(comp.itemName);
        }
        
        // ② 상태 변화 시에만 로그
        if (willShow != wasShowingItem)
        {
            if (willShow)
                Debug.Log($"{LOG_PREFIX} UpdateDetectionUI: hit {hit.transform.name}, showing item {comp.itemName}");
            else
                Debug.Log($"{LOG_PREFIX} UpdateDetectionUI: hiding item UI");
            wasShowingItem = willShow;
        }

        // ③ UI 토글 (기존 로직)
        if (willShow)
            UIManager.Instance.ShowItemName(comp.itemName);
        else
            UIManager.Instance.HideItemName();
    }

    public bool TryPerformRaycast(out RaycastHit hit)
    {
        var origin = playerCamera != null ? playerCamera.position : Vector3.zero;
        var direction = playerCamera != null ? playerCamera.forward : Vector3.forward;
        //Debug.Log($"{LOG_PREFIX} TryPerformRaycast: origin={origin}, direction={direction}, distance={detectDistance}");
        Ray ray = new Ray(origin, direction);
        if (Physics.Raycast(ray, out hit, detectDistance))
        {
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
        Debug.Log($"{LOG_PREFIX} TryInteract: called");
        if (TryPerformRaycast(out RaycastHit hit))
        {
            var inter = hit.collider.GetComponent<IInteractable>();
            if (inter != null)
            {
                Debug.Log($"{LOG_PREFIX} TryInteract: invoking Interact() on {hit.collider.name}");
                inter.Interact();
            }
            else
            {
                Debug.LogWarning($"{LOG_PREFIX} TryInteract: IInteractable not found on {hit.collider.name}");
            }
        }
        else
        {
            Debug.Log($"{LOG_PREFIX} TryInteract: nothing to interact with");
        }
    }
}
