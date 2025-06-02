using UnityEngine;

public class DayNightSystem : MonoBehaviour
{
    private const string LOG_PREFIX = "[DayNightSystem]";
    public static DayNightSystem Instance { get; private set; }

    [Header("라이트 설정")]
    public Light mainLight;
    
    [Header("시간대별 태양 각도")]
    public float dawn = -30f;     // 새벽 (6시)
    public float noon = 60f;      // 정오 (12시)
    public float dusk = 150f;     // 해질녘 (18시)
    public float night = 210f;    // 밤 (24시)

    [Header("시간대별 라이트 색상")]
    public Color dawnColor = new Color(1f, 0.8f, 0.6f, 1f);   // 새벽
    public Color dayColor = Color.white;                       // 낮
    public Color duskColor = new Color(1f, 0.6f, 0.3f, 1f);   // 해질녘
    public Color nightColor = new Color(0.2f, 0.2f, 0.3f, 1f);// 밤

    [Header("라이트 강도")]
    public float dayIntensity = 1f;     // 낮 시 강도
    public float nightIntensity = 0.1f; // 밤 시 강도

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

        // 메인 라이트가 없으면 찾기
        if (mainLight == null)
        {
            mainLight = FindObjectOfType<Light>();
            if (mainLight == null)
            {
                Debug.LogError($"{LOG_PREFIX} Scene에 메인 라이트가 없습니다!");
            }
        }
    }

    private void Start()
    {
        // 시작 시 현재 시간에 맞는 라이트 설정
        if (TimeManager.Instance != null)
        {
            UpdateDayNightCycle();
        }
    }

    private void Update()
    {
        if (mainLight == null || TimeManager.Instance == null) return;
        UpdateDayNightCycle();
    }

    private void UpdateDayNightCycle()
    {
        float currentHour = TimeManager.Instance.gameHours;
        
        // 각도, 색상, 강도 계산
        float currentAngle;
        Color currentColor;
        float currentIntensity;

        if (currentHour < 6) // 밤 -> 새벽 (0시 ~ 6시)
        {
            float t = (currentHour + 24f - 0f) / 6f;
            currentAngle = Mathf.Lerp(night, dawn, t);
            currentColor = Color.Lerp(nightColor, dawnColor, t);
            currentIntensity = Mathf.Lerp(nightIntensity, dayIntensity * 0.5f, t);
        }
        else if (currentHour < 12) // 새벽 -> 낮 (6시 ~ 12시)
        {
            float t = (currentHour - 6f) / 6f;
            currentAngle = Mathf.Lerp(dawn, noon, t);
            currentColor = Color.Lerp(dawnColor, dayColor, t);
            currentIntensity = Mathf.Lerp(dayIntensity * 0.5f, dayIntensity, t);
        }
        else if (currentHour < 18) // 낮 -> 해질녘 (12시 ~ 18시)
        {
            float t = (currentHour - 12f) / 6f;
            currentAngle = Mathf.Lerp(noon, dusk, t);
            currentColor = Color.Lerp(dayColor, duskColor, t);
            currentIntensity = Mathf.Lerp(dayIntensity, dayIntensity * 0.5f, t);
        }
        else // 해질녘 -> 밤 (18시 ~ 24시)
        {
            float t = (currentHour - 18f) / 6f;
            currentAngle = Mathf.Lerp(dusk, night, t);
            currentColor = Color.Lerp(duskColor, nightColor, t);
            currentIntensity = Mathf.Lerp(dayIntensity * 0.5f, nightIntensity, t);
        }

        // 라이트 업데이트
        mainLight.transform.rotation = Quaternion.Euler(currentAngle, -30f, 0f);
        mainLight.color = currentColor;
        mainLight.intensity = currentIntensity;

        // 디버그 로그 (1시간마다)
        if (Mathf.FloorToInt(currentHour) != Mathf.FloorToInt((currentHour + Time.deltaTime * 24f / TimeManager.Instance.dayDuration) % 24f))
        {
            Debug.Log($"{LOG_PREFIX} 현재 시간: {TimeManager.Instance.GetFormattedTime()} - 각도: {currentAngle:F1}, 강도: {currentIntensity:F2}");
        }
    }
} 