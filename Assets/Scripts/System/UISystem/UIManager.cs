using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    private const string LOG_PREFIX = "[UIManager]";
    public static UIManager Instance { get; private set; }

    [Header("서브 컨트롤러")]
    public HUDController hud;
    public DialogueUIController dialogueUI;
    public EffectUIController effectUI;
    public GrindingUIController grindingUI;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private GameObject summaryPanel;
    [SerializeField] private TextMeshProUGUI earningsText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log($"{LOG_PREFIX} 초기화 완료");
        }
        else
        {
            Debug.LogWarning($"{LOG_PREFIX} 중복 인스턴스 감지됨. 제거됩니다.");
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        SubscribeToEvents();
        UpdateAllUI();
        HideSummary();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        // TimeManager 이벤트
        TimeManager.Instance.OnTimerUpdated += UpdateTimerUI;
        TimeManager.Instance.OnDayChanged += UpdateDayUI;

        // EconomyManager 이벤트
        EconomyManager.Instance.OnGoldChanged += UpdateGoldUI;
        EconomyManager.Instance.OnDailyEarningsUpdated += UpdateEarningsUI;

        // GameStateManager 이벤트
        GameStateManager.Instance.OnDaySummaryStarted += ShowSummary;
        GameStateManager.Instance.OnDaySummaryEnded += HideSummary;
    }

    private void UnsubscribeFromEvents()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTimerUpdated -= UpdateTimerUI;
            TimeManager.Instance.OnDayChanged -= UpdateDayUI;
        }

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnGoldChanged -= UpdateGoldUI;
            EconomyManager.Instance.OnDailyEarningsUpdated -= UpdateEarningsUI;
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnDaySummaryStarted -= ShowSummary;
            GameStateManager.Instance.OnDaySummaryEnded -= HideSummary;
        }
    }

    private void UpdateAllUI()
    {
        UpdateGoldUI(EconomyManager.Instance.playerGold);
        UpdateDayUI(TimeManager.Instance.currentDay);
        UpdateTimerUI(TimeManager.Instance.gameTimer);
        UpdateEarningsUI(EconomyManager.Instance.dailyEarnings);
    }

    // HUD 업데이트
    public void UpdateHUD(int gold, int day, float timer)
    {
        if (hud != null)
        {
            hud.UpdateHUD(gold, day, timer);
        }
    }

    private void UpdateGoldUI(int gold)
    {
        if (goldText != null)
        {
            goldText.text = $"{gold}";
        }
    }

    private void UpdateTimerUI(float time)
    {
        if (timerText != null)
        {
            timerText.text = TimeManager.Instance.GetFormattedTime();
        }
    }

    private void UpdateDayUI(int day)
    {
        if (dayText != null)
        {
            dayText.text = $"Day {day}";
        }
    }

    private void UpdateEarningsUI(int earnings)
    {
        if (earningsText != null && summaryPanel != null && summaryPanel.activeSelf)
        {
            earningsText.text = $"MONEY EARNED: {earnings} Gold";
        }
    }

    // 요약 패널
    public void ShowSummary()
    {
        if (summaryPanel != null)
        {
            summaryPanel.SetActive(true);
            PlayerController.Instance.ToggleUI(true);
            UpdateEarningsUI(EconomyManager.Instance.dailyEarnings);
        }
    }

    public void HideSummary()
    {
        if (summaryPanel != null)
        {
            summaryPanel.SetActive(false);
            PlayerController.Instance.ToggleUI(false);
        }
    }

    // 포커스 UI
    public void SetFocusActive(bool active)
    {
        if (hud != null)
        {
            hud.SetFocusActive(active);
        }
    }

    // 아이템 이름 UI
    public void ShowItemName(string name)
    {
        if (hud != null)
        {
            hud.ShowItemName(name);
        }
    }

    public void HideItemName()
    {
        if (hud != null)
        {
            hud.HideItemName();
        }
    }

    // 다이얼로그 UI
    public void ShowDialogue(string speaker, string text, Sprite portrait, bool showSell = false)
    {
        if (dialogueUI != null)
        {
            dialogueUI.Show(speaker, text, portrait, showSell);
        }
    }

    public void HideDialogue()
    {
        if (dialogueUI != null)
        {
            dialogueUI.Hide();
        }
    }

    // 화면 효과 UI
    public void FadeIn()
    {
        if (effectUI != null)
        {
            effectUI.FadeIn();
        }
    }

    public void FadeOut()
    {
        if (effectUI != null)
        {
            effectUI.FadeOut();
        }
    }
    
    public void FadeIn(float duration)
    {
        if (effectUI != null)
        {
            effectUI.FadeIn(duration);
        }
    }

    public void FadeOut(float duration)
    {
        if (effectUI != null)
        {
            effectUI.FadeOut(duration);
        }
    }

    public void SetDarkImmediate()
    {
        if (effectUI != null)
        {
            effectUI.SetDarkImmediate();
        }
    }

    public void SetBrightImmediate()
    {
        if (effectUI != null)
        {
            effectUI.SetBrightImmediate();
        }
    }
    
    // 연마 UI
    public void OpenGrindingUI(GameObject weapon, float weaponAttack)
    {
        if (grindingUI != null)
        {
            GrindingWheel wheel = FindObjectOfType<GrindingWheel>();
            if (wheel != null)
            {
                grindingUI.OpenUI(weapon, weaponAttack, wheel);
                Debug.Log($"{LOG_PREFIX} 연마 UI 오픈");
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX} GrindingWheel을 찾을 수 없습니다.");
            }
        }
    }
    
    public void CloseGrindingUI()
    {
        if (grindingUI != null)
        {
            grindingUI.CloseUI();
            Debug.Log($"{LOG_PREFIX} 연마 UI 닫기");
        }
    }
}