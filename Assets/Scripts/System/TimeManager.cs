using UnityEngine;
using System;

public class TimeManager : MonoBehaviour
{
    private const string LOG_PREFIX = "[TimeManager]";
    public static TimeManager Instance { get; private set; }

    [Header("시간 설정")]
    public float dayDuration = 600f;  // 하루 지속 시간 (초)
    [Range(0, 23)] public int dayStartHour = 6;  // 하루 시작 시간 (24시간 기준)
    
    public float gameTimer { get; private set; }
    public int currentDay { get; private set; } = 1;
    public bool isDayOver { get; private set; }
    public bool isTimePaused { get; private set; }

    // 게임 시간 정보
    public float normalizedTime => Mathf.Clamp01(gameTimer / dayDuration);
    public float gameHours => (normalizedTime * 24f + dayStartHour) % 24f;
    public int hours => Mathf.FloorToInt(gameHours);
    public int minutes => Mathf.FloorToInt((gameHours % 1f) * 60f);

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
        Debug.Log($"{LOG_PREFIX} Day {currentDay} 시작 - {GetFormattedTime()}");
    }

    public void EndDay()
    {
        isDayOver = true;
        OnDayEnded?.Invoke();
        Debug.Log($"{LOG_PREFIX} Day {currentDay} 종료 - {GetFormattedTime()}");
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
        Debug.Log($"{LOG_PREFIX} 시간 {(pause ? "정지" : "재개")} - {GetFormattedTime()}");
    }

    public string GetFormattedTime()
    {
        return $"{hours:00}:{minutes:00}";
    }

    // 실제 시간과 게임 시간의 비율을 반환 (예: 1분당 2.4시간)
    public float GetTimeRatio()
    {
        return (24f * 60f) / dayDuration;
    }

    // 현재 게임 시간을 설정 (0-24 사이의 시간)
    public void SetGameTime(float targetHour)
    {
        targetHour = Mathf.Clamp(targetHour, 0f, 24f);
        float normalizedTargetTime = ((targetHour - dayStartHour + 24f) % 24f) / 24f;
        gameTimer = normalizedTargetTime * dayDuration;
        OnTimerUpdated?.Invoke(gameTimer);
        Debug.Log($"{LOG_PREFIX} 시간 설정: {GetFormattedTime()}");
    }
} 