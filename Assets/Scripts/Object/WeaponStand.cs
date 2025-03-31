using UnityEngine;

public class WeaponStand : MonoBehaviour
{
    [Header("Weapon Slots")]
    public Transform[] weaponSlots = new Transform[4]; // 최대 4개 보관 가능

    private void OnTriggerEnter(Collider other)
    {
        ItemComponent item = other.GetComponent<ItemComponent>();
        if (item != null && item.weaponType != WeaponType.None && item.partsType == PartsType.Blade) // 무기인지 확인
        {
            Debug.Log("🔍 무기 감지: " + other.gameObject.name);
            AddWeapon(other.gameObject);
        }
    }

    public bool AddWeapon(GameObject weapon)
    {
        for (int i = 0; i < weaponSlots.Length; i++)
        {
            if (weaponSlots[i].childCount == 0) // 슬롯이 비어 있는지 확인
            {
                weapon.transform.SetParent(weaponSlots[i]);
                weapon.transform.localPosition = Vector3.zero;
                
                // X/Z축 기준 90도 회전 적용
                weapon.transform.localRotation = Quaternion.Euler(90, 0, 90);

                Debug.Log("✅ 무기 거치 완료: " + weapon.name);
                return true;
            }
        }
        Debug.LogWarning("⚠ 무기 거치대가 가득 찼습니다!");
        return false;
    }
}