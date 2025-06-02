using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    private const string LOG_PREFIX = "[ItemInteractionDetector]";

    [Header("HUD 기본")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Summary Panel")]
    [SerializeField] private GameObject summaryPanel;
    [SerializeField] private TextMeshProUGUI earningsText;

    [Header("Interaction Item Name")]
    [SerializeField] private TextMeshProUGUI itemNameText;
    
    [Header("Reticle / Focus")]
    [SerializeField] private GameObject focusImage;

    // 기존 HUD 업데이트
    public void UpdateHUD(int gold, int day, float timer)
    {
        goldText.text = gold.ToString();
        dayText.text = $"Day {day}";
        timerText.text = $"{Mathf.FloorToInt(timer / 60):00}:{Mathf.FloorToInt(timer % 60):00}";
        Debug.Log($"[UI][HUD] UpdateHUD – Gold:{gold}, Day:{day}, Timer:{timer:F1}");
    }

    // Summary
    public void ShowSummary(int earn)
    {
        summaryPanel.SetActive(true);
        earningsText.text = $"MONEY EARNED: {earn} Gold";
        Debug.Log($"[UI][HUD] ShowSummary – Earned:{earn}");
    }

    public void HideSummary()
    {
        summaryPanel.SetActive(false);
        Debug.Log("[UI][HUD] HideSummary");
    }

    // Interaction: 아이템 이름 표시
    public void ShowItemName(string name)
    {
        itemNameText.text = name;
        itemNameText.enabled = true;
        Debug.Log($"[UI][HUD] ShowItemName – {name}");
    }

    public void HideItemName()
    {
        itemNameText.enabled = false;
        Debug.Log("[UI][HUD] HideItemName");
    }
    
    public void SetFocusActive(bool active)
    {
        if (focusImage != null)
            focusImage.SetActive(active);
        Debug.Log($"[UI][HUD] FocusImage Active: {active}");
    }
}
