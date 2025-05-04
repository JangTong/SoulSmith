using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Forge : MonoBehaviour
{
    public Transform targetPosition;
    public GameObject itemPrefab;
    public List<GameObject> storedItems = new List<GameObject>();
    private ForgeFire forgeFire;
    private ParticleSystem sparkEffect;

    // 재사용 가능한 스탯 저장용 컴포넌트
    private ItemComponent statsComponent;
    private GameObject statsContainer;

    private void Start()
    {
        forgeFire = GetComponentInChildren<ForgeFire>();
        sparkEffect = GetComponentInChildren<ParticleSystem>();

        // statsComponent 초기화
        statsContainer = new GameObject("ForgeStatsHolder");
        statsContainer.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideAndDontSave;
        statsComponent = statsContainer.AddComponent<ItemComponent>();
    }

    private void OnDestroy()
    {
        if (statsContainer != null)
            Destroy(statsContainer);
    }

    public void StartForging()
    {
        StartCoroutine(Forging());
    }

    public IEnumerator Forging()
    {
        if (storedItems.Count == 0)
        {
            Debug.LogWarning("저장된 아이템이 없습니다!");
            yield break;
        }

        List<ItemComponent> components = GetItemComponents(storedItems);

        if (!ValidateForgeCondition(components))
        {
            Debug.LogWarning("Forging 실패: Metal과 Fuel의 비율이 1:1이 아닙니다.");
            yield break;
        }

        if (forgeFire != null)
            forgeFire.OnFire = true;

        // 스탯 초기화 및 합산
        ResetStatsComponent();
        Color totalColor = Color.black;
        foreach (var comp in components)
        {
            statsComponent.AddStatsFrom(comp);
            // 동적 희귀도 계산
            if (comp.itemRarity > statsComponent.itemRarity)
                statsComponent.itemRarity = comp.itemRarity;
            if (comp.materialType == MaterialType.Metal)
                totalColor = AdditiveBlend(totalColor, comp.itemColor);
        }

        Debug.Log("아이템을 처리 중입니다... 3초 후 새로운 아이템이 생성됩니다.");
        yield return new WaitForSeconds(3f);

        // 새로운 아이템 생성 및 설정
        GameObject newItem = Instantiate(itemPrefab, transform.position, Quaternion.identity);
        ItemComponent newItemComponent = newItem.GetComponent<ItemComponent>();
        Rigidbody newItemRb = newItem.GetComponent<Rigidbody>();

        if (newItemComponent != null)
        {
            ApplyCombinedStats(newItemComponent, statsComponent, totalColor);
            // 재료 등록
            foreach (var comp in components)
            {
                newItemComponent.AddMaterial(comp);
                newItemComponent.AddMaterialsFrom(comp);
            }

            newItemRb.isKinematic = false;
            newItem.transform.SetParent(targetPosition);
            newItem.transform.localPosition = Vector3.zero;

            Debug.Log($"새로운 아이템 생성: {newItemComponent}");
        }
        else
        {
            Debug.LogError("새로운 아이템에 ItemComponent가 없습니다!");
        }

        // 원재료 파괴 및 리스트 정리
        foreach (var item in storedItems)
            Destroy(item);
        storedItems.Clear();

        if (forgeFire != null)
            forgeFire.OnFire = false;
        sparkEffect.Play();
    }

    private void ResetStatsComponent()
    {
        statsComponent.weight = 0f;
        statsComponent.atkPower = 0f;
        statsComponent.defPower = 0f;
        statsComponent.buyPrice = 0;
        statsComponent.sellPrice = 0;
        statsComponent.elementalMana = new ElementalMana();
        statsComponent.elementalResistance = new ElementalResistance();
        statsComponent.elementalAffinity = new ElementalAffinity();
        statsComponent.itemRarity = Rarity.None;
    }

    private List<ItemComponent> GetItemComponents(List<GameObject> items)
    {
        List<ItemComponent> list = new List<ItemComponent>();
        foreach (var go in items)
        {
            ItemComponent comp = go.GetComponent<ItemComponent>();
            if (comp != null)
                list.Add(comp);
        }
        return list;
    }

    private bool ValidateForgeCondition(List<ItemComponent> items)
    {
        int metalCount = 0, fuelCount = 0;
        foreach (var item in items)
        {
            if (item.materialType == MaterialType.Metal)
                metalCount++;
            else if (item.materialType == MaterialType.Fuel)
                fuelCount++;
        }
        return metalCount == fuelCount;
    }

    private void ApplyCombinedStats(ItemComponent target, ItemComponent sourceStats, Color color)
    {
        target.weight = sourceStats.weight;
        target.atkPower = sourceStats.atkPower;
        target.defPower = sourceStats.defPower;
        target.buyPrice = sourceStats.buyPrice;
        target.sellPrice = sourceStats.sellPrice;

        target.elementalMana = sourceStats.elementalMana;
        target.elementalResistance = sourceStats.elementalResistance;
        target.elementalAffinity = sourceStats.elementalAffinity;

        target.materialType = MaterialType.Metal;
        target.itemRarity = sourceStats.itemRarity;
        target.itemColor = color;
    }

    private Color AdditiveBlend(Color c1, Color c2)
    {
        return new Color(
            Mathf.Min(c1.r + c2.r, 1f),
            Mathf.Min(c1.g + c2.g, 1f),
            Mathf.Min(c1.b + c2.b, 1f),
            Mathf.Min(c1.a + c2.a, 1f)
        );
    }

    public List<GameObject> GetStoredItems()
    {
        return storedItems;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Items") && ItemPickup.Instance.currentState == ItemPickupState.Idle)
        {
            if (!storedItems.Contains(other.gameObject))
            {
                storedItems.Add(other.gameObject);
                Debug.Log($"{other.name}이(가) Forge에 추가되었습니다.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Items") && storedItems.Contains(other.gameObject))
        {
            storedItems.Remove(other.gameObject);
            Debug.Log($"{other.name}이(가) Forge에서 제거되었습니다.");
        }
    }
}
