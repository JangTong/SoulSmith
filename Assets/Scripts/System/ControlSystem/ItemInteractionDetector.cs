// 파일명: ItemInteractionDetector.cs
using UnityEngine;

public class ItemInteractionDetector : MonoBehaviour
{
    private const string LOG_PREFIX = "[ItemInteractionDetector]";
    public static ItemInteractionDetector Instance { get; private set; }

    [Header("Settings")]
    public float detectDistance = 4f;

    private Transform playerCamera;
    

    void Awake()
    {
        Debug.Log($"{LOG_PREFIX} Awake: initializing instance");
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"{LOG_PREFIX} Duplicate instance detected, destroying this");
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
        Debug.Log($"{LOG_PREFIX} Update: calling UpdateDetectionUI");
        UpdateDetectionUI();
    }

    private void UpdateDetectionUI()
    {
        Debug.Log($"{LOG_PREFIX} UpdateDetectionUI: attempting raycast");
        if (TryPerformRaycast(out RaycastHit hit) && hit.transform.CompareTag("Items"))
        {
            Debug.Log($"{LOG_PREFIX} UpdateDetectionUI: hit {hit.transform.name}");
            var comp = hit.transform.GetComponent<ItemComponent>();
            if (comp != null && !string.IsNullOrEmpty(comp.itemName))
            {
                Debug.Log($"{LOG_PREFIX} UpdateDetectionUI: showing item name {comp.itemName}");
                UIManager.Instance.ShowItemName(comp.itemName);
                return;
            }
            Debug.Log($"{LOG_PREFIX} UpdateDetectionUI: no valid ItemComponent on {hit.transform.name}");
        }
        Debug.Log($"{LOG_PREFIX} UpdateDetectionUI: hiding item name UI");
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
                Debug.Log($"{LOG_PREFIX} TryPerformRaycast: hit held child {hit.transform.name}, ignoring");
                hit = default;
                return false;
            }
            Debug.Log($"{LOG_PREFIX} TryPerformRaycast: hit valid object {hit.transform.name}");
            return true;
        }
        Debug.Log($"{LOG_PREFIX} TryPerformRaycast: no hit");
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
