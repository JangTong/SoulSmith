using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Forge : MonoBehaviour
{
    [Header("가공 설정")]
    public Transform targetPosition;
    public GameObject itemPrefab;
    public List<GameObject> storedItems = new List<GameObject>();
    
    [Header("애니메이션 설정")]
    [SerializeField] private float sinkDepth = 0.5f;  // 가라앉는 깊이
    [SerializeField] private float sinkDuration = 2.5f;  // 가라앉는 시간 (3초보다 짧게)

    private bool isForging = false;
    private ForgeFire forgeFire;
    private ParticleSystem sparkEffect;

    // 재사용 가능한 스탯 저장용 컴포넌트
    private ItemComponent statsComponent;
    private GameObject statsContainer;
    
    private const string LOG_PREFIX = "[Forge]";

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
            Debug.LogWarning($"{LOG_PREFIX} 이미 가동 중입니다! 추가 입력은 무시됩니다.");
            return;
        }
        StartCoroutine(Forging());
    }

    public IEnumerator Forging()
    {
        isForging = true;

        // 1) 가공 시작 시점에 트리거 영역 내 아이템들을 한 번만 검사
        List<GameObject> itemsToProcess = GetItemsInTriggerArea();
        
        if (itemsToProcess.Count == 0)
        {
            Debug.LogWarning($"{LOG_PREFIX} 트리거 영역에 가공 가능한 아이템이 없습니다!");
            isForging = false;
            yield break;
        }

        // 2) 실제 컴포넌트만 추출
        List<ItemComponent> components = GetItemComponents(itemsToProcess);

        // 3) Metal/Fuel 개수 집계 & 로그
        int metalCount = 0, fuelCount = 0;
        foreach (var comp in components)
        {
            if (comp.materialType == MaterialType.Metal) metalCount++;
            else if (comp.materialType == MaterialType.Fuel) fuelCount++;
        }
        Debug.Log($"{LOG_PREFIX} MetalCount={metalCount}, FuelCount={fuelCount}");

        // 4) 최소 1개 이상 & 1:1 비율 검사
        if (metalCount == 0 || fuelCount == 0)
        {
            Debug.LogWarning($"{LOG_PREFIX} 저장된 아이템 부족: Metal={metalCount}, Fuel={fuelCount} (최소 1개씩 필요)");
            isForging = false;
            yield break;
        }
        if (metalCount != fuelCount)
        {
            Debug.LogWarning($"{LOG_PREFIX} Forging 실패: Metal({metalCount})과 Fuel({fuelCount}) 비율이 1:1이 아닙니다.");
            isForging = false;
            yield break;
        }

        // 5) 불꽃 점화
        if (forgeFire != null)
            forgeFire.OnFire = true;

        // 6) 아이템 태그 변경 및 가라앉는 애니메이션 시작
        StartItemSinkingAnimation(itemsToProcess);

        // 7) 스탯 초기화 및 합산
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

        Debug.Log($"{LOG_PREFIX} 아이템을 처리 중입니다... 3초 후 새로운 아이템이 생성됩니다.");
        yield return new WaitForSeconds(3f);

        // 8) 새로운 아이템 생성
        GameObject newItem = Instantiate(itemPrefab, transform.position, Quaternion.identity);
        ItemComponent newItemComponent = newItem.GetComponent<ItemComponent>();
        Rigidbody newItemRb = newItem.GetComponent<Rigidbody>();

        if (newItemComponent != null)
        {
            ApplyCombinedStats(newItemComponent, statsComponent, totalColor);
            foreach (var comp in components)
            {
                if (comp.itemType == ItemType.Resource)
                {
                    // Resource라면 직접 MaterialUsed에 추가
                newItemComponent.AddMaterial(comp);
                }
                else
                {
                    // Resource가 아니라면 해당 아이템의 MaterialUsed에 있는 Resource들을 계승
                newItemComponent.AddMaterialsFrom(comp);
                }
            }
            newItemRb.isKinematic = false;
            newItem.transform.SetParent(targetPosition);
            newItem.transform.localPosition = Vector3.zero;
            Debug.Log($"{LOG_PREFIX} 새로운 아이템 생성: {newItemComponent}");
        }
        else
        {
            Debug.LogError($"{LOG_PREFIX} 새로운 아이템에 ItemComponent가 없습니다!");
        }

        // 9) 원재료 파괴
        foreach (var item in itemsToProcess)
        {
            if (item != null)
            {
                // DOTween Kill 처리
                item.transform.DOKill();
            Destroy(item);
            }
        }

        // 10) 불꽃 소멸 및 파티클 연출
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
            // null 체크 추가
            if (go == null) continue;
            
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

    /// <summary>
    /// 가공 시작 시점에 트리거 영역 내 아이템들을 검사하여 반환
    /// </summary>
    private List<GameObject> GetItemsInTriggerArea()
    {
        List<GameObject> itemsFound = new List<GameObject>();
        Collider forgeCollider = GetComponent<Collider>();
        if (forgeCollider == null) return itemsFound;

        // 트리거 영역 내의 모든 콜라이더 검사
        Collider[] overlappingColliders = Physics.OverlapBox(
            forgeCollider.bounds.center,
            forgeCollider.bounds.extents,
            transform.rotation
        );

        foreach (var collider in overlappingColliders)
        {
            if (!collider.CompareTag("Items")) continue;

            var comp = collider.GetComponent<ItemComponent>();
            if (comp == null) continue;

            itemsFound.Add(collider.gameObject);
            Debug.Log($"{LOG_PREFIX} 가공 대상: {collider.name} 아이템 발견 (MaterialType: {comp.materialType})");
        }

        Debug.Log($"{LOG_PREFIX} 트리거 영역에서 {itemsFound.Count}개의 가공 가능한 아이템을 발견했습니다.");
        return itemsFound;
    }

    /// <summary>
    /// 지정된 아이템들의 태그를 변경하고 가라앉는 애니메이션을 시작
    /// </summary>
    private void StartItemSinkingAnimation(List<GameObject> items)
    {
        foreach (var item in items)
        {
            if (item == null) continue;

            // 태그 변경으로 수집 방지
            item.tag = "Untagged";

            // Rigidbody 비활성화 (물리적 상호작용 방지)
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            // 가라앉는 애니메이션 (Y축으로 하강)
            Vector3 startPos = item.transform.position;
            Vector3 targetPos = startPos - Vector3.up * sinkDepth;

            item.transform.DOMove(targetPos, sinkDuration)
                .SetEase(Ease.InCubic);

            // 페이드 아웃 효과 (Renderer가 있는 경우)
            Renderer renderer = item.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = renderer.material;
                if (mat.HasProperty("_Color"))
                {
                    Color originalColor = mat.color;
                    mat.DOColor(new Color(originalColor.r, originalColor.g, originalColor.b, 0.3f), sinkDuration)
                        .SetEase(Ease.InCubic);
                }
            }
        }

        Debug.Log($"{LOG_PREFIX} {items.Count}개 아이템의 가라앉는 애니메이션 시작");
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

        // 가공 중일 때는 새로운 아이템을 인식하지 않음
        if (isForging)
        {
            Debug.Log($"{LOG_PREFIX} 가공 중이므로 {other.name} 아이템을 인식할 수 없습니다.");
            return;
        }

        var comp = other.GetComponent<ItemComponent>();
        if (comp == null) return;

        // 저장소에는 Resource 타입만 허용
        if (comp.itemType != ItemType.Resource)
        {
            Debug.Log($"{LOG_PREFIX} {other.name}은(는) Resource 타입이 아니므로 저장소에 추가할 수 없습니다. (현재 타입: {comp.itemType})");
            return;
        }

        if (!storedItems.Contains(other.gameObject))
        {
            storedItems.Add(other.gameObject);
            Debug.Log($"{LOG_PREFIX} {other.name} Resource 아이템이 저장소에 추가되었습니다. (MaterialType: {comp.materialType})");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 가공 중일 때는 아이템을 리스트에서 제거하지 않음 (가라앉는 애니메이션으로 인한 트리거 이탈 방지)
        if (isForging) return;
        
        if (storedItems.Contains(other.gameObject))
        {
            storedItems.Remove(other.gameObject);
            Debug.Log($"{LOG_PREFIX} {other.name} 이(가) 저장소에서 제거되었습니다.");
        }
    }
}
