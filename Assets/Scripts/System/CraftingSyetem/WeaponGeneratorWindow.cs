using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public class WeaponGeneratorWindow : EditorWindow
{
    string weaponName = "NewWeapon";
    string description = "";
    float weight = 1f;
    float atkPower = 10f;
    float defPower = 5f;
    int buyPrice = 100;
    int sellPrice = 50;
    bool isPolished = false;
    bool canCombine = true;
    Color itemColor = Color.white;
    Rarity itemRarity = Rarity.Common;
    ItemType itemType = ItemType.Weapon;
    WeaponType weaponType = WeaponType.Sword;
    PartsType partsType = PartsType.None;
    ArmorType armorType = ArmorType.None;
    MaterialType materialType = MaterialType.Metal;

    // Elemental
    int manaFire = 0, manaWater = 0, manaEarth = 0, manaAir = 0;
    int resistFire = 0, resistWater = 0, resistEarth = 0, resistAir = 0;
    float affinityFire = 0f, affinityWater = 0f, affinityEarth = 0f, affinityAir = 0f;

    // Special
    bool isCursed = false;
    bool isBlessed = false;
    string specialEffect = "";

    Material customMaterial = null;
    GameObject basePrefab;

    [MenuItem("Tools/Weapon Generator With Save + Addressables")]
    public static void ShowWindow()
    {
        GetWindow<WeaponGeneratorWindow>("Weapon Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("⚙️ 무기 생성기", EditorStyles.boldLabel);

        weaponName = EditorGUILayout.TextField("무기 이름", weaponName);
        description = EditorGUILayout.TextField("설명", description);
        basePrefab = (GameObject)EditorGUILayout.ObjectField("기본 프리팹", basePrefab, typeof(GameObject), false);
        customMaterial = (Material)EditorGUILayout.ObjectField("머테리얼", customMaterial, typeof(Material), false);

        GUILayout.Space(10);
        GUILayout.Label("기본 속성", EditorStyles.boldLabel);
        weight = EditorGUILayout.FloatField("무게", weight);
        atkPower = EditorGUILayout.FloatField("공격력", atkPower);
        defPower = EditorGUILayout.FloatField("방어력", defPower);
        isPolished = EditorGUILayout.Toggle("광택 여부", isPolished);
        buyPrice = EditorGUILayout.IntField("구매가", buyPrice);
        sellPrice = EditorGUILayout.IntField("판매가", sellPrice);
        itemColor = EditorGUILayout.ColorField("아이템 색상", itemColor);

        GUILayout.Space(10);
        GUILayout.Label("카테고리", EditorStyles.boldLabel);
        itemRarity = (Rarity)EditorGUILayout.EnumPopup("희귀도", itemRarity);
        itemType = (ItemType)EditorGUILayout.EnumPopup("아이템 종류", itemType);
        weaponType = (WeaponType)EditorGUILayout.EnumPopup("무기 타입", weaponType);
        partsType = (PartsType)EditorGUILayout.EnumPopup("부품 타입", partsType);
        armorType = (ArmorType)EditorGUILayout.EnumPopup("방어구 타입", armorType);
        materialType = (MaterialType)EditorGUILayout.EnumPopup("재질 종류", materialType);

        GUILayout.Space(10);
        GUILayout.Label("원소 마나", EditorStyles.boldLabel);
        manaFire = EditorGUILayout.IntField("불 마나", manaFire);
        manaWater = EditorGUILayout.IntField("물 마나", manaWater);
        manaEarth = EditorGUILayout.IntField("땅 마나", manaEarth);
        manaAir = EditorGUILayout.IntField("바람 마나", manaAir);

        GUILayout.Label("원소 저항", EditorStyles.boldLabel);
        resistFire = EditorGUILayout.IntField("불 저항", resistFire);
        resistWater = EditorGUILayout.IntField("물 저항", resistWater);
        resistEarth = EditorGUILayout.IntField("땅 저항", resistEarth);
        resistAir = EditorGUILayout.IntField("바람 저항", resistAir);

        GUILayout.Label("원소 친화도", EditorStyles.boldLabel);
        affinityFire = EditorGUILayout.FloatField("불 친화도", affinityFire);
        affinityWater = EditorGUILayout.FloatField("물 친화도", affinityWater);
        affinityEarth = EditorGUILayout.FloatField("땅 친화도", affinityEarth);
        affinityAir = EditorGUILayout.FloatField("바람 친화도", affinityAir);

        GUILayout.Space(10);
        GUILayout.Label("특수 속성", EditorStyles.boldLabel);
        isCursed = EditorGUILayout.Toggle("저주 여부", isCursed);
        isBlessed = EditorGUILayout.Toggle("축복 여부", isBlessed);
        specialEffect = EditorGUILayout.TextField("특수 효과 설명", specialEffect);

        canCombine = EditorGUILayout.Toggle("조합 가능 여부", canCombine);

        GUILayout.Space(10);
        if (GUILayout.Button("무기 생성 및 저장 + Addressables 등록"))
        {
            CreateAndSaveWeapon();
        }
    }

    void CreateAndSaveWeapon()
    {
        if (basePrefab == null)
        {
            Debug.LogWarning("⚠️ 기본 프리팹을 설정해주세요!");
            return;
        }

        GameObject newWeapon = Instantiate(basePrefab);
        newWeapon.name = weaponName;

        var renderer = newWeapon.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (customMaterial != null)
            {
                renderer.sharedMaterial = customMaterial;
            }
            else
            {
                renderer.sharedMaterial = new Material(renderer.sharedMaterial);
                if (renderer.sharedMaterial.HasProperty("_Color"))
                    renderer.sharedMaterial.color = itemColor;
            }
        }

        var item = newWeapon.GetComponent<ItemComponent>();
        if (item != null)
        {
            item.itemName = weaponName;
            item.description = description;
            item.weight = weight;
            item.atkPower = atkPower;
            item.defPower = defPower;
            item.buyPrice = buyPrice;
            item.sellPrice = sellPrice;
            item.isPolished = isPolished;
            item.itemColor = itemColor;
            item.itemRarity = itemRarity;
            item.itemType = itemType;
            item.weaponType = weaponType;
            item.partsType = partsType;
            item.armorType = armorType;
            item.materialType = materialType;
            item.canCombine = canCombine;

            item.elementalMana = new ElementalMana { fire = manaFire, water = manaWater, earth = manaEarth, air = manaAir };
            item.elementalResistance = new ElementalResistance
            {
                fireResistance = resistFire,
                waterResistance = resistWater,
                earthResistance = resistEarth,
                airResistance = resistAir
            };
            item.elementalAffinity = new ElementalAffinity
            {
                fireAffinity = affinityFire,
                waterAffinity = affinityWater,
                earthAffinity = affinityEarth,
                airAffinity = affinityAir
            };
            item.specialAttributes = new SpecialAttributes
            {
                isCursed = isCursed,
                isBlessed = isBlessed,
                specialEffect = specialEffect
            };
        }

        string path = $"Assets/Prefabs/Weapons/{weaponName}.prefab";
        Directory.CreateDirectory("Assets/Prefabs/Weapons");

        PrefabUtility.SaveAsPrefabAsset(newWeapon, path);
        DestroyImmediate(newWeapon);

        RegisterAddressable(path, weaponName);

        Debug.Log($"✅ {weaponName} 생성 완료 및 저장");
        AssetDatabase.Refresh();
    }

    void RegisterAddressable(string path, string addressName)
    {
        string guid = AssetDatabase.AssetPathToGUID(path);
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var group = settings.FindGroup("WeaponPrefabs") ?? settings.CreateGroup("WeaponPrefabs", false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));

        var entry = settings.CreateOrMoveEntry(guid, group);
        entry.address = addressName;

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
        AssetDatabase.SaveAssets();
    }
}