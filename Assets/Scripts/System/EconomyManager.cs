using UnityEngine;
using System;

public class EconomyManager : MonoBehaviour
{
    private const string LOG_PREFIX = "[EconomyManager]";
    public static EconomyManager Instance { get; private set; }

    [Header("초기 설정")]
    [SerializeField] private int startingGold = 100;

    public int playerGold { get; private set; }
    public int dailyEarnings { get; private set; }

    // 이벤트
    public event Action<int> OnGoldChanged;
    public event Action<int> OnDailyEarningsUpdated;

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
        InitializeEconomy();
        
        // TimeManager 이벤트 구독
        TimeManager.Instance.OnDayStarted += OnDayStarted;
    }

    private void OnDestroy()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayStarted -= OnDayStarted;
        }
    }

    private void InitializeEconomy()
    {
        playerGold = startingGold;
        dailyEarnings = 0;
        OnGoldChanged?.Invoke(playerGold);
        OnDailyEarningsUpdated?.Invoke(dailyEarnings);
        Debug.Log($"{LOG_PREFIX} 경제 시스템 초기화: 시작 골드 {startingGold}");
    }

    private void OnDayStarted()
    {
        dailyEarnings = 0;
        OnDailyEarningsUpdated?.Invoke(dailyEarnings);
        Debug.Log($"{LOG_PREFIX} 새로운 날 시작: 일일 수익 초기화");
    }

    public void AddGold(int amount)
    {
        if (amount < 0)
        {
            Debug.LogError($"{LOG_PREFIX} AddGold에 음수 값이 전달됨: {amount}");
            return;
        }

        playerGold += amount;
        dailyEarnings += amount;
        
        OnGoldChanged?.Invoke(playerGold);
        OnDailyEarningsUpdated?.Invoke(dailyEarnings);
        
        Debug.Log($"{LOG_PREFIX} {amount} 골드 추가됨. 현재 골드: {playerGold}, 일일 수익: {dailyEarnings}");
    }

    public bool SubtractGold(int amount)
    {
        if (amount < 0)
        {
            Debug.LogError($"{LOG_PREFIX} SubtractGold에 음수 값이 전달됨: {amount}");
            return false;
        }

        if (playerGold < amount)
        {
            Debug.LogWarning($"{LOG_PREFIX} 골드가 부족합니다. 필요: {amount}, 보유: {playerGold}");
            return false;
        }

        playerGold -= amount;
        dailyEarnings -= amount;
        
        OnGoldChanged?.Invoke(playerGold);
        OnDailyEarningsUpdated?.Invoke(dailyEarnings);
        
        Debug.Log($"{LOG_PREFIX} {amount} 골드 차감됨. 현재 골드: {playerGold}, 일일 수익: {dailyEarnings}");
        return true;
    }

    public bool HasEnoughGold(int amount)
    {
        return playerGold >= amount;
    }
} 