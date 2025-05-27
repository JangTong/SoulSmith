using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueUIController : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] TextMeshProUGUI nameText, dialogText;
    [SerializeField] Image portrait;
    [SerializeField] Button nextBtn, sellBtn;
    public static event Action OnNextClicked;
    public static event Action OnSellClicked;

    private void Awake()
    {
        nextBtn.onClick.RemoveAllListeners();
        nextBtn.onClick.AddListener(() => OnNextClicked?.Invoke());

        sellBtn.onClick.RemoveAllListeners();
        sellBtn.onClick.AddListener(() => OnSellClicked?.Invoke());
    }

    public void Show(string speaker, string message, Sprite portraitSprite, bool showSell)
    {
        panel.SetActive(true);
        nameText.text = speaker;
        dialogText.text = message;
        portrait.gameObject.SetActive(portraitSprite != null);
        if (portraitSprite != null) portrait.sprite = portraitSprite;
        sellBtn.gameObject.SetActive(showSell);
        Debug.Log($"[UI][Dialog] Show – {speaker}: {message}");
    }

    public void Hide()
    {
        panel.SetActive(false);
        Debug.Log("[UI][Dialog] Hide");
    }
}
