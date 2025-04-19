using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class EnchantMapUI : MonoBehaviour
{
    public RectTransform mapContainer;
    public GameObject playerIcon;
    public Vector2Int currentCoord = Vector2Int.zero;
    public float tileSize;
    public GameObject currentItem;
    public TextMeshProUGUI feedbackText;
    public ParticleSystem sparkEffect;
    private EnchantComponent enchant;

    private void OnEnable()
    {
        var table = GetComponentInParent<EnchantTable>();
        if (table != null)
        {
            currentItem = table.objectOnTable;
            enchant = currentItem.GetComponent<EnchantComponent>();
        }

        mapContainer.anchoredPosition = Vector2.zero;
        currentCoord = Vector2Int.zero;
        feedbackText.text = "";
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W)) TryMove(Vector2Int.up);
        if (Input.GetKeyDown(KeyCode.S)) TryMove(Vector2Int.down);
        if (Input.GetKeyDown(KeyCode.A)) TryMove(Vector2Int.left);
        if (Input.GetKeyDown(KeyCode.D)) TryMove(Vector2Int.right);
    }

    void TryMove(Vector2Int dir)
    {
        if (!enchant.HasEnoughMana(dir))
        {
            ShowFeedback("마나가 부족합니다");
            return;
        }

        enchant.ConsumeMana(dir);
        currentCoord += dir;

        Vector2 newPos = -new Vector2(currentCoord.x * tileSize, currentCoord.y * tileSize);
        mapContainer.DOAnchorPos(newPos, 0.2f).SetEase(Ease.OutQuad);
        ShowFeedback("");
    }

    public void ApplyCurrentSpell()
    {
        var tile = FindTileAt(currentCoord);
        if (tile == null)
        {
            ShowFeedback("마법 타일이 없습니다");
            Debug.Log("마법 타일이 없습니다.");
            return;
        }

        var spell = tile.GetComponent<SpellHolder>()?.spell;
        if (spell == null)
        {
            ShowFeedback("유효한 마법이 없습니다");
            return;
        }

        if (!enchant.appliedSpells.Contains(spell))
        {
            enchant.appliedSpells.Add(spell);
            enchant.ApplyElementalEffects(currentItem.transform); // 바로 이펙트 적용!
            sparkEffect.Play();
            ShowFeedback($"마법 '{spell.name}' 부여 완료");
            Debug.Log("마법 부여 완료");
        }
        else
        {
            ShowFeedback($"⚠ 이미 부여된 마법입니다: {spell.name}");
            Debug.Log("이미 부여된 마법입니다.");
        }
    }

    void ShowFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
    }

    GameObject FindTileAt(Vector2Int coord)
    {
        var tiles = mapContainer.GetComponentsInChildren<SpellHolder>();
        foreach (var tile in tiles)
        {
            if (tile.coord == coord)
                return tile.gameObject;
        }
        return null;
    }
}
