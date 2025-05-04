using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CustomerRequest", menuName = "Shop/Customer Request")]
public class CustomerRequestSO : ScriptableObject
{
    public string customerName;
    [TextArea] public string dialogue;
    [TextArea] public string successDialogue;       // 거래 성공 시 출력할 대사
    [TextArea] public string failureDialogue;       // 거래 실패 시 출력할 대사

    [Header("=== Weapon Type ===")]
    public bool useWeaponType;
    public WeaponType requiredWeaponType;
    [Range(0f, 1f)] public float weaponTypeWeight = 1f;

    [Header("=== Attack Conditions ===")]
    public bool useMinAtk;
    public float minAtkPower;
    [Range(0f, 1f)] public float atkWeight = 0.5f;
    public bool useMaxAtk;
    public float maxAtkPower;
    [Range(0f, 1f)] public float maxAtkWeight = 0f;

    [Header("=== Defense Conditions ===")]
    public bool useMinDef;
    public float minDefPower;
    [Range(0f, 1f)] public float defWeight = 0.5f;
    public bool useMaxDef;
    public float maxDefPower;
    [Range(0f, 1f)] public float maxDefWeight = 0f;

    [Header("=== Physical Attributes ===")]
    public bool useMaxWeight;
    public float maxWeight;
    [Range(0f, 1f)] public float weightWeight = 0.3f;

    [Header("=== Rarity ===")]
    public bool useMinRarity;
    public Rarity minRarity;
    [Range(0f, 1f)] public float rarityWeight = 0.7f;

    [Header("=== Elemental Mana ===")]
    public bool useFire;
    [Range(0f, 1f)] public float fireWeight = 0f;
    public bool useWater;
    [Range(0f, 1f)] public float waterWeight = 0f;
    public bool useEarth;
    [Range(0f, 1f)] public float earthWeight = 0f;
    public bool useAir;
    [Range(0f, 1f)] public float airWeight = 0f;

    [Header("=== Elemental Resistance ===")]
    public bool useFireRes;
    public int minFireRes;
    [Range(0f, 1f)] public float fireResWeight = 0f;
    public bool useWaterRes;
    public int minWaterRes;
    [Range(0f, 1f)] public float waterResWeight = 0f;
    public bool useEarthRes;
    public int minEarthRes;
    [Range(0f, 1f)] public float earthResWeight = 0f;
    public bool useAirRes;
    public int minAirRes;
    [Range(0f, 1f)] public float airResWeight = 0f;

    [Header("=== Elemental Affinity ===")]
    public bool useFireAffinity;
    public float minFireAffinity;
    [Range(0f, 1f)] public float fireAffinityWeight = 0f;
    public bool useWaterAffinity;
    public float minWaterAffinity;
    [Range(0f, 1f)] public float waterAffinityWeight = 0f;
    public bool useEarthAffinity;
    public float minEarthAffinity;
    [Range(0f, 1f)] public float earthAffinityWeight = 0f;
    public bool useAirAffinity;
    public float minAirAffinity;
    [Range(0f, 1f)] public float airAffinityWeight = 0f;

    [Header("=== Special Effects ===")]
    public bool useSpecialEffect;
    public string requiredEffect;
    [Range(0f, 1f)] public float effectWeight = 0f;
    public bool requireBlessed;
    [Range(0f, 1f)] public float blessedWeight = 0f;
    public bool requireCursed;
    [Range(0f, 1f)] public float cursedWeight = 0f;

    [Header("=== Spell Conditions ===")]
    public bool useRequiredSpells;
    public List<MagicSpell> requiredSpells = new List<MagicSpell>();
    [Range(0f, 1f)] public float spellWeight = 0f;
    public bool useForbiddenSpells;
    public List<MagicSpell> forbiddenSpells = new List<MagicSpell>();
    [Range(0f, 1f)] public float forbiddenSpellWeight = 0f;

    [Header("=== Material Usage Conditions ===")]
    public bool useRequiredMaterials;
    public List<string> requiredMaterialNames = new List<string>();
    public int requiredMaterialCount = 1;
    [Range(0f, 1f)] public float materialWeight = 0f;

    /// <summary>
    /// 평가 점수와 거래 허용 여부를 반환합니다.
    /// </summary>
    public float Evaluate(ItemComponent item, EnchantComponent enchant, out bool isAcceptable)
    {
        float score = 0f;
        isAcceptable = true;

        // Weapon Type
        if (useWeaponType)
        {
            bool ok = item.weaponType == requiredWeaponType;
            if (!ok && weaponTypeWeight >= 1f) { isAcceptable = false; return 0f; }
            if (ok) score += weaponTypeWeight;
        }

        // Attack Conditions
        if (useMinAtk)
        {
            bool ok = item.atkPower >= minAtkPower;
            if (!ok && atkWeight >= 1f) { isAcceptable = false; return 0f; }
            if (ok) score += atkWeight;
        }
        if (useMaxAtk)
        {
            bool ok = item.atkPower <= maxAtkPower;
            if (!ok && maxAtkWeight >= 1f) { isAcceptable = false; return 0f; }
            if (!ok) score -= maxAtkWeight;
        }

        // Defense Conditions
        if (useMinDef)
        {
            bool ok = item.defPower >= minDefPower;
            if (!ok && defWeight >= 1f) { isAcceptable = false; return 0f; }
            if (ok) score += defWeight;
        }
        if (useMaxDef)
        {
            bool ok = item.defPower <= maxDefPower;
            if (!ok && maxDefWeight >= 1f) { isAcceptable = false; return 0f; }
            if (!ok) score -= maxDefWeight;
        }

        // Physical Attributes
        if (useMaxWeight)
        {
            bool ok = item.weight <= maxWeight;
            if (!ok && weightWeight >= 1f) { isAcceptable = false; return 0f; }
            if (ok) score += weightWeight;
        }

        // Rarity
        if (useMinRarity)
        {
            bool ok = item.itemRarity >= minRarity;
            if (!ok && rarityWeight >= 1f) { isAcceptable = false; return 0f; }
            if (ok) score += rarityWeight;
        }

        // Elemental Mana
        if (useFire && item.elementalMana.fire > 0) score += fireWeight;
        if (useWater && item.elementalMana.water > 0) score += waterWeight;
        if (useEarth && item.elementalMana.earth > 0) score += earthWeight;
        if (useAir && item.elementalMana.air > 0) score += airWeight;

        // Elemental Resistance
        if (useFireRes)
        {
            bool ok = item.elementalResistance.fireResistance >= minFireRes;
            if (ok) score += fireResWeight;
            else if (fireResWeight >= 1f) { isAcceptable = false; return 0f; }
        }
        if (useWaterRes)
        {
            bool ok = item.elementalResistance.waterResistance >= minWaterRes;
            if (ok) score += waterResWeight;
            else if (waterResWeight >= 1f) { isAcceptable = false; return 0f; }
        }
        if (useEarthRes)
        {
            bool ok = item.elementalResistance.earthResistance >= minEarthRes;
            if (ok) score += earthResWeight;
            else if (earthResWeight >= 1f) { isAcceptable = false; return 0f; }
        }
        if (useAirRes)
        {
            bool ok = item.elementalResistance.airResistance >= minAirRes;
            if (ok) score += airResWeight;
            else if (airResWeight >= 1f) { isAcceptable = false; return 0f; }
        }

        // Elemental Affinity
        if (useFireAffinity)
        {
            bool ok = item.elementalAffinity.fireAffinity >= minFireAffinity;
            if (ok) score += fireAffinityWeight;
            else if (fireAffinityWeight >= 1f) { isAcceptable = false; return 0f; }
        }
        if (useWaterAffinity)
        {
            bool ok = item.elementalAffinity.waterAffinity >= minWaterAffinity;
            if (ok) score += waterAffinityWeight;
            else if (waterAffinityWeight >= 1f) { isAcceptable = false; return 0f; }
        }
        if (useEarthAffinity)
        {
            bool ok = item.elementalAffinity.earthAffinity >= minEarthAffinity;
            if (ok) score += earthAffinityWeight;
            else if (earthAffinityWeight >= 1f) { isAcceptable = false; return 0f; }
        }
        if (useAirAffinity)
        {
            bool ok = item.elementalAffinity.airAffinity >= minAirAffinity;
            if (ok) score += airAffinityWeight;
            else if (airAffinityWeight >= 1f) { isAcceptable = false; return 0f; }
        }

        // Special Effects
        if (useSpecialEffect)
        {
            bool ok = item.specialAttributes != null && item.specialAttributes.specialEffect == requiredEffect;
            if (ok) score += effectWeight;
            else if (effectWeight >= 1f) { isAcceptable = false; return 0f; }
        }
        if (requireBlessed)
        {
            bool ok = item.specialAttributes != null && item.specialAttributes.isBlessed;
            if (ok) score += blessedWeight;
            else if (blessedWeight >= 1f) { isAcceptable = false; return 0f; }
        }
        if (requireCursed)
        {
            bool ok = item.specialAttributes != null && item.specialAttributes.isCursed;
            if (ok) score += cursedWeight;
            else if (cursedWeight >= 1f) { isAcceptable = false; return 0f; }
        }

        // Spell Conditions
        if (useRequiredSpells && enchant != null)
        {
            foreach (var spell in enchant.appliedSpells)
            {
                if (spell != null && requiredSpells.Contains(spell)) score += spellWeight;
            }
        }
        if (useForbiddenSpells && enchant != null)
        {
            foreach (var spell in enchant.appliedSpells)
            {
                if (spell != null && forbiddenSpells.Contains(spell))
                {
                    if (forbiddenSpellWeight >= 1f) { isAcceptable = false; return 0f; }
                    score -= forbiddenSpellWeight;
                }
            }
        }

        // Material Usage Conditions
        if (useRequiredMaterials)
        {
            int count = 0;
            foreach (var matEntry in item.materialsUsed)
            {
                if (requiredMaterialNames.Contains(matEntry.name))
                    count++;
            }
            if (count < requiredMaterialCount && materialWeight >= 1f)
            {
                isAcceptable = false;
                return 0f;
            }
            if (count >= requiredMaterialCount)
                score += materialWeight;
        }

        return score;
    }
}
