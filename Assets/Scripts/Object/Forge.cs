using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Forge : MonoBehaviour
{
    public Transform targetPosition;
    public GameObject itemPrefab;
    public List<GameObject> storedItems = new List<GameObject>();

    private bool isForging = false;
    private ForgeFire forgeFire;
    private ParticleSystem sparkEffect;

    // 재사용 가능한 스탯 저장용 컴포넌트
    private ItemComponent statsComponent;
    private GameObject statsContainer;

    private void Start()
    {
        // ForgeFire, 파티클 참조
        forgeFire = GetComponentInChildren<ForgeFire>();
        sparkEffect = GetComponentInChildren<ParticleSystem>();

        // 임시 스탯 컨테이너 생성
        statsContainer = new GameObject("ForgeStatsHolder");
        statsContainer.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideAndDontSave;
        statsComponent = statsContainer.AddComponent<ItemComponent>();
    }

    private void OnDestroy()
    {
        if (statsContainer != null)
            Destroy(statsContainer);
    }

    // 외부(Bellows)에서 호출
    public void StartForging()
    {
        if (isForging)
        {
            Debug.LogWarning("[Forge] 이미 가동 중입니다! 추가 입력은 무시됩니다.");
            return;
        }
        StartCoroutine(Forging());
    }

    public IEnumerator Forging()
    {
        isForging = true;

        // 1) 저장된 아이템이 아예 없으면 중단
        if (storedItems.Count == 0)
        {
            Debug.LogWarning("[Forge] 저장된 Metal 또는 Fuel 아이템이 없습니다!");
            isForging = false;
            yield break;
        }

        // 2) 실제 컴포넌트만 추출
        List<ItemComponent> components = GetItemComponents(storedItems);

        // 3) Metal/Fuel 개수 집계 & 로그
        int metalCount = 0, fuelCount = 0;
        foreach (var comp in components)
        {
            if (comp.materialType == MaterialType.Metal) metalCount++;
            else if (comp.materialType == MaterialType.Fuel) fuelCount++;
        }
        Debug.Log($"[Forge] MetalCount={metalCount}, FuelCount={fuelCount}");

        // 4) 최소 1개 이상 & 1:1 비율 검사
        if (metalCount == 0 || fuelCount == 0)
        {
            Debug.LogWarning($"[Forge] 저장된 아이템 부족: Metal={metalCount}, Fuel={fuelCount} (최소 1개씩 필요)");
            isForging = false;
            yield break;
        }
        if (metalCount != fuelCount)
        {
            Debug.LogWarning($"[Forge] Forging 실패: Metal({metalCount})과 Fuel({fuelCount}) 비율이 1:1이 아닙니다.");
            isForging = false;
            yield break;
        }

        // 5) 불꽃 점화
        if (forgeFire != null)
            forgeFire.OnFire = true;

        // 6) 스탯 초기화 및 합산
        ResetStatsComponent();
        Color totalColor = Color.black;
        foreach (var comp in components)
        {
            statsComponent.AddStatsFrom(comp);
            if (comp.itemRarity > statsComponent.itemRarity)
                statsComponent.itemRarity = comp.itemRarity;
            if (comp.materialType == MaterialType.Metal)
                totalColor = AdditiveBlend(totalColor, comp.itemColor);
        }

        Debug.Log("아이템을 처리 중입니다... 3초 후 새로운 아이템이 생성됩니다.");
        yield return new WaitForSeconds(3f);

        // 7) 새로운 아이템 생성
        GameObject newItem = Instantiate(itemPrefab, transform.position, Quaternion.identity);
        ItemComponent newItemComponent = newItem.GetComponent<ItemComponent>();
        Rigidbody newItemRb = newItem.GetComponent<Rigidbody>();

        if (newItemComponent != null)
        {
            ApplyCombinedStats(newItemComponent, statsComponent, totalColor);
            foreach (var comp in components)
            {
                newItemComponent.AddMaterial(comp);
                newItemComponent.AddMaterialsFrom(comp);
            }
            newItemRb.isKinematic = false;
            newItem.transform.SetParent(targetPosition);
            newItem.transform.localPosition = Vector3.zero;
            Debug.Log($"[Forge] 새로운 아이템 생성: {newItemComponent}");
        }
        else
        {
            Debug.LogError("[Forge] 새로운 아이템에 ItemComponent가 없습니다!");
        }

        // 8) 원재료 파괴 및 리스트 초기화
        foreach (var item in storedItems)
            Destroy(item);
        storedItems.Clear();

        // 9) 불꽃 소멸 및 파티클 연출
        if (forgeFire != null)
            forgeFire.OnFire = false;
        sparkEffect.Play();

        isForging = false;
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
            if (item.materialType == MaterialType.Metal) metalCount++;
            else if (item.materialType == MaterialType.Fuel) fuelCount++;
        }
        // 최소 1개 이상이어야 하고, 1:1 비율
        if (metalCount == 0 || fuelCount == 0) return false;
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
        if (!other.CompareTag("Items")) return;

        var comp = other.GetComponent<ItemComponent>();
        if (comp == null) return;

        if (!storedItems.Contains(other.gameObject))
        {
            storedItems.Add(other.gameObject);
            Debug.Log($"[Forge] {other.name} 이(가) 저장소에 추가되었습니다.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (storedItems.Contains(other.gameObject))
        {
            storedItems.Remove(other.gameObject);
            Debug.Log($"[Forge] {other.name} 이(가) 저장소에서 제거되었습니다.");
        }
    }
}
