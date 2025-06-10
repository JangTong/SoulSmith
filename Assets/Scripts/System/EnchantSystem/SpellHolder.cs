using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 마법 타일을 관리하는 컴포넌트
/// 마법 데이터와 시각적 표현을 담당
/// </summary>
public class SpellHolder : MonoBehaviour
{
    private const string LOG_PREFIX = "[SpellHolder]";
    
    [Header("Spell Data")]
    public MagicSpell spell;
    public Vector2Int coord; // 인첸트 맵상의 좌표
    
    [Header("Visual Settings")]
    [SerializeField] private Image spellImage;
    [SerializeField] private Sprite defaultSprite; // 기본 스프라이트 (마법이 없을 때)
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color emptyColor = Color.gray;

    // 캐싱된 참조
    private Image cachedSpellImage;

    private void Awake()
    {
        // Image 컴포넌트 캐싱
        cachedSpellImage = spellImage != null ? spellImage : GetComponent<Image>();
    }

    private void Start()
    {
        UpdateVisual();
    }

    /// <summary>
    /// 마법 설정 및 시각적 업데이트
    /// </summary>
    public void SetSpell(MagicSpell newSpell)
    {
        spell = newSpell;
        UpdateVisual();
        
        string spellName = spell != null ? spell.spellName : "None";
        Debug.Log($"{LOG_PREFIX} Spell set to: {spellName} at {coord}");
    }

    /// <summary>
    /// 할당된 마법에 따라 시각적 업데이트
    /// </summary>
    public void UpdateVisual()
    {
        if (cachedSpellImage == null) return;

        if (HasValidSpell())
        {
            ApplySpellVisual();
        }
        else
        {
            ApplyEmptyVisual();
        }
    }

    /// <summary>
    /// 에디터용 좌표 설정
    /// </summary>
    public void SetCoordinate(Vector2Int newCoord)
    {
        coord = newCoord;
        name = $"SpellHolder_{coord.x}_{coord.y}";
    }

    /// <summary>
    /// 유효한 마법이 있는지 확인
    /// </summary>
    private bool HasValidSpell()
    {
        return spell != null && spell.spellIcon != null;
    }

    /// <summary>
    /// 마법 아이콘 적용
    /// </summary>
    private void ApplySpellVisual()
    {
        cachedSpellImage.sprite = spell.spellIcon;
        cachedSpellImage.color = defaultColor;
    }

    /// <summary>
    /// 빈 상태 시각적 적용
    /// </summary>
    private void ApplyEmptyVisual()
    {
        if (defaultSprite != null)
        {
            // 기본 스프라이트 표시
            cachedSpellImage.sprite = defaultSprite;
        }
        else
        {
            // 스프라이트 없이 색상으로만 표시
            cachedSpellImage.sprite = null;
        }
        
        cachedSpellImage.color = emptyColor;
    }

    /// <summary>
    /// Inspector에서 값 변경 시 즉시 반영
    /// </summary>
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdateVisual();
        }
    }
}