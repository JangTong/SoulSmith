using UnityEngine;
using TMPro; // TextMeshPro 네임스페이스

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // 싱글톤 인스턴스

    public int playerGold = 100; // 플레이어의 소지금
    public TextMeshProUGUI goldText; // 소지금을 표시할 TextMeshProUGUI 텍스트
    public TextMeshProUGUI timerText; // 타이머 UI 텍스트
    public TextMeshProUGUI dayText; // 날짜 UI 텍스트

    private float timer = 0f; // 초 단위 타이머
    private int currentDay = 1; // 현재 날짜

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
    }

    private void Update()
    {
        UpdateTimer();
    }

    /// 소지금을 증가시킵니다.
    public void AddGold(int amount)
    {
        playerGold += amount;
        UpdateGoldUI();
    }

    /// 소지금을 감소시킵니다.
    public void SubtractGold(int amount)
    {
        if (playerGold >= amount)
        {
            playerGold -= amount;
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
    }
}
