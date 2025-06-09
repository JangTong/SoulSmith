using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public enum WallType
{
    Stone,      // 기본 돌 벽
    Metal,      // 금속 벽
    Magic,      // 마법 벽 (특수 효과)
    Breakable   // 파괴 가능한 벽
}

public class WallTile : MonoBehaviour
{
    private const string LOG_PREFIX = "[WallTile]";
    
    [Header("Wall Properties")]
    [SerializeField] private Vector2Int coord;
    [SerializeField] private WallType wallType = WallType.Stone;
    [SerializeField] private bool isBreakable = false;
    [SerializeField] private int requiredManaToBreak = 10;
    
    [Header("UI Visual")]
    [SerializeField] private Image wallImage;
    [SerializeField] private float breakableAlpha = 0.7f; // 파괴 가능한 벽 투명도
    
    public Vector2Int Coord 
    { 
        get => coord; 
        set => coord = value; 
    }
    
    public WallType Type => wallType;
    public bool IsBreakable => isBreakable;
    public int RequiredManaToBreak => requiredManaToBreak;

    private void Start()
    {
        InitializeWallImage();
        UpdateVisual();
        Debug.Log($"{LOG_PREFIX} Wall initialized at {coord} - Type: {wallType}");
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
        
        // 기본 벽 색상 설정
        if (wallImage.sprite == null)
        {
            wallImage.color = GetDefaultWallColor();
        }
    }

    /// <summary>
    /// 벽 파괴 시도
    /// </summary>
    public bool TryBreakWall(int availableMana)
    {
        if (!isBreakable)
        {
            Debug.Log($"{LOG_PREFIX} Wall at {coord} is not breakable");
            return false;
        }

        if (availableMana < requiredManaToBreak)
        {
            Debug.Log($"{LOG_PREFIX} Not enough mana to break wall at {coord}. Required: {requiredManaToBreak}, Available: {availableMana}");
            return false;
        }

        // 벽 파괴 효과
        PlayBreakEffect();
        gameObject.SetActive(false);
        Debug.Log($"{LOG_PREFIX} Wall at {coord} was broken!");
        return true;
    }

    /// <summary>
    /// 벽 파괴 효과 재생
    /// </summary>
    private void PlayBreakEffect()
    {
        // UI 기반 파괴 효과
        if (wallImage != null)
        {
            // 간단한 페이드 아웃 효과
            var color = wallImage.color;
            color.a = 0f;
            wallImage.color = color;
        }
    }

    /// <summary>
    /// 벽 타입에 따른 시각적 업데이트
    /// </summary>
    private void UpdateVisual()
    {
        if (wallImage == null) return;

        // 벽 타입별 색상 설정
        var baseColor = GetDefaultWallColor();
        
        // 파괴 가능한 벽은 투명도 조정
        if (isBreakable)
        {
            baseColor.a = breakableAlpha;
        }
        
        wallImage.color = baseColor;
    }

    private Color GetDefaultWallColor()
    {
        switch (wallType)
        {
            case WallType.Stone: return Color.gray;
            case WallType.Metal: return new Color(0.7f, 0.7f, 0.8f); // 은색
            case WallType.Magic: return Color.magenta;
            case WallType.Breakable: return Color.yellow;
            default: return Color.gray;
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
    /// 벽 타입 설정
    /// </summary>
    public void SetWallType(WallType newType, bool breakable = false)
    {
        wallType = newType;
        isBreakable = breakable;
        UpdateVisual();
    }

    /// <summary>
    /// Inspector에서 값 변경 시 즉시 반영
    /// </summary>
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdateVisual();
        }
    }
} 