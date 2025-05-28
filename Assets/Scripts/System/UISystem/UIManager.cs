using UnityEngine;


public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("서브 컨트롤러")]
    public HUDController hud;
    public DialogueUIController dialogueUI;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    // HUD
    public void UpdateHUD(int gold, int day, float timer)
        => hud.UpdateHUD(gold, day, timer);

    public void ShowSummary(int earn)
        => hud.ShowSummary(earn);

    public void HideSummary()
        => hud.HideSummary();
        
    public void SetFocusActive(bool active)
        => hud.SetFocusActive(active);

    // 아이템 이름 (이제 HUDController에 합침)
    public void ShowItemName(string name)
        => hud.ShowItemName(name);
    public void HideItemName()
        => hud.HideItemName();

    // 다이얼로그
    public void ShowDialogue(string speaker, string text, Sprite portrait, bool showSell = false)
        => dialogueUI.Show(speaker, text, portrait, showSell);
    public void HideDialogue()
        => dialogueUI.Hide();
}