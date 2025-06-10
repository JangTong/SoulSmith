using UnityEngine;
using DG.Tweening;
using System;

public class EnchantTable : MonoBehaviour
{
    private const string LOG_PREFIX = "[EnchantTable]";
    
    [Header("Table Setup")]
    public Transform fixedPosition;
    public GameObject enchantUI;
    public Transform cameraEnchantViewPoint;
    
    [Header("Animation Settings")]
    [SerializeField] private float itemMoveDuration = 0.3f;
    [SerializeField] private float cameraMoveDuration = 0.5f;
    [SerializeField] private Ease itemMoveEase = Ease.OutSine;

    [Header("Input Settings")]
    [SerializeField] private KeyCode closeUIKey = KeyCode.Space;

    // 런타임 상태
    public GameObject objectOnTable { get; private set; }
    private bool isEnchanting = false;

    // 이벤트
    public static event Action OnWeaponPlaced;

    // 캐싱된 참조
    private PlayerController playerController;
    private ItemInteractionController itemController;

    private void Start()
    {
        if (enchantUI != null) 
            enchantUI.SetActive(false);
            
        // Start에서 Instance 가져오기 (Awake보다 늦게 실행됨)
        playerController = PlayerController.Instance;
        itemController = ItemInteractionController.Instance;
        
        Debug.Log($"{LOG_PREFIX} Start: PlayerController = {playerController?.name}, ItemController = {itemController?.name}");
    }

    private void Update()
    {
        // 인첸트 UI가 열려있을 때만 입력 처리
        if (Input.GetKeyDown(closeUIKey) && isEnchanting)
        {
            CloseEnchantUI();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 이미 아이템이 올려져 있으면 무시
        if (objectOnTable != null) return;
        
        // Items 태그가 아니면 무시
        if (!other.CompareTag("Items")) return;

        // 플레이어가 들고 있는 아이템이면 무시
        if (itemController != null && other.transform.IsChildOf(itemController.playerCamera))
            return;

        // ItemComponent 확인
        var item = other.GetComponent<ItemComponent>();
        if (item == null)
        {
            Debug.LogWarning($"{LOG_PREFIX} Object {other.name} has no ItemComponent");
            return;
        }

        // 마나 확인
        if (!HasValidMana(item))
        {
            Debug.Log($"{LOG_PREFIX} Item {other.name} has insufficient mana for enchanting");
            return;
        }

        PlaceItemOnTable(other.gameObject);
    }

    /// <summary>
    /// 아이템이 유효한 마나를 가지고 있는지 확인
    /// </summary>
    private bool HasValidMana(ItemComponent item)
    {
        return item.elementalMana != null && item.elementalMana.Total() > 0;
    }

    /// <summary>
    /// 아이템을 테이블에 배치
    /// </summary>
    private void PlaceItemOnTable(GameObject item)
    {
        // EnchantComponent 추가 (없으면)
        if (!item.TryGetComponent(out EnchantComponent enchant))
        {
            enchant = item.AddComponent<EnchantComponent>();
        }

        objectOnTable = item;

        // 물리 상태 변경
        var rigidbody = item.GetComponent<Rigidbody>();
        if (rigidbody != null) 
            rigidbody.isKinematic = true;

        // 테이블 위치로 이동
        item.transform.SetParent(fixedPosition);
        item.transform.DOLocalMove(Vector3.zero, itemMoveDuration).SetEase(itemMoveEase);
        item.transform.DOLocalRotate(Vector3.zero, itemMoveDuration).SetEase(itemMoveEase);

        OpenEnchantUI();
        
        // 무기 배치 이벤트 발생
        Debug.Log($"{LOG_PREFIX} Item placed: {item.name}");
        OnWeaponPlaced?.Invoke();
    }

    /// <summary>
    /// 인챈트 UI 열기
    /// </summary>
    private void OpenEnchantUI()
    {
        Debug.Log($"{LOG_PREFIX} OpenEnchantUI called");
        
        if (enchantUI != null)
            enchantUI.SetActive(true);
            
        isEnchanting = true;
        
        if (playerController != null && cameraEnchantViewPoint != null)
        {
            Debug.Log($"{LOG_PREFIX} Moving camera to enchant view point");
            playerController.cam.MoveTo(cameraEnchantViewPoint, cameraMoveDuration);
        }
        else
        {
            Debug.LogError($"{LOG_PREFIX} Cannot move camera - PlayerController: {playerController}, cameraEnchantViewPoint: {cameraEnchantViewPoint}");
        }
    }

    /// <summary>
    /// 인챈트 UI 닫기
    /// </summary>
    private void CloseEnchantUI()
    {
        Debug.Log($"{LOG_PREFIX} CloseEnchantUI called");
        
        if (enchantUI != null)
            enchantUI.SetActive(false);
            
        objectOnTable = null;
        isEnchanting = false;
        
        if (playerController != null)
        {
            Debug.Log($"{LOG_PREFIX} Resetting camera to default");
            playerController.cam.ResetToDefault(cameraMoveDuration);
        }
        else
        {
            Debug.LogError($"{LOG_PREFIX} Cannot reset camera - PlayerController is null");
        }
        
        Debug.Log($"{LOG_PREFIX} Enchant UI closed");
    }
}