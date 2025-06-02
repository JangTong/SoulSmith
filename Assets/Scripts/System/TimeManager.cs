using UnityEngine;
using System;

public class TimeManager : MonoBehaviour
{
    private const string LOG_PREFIX = "[TimeManager]";
    public static TimeManager Instance { get; private set; }

    [Header("시간 설정")]
    [SerializeField] private float dayDuration = 300f;  // 하루 지속 시간 (초)
    
    public float gameTimer { get; private set; }
    public int currentDay { get; private set; } = 1;
    public bool isDayOver { get; private set; }
    public bool isTimePaused { get; private set; }

    // 이벤트
    public event Action<float> OnTimerUpdated;
    public event Action<int> OnDayChanged;
    public event Action OnDayStarted;
    public event Action OnDayEnded;

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
        }
    }

    private void Start()
    {
        StartDay();
    }

    private void Update()
    {
        if (!isDayOver && !isTimePaused)
        {
            UpdateGameTimer();
        }
    }

    private void UpdateGameTimer()
    {
        gameTimer += Time.deltaTime;
        OnTimerUpdated?.Invoke(gameTimer);

        // 하루 종료 조건 체크
        if (gameTimer >= dayDuration)
        {
            EndDay();
        }
    }

    public void StartDay()
    {
        isDayOver = false;
        gameTimer = 0f;
        OnDayStarted?.Invoke();
        OnTimerUpdated?.Invoke(gameTimer);
        Debug.Log($"{LOG_PREFIX} Day {currentDay} 시작");
    }

    public void EndDay()
    {
        isDayOver = true;
        OnDayEnded?.Invoke();
        Debug.Log($"{LOG_PREFIX} Day {currentDay} 종료");
    }

    public void StartNextDay()
    {
        currentDay++;
        OnDayChanged?.Invoke(currentDay);
        StartDay();
    }

    public void ToggleTime(bool pause)
    {
        isTimePaused = pause;
        Time.timeScale = pause ? 0f : 1f;
        Debug.Log($"{LOG_PREFIX} 시간 {(pause ? "정지" : "재개")}");
    }

    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(gameTimer / 60);
        int seconds = Mathf.FloorToInt(gameTimer % 60);
        return $"{minutes:00}:{seconds:00}";
    }
} 