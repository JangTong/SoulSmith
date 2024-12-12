using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // TextMeshPro 네임스페이스

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // 싱글톤 인스턴스

    public int playerGold = 100; // 플레이어의 소지금
    public TextMeshProUGUI goldText; // 소지금을 표시할 TextMeshProUGUI 텍스트
    public TextMeshProUGUI timerText; // 타이머 UI 텍스트
    public TextMeshProUGUI dayText; // 날짜 UI 텍스트
    public GameObject summaryPanel; // 하루 종료 결과 창 패널
    public TextMeshProUGUI earningsText; // 하루 번 돈 텍스트

    private float timer = 0f; // 초 단위 타이머
    private int currentDay = 1; // 현재 날짜
    private int dailyEarnings = 0; // 하루 동안 번 돈
    private bool isDayOver = false; // 하루 종료 여부

    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 변경되어도 유지
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateGoldUI(); // 초기 소지금 표시
        UpdateDayUI(); // 초기 날짜 표시
        HideSummaryPanel(); // 결과 창 숨기기
    }

    private void Update()
    {
        if (!isDayOver)
        {
            UpdateTimer();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

    }

    /// 소지금을 증가시킵니다.
    public void AddGold(int amount)
    {
        playerGold += amount;
        dailyEarnings += amount; // 하루 번 돈 증가
        UpdateGoldUI();
    }

    /// 소지금을 감소시킵니다.
    public void SubtractGold(int amount)
    {
        if (playerGold >= amount)
        {
            playerGold -= amount;
            dailyEarnings -= amount;
            UpdateGoldUI();
        }
        else
        {
            Debug.LogWarning("소지금이 부족합니다.");
        }
    }

    /// 소지금을 표시하는 UI를 업데이트합니다.
    private void UpdateGoldUI()
    {
        if (goldText != null)
        {
            goldText.text = $"{playerGold}";
        }
        else
        {
            Debug.LogError("goldText가 설정되지 않았습니다.");
        }
    }

    /// 날짜 UI 업데이트
    private void UpdateDayUI()
    {
        if (dayText != null)
        {
            dayText.text = $"Day {currentDay}";
        }
        else
        {
            Debug.LogError("dayText가 설정되지 않았습니다.");
        }
    }

    /// 타이머 업데이트
    private void UpdateTimer()
    {
        timer += Time.deltaTime;

        int minutes = Mathf.FloorToInt(timer / 60);
        int seconds = Mathf.FloorToInt(timer % 60);

        if (timerText != null)
        {
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
        else
        {
            Debug.LogError("timerText가 설정되지 않았습니다.");
        }

        // 하루 종료 조건 (예: 1분마다 하루 종료)
        if (timer >= 180)
        {
            EndDay();
        }
    }

    /// 하루 종료 처리
    public void EndDay()
    {
        isDayOver = true; // 하루 종료
        timer = 0; // 타이머 초기화

        // 결과 창 표시
        ShowSummaryPanel();
    }

    /// 다음 날 시작
    public void StartNextDay()
    {
        isDayOver = false; // 하루 재개
        dailyEarnings = 0; // 번 돈 초기화
        currentDay++; // 다음 날로 이동
        UpdateDayUI(); // 날짜 UI 업데이트
        HideSummaryPanel(); // 결과 창 숨기기
        Debug.Log("새로운 하루가 시작되었습니다.");
    }

    /// 결과 창 표시
    private void ShowSummaryPanel()
    {
        if (summaryPanel != null)
        {
            summaryPanel.SetActive(true);
            PlayerController.Instance.ToggleUI(true);
        }

        if (earningsText != null)
        {
            earningsText.text = $"MONEY EARNED: {dailyEarnings} Gold";
        }
    }

    /// 결과 창 숨기기
    private void HideSummaryPanel()
    {
        if (summaryPanel != null)
        {
            summaryPanel.SetActive(false);
        }
    }
}
