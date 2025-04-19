using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Forge : MonoBehaviour
{
    public Transform targetPosition; // Items를 고정할 위치

    [SerializeField]
    public Vector3 randomRange = new Vector3(0.1f, 0f, 0.1f); // 랜덤 위치 범위
    public GameObject itemPrefab; // 미리 준비된 3D Object Prefab
    public List<GameObject> storedItems = new List<GameObject>(); // 저장된 Items 오브젝트 배열
    private ForgeFire forgeFire; // ForgeFire 컴포넌트 참조
    private ParticleSystem sparkEffect;

    private void Start()
    {
        forgeFire = GetComponentInChildren<ForgeFire>();
        sparkEffect = GetComponentInChildren<ParticleSystem>();
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

        if (itemPrefab == null)
        {
            Debug.LogError("아이템 Prefab이 설정되지 않았습니다!");
            yield break;
        }

        // MaterialType 개수 확인
        int metalCount = 0;
        int fuelCount = 0;
        Color totalMetalColor = Color.black; // 초기값은 검은색 (가산혼합 초기화)
        
        foreach (var item in storedItems)
        {
            ItemComponent itemComponent = item.GetComponent<ItemComponent>();
            if (itemComponent != null)
            {
                if (itemComponent.materialType == MaterialType.Metal)
                {
                    metalCount++;
                    totalMetalColor = AdditiveBlend(totalMetalColor, itemComponent.itemColor);
                }
                else if (itemComponent.materialType == MaterialType.Fuel)
                {
                    fuelCount++;
                }
            }
        }

        // Metal:Fuel 비율이 1:1이 아닌 경우 작동하지 않음
        if (metalCount != fuelCount)
        {
            Debug.LogWarning($"Forging 실패: Metal과 Fuel의 비율이 1:1이 아닙니다. (Metal: {metalCount}, Fuel: {fuelCount})");
            yield break;
        }

        if (forgeFire != null)
        {
            forgeFire.OnFire = true;
        }

        // 새로운 아이템 속성 초기화
        float totalWeight = 0f;
        float totalAtkPower = 0f;
        float totalDefPower = 0f;
        int totalBuyPrice = 0;
        int totalSellPrice = 0;

        ElementalMana totalMana = new ElementalMana();
        ElementalResistance totalResistance = new ElementalResistance();
        ElementalAffinity totalAffinity = new ElementalAffinity();

        // 저장된 아이템 데이터 합산
        foreach (var item in storedItems)
        {
            ItemComponent itemComponent = item.GetComponent<ItemComponent>();
            if (itemComponent != null)
            {
                totalWeight += itemComponent.weight;
                totalAtkPower += itemComponent.atkPower;
                totalDefPower += itemComponent.defPower;
                totalBuyPrice += itemComponent.buyPrice;
                totalSellPrice += itemComponent.sellPrice;

                // Elemental 속성 합산
                totalMana.fire += itemComponent.elementalMana.fire;
                totalMana.water += itemComponent.elementalMana.water;
                totalMana.earth += itemComponent.elementalMana.earth;
                totalMana.air += itemComponent.elementalMana.air;

                totalResistance.fireResistance += itemComponent.elementalResistance.fireResistance;
                totalResistance.waterResistance += itemComponent.elementalResistance.waterResistance;
                totalResistance.earthResistance += itemComponent.elementalResistance.earthResistance;
                totalResistance.airResistance += itemComponent.elementalResistance.airResistance;

                totalAffinity.fireAffinity += itemComponent.elementalAffinity.fireAffinity;
                totalAffinity.waterAffinity += itemComponent.elementalAffinity.waterAffinity;
                totalAffinity.earthAffinity += itemComponent.elementalAffinity.earthAffinity;
                totalAffinity.airAffinity += itemComponent.elementalAffinity.airAffinity;
            }
        }

        // 기존 아이템 삭제
        foreach (var item in storedItems)
        {
            Destroy(item);
        }
        storedItems.Clear();

        Debug.Log($"아이템을 처리 중입니다... 3초 후 새로운 아이템이 생성됩니다. (Metal: {metalCount}, Fuel: {fuelCount})");
        yield return new WaitForSeconds(3f); // 3초 딜레이

        // Prefab에서 새로운 아이템 생성
        GameObject newItem = Instantiate(itemPrefab, transform.position, Quaternion.identity);
        ItemComponent newItemComponent = newItem.GetComponent<ItemComponent>();
        Rigidbody newItemRb = newItem.GetComponent<Rigidbody>();

        newItemRb.isKinematic = false;
        newItem.transform.SetParent(targetPosition);
        newItem.transform.localPosition = Vector3.zero;

        if (newItemComponent != null)
        {
            // 기본 속성 설정
            newItemComponent.itemName = "WeaponBase";
            newItemComponent.weight = totalWeight;
            newItemComponent.atkPower = totalAtkPower;
            newItemComponent.defPower = totalDefPower;
            newItemComponent.buyPrice = totalBuyPrice;
            newItemComponent.sellPrice = totalSellPrice;

            // 원소 속성 설정
            newItemComponent.elementalMana = totalMana;
            newItemComponent.elementalResistance = totalResistance;
            newItemComponent.elementalAffinity = totalAffinity;

            // 추가 속성 설정
            newItemComponent.materialType = MaterialType.Metal;
            newItemComponent.itemRarity = Rarity.Uncommon;
            newItemComponent.itemColor = totalMetalColor; // 가산 혼합된 색상 적용

            Debug.Log($"새로운 아이템 생성: {newItemComponent.itemName} - " +
                      $"무게: {newItemComponent.weight}, 공격력: {newItemComponent.atkPower}, 방어력: {newItemComponent.defPower}, " +
                      $"구매 가격: {newItemComponent.buyPrice}, 판매 가격: {newItemComponent.sellPrice}");
        }
        else
        {
            Debug.LogError("새로운 아이템에 ItemComponent가 없습니다!");
        }

        if (forgeFire != null) // ForgeFire 비활성화
        {
            forgeFire.OnFire = false;
        }
        sparkEffect.Play();
        yield break;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Items 태그를 가진 오브젝트가 Trigger에 들어왔을 때 처리
        if (other.CompareTag("Items") && ItemPickup.Instance.currentState == ItemPickupState.Idle)
        {
            // 배열에 추가
            if (!storedItems.Contains(other.gameObject))
            {
                storedItems.Add(other.gameObject);
                Debug.Log($"{other.name}이(가) Forge에 추가되었습니다.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Items 태그를 가진 오브젝트가 Trigger를 벗어날 때 배열에서 제거
        if (other.CompareTag("Items") && storedItems.Contains(other.gameObject))
        {
            // 배열에서 제거 및 부모 관계 해제
            storedItems.Remove(other.gameObject);

            Debug.Log($"{other.name}이(가) Forge에서 제거되었습니다.");
        }
    }

    private Color AdditiveBlend(Color color1, Color color2)
    {
        return new Color(
            Mathf.Min(color1.r + color2.r, 1f),
            Mathf.Min(color1.g + color2.g, 1f),
            Mathf.Min(color1.b + color2.b, 1f),
            Mathf.Min(color1.a + color2.a, 1f)
        );
    }

    public List<GameObject> GetStoredItems()
    {
        return storedItems;
    }
    
}
