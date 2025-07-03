using UnityEngine;

public abstract class MagicSpell : ScriptableObject
{
    [Header("Basic Info")]
    public string spellName;
    public string description;
    public Sprite spellIcon; // 마법 아이콘
    
    [Header("Properties")]
    public ElementalMana cost;

    public abstract void Fire(Transform caster);
}
