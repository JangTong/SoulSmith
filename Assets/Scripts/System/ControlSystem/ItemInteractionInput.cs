using UnityEngine;

[RequireComponent(typeof(ItemInteractionController))]
public class ItemInteractionInput : MonoBehaviour
{
    public static ItemInteractionInput Instance { get; private set; }

    private const string LOG_PREFIX = "[ItemInteractionInput]";
    private ItemInteractionController controller;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log($"{LOG_PREFIX} Awake: Instance assigned");
        }
        else
        {
            Debug.Log($"{LOG_PREFIX} Awake: Duplicate instance destroyed");
            Destroy(gameObject);
            return;
        }

        controller = GetComponent<ItemInteractionController>();
        Debug.Log($"{LOG_PREFIX} Awake: Controller reference set");
    }

    private void Update()
    {
        bool uiActive = PlayerController.Instance != null && PlayerController.Instance.IsUIActive();
        Debug.Log($"{LOG_PREFIX} Update: UIActive={uiActive}");
        if (uiActive)
        {
            Debug.Log($"{LOG_PREFIX} Update: Skipping input due to UI");
            return;
        }

        // 왼손 장착 토글 (Q 키)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log($"{LOG_PREFIX} Update: Q pressed -> ToggleSecondary Left");
            controller.OnSecondaryAction(ItemInteractionController.Hand.Left);
        }
        // 주 행동 (좌클릭)
        else if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"{LOG_PREFIX} Update: Fire1 pressed -> PrimaryAction");
            controller.OnPrimaryAction();
        }
        // 오른손 장착 토글 (우클릭)
        else if (Input.GetMouseButtonDown(1))
        {
            Debug.Log($"{LOG_PREFIX} Update: Fire2 pressed -> ToggleSecondary Right");
            controller.OnSecondaryAction(ItemInteractionController.Hand.Right);
        }

        // 마우스 휠로 들고 있는 아이템 회전
        if (controller.currentState == ItemInteractionController.State.Holding)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0f)
            {
                Debug.Log($"{LOG_PREFIX} Update: ScrollWheel={scroll} -> RotateHeldItem");
                controller.RotateHeldItem(scroll);
            }
        }
    }
}
