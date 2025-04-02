using System.Collections.Generic;
using UnityEngine;

public class Anvil : MonoBehaviour
{
    public Transform fixedPosition; // Item을 고정할 자식 Transform 위치
    public GameObject objectOnAnvil = null; // Anvil에 고정된 오브젝트
    public List<GameObject> weaponList = new List<GameObject>();
    public ItemComponent itemComponent;
    public WeaponBase weaponBase;
    public ParticleSystem sparkEffect;

    private void Update()
    {
        CreateWeapon();
    }

    private void OnTriggerEnter(Collider other)
    {
        itemComponent = other.GetComponent<ItemComponent>();

        if (ItemPickup.Instance.currentState == ItemPickupState.Idle &&
            objectOnAnvil == null && itemComponent != null &&
            itemComponent.itemType == ItemType.Resource && itemComponent.materialType == MaterialType.Metal)
        {
            objectOnAnvil = other.gameObject;

            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            other.transform.SetParent(fixedPosition);
            other.transform.localPosition = Vector3.zero;

            Debug.Log($"{other.name}이(가) Anvil에 고정되었습니다.");

            // ✅ WeaponBase에 상태 전달
            WeaponBase weaponBase = objectOnAnvil.GetComponent<WeaponBase>();
            if (weaponBase != null)
            {
                weaponBase.isOnAnvil = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (objectOnAnvil == other.gameObject)
        {
            // ✅ WeaponBase에 상태 전달 해제
            WeaponBase weaponBase = objectOnAnvil.GetComponent<WeaponBase>();
            if (weaponBase != null)
            {
                weaponBase.isOnAnvil = false;
            }

            objectOnAnvil = null;
            Debug.Log($"{other.name}이(가) Anvil에서 제거되었습니다.");
        }
    }

    public void CreateWeapon()
    {
        if (objectOnAnvil == null) return;

        itemComponent = objectOnAnvil.GetComponent<ItemComponent>();
        weaponBase = objectOnAnvil.GetComponent<WeaponBase>();

        if (itemComponent == null || weaponBase == null)
        {
            Debug.LogError("ItemComponent 또는 WeaponBase 컴포넌트가 누락되었습니다.");
            return;
        }

        // 무기 종류 판별 및 생성
        if (ChechCollisionData(2, 2, 2, 2, 2)) // Sword
        {
            string weaponName = (itemComponent.weight >= 2) ? "TwoHandedSword" : "One_Handed_Sword_Blade";
            CreateWeaponInstance(weaponName, 0.8f, 1.2f, 0.8f, 30, 2.2f);
        }
        else if (ChechCollisionData(1, 1, 1, 1, 3)) // Axe
        {
            string weaponName = (itemComponent.weight >= 2) ? "TwoHandedAxe" : "Axe_Blade";
            CreateWeaponInstance(weaponName, 0.8f, 1.3f, 0.6f, 30, 1.9f);
        }
        else if (ChechCollisionData(2, 2, 2, 2, 0)) // Shield
        {
            CreateWeaponInstance("Shield", 0.8f, 0.5f, 1.2f, 30, 2.0f);
        }
        else if (ChechCollisionData(1, 1, 2, 2, 1)) // RoundShield
        {
            CreateWeaponInstance("RoundShield", 0.6f, 0.5f, 1.15f, 30, 2.2f);
        }
        else if (ChechCollisionDataSum() >= 20) // Dagger (20회 이상 타격)
        {
            CreateWeaponInstance("Dagger", 0.3f, 1.05f, 0.3f, 20, 2.0f);
        }
    }

    private void CreateWeaponInstance(string weaponName, float weightFactor, float atkFactor, float defFactor, int basePrice, float priceMultiplier)
    {
        GameObject weaponPrefab = weaponList.Find(weapon => weapon.name == weaponName);
        if (weaponPrefab == null)
        {
            Debug.LogError($"무기 프리팹을 찾을 수 없습니다: {weaponName}");
            return;
        }

        GameObject newWeapon = Instantiate(weaponPrefab, fixedPosition.position, objectOnAnvil.transform.rotation * Quaternion.Euler(0, 90, 0));
        ItemComponent newItemComponent = newWeapon.GetComponent<ItemComponent>();

        if (newItemComponent != null)
        {
            newItemComponent.itemName = weaponName;
            newItemComponent.weight = itemComponent.weight * weightFactor;
            newItemComponent.atkPower = itemComponent.atkPower * atkFactor;
            newItemComponent.defPower = itemComponent.defPower * defFactor;
            newItemComponent.sellPrice = itemComponent.sellPrice + basePrice + (int)(newItemComponent.atkPower * priceMultiplier);
            newItemComponent.buyPrice = newItemComponent.sellPrice * 2;

            // 기존 오브젝트 색상을 적용
            newItemComponent.itemColor = itemComponent.itemColor;

            Debug.Log($"새로운 무기 생성: {newWeapon.name} - 공격력: {newItemComponent.atkPower}, 방어력: {newItemComponent.defPower}, 색상: {newItemComponent.itemColor}");
        }

        // 머테리얼 색상 적용
        ApplyMaterialColor(newWeapon, newItemComponent.itemColor);

        Destroy(objectOnAnvil);
        objectOnAnvil = null;
        sparkEffect.Play();
    }

    private void ApplyMaterialColor(GameObject weapon, Color color)
    {
        Renderer weaponRenderer = weapon.GetComponent<Renderer>();
        if (weaponRenderer != null)
        {
            weaponRenderer.material = new Material(weaponRenderer.material); // 머테리얼 인스턴스화
            weaponRenderer.material.color = color; // 색상 적용
        }
    }

    private bool ChechCollisionData(int a, int b, int c, int d, int e)
    {
        WeaponBase weaponBase = objectOnAnvil.GetComponent<WeaponBase>();
        return weaponBase != null &&
               weaponBase.collisionDataList[0].collisionCount == a &&
               weaponBase.collisionDataList[1].collisionCount == b &&
               weaponBase.collisionDataList[2].collisionCount == c &&
               weaponBase.collisionDataList[3].collisionCount == d &&
               weaponBase.collisionDataList[4].collisionCount == e;
    }

    private int ChechCollisionDataSum()
    {
        WeaponBase weaponBase = objectOnAnvil.GetComponent<WeaponBase>();
        int collisionSum = 0;
        if (weaponBase != null)
        {
            for (int i = 0; i < 5; i++)
                collisionSum += weaponBase.collisionDataList[i].collisionCount;
        }
        return collisionSum;
    }
}
