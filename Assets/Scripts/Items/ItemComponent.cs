using UnityEngine;

// MonoBehaviour 기반의 아이템 컴포넌트
[DisallowMultipleComponent]
public class ItemComponent : MonoBehaviour
{
    [Header("Basic Information")]
    public string itemName = "New Item";       // 아이템 이름
    [TextArea] public string description;     // 아이템 설명

    [Header("Visual Properties")]
    public Color itemColor = Color.white;     // 아이템 색상

    [Header("Item Attributes")]
    public Rarity itemRarity = Rarity.None;   // 희귀도
    public ItemType itemType = ItemType.None; // 아이템 타입
    public WeaponType weaponType = WeaponType.None; // 무기 타입
    public PartsType  partsType = PartsType.None;
    public ArmorType armorType = ArmorType.None;     // 방어구 타입
    public MaterialType materialType = MaterialType.None; // 소재 타입

    [Header("Physical Attributes")]
    public float weight = 1f;                // 무게
    public float atkPower;
    public float defPower;
    public bool isPolished = false;

    [Header("Elemental Attributes")]
    public ElementalMana elementalMana;                // 원소 마나
    public ElementalResistance elementalResistance;    // 원소 저항
    public ElementalAffinity elementalAffinity;        // 원소 친화도

    [Header("Special Attributes")]
    public SpecialAttributes specialAttributes;        // 특수 속성

    [Header("Price and Trade")]
    public int buyPrice;                     // 구매 가격
    public int sellPrice;                    // 판매 가격

    [Header("System Flags")]
    public bool canCombine = true;  

    /// 아이템 초기화 메서드 (외부에서 데이터 적용 가능)
    public void Initialize(
        string itemName,
        string description,
        Rarity rarity,
        ItemType itemType,
        WeaponType weaponType,
        PartsType partsType,
        ArmorType armorType,
        MaterialType materialType,
        float weight,
        ElementalMana elementalMana,
        ElementalResistance elementalResistance,
        ElementalAffinity elementalAffinity,
        SpecialAttributes specialAttributes,
        int buyPrice,
        int sellPrice,
        Color? itemColor = null,
        bool canCombine = true
    )
    {
        this.itemName = itemName;
        this.description = description;
        this.itemRarity = rarity;
        this.itemType = itemType;
        this.weaponType = weaponType;
        this.partsType = partsType;
        this.armorType = armorType;
        this.materialType = materialType;
        this.weight = weight;
        this.elementalMana = elementalMana;
        this.elementalResistance = elementalResistance;
        this.elementalAffinity = elementalAffinity;
        this.specialAttributes = specialAttributes;
        this.buyPrice = buyPrice;
        this.sellPrice = sellPrice;
        this.itemColor = itemColor ?? Color.white;
        this.canCombine = canCombine;
    }

    public override string ToString()
    {
        return $"{itemName} (Rarity: {itemRarity}, Type: {itemType}, Weight: {weight}, Value: {buyPrice}/{sellPrice}";
    }

    // 다른 ItemComponent의 스탯을 이 객체에 더함
    public void AddStatsFrom(ItemComponent other)
    {
        atkPower += other.atkPower;
        defPower += other.defPower;
        weight += other.weight;

        elementalMana.fire += other.elementalMana.fire;
        elementalMana.water += other.elementalMana.water;
        elementalMana.earth += other.elementalMana.earth;
        elementalMana.air += other.elementalMana.air;

        elementalResistance.fireResistance += other.elementalResistance.fireResistance;
        elementalResistance.waterResistance += other.elementalResistance.waterResistance;
        elementalResistance.earthResistance += other.elementalResistance.earthResistance;
        elementalResistance.airResistance += other.elementalResistance.airResistance;

        elementalAffinity.fireAffinity += other.elementalAffinity.fireAffinity;
        elementalAffinity.waterAffinity += other.elementalAffinity.waterAffinity;
        elementalAffinity.earthAffinity += other.elementalAffinity.earthAffinity;
        elementalAffinity.airAffinity += other.elementalAffinity.airAffinity;
    }

    // 다른 ItemComponent의 스탯을 이 객체에서 뺌
    public void SubtractStatsFrom(ItemComponent other)
    {
        atkPower -= other.atkPower;
        defPower -= other.defPower;
        weight -= other.weight;

        elementalMana.fire -= other.elementalMana.fire;
        elementalMana.water -= other.elementalMana.water;
        elementalMana.earth -= other.elementalMana.earth;
        elementalMana.air -= other.elementalMana.air;

        elementalResistance.fireResistance -= other.elementalResistance.fireResistance;
        elementalResistance.waterResistance -= other.elementalResistance.waterResistance;
        elementalResistance.earthResistance -= other.elementalResistance.earthResistance;
        elementalResistance.airResistance -= other.elementalResistance.airResistance;

        elementalAffinity.fireAffinity -= other.elementalAffinity.fireAffinity;
        elementalAffinity.waterAffinity -= other.elementalAffinity.waterAffinity;
        elementalAffinity.earthAffinity -= other.elementalAffinity.earthAffinity;
        elementalAffinity.airAffinity -= other.elementalAffinity.airAffinity;
    }
    }

// 속성 관련 열거형 (기본 값 설정)
public enum Rarity { None, Common, Uncommon, Rare, Epic, Legendary }
public enum ItemType { None, Weapon, Armor, Consumable, Resource, QuestItem }
public enum WeaponType { None, Sword, Bow, Axe, Dagger, Mace, Spear }
public enum PartsType { None, Blade, Handle, Grip, Guard, Pommel }
public enum ArmorType { None, Helmet, Chestplate, Gauntlets, Boots, Shield }
public enum MaterialType { None, Wood, Metal, Leather, Cloth, Fuel }

// 기타 속성 클래스
[System.Serializable]
public class ElementalMana
{
    public int fire;
    public int water;
    public int earth;
    public int air;
}

[System.Serializable]
public class ElementalResistance
{
    public int fireResistance;
    public int waterResistance;
    public int earthResistance;
    public int airResistance;
}

[System.Serializable]
public class ElementalAffinity
{
    public float fireAffinity;
    public float waterAffinity;
    public float earthAffinity;
    public float airAffinity;
}

[System.Serializable]
public class SpecialAttributes
{
    public bool isCursed;
    public bool isBlessed;
    public string specialEffect; // 특수 효과 설명
}
