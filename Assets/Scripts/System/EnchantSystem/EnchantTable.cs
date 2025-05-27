using UnityEngine;
using DG.Tweening;

public class EnchantTable : MonoBehaviour
{
    public Transform fixedPosition;
    public GameObject objectOnTable;
    public GameObject enchantUI;
    public Transform cameraEnchantViewPoint;
    public float cameraMoveDuration = 0.5f;

    private bool onEnchanting = false;

    private void Start(){
        enchantUI.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && onEnchanting)
        {
            CloseEnchantUI();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (objectOnTable != null) return;
        if (!other.CompareTag("Items")) return;

        var ctrl = ItemInteractionController.Instance;
        if (ctrl != null && other.transform.IsChildOf(ctrl.playerCamera))
            return;

        var item = other.GetComponent<ItemComponent>();
        if (item == null) return;

        // 마나가 1 이상 있는지 확인
        if (item.elementalMana == null || item.elementalMana.Total() <= 0)
        {
            Debug.Log("❌ 마나가 부족하여 마법 부여 테이블에 올릴 수 없습니다.");
            return;
        }

        // EnchantComponent 부여
        if (!other.TryGetComponent(out EnchantComponent enchant))
        {
            enchant = other.gameObject.AddComponent<EnchantComponent>();
        }

        objectOnTable = other.gameObject;

        var rb = other.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        other.transform.SetParent(fixedPosition);
    // 부드러운 이동 & 회전 (0.3초)
        other.transform.DOLocalMove(Vector3.zero, 0.3f).SetEase(Ease.OutSine);
        other.transform.DOLocalRotate(Vector3.zero, 0.3f).SetEase(Ease.OutSine);

        OpenEnchantUI();
    }

    private void OpenEnchantUI()
    {
        enchantUI.SetActive(true);
        onEnchanting = true;
        PlayerController.Instance.cam.MoveTo(cameraEnchantViewPoint, cameraMoveDuration);
    }

    private void CloseEnchantUI()
    {
        enchantUI.SetActive(false);
        objectOnTable = null;
        onEnchanting = false;
        PlayerController.Instance.cam.ResetToDefault(cameraMoveDuration);
    }
}