using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class EnchantMapUI : MonoBehaviour
{
    private const string LOG_PREFIX = "[EnchantMapUI]";
    
    [Header("Map & Feedback")]
    public RectTransform mapContainer;
    public GameObject playerIcon;
    [SerializeField] private float tileSize = 100f;
    [SerializeField] private float moveAnimationDuration = 0.2f;
    public TextMeshProUGUI feedbackText;
    public ParticleSystem sparkEffect;
    
    // 처음 시작할 때의 위치 저장
    private Vector2Int initialStartPosition;
    


    [Header("Mana Bars (Fill Type)")]
    public Image fireManaBar;
    public Image waterManaBar;
    public Image earthManaBar;
    public Image airManaBar;

    [Header("Input Settings")]
    [SerializeField] private KeyCode upKey = KeyCode.W;
    [SerializeField] private KeyCode downKey = KeyCode.S;
    [SerializeField] private KeyCode leftKey = KeyCode.A;
    [SerializeField] private KeyCode rightKey = KeyCode.D;

    private EnchantComponent enchant;
    private Vector2Int currentCoord;
    private int initFire, initWater, initEarth, initAir;
    
    // 성능 최적화를 위한 캐싱
    private Dictionary<Vector2Int, GameObject> tileCache;
    private Dictionary<Vector2Int, GameObject> wallCache;
    private Tweener currentMoveTween;

    private void OnEnable()
    {
        Debug.Log($"{LOG_PREFIX} OnEnable: Initializing EnchantMapUI");
        
        // 무기 배치 이벤트 구독
        EnchantTable.OnWeaponPlaced += OnNewWeaponPlaced;
        
        var table = GetComponentInParent<EnchantTable>();
        if (table == null)
        {
            Debug.LogError($"{LOG_PREFIX} EnchantTable not found in parent");
            return;
        }

        // Ensure the EnchantComponent is retrieved from the object on the table
        if (table.objectOnTable == null)
        {
            Debug.LogError($"{LOG_PREFIX} objectOnTable is null on EnchantTable");
            return;
        }

        enchant = table.objectOnTable.GetComponent<EnchantComponent>();
        if (enchant == null)
        {
            enchant = table.objectOnTable.AddComponent<EnchantComponent>();
            Debug.LogWarning($"{LOG_PREFIX} EnchantComponent was missing on objectOnTable – added dynamically.");
        }

        // Subscribe to mana change event
        enchant.OnManaChanged += UpdateManaUI;

        // Cache initial mana values for normalization
        initFire  = enchant.manaPool.fire;
        initWater = enchant.manaPool.water;
        initEarth = enchant.manaPool.earth;
        initAir   = enchant.manaPool.air;

        // 시작 위치 찾고 저장
        currentCoord = FindPlayerStartPosition();
        initialStartPosition = currentCoord; // 처음 위치 기억
        Debug.Log($"{LOG_PREFIX} Initial start position saved: {initialStartPosition}");
        
        // 맵을 플레이어 시작 위치에 맞게 초기화
        if (mapContainer != null)
        {
            Vector2 initialMapPos = -new Vector2(currentCoord.x * tileSize, currentCoord.y * tileSize);
            mapContainer.anchoredPosition = initialMapPos;
        }

        feedbackText.text = string.Empty;
        
        // 타일 캐시 초기화
        InitializeTileCache();
        
        // 플레이어 아이콘 위치 동기화
        UpdatePlayerIconPosition();
        
        // Initialize UI with current mana
        UpdateManaUI(enchant.manaPool);
    }

    private void OnDisable()
    {
        Debug.Log($"{LOG_PREFIX} OnDisable: Resetting EnchantMapUI state");
        
        // 무기 배치 이벤트 구독 해제
        EnchantTable.OnWeaponPlaced -= OnNewWeaponPlaced;
        
        // DOTween 메모리 누수 방지
        currentMoveTween?.Kill();
        currentMoveTween = null;
        
        if (enchant != null)
            enchant.OnManaChanged -= UpdateManaUI;

        // 리셋은 하지 않음 - 다음에 OnEnable에서 다시 찾을 것
        // currentCoord와 mapContainer는 그대로 유지
            
        // 캐시 정리
        tileCache?.Clear();
    }

    private void Update()
    {
        if (enchant == null) return;
        
        // 키 입력 유지하되 최적화
        if (Input.GetKeyDown(upKey)) TryMove(Vector2Int.up);
        else if (Input.GetKeyDown(downKey)) TryMove(Vector2Int.down);
        else if (Input.GetKeyDown(leftKey)) TryMove(Vector2Int.left);
        else if (Input.GetKeyDown(rightKey)) TryMove(Vector2Int.right);
        
        // 테스트용 리셋 키
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log($"{LOG_PREFIX} R key pressed - Force reset to start");
            ResetToStartPosition();
        }
    }

    private void TryMove(Vector2Int dir)
    {
        if (enchant == null)
        {
            Debug.LogError($"{LOG_PREFIX} Cannot move: EnchantComponent is null");
            return;
        }

        Vector2Int targetCoord = currentCoord + dir;
        
        // UI 벽 충돌 검사 및 파괴 시도
        if (wallCache != null && wallCache.ContainsKey(targetCoord))
        {
            var wallObj = wallCache[targetCoord];
            var wallTile = wallObj?.GetComponent<WallTile>();
            
            if (wallTile != null)
            {
                // 파괴 가능한 벽인 경우 파괴 시도
                if (wallTile.IsBreakable)
                {
                    // 벽 파괴 전에 마나 정보 미리 저장 (벽이 비활성화되면 접근 불가)
                    ElementalMana wallManaRequirement = wallTile.RequiredMana;
                    
                    bool wallBroken = wallTile.TryBreakWall(enchant.manaPool);
                    
                    if (wallBroken)
                    {
                        // 마나 소모 전 상태 로그
                        Debug.Log($"{LOG_PREFIX} Before wall break mana consumption - Fire:{enchant.manaPool.fire} Water:{enchant.manaPool.water} Earth:{enchant.manaPool.earth} Air:{enchant.manaPool.air}");
                        Debug.Log($"{LOG_PREFIX} Required mana for wall break - Fire:{wallManaRequirement.fire} Water:{wallManaRequirement.water} Earth:{wallManaRequirement.earth} Air:{wallManaRequirement.air}");
                        
                        // 벽 파괴에 필요한 마나 소모 (미리 저장된 정보 사용)
                        enchant.ConsumeWallBreakMana(wallManaRequirement);
                        
                        // 마나 소모 후 상태 로그
                        Debug.Log($"{LOG_PREFIX} After wall break mana consumption - Fire:{enchant.manaPool.fire} Water:{enchant.manaPool.water} Earth:{enchant.manaPool.earth} Air:{enchant.manaPool.air}");
                        
                        // 벽이 파괴되면 캐시에서 제거하고 이동 계속
                        wallCache.Remove(targetCoord);
                        ShowFeedback($"벽이 파괴되었습니다! 마나 소모: Fire:{wallManaRequirement.fire} Water:{wallManaRequirement.water} Earth:{wallManaRequirement.earth} Air:{wallManaRequirement.air}");
                        
                        Debug.Log($"{LOG_PREFIX} Wall broken at {targetCoord}, continuing movement");
                    }
                    else
                    {
                        // 파괴 실패 (마나 부족 등)
                        ShowFeedback("벽을 파괴할 마나가 부족합니다");
                        Debug.Log($"{LOG_PREFIX} Failed to break wall at {targetCoord} - insufficient mana");
                        return;
                    }
                }
                else
                {
                    // 파괴 불가능한 벽
                    ShowFeedback("파괴할 수 없는 벽입니다");
                    Debug.Log($"{LOG_PREFIX} Hit unbreakable wall at {targetCoord}");
                    return;
                }
            }
            else
            {
                // WallTile 컴포넌트가 없는 경우 (일반 벽으로 처리)
                ShowFeedback("통과할 수 없는 벽입니다");
                Debug.Log($"{LOG_PREFIX} Hit wall at {targetCoord} (no WallTile component)");
                return;
            }
        }

        // 이동에 필요한 마나 확인
        if (!enchant.HasEnoughMana(dir))
        {
            ShowFeedback("이동할 마나가 부족합니다");
            Debug.Log($"{LOG_PREFIX} Not enough mana to move {dir}");
            return;
        }

        // 이동 마나 소모
        enchant.ConsumeMana(dir);
        currentCoord = targetCoord;

        // 맵 이동 (카메라 효과)
        Vector2 newMapPos = -new Vector2(currentCoord.x * tileSize, currentCoord.y * tileSize);
        
        // 기존 애니메이션 정리 후 새 애니메이션 시작
        currentMoveTween?.Kill();
        currentMoveTween = mapContainer.DOAnchorPos(newMapPos, moveAnimationDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => currentMoveTween = null);

        // 플레이어 아이콘 위치 동기화
        UpdatePlayerIconPosition();

        ShowFeedback(string.Empty);
        Debug.Log($"{LOG_PREFIX} Moved {dir} to {currentCoord}, Mana now Fire:{enchant.manaPool.fire}/{initFire}, Water:{enchant.manaPool.water}/{initWater}, Earth:{enchant.manaPool.earth}/{initEarth}, Air:{enchant.manaPool.air}/{initAir}");
    }

    public void ApplyCurrentSpell()
    {
        if (enchant == null)
        {
            Debug.LogError($"{LOG_PREFIX} Cannot apply spell: EnchantComponent is null");
            ShowFeedback("예외: EnchantComponent 없음");
            return;
        }

        var tileObj = FindTileAt(currentCoord);
        if (tileObj == null)
        {
            ShowFeedback("마법 타일이 없습니다");
            Debug.Log($"{LOG_PREFIX} ApplyCurrentSpell failed: No tile at {currentCoord}");
            return;
        }

        var spellHolder = tileObj.GetComponent<SpellHolder>();
        if (spellHolder == null || spellHolder.spell == null)
        {
            ShowFeedback("유효한 마법이 없습니다");
            Debug.Log($"{LOG_PREFIX} ApplyCurrentSpell failed: No spell on tile at {currentCoord}");
            return;
        }

        var spell = spellHolder.spell;
        if (!enchant.appliedSpells.Contains(spell))
        {
            enchant.appliedSpells.Add(spell);
            enchant.ApplyElementalEffects(tileObj.transform);
            sparkEffect.Play();
            ShowFeedback($"마법 '{spell.name}' 부여 완료");
            Debug.Log($"{LOG_PREFIX} Applied spell: {spell.name} at {currentCoord}");
        }
        else
        {
            ShowFeedback($"⚠ 이미 부여된 마법입니다: {spell.name}");
            Debug.Log($"{LOG_PREFIX} Spell '{spell.name}' already applied");
        }
    }

    private void UpdateManaUI(ElementalMana mana)
    {
        if (enchant == null) return;
        if (initFire  > 0) fireManaBar.fillAmount  = mana.fire  / (float)initFire;
        if (initWater > 0) waterManaBar.fillAmount = mana.water / (float)initWater;
        if (initEarth > 0) earthManaBar.fillAmount = mana.earth / (float)initEarth;
        if (initAir   > 0) airManaBar.fillAmount   = mana.air   / (float)initAir;
        Debug.Log($"{LOG_PREFIX} Mana UI Updated - Fire:{mana.fire}/{initFire}, Water:{mana.water}/{initWater}, Earth:{mana.earth}/{initEarth}, Air:{mana.air}/{initAir}");
    }

    private void ShowFeedback(string message)
    {
        feedbackText.text = message;
    }



    /// <summary>
    /// 씬에 배치된 PlayerIcon 위치를 찾아서 시작 좌표 반환
    /// </summary>
    private Vector2Int FindPlayerStartPosition()
    {
        if (mapContainer == null)
        {
            Debug.LogWarning($"{LOG_PREFIX} Map container is null, defaulting to (0,0)");
            return Vector2Int.zero;
        }

        // 1. Inspector에서 직접 할당된 playerIcon 확인
        if (playerIcon != null)
        {
            var iconRect = playerIcon.GetComponent<RectTransform>();
            if (iconRect != null)
            {
                Vector2Int coordFromIcon = CalculateCoordFromPosition(iconRect.anchoredPosition);
                Debug.Log($"{LOG_PREFIX} Found player icon at inspector assignment: {coordFromIcon}");
                return coordFromIcon;
            }
        }

        // 2. 씬에서 PlayerIcon_ 패턴으로 찾기
        foreach (Transform child in mapContainer)
        {
            if (child.name.StartsWith("PlayerIcon_"))
            {
                Vector2Int coord = ParseCoordFromName(child.name);
                if (coord != Vector2Int.zero || child.name == "PlayerIcon_0_0")
                {
                    // playerIcon 참조도 설정
                    playerIcon = child.gameObject;
                    Debug.Log($"{LOG_PREFIX} Found player start at {coord} from scene object: {child.name}");
                    return coord;
                }
            }
        }

        Debug.LogWarning($"{LOG_PREFIX} No player start position found, defaulting to (0,0)");
        return Vector2Int.zero;
    }

    /// <summary>
    /// UI 위치에서 그리드 좌표 계산
    /// </summary>
    private Vector2Int CalculateCoordFromPosition(Vector2 uiPosition)
    {
        int x = Mathf.RoundToInt(uiPosition.x / tileSize);
        int y = Mathf.RoundToInt(uiPosition.y / tileSize);
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// 오브젝트 이름에서 좌표 파싱
    /// </summary>
    private Vector2Int ParseCoordFromName(string objName)
    {
        var parts = objName.Split('_');
        if (parts.Length >= 3 && int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y))
        {
            return new Vector2Int(x, y);
        }
        return Vector2Int.zero;
    }

    /// <summary>
    /// 플레이어 아이콘을 현재 좌표에 맞게 위치 업데이트
    /// </summary>
    private void UpdatePlayerIconPosition()
    {
        if (playerIcon == null) return;

        var iconRect = playerIcon.GetComponent<RectTransform>();
        if (iconRect == null) return;

        // 플레이어 아이콘을 현재 좌표 위치로 이동
        Vector2 iconPos = new Vector2(currentCoord.x * tileSize, currentCoord.y * tileSize);
        iconRect.anchoredPosition = iconPos;

        Debug.Log($"{LOG_PREFIX} Player icon moved to {currentCoord} at UI position {iconPos}");
    }

    private void InitializeTileCache()
    {
        tileCache = new Dictionary<Vector2Int, GameObject>();
        wallCache = new Dictionary<Vector2Int, GameObject>();
        
        if (mapContainer == null) return;
        
        var spellHolders = mapContainer.GetComponentsInChildren<SpellHolder>();
        foreach (var holder in spellHolders)
        {
            tileCache[holder.coord] = holder.gameObject;
        }
        
        // 벽 타일 캐시 초기화 (이름 패턴 기반)
        var allChildren = mapContainer.GetComponentsInChildren<Transform>();
        foreach (var child in allChildren)
        {
            if (child.name.StartsWith("Wall_"))
            {
                // 벽 이름 패턴에서 좌표 추출 (예: Wall_1_2)
                var parts = child.name.Split('_');
                if (parts.Length >= 3 && int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y))
                {
                    wallCache[new Vector2Int(x, y)] = child.gameObject;
                }
            }
        }
        
        Debug.Log($"{LOG_PREFIX} Initialized tile cache with {tileCache.Count} tiles and {wallCache.Count} walls");
    }

    private GameObject FindTileAt(Vector2Int coord)
    {
        if (tileCache != null && tileCache.TryGetValue(coord, out GameObject cachedTile))
        {
            return cachedTile;
        }
        
        // 캐시에 없으면 직접 검색 (fallback)
        foreach (var spellHolder in mapContainer.GetComponentsInChildren<SpellHolder>())
            if (spellHolder.coord == coord)
                return spellHolder.gameObject;
        return null;
    }

    // Inspector에서 타일 캐시 재초기화용 (개발 편의)
    [ContextMenu("Refresh Tile Cache")]
    private void RefreshTileCache()
    {
        InitializeTileCache();
    }

    /// <summary>
    /// 새 무기가 배치되었을 때 호출 - 맵을 초기 상태로 리셋
    /// </summary>
    private void OnNewWeaponPlaced()
    {
        Debug.Log($"{LOG_PREFIX} New weapon detected! Resetting to start position");
        
        // 1. 모든 파괴된 벽 복원
        RestoreAllWalls();
        
        // 2. 플레이어를 시작 위치로 강제 이동
        ResetToStartPosition();
        
        // 3. 피드백 메시지
        ShowFeedback("새로운 무기가 배치되었습니다! 시작 위치로 돌아갑니다.");
    }

    /// <summary>
    /// 플레이어를 처음 저장된 시작 위치로 리셋
    /// </summary>
    private void ResetToStartPosition()
    {
        Debug.Log($"{LOG_PREFIX} Resetting from {currentCoord} to initial start position {initialStartPosition}");
        
        // 현재 좌표를 처음 저장된 위치로 변경
        currentCoord = initialStartPosition;
        
        // 맵 컨테이너 위치를 시작 위치에 맞게 즉시 이동
        if (mapContainer != null)
        {
            Vector2 targetMapPos = -new Vector2(currentCoord.x * tileSize, currentCoord.y * tileSize);
            mapContainer.anchoredPosition = targetMapPos;
            Debug.Log($"{LOG_PREFIX} Map moved to {targetMapPos}");
        }
        
        // 플레이어 아이콘 위치를 시작 위치로 강제 이동
        if (playerIcon != null)
        {
            var iconRect = playerIcon.GetComponent<RectTransform>();
            if (iconRect != null)
            {
                Vector2 iconPos = new Vector2(currentCoord.x * tileSize, currentCoord.y * tileSize);
                iconRect.anchoredPosition = iconPos;
                Debug.Log($"{LOG_PREFIX} Player icon forced to {iconPos}");
            }
        }
        
        Debug.Log($"{LOG_PREFIX} Reset complete - Player back at initial position {currentCoord}");
    }

    /// <summary>
    /// 파괴된 벽들을 모두 복원
    /// </summary>
    private void RestoreAllWalls()
    {
        if (mapContainer == null) return;
        
        int restored = 0;
        
        // 비활성화된 모든 벽 찾아서 복원
        foreach (Transform child in mapContainer)
        {
            var wallTile = child.GetComponent<WallTile>();
            if (wallTile != null && !child.gameObject.activeInHierarchy)
            {
                child.gameObject.SetActive(true);
                restored++;
                Debug.Log($"{LOG_PREFIX} Restored wall at {wallTile.Coord}");
            }
        }
        
        // 벽 캐시 재구성
        InitializeTileCache();
        
        Debug.Log($"{LOG_PREFIX} Restored {restored} walls");
    }
}
