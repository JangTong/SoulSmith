using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 마법 부여 시스템을 위한 컴포넌트
/// 아이템의 마나 관리 및 마법 효과 적용을 담당
/// </summary>
public class EnchantComponent : MonoBehaviour
{
    private const string LOG_PREFIX = "[EnchantComponent]";
    
    [Header("Mana Pool")]
    public ElementalMana manaPool;
    
    [Header("Applied Spells")]
    public List<MagicSpell> appliedSpells = new List<MagicSpell>();

    // 이벤트
    public event Action<ElementalMana> OnManaChanged;

    private void Awake()
    {
        InitializeManaPool();
    }

    /// <summary>
    /// 아이템 컴포넌트에서 마나풀 초기화
    /// </summary>
    private void InitializeManaPool()
    {
        var item = GetComponent<ItemComponent>();
        if (item?.elementalMana != null)
        {
            manaPool = new ElementalMana(item.elementalMana);
            Debug.Log($"{LOG_PREFIX} Mana pool initialized: {manaPool}");
        }
        else
        {
            Debug.LogWarning($"{LOG_PREFIX} No ItemComponent or ElementalMana found");
        }
    }

    /// <summary>
    /// 방향 이동에 필요한 마나가 충분한지 확인
    /// </summary>
    public bool HasEnoughMana(Vector2Int direction)
    {
        return GetRequiredMana(direction) <= GetAvailableMana(direction);
    }

    /// <summary>
    /// 방향 이동에 필요한 마나 소모
    /// </summary>
    public void ConsumeMana(Vector2Int direction)
    {
        if (!HasEnoughMana(direction)) 
        {
            Debug.LogWarning($"{LOG_PREFIX} Insufficient mana for direction {direction}");
            return;
        }

        // 방향별 마나 소모
        switch (direction.x, direction.y)
        {
            case (0, 1):   // 위 (Air)
                manaPool.air -= 1;
                break;
            case (0, -1):  // 아래 (Earth)
                manaPool.earth -= 1;
                break;
            case (-1, 0):  // 왼쪽 (Water)
                manaPool.water -= 1;
                break;
            case (1, 0):   // 오른쪽 (Fire)
                manaPool.fire -= 1;
                break;
        }

        OnManaChanged?.Invoke(manaPool);
    }

    /// <summary>
    /// 벽 파괴용 마나 소모
    /// </summary>
    public void ConsumeWallBreakMana(ElementalMana requiredMana)
    {
        if (requiredMana == null) return;

        // 마나 소모
        manaPool.fire -= requiredMana.fire;
        manaPool.water -= requiredMana.water;
        manaPool.earth -= requiredMana.earth;
        manaPool.air -= requiredMana.air;

        // 음수 방지
        manaPool.fire = Mathf.Max(0, manaPool.fire);
        manaPool.water = Mathf.Max(0, manaPool.water);
        manaPool.earth = Mathf.Max(0, manaPool.earth);
        manaPool.air = Mathf.Max(0, manaPool.air);

        OnManaChanged?.Invoke(manaPool);
    }

    /// <summary>
    /// 적용된 모든 마법 시전
    /// </summary>
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

    /// <summary>
    /// 방향에 따른 필요 마나량 반환
    /// </summary>
    private int GetRequiredMana(Vector2Int direction)
    {
        return 1; // 기본 이동 마나 비용
    }

    /// <summary>
    /// 방향에 따른 사용 가능한 마나량 반환
    /// </summary>
    private int GetAvailableMana(Vector2Int direction)
    {
        return direction switch
        {
            { x: 0, y: 1 } => manaPool.air,      // 위
            { x: 0, y: -1 } => manaPool.earth,   // 아래
            { x: -1, y: 0 } => manaPool.water,   // 왼쪽
            { x: 1, y: 0 } => manaPool.fire,     // 오른쪽
            _ => 0
        };
    }
} 