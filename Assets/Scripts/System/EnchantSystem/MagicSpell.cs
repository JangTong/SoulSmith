using UnityEngine;

public abstract class MagicSpell : ScriptableObject
{
    public string spellName;
    public string description;
    public ElementalMana cost;
    public GameObject elementalEffectPrefab;

    public abstract void Fire(Transform caster);
}
