using UnityEngine;
using OccaSoftware.SuperSimpleSkybox.Runtime;

public class DayNightSystem : MonoBehaviour
{
    private const string LOG_PREFIX = "[DayNightSystem]";
    public static DayNightSystem Instance { get; private set; }
    
    [Header("라이트 설정")]
    public Light mainLight;
    
    [Header("SuperSimpleSkybox 통합")]
    [SerializeField] private Sun sunComponent;
    [SerializeField] private Moon moonComponent;
    [SerializeField] private Material skyboxMaterial;
    
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

        InitializeSystem();
    }

    private void InitializeSystem()
    {
        // 메인 라이트가 없으면 찾기
        if (mainLight == null)
        {
            mainLight = FindObjectOfType<Light>();
            if (mainLight == null)
            {
                Debug.LogError($"{LOG_PREFIX} Scene에 메인 라이트가 없습니다!");
            }
        }

        // SuperSimpleSkybox 컴포넌트 자동 탐지
        if (sunComponent == null)
        {
            sunComponent = FindObjectOfType<Sun>();
        }
        if (moonComponent == null)
        {
            moonComponent = FindObjectOfType<Moon>();
        }

        // 스카이박스 머티리얼 자동 설정
        if (skyboxMaterial == null)
        {
            skyboxMaterial = RenderSettings.skybox;
        }

        // Sun/Moon 컴포넌트 자동 회전 비활성화 (우리가 제어)
        if (sunComponent != null)
        {
            sunComponent.RotationsPerHour = 0f;
            Debug.Log($"{LOG_PREFIX} Sun 컴포넌트 연동 완료");
        }
        if (moonComponent != null)
        {
            moonComponent.RotationsPerHour = 0f;
            Debug.Log($"{LOG_PREFIX} Moon 컴포넌트 연동 완료");
        }

        Debug.Log($"{LOG_PREFIX} 시스템 초기화 완료");
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

        // 메인 라이트 업데이트
        mainLight.transform.rotation = Quaternion.Euler(currentAngle, -30f, 0f);
        mainLight.color = currentColor;
        mainLight.intensity = currentIntensity;

        // SuperSimpleSkybox 업데이트
        UpdateSunMoonComponents(currentAngle, currentIntensity);
        UpdateSkyboxProperties(currentHour);

        // 디버그 로그 (1시간마다)
        if (Mathf.FloorToInt(currentHour) != Mathf.FloorToInt((currentHour + Time.deltaTime * 24f / TimeManager.Instance.dayDuration) % 24f))
        {
            Debug.Log($"{LOG_PREFIX} 현재 시간: {TimeManager.Instance.GetFormattedTime()} - 각도: {currentAngle:F1}, 강도: {currentIntensity:F2}");
        }
    }

    /// <summary>
    /// Sun/Moon 컴포넌트 업데이트
    /// </summary>
    private void UpdateSunMoonComponents(float sunAngle, float lightIntensity)
    {
        if (sunComponent != null)
        {
            // Sun 위치 및 회전 설정
            sunComponent.transform.rotation = Quaternion.Euler(sunAngle, -30f, 0f);
            
            // **중요: Sun 컴포넌트의 자동 라이트 강도 활성화**
            sunComponent.AutomaticLightIntensity = true;
            sunComponent.MaximumLightIntensity = lightIntensity;
            
            // **Sun의 Directional Light 직접 제어**
            Light sunLight = sunComponent.GetComponent<Light>();
            if (sunLight != null)
            {
                sunLight.intensity = lightIntensity;
                sunLight.transform.rotation = sunComponent.transform.rotation;
                
                // 낮에만 Sun Light 활성화, 밤에는 비활성화
                sunLight.enabled = lightIntensity > 0.2f;
            }
        }

        if (moonComponent != null)
        {
            // Moon은 Sun의 반대편에 위치 (180도 차이)
            float moonAngle = sunAngle + 180f;
            moonComponent.transform.rotation = Quaternion.Euler(moonAngle, -30f, 0f);
            
            // 밤에 달이 더 밝게 보이도록 설정
            float moonIntensity = lightIntensity < 0.5f ? 0.3f : 0.1f;
            moonComponent.AutomaticLightIntensity = true;
            moonComponent.MaximumLightIntensity = moonIntensity;
            
            // **Moon의 Directional Light 직접 제어**
            Light moonLight = moonComponent.GetComponent<Light>();
            if (moonLight != null)
            {
                moonLight.intensity = moonIntensity;
                moonLight.transform.rotation = moonComponent.transform.rotation;
                
                // 밤에만 Moon Light 활성화
                moonLight.enabled = lightIntensity < 0.5f;
            }
        }
    }

    /// <summary>
    /// 스카이박스 프로퍼티 업데이트 (노을 효과 등)
    /// </summary>
    private void UpdateSkyboxProperties(float currentHour)
    {
        if (skyboxMaterial == null) return;

        // **중요: Sun 방향을 스카이박스에 전달 (해가 보이게 하는 핵심)**
        if (sunComponent != null)
        {
            Vector3 sunDirection = -sunComponent.transform.forward;
            
            if (skyboxMaterial.HasProperty("_SunDirection"))
            {
                skyboxMaterial.SetVector("_SunDirection", sunDirection);
            }
            
            // Sun 크기와 강도 설정 (해가 잘 보이도록)
            if (skyboxMaterial.HasProperty("_SunAngularDiameter"))
            {
                skyboxMaterial.SetFloat("_SunAngularDiameter", 0.8f); // 해 크기
            }
            
            if (skyboxMaterial.HasProperty("_SunIntensity"))
            {
                float sunIntensity = IsDay() ? 5.0f : 0.1f; // 낮에 밝게
                skyboxMaterial.SetFloat("_SunIntensity", sunIntensity);
            }
            
            if (skyboxMaterial.HasProperty("_SunColor"))
            {
                skyboxMaterial.SetColor("_SunColor", Color.white);
            }
        }

        // 일출/일몰 시간대 노을 효과
        float sunsetIntensity = CalculateSunsetIntensity(currentHour);
        
        // 스카이박스 프로퍼티 설정
        if (skyboxMaterial.HasProperty("_SunsetIntensity"))
        {
            skyboxMaterial.SetFloat("_SunsetIntensity", sunsetIntensity);
        }
        
        // 하늘 색상 강도 조절
        if (skyboxMaterial.HasProperty("_Exposure"))
        {
            float exposure = Mathf.Lerp(0.8f, 1.3f, sunsetIntensity);
            skyboxMaterial.SetFloat("_Exposure", exposure);
        }
    }

    /// <summary>
    /// 노을 강도 계산 (일출/일몰 시간대에 강해짐)
    /// </summary>
    private float CalculateSunsetIntensity(float currentHour)
    {
        // 일출 (5-7시)과 일몰 (17-19시) 시간대에 노을 효과
        if (currentHour >= 5f && currentHour <= 7f)
        {
            // 일출 노을
            float t = Mathf.Abs(currentHour - 6f) / 1f; // 6시를 중심으로 1시간씩
            return Mathf.Lerp(2.0f, 0.5f, t);
        }
        else if (currentHour >= 17f && currentHour <= 19f)
        {
            // 일몰 노을
            float t = Mathf.Abs(currentHour - 18f) / 1f; // 18시를 중심으로 1시간씩
            return Mathf.Lerp(2.5f, 0.5f, t);
        }
        else
        {
            return 0.5f; // 기본 값
        }
    }

    /// <summary>
    /// 현재 낮인지 밤인지 반환
    /// </summary>
    public bool IsDay()
    {
        if (TimeManager.Instance == null) return true;
        float currentHour = TimeManager.Instance.gameHours;
        return currentHour >= 6f && currentHour <= 18f;
    }

    /// <summary>
    /// 특정 시간으로 즉시 설정
    /// </summary>
    public void SetTime(float hour)
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.SetGameTime(hour);
            UpdateDayNightCycle(); // 즉시 업데이트
        }
    }

    /// <summary>
    /// Sun/Moon 오브젝트를 자동으로 생성하고 설정
    /// </summary>
    public void CreateSunMoonObjects()
    {
        // Sun 오브젝트 생성
        if (sunComponent == null)
        {
            GameObject sunObj = new GameObject("Sun");
            Light sunLight = sunObj.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.intensity = dayIntensity;
            sunLight.color = Color.white;
            sunLight.shadows = LightShadows.Soft;
            sunLight.enabled = true; // 확실히 활성화
            
            sunComponent = sunObj.AddComponent<Sun>();
            sunComponent.RotationsPerHour = 0f;
            sunComponent.AutomaticLightIntensity = true; // **중요: 자동 라이트 강도 활성화**
            sunComponent.MaximumLightIntensity = dayIntensity;
            
            // Sun을 정오 위치로 설정 (해가 보이는 위치)
            sunObj.transform.rotation = Quaternion.Euler(noon, -30f, 0f);
            
            // Sun을 메인 라이트로 설정
            if (mainLight == null)
            {
                mainLight = sunLight;
            }
            
            Debug.Log($"{LOG_PREFIX} Sun 오브젝트 생성 완료 - 위치: {sunObj.transform.eulerAngles}, 라이트 강도: {sunLight.intensity}");
        }

        // Moon 오브젝트 생성
        if (moonComponent == null)
        {
            GameObject moonObj = new GameObject("Moon");
            Light moonLight = moonObj.AddComponent<Light>();
            moonLight.type = LightType.Directional;
            moonLight.intensity = 0.1f;
            moonLight.color = new Color(0.8f, 0.9f, 1f); // 푸른빛 달빛
            moonLight.enabled = false; // 처음에는 비활성화 (밤에만 활성화)
            
            moonComponent = moonObj.AddComponent<Moon>();
            moonComponent.RotationsPerHour = 0f;
            moonComponent.AutomaticLightIntensity = true; // **자동 라이트 강도 활성화**
            moonComponent.MaximumLightIntensity = 0.3f;
            
            // Moon을 Sun 반대편에 위치
            moonObj.transform.rotation = Quaternion.Euler(noon + 180f, -30f, 0f);
            
            Debug.Log($"{LOG_PREFIX} Moon 오브젝트 생성 완료 - 위치: {moonObj.transform.eulerAngles}");
        }
    }

    /// <summary>
    /// 해가 보이도록 강제 설정
    /// </summary>
    public void ForceSunVisible()
    {
        if (skyboxMaterial == null)
        {
            skyboxMaterial = RenderSettings.skybox;
        }
        
        if (sunComponent == null)
        {
            CreateSunMoonObjects();
        }
        
        if (sunComponent != null && skyboxMaterial != null)
        {
            // 해를 정오 위치로 강제 설정
            sunComponent.transform.rotation = Quaternion.Euler(noon, -30f, 0f);
            Vector3 sunDirection = -sunComponent.transform.forward;
            
            // 스카이박스에 해 방향 강제 설정
            skyboxMaterial.SetVector("_SunDirection", sunDirection);
            
            // 해가 잘 보이도록 강제 설정
            if (skyboxMaterial.HasProperty("_SunAngularDiameter"))
                skyboxMaterial.SetFloat("_SunAngularDiameter", 1.0f);
            if (skyboxMaterial.HasProperty("_SunIntensity"))
                skyboxMaterial.SetFloat("_SunIntensity", 8.0f);
            if (skyboxMaterial.HasProperty("_SunColor"))
                skyboxMaterial.SetColor("_SunColor", Color.yellow);
            if (skyboxMaterial.HasProperty("_Exposure"))
                skyboxMaterial.SetFloat("_Exposure", 1.2f);
                
            Debug.Log($"{LOG_PREFIX} 해를 강제로 보이게 설정 완료!");
        }
    }

    #region Unity 에디터 디버그 메서드
    [ContextMenu("Sun/Moon 오브젝트 생성")]
    private void DebugCreateSunMoon()
    {
        CreateSunMoonObjects();
    }

    [ContextMenu("⭐ 해 강제로 보이게 하기")]
    private void DebugForceSunVisible()
    {
        ForceSunVisible();
    }

    [ContextMenu("🔍 라이트 상태 확인")]
    private void DebugCheckLightStatus()
    {
        Debug.Log($"{LOG_PREFIX} === 라이트 상태 확인 ===");
        
        if (mainLight != null)
        {
            Debug.Log($"{LOG_PREFIX} 메인 라이트: {mainLight.name}, 강도: {mainLight.intensity}, 활성화: {mainLight.enabled}");
        }
        else
        {
            Debug.LogWarning($"{LOG_PREFIX} 메인 라이트가 설정되지 않음!");
        }
        
        if (sunComponent != null)
        {
            Light sunLight = sunComponent.GetComponent<Light>();
            Debug.Log($"{LOG_PREFIX} Sun 라이트: {sunLight?.name}, 강도: {sunLight?.intensity}, 활성화: {sunLight?.enabled}, AutoIntensity: {sunComponent.AutomaticLightIntensity}");
        }
        else
        {
            Debug.LogWarning($"{LOG_PREFIX} Sun 컴포넌트가 없음!");
        }
        
        if (moonComponent != null)
        {
            Light moonLight = moonComponent.GetComponent<Light>();
            Debug.Log($"{LOG_PREFIX} Moon 라이트: {moonLight?.name}, 강도: {moonLight?.intensity}, 활성화: {moonLight?.enabled}, AutoIntensity: {moonComponent.AutomaticLightIntensity}");
        }
        else
        {
            Debug.LogWarning($"{LOG_PREFIX} Moon 컴포넌트가 없음!");
        }
        
        Debug.Log($"{LOG_PREFIX} 현재 시간: {TimeManager.Instance?.GetFormattedTime()}, 낮인가: {IsDay()}");
    }

    [ContextMenu("낮으로 설정 (12시)")]
    private void DebugSetNoon()
    {
        SetTime(12f);
    }

    [ContextMenu("밤으로 설정 (0시)")]
    private void DebugSetMidnight()
    {
        SetTime(0f);
    }

    [ContextMenu("일출 시간 (6시)")]
    private void DebugSetSunrise()
    {
        SetTime(6f);
    }

    [ContextMenu("일몰 시간 (18시)")]
    private void DebugSetSunset()
    {
        SetTime(18f);
    }

    [ContextMenu("노을 시간 (18시)")]
    private void DebugSetSunset2()
    {
        SetTime(17.5f);
    }
    #endregion
} 