using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class ItemComponent : MonoBehaviour
{
    [Header("Basic Information")]
    public string itemName = "New Item";
    [TextArea] public string description;

    [Header("Visual Properties")]
    public Color itemColor = Color.white;

    [Header("Item Attributes")]
    public Rarity itemRarity = Rarity.None;
    public ItemType itemType = ItemType.None;
    public WeaponType weaponType = WeaponType.None;
    public PartsType partsType = PartsType.None;
    public ArmorType armorType = ArmorType.None;
    public MaterialType materialType = MaterialType.None;

    [Header("Physical Attributes")]
    public float weight = 1f;
    public float atkPower;
    public float defPower;
    public bool isPolished = false;

    [Header("연마 속성")]
    [SerializeField] private float smoothness = 0f; // 연마도 (0.0 ~ 1.0)

    [Header("Elemental Attributes")]
    public ElementalMana elementalMana;
    public ElementalResistance elementalResistance;
    public ElementalAffinity elementalAffinity;

    [Header("Special Attributes")]
    public SpecialAttributes specialAttributes;

    [Header("Price and Trade")]
    public int buyPrice;
    public int sellPrice;

    [Header("System Flags")]
    public bool canCombine = true;

    [Header("Crafting Information")]
    public List<MaterialEntry> materialsUsed = new List<MaterialEntry>();

    [System.Serializable]
    public class MaterialEntry
    {
        public string name;
        public Rarity rarity;
        public ItemType type;
        public WeaponType weaponType;
        public MaterialType materialType;
        public float weight;
        public Color color;

        public MaterialEntry(ItemComponent source)
        {
            name = source.itemName;
            rarity = source.itemRarity;
            type = source.itemType;
            weaponType = source.weaponType;
            materialType = source.materialType;
            weight = source.weight;
            color = source.itemColor;
        }
    }

    public void AddMaterial(ItemComponent material)
    {
        materialsUsed.Add(new MaterialEntry(material));
    }

    public void AddMaterialsFrom(ItemComponent other)
    {
        foreach (var entry in other.materialsUsed)
        {
            materialsUsed.Add(entry);
        }
    }

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
        return $"{itemName} (Rarity: {itemRarity}, Type: {itemType}, Weight: {weight}, Value: {buyPrice}/{sellPrice})";
    }

    /// <summary>
    /// 연마 가능 여부 - 블레이드나 무기 타입인 경우 연마 가능
    /// </summary>
    public bool IsSharpenable
    {
        get
        {
            // 블레이드 파츠이거나 무기 타입인 경우 연마 가능
            return (partsType == PartsType.Blade) || 
                   (itemType == ItemType.Weapon && weaponType != WeaponType.None);
        }
    }

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

        buyPrice += other.buyPrice;
        sellPrice += other.sellPrice;
    }

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

    /// <summary>
    /// 연마도 프로퍼티 (0.0 ~ 1.0)
    /// </summary>
    public float Smoothness
    {
        get => smoothness;
        set => smoothness = Mathf.Clamp01(value);
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

    public ElementalMana() { }
    public ElementalMana(ElementalMana source)
    {
        fire = source.fire;
        water = source.water;
        earth = source.earth;
        air = source.air;
    }

    public int Total()
    {
        return fire + water + earth + air;
    }
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
