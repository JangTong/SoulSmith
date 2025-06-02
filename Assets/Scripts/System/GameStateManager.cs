using UnityEngine;
using System;

public class GameStateManager : MonoBehaviour
{
    private const string LOG_PREFIX = "[GameStateManager]";
    public static GameStateManager Instance { get; private set; }

    public enum GameState
    {
        Playing,
        Paused,
        DaySummary,
        GameOver
    }

    public GameState currentState { get; private set; } = GameState.Playing;
    
    // 이벤트
    public event Action<GameState> OnGameStateChanged;
    public event Action OnGamePaused;
    public event Action OnGameResumed;
    public event Action OnDaySummaryStarted;
    public event Action OnDaySummaryEnded;
    public event Action OnGameOver;

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
        // TimeManager 이벤트 구독
        TimeManager.Instance.OnDayEnded += OnDayEnded;
        
        // 초기 상태 설정
        SetGameState(GameState.Playing);
    }

    private void OnDestroy()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayEnded -= OnDayEnded;
        }
    }

    private void OnDayEnded()
    {
        SetGameState(GameState.DaySummary);
    }

    public void SetGameState(GameState newState)
    {
        if (currentState == newState) return;

        GameState previousState = currentState;
        currentState = newState;

        // 상태 변경에 따른 처리
        HandleStateTransition(previousState, newState);
        
        // 이벤트 발생
        OnGameStateChanged?.Invoke(newState);
        
        Debug.Log($"{LOG_PREFIX} 게임 상태 변경: {previousState} -> {newState}");
    }

    private void HandleStateTransition(GameState previousState, GameState newState)
    {
        switch (newState)
        {
            case GameState.Playing:
                TimeManager.Instance.ToggleTime(false);
                OnGameResumed?.Invoke();
                break;

            case GameState.Paused:
                TimeManager.Instance.ToggleTime(true);
                OnGamePaused?.Invoke();
                break;

            case GameState.DaySummary:
                TimeManager.Instance.ToggleTime(true);
                OnDaySummaryStarted?.Invoke();
                break;

            case GameState.GameOver:
                TimeManager.Instance.ToggleTime(true);
                OnGameOver?.Invoke();
                break;
        }
    }

    public void PauseGame()
    {
        if (currentState == GameState.Playing)
        {
            SetGameState(GameState.Paused);
        }
    }

    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
        {
            SetGameState(GameState.Playing);
        }
    }

    public void StartNextDay()
    {
        if (currentState == GameState.DaySummary)
        {
            OnDaySummaryEnded?.Invoke();
            TimeManager.Instance.StartNextDay();
            SetGameState(GameState.Playing);
        }
    }

    public void TriggerGameOver()
    {
        SetGameState(GameState.GameOver);
    }
} 