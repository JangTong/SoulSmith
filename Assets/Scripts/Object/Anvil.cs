using System.Collections.Generic;
using UnityEngine;

public class Anvil : MonoBehaviour
{
    public Transform fixedPosition; // Item을 고정할 자식 Transform 위치
    public GameObject objectOnAnvil = null; // Anvil에 고정된 오브젝트
    public List<GameObject> weaponList = new List<GameObject>();
    public ItemComponent itemComponent;
    public WeaponBase weaponBase;

    private void Update()
    {
        CreateWeapon();
    }

    private void OnTriggerEnter(Collider other)
    {
        // GameObject에서 ItemComponent 가져오기
        itemComponent = other.GetComponent<ItemComponent>();

        // ItemComponent가 존재하고 조건을 만족하는 경우 처리
        if (!ItemPickup.Instance.isSwinging && !ItemPickup.Instance.isEquipped && objectOnAnvil == null && itemComponent != null && itemComponent.itemType == ItemType.Resource && itemComponent.materialType == MaterialType.Metal)
        {
            objectOnAnvil = other.gameObject;

            // Rigidbody 가져오기
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true; // Rigidbody를 Kinematic으로 설정
            }

            // Item의 부모를 Anvil로 설정하고 위치 고정
            other.transform.SetParent(fixedPosition);
            other.transform.localPosition = Vector3.zero; // 자식 위치로 고정

            Debug.Log($"{other.name}이(가) Anvil에 고정되었습니다.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Trigger를 벗어난 오브젝트가 objectOnAnvil과 동일한 경우 초기화
        if (objectOnAnvil == other.gameObject)
        {
            objectOnAnvil = null; // 초기화
            Debug.Log($"{other.name}이(가) Anvil에서 제거되었습니다.");
        }
    }

    public void CreateWeapon()
    {

        // 조건 확인
        if (objectOnAnvil == null)return;

        // ItemComponent와 WeaponBase 컴포넌트 가져오기
        itemComponent = objectOnAnvil.GetComponent<ItemComponent>();
        weaponBase = objectOnAnvil.GetComponent<WeaponBase>();

        if (itemComponent == null || weaponBase == null)
        {
            Debug.LogError("ItemComponent 또는 WeaponBase 컴포넌트가 누락되었습니다.");
            return;
        }

        // 타격횟수 확인 후 무기 생성 및 itemComponent수치 복사
        if(ChechCollisionData(2, 2, 2, 2, 2)) //Sword생성
        {
            if(itemComponent.weight >= 2) // 무게가 2보다 크면 TwoHandedSword생성
            {
                GameObject swordPrefab = weaponList.Find(weapon => weapon.name == "TwoHandedSword");

                if (swordPrefab != null)
                {
                    GameObject newWeapon = Instantiate(swordPrefab, fixedPosition.position, objectOnAnvil.transform.rotation * Quaternion.Euler(0, 90, 0));

                    ItemComponent newItemComponent = newWeapon.GetComponent<ItemComponent>();

                    if (newItemComponent != null)
                    {
                        newItemComponent.itemName = "TwoHandedSword" + " (Forged)";
                        newItemComponent.weight = itemComponent.weight * 0.8f; // 무게 10% 증가
                        newItemComponent.atkPower = itemComponent.atkPower * 1.2f; // 공격력 20% 증가
                        newItemComponent.defPower = itemComponent.defPower * 1.05f; // 방어력 5% 증가
                        newItemComponent.buyPrice = itemComponent.buyPrice + 100; // 구매 가격 증가
                        newItemComponent.sellPrice = itemComponent.sellPrice + 60; // 판매 가격 증가

                        Debug.Log($"새로운 무기 생성: {newWeapon.name} - 공격력: {newItemComponent.atkPower}, 방어력: {newItemComponent.defPower}");
                    }
                }
            }
            else // Sword 생성
            {
                GameObject swordPrefab = weaponList.Find(weapon => weapon.name == "Sword");

                if (swordPrefab != null)
                {
                    GameObject newWeapon = Instantiate(swordPrefab, fixedPosition.position, objectOnAnvil.transform.rotation * Quaternion.Euler(0, 90, 0));

                    ItemComponent newItemComponent = newWeapon.GetComponent<ItemComponent>();

                    if (newItemComponent != null)
                    {
                        newItemComponent.itemName = "Sword" + " (Forged)";
                        newItemComponent.weight = itemComponent.weight * 1.1f; // 무게 10% 증가
                        newItemComponent.atkPower = itemComponent.atkPower * 1.2f; // 공격력 20% 증가
                        newItemComponent.defPower = itemComponent.defPower * 1.05f; // 방어력 5% 증가
                        newItemComponent.buyPrice = itemComponent.buyPrice + 50; // 구매 가격 증가
                        newItemComponent.sellPrice = itemComponent.sellPrice + 30; // 판매 가격 증가

                        Debug.Log($"새로운 무기 생성: {newWeapon.name} - 공격력: {newItemComponent.atkPower}, 방어력: {newItemComponent.defPower}");
                    }
                }
            }
            // WeaponBase제거
            Destroy(objectOnAnvil);
            objectOnAnvil = null;
        }
        else if(ChechCollisionData(1,1,1,1,3)) //Axe생성
        {
            GameObject axePrefab = weaponList.Find(weapon => weapon.name == "Axe");

            if (axePrefab != null)
            {
                GameObject newWeapon = Instantiate(axePrefab, fixedPosition.position, objectOnAnvil.transform.rotation * Quaternion.Euler(0, 90, 0));

                // 새로운 무기의 ItemComponent 가져오기
                ItemComponent newItemComponent = newWeapon.GetComponent<ItemComponent>();

                if (newItemComponent != null)
                {
                    // 기존 ItemComponent 수치 복사
                    newItemComponent.itemName = "Axe" + " (Forged)";
                    newItemComponent.weight = itemComponent.weight * 0.8f; // 무게 10% 증가
                    newItemComponent.atkPower = itemComponent.atkPower * 1.2f; // 공격력 20% 증가
                    newItemComponent.defPower = itemComponent.defPower * 1.05f; // 방어력 5% 증가
                    newItemComponent.buyPrice = itemComponent.buyPrice + 100; // 구매 가격 증가
                    newItemComponent.sellPrice = itemComponent.sellPrice + 60; // 판매 가격 증가

                    Debug.Log($"새로운 무기 생성: {newWeapon.name} - 공격력: {newItemComponent.atkPower}, 방어력: {newItemComponent.defPower}");
                }
            }
            // WeaponBase제거
            Destroy(objectOnAnvil);
            objectOnAnvil = null;
        }

        return;
    }

    // 타격횟수 조건체크
    private bool ChechCollisionData(int a, int b, int c, int d, int e)
    {
        WeaponBase weaponBase = objectOnAnvil.GetComponent<WeaponBase>();
        bool isSame = false;
        if(weaponBase.collisionDataList[0].collisionCount == a &&
        weaponBase.collisionDataList[1].collisionCount == b &&
        weaponBase.collisionDataList[2].collisionCount == c &&
        weaponBase.collisionDataList[3].collisionCount == d &&
        weaponBase.collisionDataList[4].collisionCount == e)
        {
            isSame = true;
        }

        return isSame;
    }
}