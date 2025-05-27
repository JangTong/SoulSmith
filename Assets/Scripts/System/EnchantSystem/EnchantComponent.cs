using System;
using System.Collections.Generic;
using UnityEngine;

public class EnchantComponent : MonoBehaviour
{
    public ElementalMana manaPool;

    public event Action<ElementalMana> OnManaChanged;
    public List<MagicSpell> appliedSpells = new();

    private void Awake()
    {
        var item = GetComponent<ItemComponent>();
        if (item != null)
        {
            manaPool = new ElementalMana(item.elementalMana);
        }
    }

    public bool HasEnoughMana(Vector2Int dir)
    {

        if (dir == Vector2Int.up) return manaPool.air >= 1;
        if (dir == Vector2Int.down) return manaPool.earth >= 1;
        if (dir == Vector2Int.left) return manaPool.water >= 1;
        if (dir == Vector2Int.right) return manaPool.fire >= 1;

        return false;
    }

    public void ConsumeMana(Vector2Int dir)
    {
        if (!HasEnoughMana(dir)) return;

        if (dir == Vector2Int.up) manaPool.air -= 1;
        if (dir == Vector2Int.down) manaPool.earth -= 1;
        if (dir == Vector2Int.left) manaPool.water -= 1;
        if (dir == Vector2Int.right) manaPool.fire -= 1;
  
        OnManaChanged?.Invoke(manaPool);
        Debug.Log($"[EnchantComponent] Mana changed: {manaPool}");  // 디버깅용
    }   

    public void CastAllSpells(Transform caster)
    {
        if (appliedSpells == null || appliedSpells.Count == 0) return;

        foreach (var spell in appliedSpells)
        {
            if (spell != null)
            {
                spell.Fire(caster);
            }
        }
    }

    public void ApplyElementalEffects(Transform target)
    {
        foreach (var spell in appliedSpells)
        {
            if (spell.elementalEffectPrefab == null) continue;

            GameObject fx = Instantiate(spell.elementalEffectPrefab, target);
            fx.transform.localPosition = Vector3.zero;

            // 메쉬 찾기: MeshFilter 또는 SkinnedMeshRenderer 우선순위로
            Mesh mesh = null;
            MeshFilter mf = target.GetComponentInChildren<MeshFilter>();
            if (mf != null)
                mesh = mf.sharedMesh;
            else
            {
                var smr = target.GetComponentInChildren<SkinnedMeshRenderer>();
                if (smr != null)
                    mesh = smr.sharedMesh;
            }

            // 파티클 시스템에 메쉬 적용
            var ps = fx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Mesh;
                shape.mesh = mesh;
                shape.alignToDirection = true;
            }
        }
    }
} 