using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class EnchantMapUI : MonoBehaviour
{
    [Header("Map & Feedback")]
    public RectTransform mapContainer;
    public GameObject playerIcon;
    public float tileSize = 100f;
    public TextMeshProUGUI feedbackText;
    public ParticleSystem sparkEffect;

    [Header("Mana Bars (Fill Type)")]
    public Image fireManaBar;
    public Image waterManaBar;
    public Image earthManaBar;
    public Image airManaBar;

    private EnchantComponent enchant;
    private Vector2Int currentCoord;
    private int initFire, initWater, initEarth, initAir;

    private void OnEnable()
    {
        Debug.Log("[EnchantMapUI] OnEnable: Initializing EnchantMapUI");
        var table = GetComponentInParent<EnchantTable>();
        if (table == null)
        {
            Debug.LogError("[EnchantMapUI] EnchantTable not found in parent");
            return;
        }

        // Ensure the EnchantComponent is retrieved from the object on the table
        if (table.objectOnTable == null)
        {
            Debug.LogError("[EnchantMapUI] objectOnTable is null on EnchantTable");
            return;
        }

        enchant = table.objectOnTable.GetComponent<EnchantComponent>();
        if (enchant == null)
        {
            enchant = table.objectOnTable.AddComponent<EnchantComponent>();
            Debug.LogWarning("[EnchantMapUI] EnchantComponent was missing on objectOnTable – added dynamically.");
        }

        // Subscribe to mana change event
        enchant.OnManaChanged += UpdateManaUI;

        // Cache initial mana values for normalization
        initFire  = enchant.manaPool.fire;
        initWater = enchant.manaPool.water;
        initEarth = enchant.manaPool.earth;
        initAir   = enchant.manaPool.air;

        currentCoord = Vector2Int.zero;
        if (mapContainer != null)
            mapContainer.anchoredPosition = Vector2.zero;

        feedbackText.text = string.Empty;
        // Initialize UI with current mana
        UpdateManaUI(enchant.manaPool);
    }

    private void OnDisable()
    {
        Debug.Log("[EnchantMapUI] OnDisable: Resetting EnchantMapUI state");
        if (enchant != null)
            enchant.OnManaChanged -= UpdateManaUI;

        currentCoord = Vector2Int.zero;
        if (mapContainer != null)
            mapContainer.anchoredPosition = Vector2.zero;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W)) TryMove(Vector2Int.up);
        if (Input.GetKeyDown(KeyCode.S)) TryMove(Vector2Int.down);
        if (Input.GetKeyDown(KeyCode.A)) TryMove(Vector2Int.left);
        if (Input.GetKeyDown(KeyCode.D)) TryMove(Vector2Int.right);
    }

    private void TryMove(Vector2Int dir)
    {
        if (enchant == null)
        {
            Debug.LogError("[EnchantMapUI] Cannot move: EnchantComponent is null");
            return;
        }
        if (!enchant.HasEnoughMana(dir))
        {
            ShowFeedback("마나가 부족합니다");
            Debug.Log($"[EnchantMapUI] Not enough mana to move {dir}");
            return;
        }

        enchant.ConsumeMana(dir);
        currentCoord += dir;

        Vector2 newPos = -new Vector2(currentCoord.x * tileSize, currentCoord.y * tileSize);
        mapContainer.DOAnchorPos(newPos, 0.2f).SetEase(Ease.OutQuad);

        ShowFeedback(string.Empty);
        Debug.Log($"[EnchantMapUI] Moved {dir} to {currentCoord}, Mana now Fire:{enchant.manaPool.fire}/{initFire}, Water:{enchant.manaPool.water}/{initWater}, Earth:{enchant.manaPool.earth}/{initEarth}, Air:{enchant.manaPool.air}/{initAir}");
    }

    public void ApplyCurrentSpell()
    {
        if (enchant == null)
        {
            Debug.LogError("[EnchantMapUI] Cannot apply spell: EnchantComponent is null");
            ShowFeedback("예외: EnchantComponent 없음");
            return;
        }

        var tileObj = FindTileAt(currentCoord);
        if (tileObj == null)
        {
            ShowFeedback("마법 타일이 없습니다");
            Debug.Log($"[EnchantMapUI] ApplyCurrentSpell failed: No tile at {currentCoord}");
            return;
        }

        var spellHolder = tileObj.GetComponent<SpellHolder>();
        if (spellHolder == null || spellHolder.spell == null)
        {
            ShowFeedback("유효한 마법이 없습니다");
            Debug.Log($"[EnchantMapUI] ApplyCurrentSpell failed: No spell on tile at {currentCoord}");
            return;
        }

        var spell = spellHolder.spell;
        if (!enchant.appliedSpells.Contains(spell))
        {
            enchant.appliedSpells.Add(spell);
            enchant.ApplyElementalEffects(tileObj.transform);
            sparkEffect.Play();
            ShowFeedback($"마법 '{spell.name}' 부여 완료");
            Debug.Log($"[EnchantMapUI] Applied spell: {spell.name} at {currentCoord}");
        }
        else
        {
            ShowFeedback($"⚠ 이미 부여된 마법입니다: {spell.name}");
            Debug.Log($"[EnchantMapUI] Spell '{spell.name}' already applied");
        }
    }

    private void UpdateManaUI(ElementalMana mana)
    {
        if (enchant == null) return;
        if (initFire  > 0) fireManaBar.fillAmount  = mana.fire  / (float)initFire;
        if (initWater > 0) waterManaBar.fillAmount = mana.water / (float)initWater;
        if (initEarth > 0) earthManaBar.fillAmount = mana.earth / (float)initEarth;
        if (initAir   > 0) airManaBar.fillAmount   = mana.air   / (float)initAir;
        Debug.Log($"[EnchantMapUI] Mana UI Updated - Fire:{mana.fire}/{initFire}, Water:{mana.water}/{initWater}, Earth:{mana.earth}/{initEarth}, Air:{mana.air}/{initAir}");
    }

    private void ShowFeedback(string message)
    {
        feedbackText.text = message;
    }

    private GameObject FindTileAt(Vector2Int coord)
    {
        foreach (var tile in mapContainer.GetComponentsInChildren<SpellHolder>())
            if (tile.coord == coord)
                return tile.gameObject;
        return null;
    }
}
