using UnityEngine;
using UnityEngine.UI;

public class SpellHolder : MonoBehaviour
{
    private const string LOG_PREFIX = "[SpellHolder]";
    
    [Header("Spell Data")]
    public MagicSpell spell;
    public Vector2Int coord; // 인첸트 맵상의 좌표
    
    [Header("Visual")]
    [SerializeField] private Image spellImage;
    [SerializeField] private Sprite defaultSprite; // 기본 스프라이트 (마법이 없을 때)
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color emptyColor = Color.gray;

    private void Start()
    {
        if (spellImage == null)
            spellImage = GetComponent<Image>();
            
        UpdateVisual();
    }

    /// <summary>
    /// 마법 설정 및 시각적 업데이트
    /// </summary>
    public void SetSpell(MagicSpell newSpell)
    {
        spell = newSpell;
        UpdateVisual();
        Debug.Log($"{LOG_PREFIX} Spell set to: {(spell != null ? spell.spellName : "None")} at {coord}");
    }

    /// <summary>
    /// 할당된 마법에 따라 시각적 업데이트
    /// </summary>
    public void UpdateVisual()
    {
        if (spellImage == null) return;

        if (spell != null && spell.spellIcon != null)
        {
            // 마법 아이콘 표시
            spellImage.sprite = spell.spellIcon;
            spellImage.color = defaultColor;
            Debug.Log($"{LOG_PREFIX} Updated visual for spell: {spell.spellName}");
        }
        else if (defaultSprite != null)
        {
            // 기본 스프라이트 표시
            spellImage.sprite = defaultSprite;
            spellImage.color = emptyColor;
        }
        else
        {
            // 스프라이트 없이 색상으로만 표시
            spellImage.sprite = null;
            spellImage.color = emptyColor;
        }
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

    /// <summary>
    /// 에디터용 좌표 설정
    /// </summary>
    public void SetCoordinate(Vector2Int newCoord)
    {
        coord = newCoord;
        name = $"SpellHolder_{coord.x}_{coord.y}";
    }
}