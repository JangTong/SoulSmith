using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public enum WallType
{
    Stone,      // 기본 돌 벽
    Metal,      // 금속 벽
    Magic,      // 마법 벽 (특수 효과)
    Breakable   // 파괴 가능한 벽
}

[System.Serializable]
public enum DisplayMode
{
    Color,      // 색상으로 구분
    Sprite      // 스프라이트로 구분
}

public class WallTile : MonoBehaviour
{
    private const string LOG_PREFIX = "[WallTile]";
    
    [Header("Wall Properties")]
    [SerializeField] private Vector2Int coord;
    [SerializeField] private WallType wallType = WallType.Stone;
    [SerializeField] private bool isBreakable = false;
    
    [Header("Mana Requirements")]
    [SerializeField] private ElementalMana requiredMana = new ElementalMana();
    [SerializeField] private bool useAutoManaCalculation = true;
    
    [Header("UI Visual")]
    [SerializeField] private Image wallImage;
    [SerializeField] private DisplayMode displayMode = DisplayMode.Color;
    [SerializeField] private float breakableAlpha = 0.7f; // 파괴 가능한 벽 투명도
    [SerializeField] private Color customColor = Color.white;
    [SerializeField] private bool useCustomColor = false;
    
    [Header("Sprite Settings")]
    [SerializeField] private Sprite defaultSprite; // 컬러 모드에서 사용할 기본 스프라이트
    [SerializeField] private Sprite stoneSprite;
    [SerializeField] private Sprite metalSprite;
    [SerializeField] private Sprite magicSprite;
    [SerializeField] private Sprite breakableSprite;
    
    [Header("Advanced Properties")]
    [SerializeField] private float destructionDelay = 0.5f;
    [SerializeField] private bool regenerateAfterTime = false;
    [SerializeField] private float regenerationTime = 30f;
    
    // 성능 최적화를 위한 디바운스 시스템
    private static float lastValidateTime = 0f;
    private const float VALIDATE_COOLDOWN = 0.1f; // 100ms 쿨다운
    
    public Vector2Int Coord 
    { 
        get => coord; 
        set => coord = value; 
    }
    
    public WallType Type => wallType;
    public bool IsBreakable => isBreakable;
    public ElementalMana RequiredMana => requiredMana;

    private void Start()
    {
        InitializeWallImage();
        if (useAutoManaCalculation)
        {
            CalculateAutoMana();
        }
        UpdateVisual();
        
        Debug.Log($"{LOG_PREFIX} Wall initialized at {coord} - Type: {wallType}, Mana: Fire:{requiredMana.fire} Water:{requiredMana.water} Earth:{requiredMana.earth} Air:{requiredMana.air}");
    }

    private void InitializeWallImage()
    {
        if (wallImage == null)
        {
            wallImage = GetComponent<Image>();
            if (wallImage == null)
            {
                wallImage = gameObject.AddComponent<Image>();
            }
        }
        
        // 초기 스프라이트 설정
        if (wallImage.sprite == null && defaultSprite != null)
        {
            wallImage.sprite = defaultSprite;
        }
    }

    /// <summary>
    /// 벽 타입에 따른 자동 마나 계산
    /// </summary>
    private void CalculateAutoMana()
    {
        switch (wallType)
        {
            case WallType.Stone:
                requiredMana = new ElementalMana { water = 5 };
                break;
            case WallType.Metal:
                requiredMana = new ElementalMana { fire = 5 };
                break;
            case WallType.Magic:
                requiredMana = new ElementalMana { air = 5 };
                break;
            case WallType.Breakable:
                requiredMana = new ElementalMana { earth = 5 };
                break;
        }
    }

    /// <summary>
    /// 벽 파괴 시도 (새로운 마나 시스템)
    /// </summary>
    public bool TryBreakWall(ElementalMana availableMana)
    {
        if (!isBreakable)
        {
            Debug.Log($"{LOG_PREFIX} Wall at {coord} is not breakable");
            return false;
        }

        if (!HasEnoughMana(availableMana))
        {
            Debug.Log($"{LOG_PREFIX} Not enough mana to break wall at {coord}. Required: Fire:{requiredMana.fire} Water:{requiredMana.water} Earth:{requiredMana.earth} Air:{requiredMana.air}");
            return false;
        }

        if (regenerateAfterTime)
        {
            // 재생성 모드: 파괴 효과 후 재생성
            PlayBreakEffect();
            StartCoroutine(RegenerateWall());
        }
        else
        {
            // 일반 파괴 모드: 효과 없이 바로 비활성화 (색상 문제 방지)
            gameObject.SetActive(false);
        }
        
        Debug.Log($"{LOG_PREFIX} Wall at {coord} was broken!");
        return true;
    }

    /// <summary>
    /// 마나 충분 여부 확인
    /// </summary>
    private bool HasEnoughMana(ElementalMana availableMana)
    {
        return availableMana.fire >= requiredMana.fire &&
               availableMana.water >= requiredMana.water &&
               availableMana.earth >= requiredMana.earth &&
               availableMana.air >= requiredMana.air;
    }

    /// <summary>
    /// 벽 파괴 효과 재생
    /// </summary>
    private void PlayBreakEffect()
    {
        // UI 기반 파괴 효과
        if (wallImage != null)
        {
            // 간단한 색상 변경 효과
            wallImage.color = Color.red;
            Invoke(nameof(RestoreWallColor), destructionDelay);
        }
    }

    /// <summary>
    /// 벽 색상 복원
    /// </summary>
    private void RestoreWallColor()
    {
        if (wallImage != null)
        {
            wallImage.color = GetDefaultWallColor();
        }
    }

    /// <summary>
    /// 벽 재생 코루틴
    /// </summary>
    private System.Collections.IEnumerator RegenerateWall()
    {
        yield return new UnityEngine.WaitForSeconds(regenerationTime);
        
        gameObject.SetActive(true);
        UpdateVisual();
        
        Debug.Log($"{LOG_PREFIX} Wall at {coord} has been regenerated");
    }

    /// <summary>
    /// 벽의 시각적 표현 업데이트
    /// </summary>
    private void UpdateVisual()
    {
        if (wallImage == null) return;

        if (displayMode == DisplayMode.Color)
        {
            // 컬러 모드: 기본 스프라이트 + 색상 변경
            if (defaultSprite != null)
            {
                wallImage.sprite = defaultSprite;
            }
            
            Color targetColor = useCustomColor ? customColor : GetDefaultWallColor();
            
            // 파괴 가능한 벽은 약간 투명하게
            if (isBreakable)
            {
                targetColor.a = breakableAlpha;
            }
            
            wallImage.color = targetColor;
        }
        else if (displayMode == DisplayMode.Sprite)
        {
            // 스프라이트 모드: 타입별 스프라이트 사용
            Sprite targetSprite;
            
            // 부술 수 없는 벽은 Default Sprite 사용
            if (!isBreakable)
            {
                targetSprite = defaultSprite;
            }
            else
            {
                targetSprite = GetWallSprite();
            }
            
            if (targetSprite != null)
            {
                wallImage.sprite = targetSprite;
            }
            
            // 스프라이트 모드에서는 색깔 조정 없이 기본 흰색 사용 (투명도 조정도 없음)
            wallImage.color = Color.white;
        }
    }

    /// <summary>
    /// 벽 타입에 따른 기본 색상 반환
    /// </summary>
    private Color GetDefaultWallColor()
    {
        // 파괴 가능한 벽일 때만 마나 속성에 어울리는 색상 적용
        if (isBreakable)
        {
            switch (wallType)
            {
                case WallType.Stone: 
                    return new Color(0.4f, 0.7f, 1f); // Water - 밝은 파란색
                case WallType.Metal: 
                    return new Color(1f, 0.5f, 0.2f); // Fire - 주황색
                case WallType.Magic: 
                    return new Color(0.8f, 0.9f, 1f); // Air - 하늘색
                case WallType.Breakable: 
                    return new Color(0.6f, 0.8f, 0.4f); // Earth - 연두색
                default: 
                    return Color.gray;
            }
        }
        else
        {
            // 파괴 불가능한 벽은 기본 회색 계열
            switch (wallType)
            {
                case WallType.Stone: return new Color(0.6f, 0.6f, 0.6f); // 진한 회색
                case WallType.Metal: return new Color(0.7f, 0.7f, 0.8f); // 은색
                case WallType.Magic: return new Color(0.5f, 0.5f, 0.6f); // 어두운 보라회색
                case WallType.Breakable: return new Color(0.5f, 0.4f, 0.3f); // 어두운 갈색
                default: return Color.gray;
            }
        }
    }

    /// <summary>
    /// 벽 타입에 따른 스프라이트 반환
    /// </summary>
    private Sprite GetWallSprite()
    {
        switch (wallType)
        {
            case WallType.Stone: return stoneSprite;
            case WallType.Metal: return metalSprite;
            case WallType.Magic: return magicSprite;
            case WallType.Breakable: return breakableSprite;
            default: return defaultSprite;
        }
    }

    /// <summary>
    /// 에디터용 좌표 설정
    /// </summary>
    public void SetCoordinate(Vector2Int newCoord)
    {
        coord = newCoord;
        name = $"Wall_{coord.x}_{coord.y}";
    }

    /// <summary>
    /// 벽 타입 설정 (에디터용)
    /// </summary>
    public void SetWallType(WallType newType, bool breakable = false)
    {
        wallType = newType;
        isBreakable = breakable;
        
        if (useAutoManaCalculation)
        {
            CalculateAutoMana();
        }
        
        UpdateVisual();
    }

    /// <summary>
    /// 디스플레이 모드 설정
    /// </summary>
    public void SetDisplayMode(DisplayMode mode)
    {
        displayMode = mode;
        UpdateVisual();
    }

    /// <summary>
    /// 수동 마나 설정
    /// </summary>
    public void SetRequiredMana(ElementalMana newMana)
    {
        requiredMana = newMana;
        useAutoManaCalculation = false;
    }

    /// <summary>
    /// Inspector에서 값 변경 시 즉시 반영 (에디터 연동 개선)
    /// </summary>
    private void OnValidate()
    {
        // 디바운스: 너무 자주 호출되는 것을 방지 (성능 최적화)
        float currentTime = Time.realtimeSinceStartup;
        if (currentTime - lastValidateTime < VALIDATE_COOLDOWN)
        {
            return;
        }
        lastValidateTime = currentTime;
        
        // 마나 자동 계산
        if (useAutoManaCalculation)
        {
            CalculateAutoMana();
        }
        
        // 에디터와 런타임 모두에서 시각적 업데이트 수행
        if (!Application.isPlaying)
        {
#if UNITY_EDITOR
            // 에디터에서는 다음 프레임에 업데이트 (안전성 확보)
            EditorApplication.delayCall += () => {
                if (this != null && wallImage != null)
                {
                    UpdateVisual();
                }
            };
#endif
        }
        else
        {
            // 런타임에서는 즉시 업데이트
            UpdateVisual();
        }
    }
} 