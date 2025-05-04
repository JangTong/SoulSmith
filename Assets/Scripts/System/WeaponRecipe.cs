using UnityEngine;

[CreateAssetMenu(fileName = "WeaponRecipe", menuName = "Crafting/Weapon Recipe")]
public class WeaponRecipe : ScriptableObject
{
    public string weaponName;
    public GameObject weaponPrefab;
    public int[] requiredCollisionCounts = new int[5]; // 충돌 카운트 매칭

    [Header("Stat Factors")]
    public float weightFactor = 1f;
    public float atkFactor = 1f;
    public float defFactor = 1f;

    [Header("Pricing")]
    public int basePrice = 0;
    public float priceMultiplier = 1f;
}
