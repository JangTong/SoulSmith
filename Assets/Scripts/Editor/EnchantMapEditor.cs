using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;

public class EnchantMapEditor : EditorWindow
{
    private const string LOG_PREFIX = "[EnchantMapEditor]";
    
    [MenuItem("SoulSmith/EnchantMap Editor")]
    public static void ShowWindow()
    {
        GetWindow<EnchantMapEditor>("EnchantMap Editor");
    }

    // Map Settings
    private int mapWidth = 10;
    private int mapHeight = 10;
    private float tileSize = 0.1f; // UI 타일 크기 (픽셀)
    private RectTransform mapContainer;
    private bool isEditingExistingMap = false;
    private string originalMapName = "";
    
    // UI Prefabs
    private GameObject wallUIPrefab;
    private GameObject spellHolderUIPrefab;
    private GameObject playerIconPrefab;
    
    // Editing
    private EditMode currentMode = EditMode.Wall;
    private Vector2Int selectedCoord = Vector2Int.zero;
    private Dictionary<Vector2Int, GameObject> placedObjects = new Dictionary<Vector2Int, GameObject>();
    
    // Wall Properties
    private WallType selectedWallType = WallType.Stone;
    private bool wallBreakable = false;
    private ElementalMana customMana = new ElementalMana();
    private bool useAutoMana = true;
    private Color wallColor = Color.white;
    private bool useCustomWallColor = false;
    private DisplayMode wallDisplayMode = DisplayMode.Color;
    
    // Spell Settings
    private MagicSpell selectedSpell = null;
    private MagicSpell[] availableSpells = null;
    private string[] spellNames = null;
    private int selectedSpellIndex = 0;
    
    private Vector2 scrollPos;
    private bool showGrid = true;
    
    private enum EditMode
    {
        Wall,
        SpellHolder,
        PlayerIcon,
        Erase
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        GUILayout.Label("EnchantMap Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        DrawMapSettings();
        EditorGUILayout.Space();
        
        DrawPrefabSettings();
        EditorGUILayout.Space();
        
        DrawEditingControls();
        EditorGUILayout.Space();
        
        DrawWallPropertySettings();
        EditorGUILayout.Space();
        
        DrawSpellSettings();
        EditorGUILayout.Space();
        
        DrawMapGrid();
        EditorGUILayout.Space();
        
        DrawSelectedTileInfo();
        EditorGUILayout.Space();
        
        DrawUtilityButtons();
        
        EditorGUILayout.EndScrollView();
    }

    private void DrawMapSettings()
    {
        EditorGUILayout.LabelField("Map Settings", EditorStyles.boldLabel);
        
        // Map Container 할당 섹션
        EditorGUILayout.LabelField("Map Container Assignment", EditorStyles.boldLabel);
        
        var newMapContainer = (RectTransform)EditorGUILayout.ObjectField("Map Container (RectTransform)", mapContainer, typeof(RectTransform), true);
        
        // mapContainer가 변경되었을 때 자동으로 맵 로드
        if (newMapContainer != mapContainer)
        {
            mapContainer = newMapContainer;
            if (mapContainer != null)
            {
                LoadMapFromContainer(mapContainer);
            }
        }
        
        // 현재 상태 표시
        if (mapContainer != null)
        {
            EditorGUILayout.HelpBox($"✓ Map Container: {mapContainer.name} ({placedObjects.Count} objects loaded)", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Please assign a Map Container by dragging a RectTransform from the scene.", MessageType.Warning);
        }
        
        // 간소화된 버튼
        if (GUILayout.Button("Refresh from Container"))
        {
            RefreshMapFromScene();
        }
        
        if (isEditingExistingMap)
        {
            EditorGUILayout.HelpBox($"편집 중: {originalMapName}", MessageType.Info);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Changes to Prefab"))
            {
                SaveChangesToPrefab();
            }
            if (GUILayout.Button("Reset to Original"))
            {
                ResetToOriginalMap();
            }
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Manual Settings", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        mapWidth = EditorGUILayout.IntSlider("Width", mapWidth, 5, 30);
        mapHeight = EditorGUILayout.IntSlider("Height", mapHeight, 5, 30);
        EditorGUILayout.EndHorizontal();
        
        tileSize = EditorGUILayout.FloatField("UI Tile Size (px)", tileSize);
        showGrid = EditorGUILayout.Toggle("Show Grid", showGrid);
    }

    private void DrawPrefabSettings()
    {
        EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);
        
        wallUIPrefab = (GameObject)EditorGUILayout.ObjectField("Wall Prefab", wallUIPrefab, typeof(GameObject), false);
        spellHolderUIPrefab = (GameObject)EditorGUILayout.ObjectField("SpellHolder Prefab", spellHolderUIPrefab, typeof(GameObject), false);
        playerIconPrefab = (GameObject)EditorGUILayout.ObjectField("Player Icon Prefab", playerIconPrefab, typeof(GameObject), false);
        
        EditorGUILayout.HelpBox("모든 프리팹은 UI 요소(Image 등)여야 합니다.\n마나 아이콘은 WallPrefab에 미리 설정되어 있습니다.", MessageType.Info);
    }

    private void DrawEditingControls()
    {
        EditorGUILayout.LabelField("Editing Mode", EditorStyles.boldLabel);
        
        currentMode = (EditMode)EditorGUILayout.EnumPopup("Current Mode", currentMode);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Wall Mode")) currentMode = EditMode.Wall;
        if (GUILayout.Button("Spell Mode")) currentMode = EditMode.SpellHolder;
        if (GUILayout.Button("Player Icon")) currentMode = EditMode.PlayerIcon;
        if (GUILayout.Button("Erase Mode")) currentMode = EditMode.Erase;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.LabelField($"Current Mode: {currentMode}", EditorStyles.helpBox);
    }

    /// <summary>
    /// 벽 속성 설정 UI
    /// </summary>
    private void DrawWallPropertySettings()
    {
        EditorGUILayout.LabelField("Wall Properties", EditorStyles.boldLabel);
        
        selectedWallType = (WallType)EditorGUILayout.EnumPopup("Wall Type", selectedWallType);
        wallBreakable = EditorGUILayout.Toggle("Is Breakable", wallBreakable);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Mana Requirements", EditorStyles.boldLabel);
        
        useAutoMana = EditorGUILayout.Toggle("Auto Calculate Mana", useAutoMana);
        
        if (!useAutoMana)
        {
            EditorGUI.indentLevel++;
            customMana.fire = EditorGUILayout.IntField("Fire Mana", customMana.fire);
            customMana.water = EditorGUILayout.IntField("Water Mana", customMana.water);
            customMana.earth = EditorGUILayout.IntField("Earth Mana", customMana.earth);
            customMana.air = EditorGUILayout.IntField("Air Mana", customMana.air);
            EditorGUI.indentLevel--;
        }
        else
        {
            // 자동 계산된 마나 표시 (읽기 전용)
            var autoMana = GetAutoMana(selectedWallType);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField("Fire Mana (Auto)", autoMana.fire);
            EditorGUILayout.IntField("Water Mana (Auto)", autoMana.water);
            EditorGUILayout.IntField("Earth Mana (Auto)", autoMana.earth);
            EditorGUILayout.IntField("Air Mana (Auto)", autoMana.air);
            EditorGUI.EndDisabledGroup();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Visual Settings", EditorStyles.boldLabel);
        
        wallDisplayMode = (DisplayMode)EditorGUILayout.EnumPopup("Display Mode", wallDisplayMode);
        
        if (wallDisplayMode == DisplayMode.Color)
        {
            useCustomWallColor = EditorGUILayout.Toggle("Use Custom Color", useCustomWallColor);
            if (useCustomWallColor)
            {
                wallColor = EditorGUILayout.ColorField("Wall Color", wallColor);
            }
            EditorGUILayout.HelpBox("컬러 모드: 기본 스프라이트에 속성별 색상이 적용됩니다.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("스프라이트 모드: 각 벽 타입별 전용 스프라이트가 사용됩니다.\nWallPrefab에 각 타입별 스프라이트를 미리 설정해주세요.", MessageType.Info);
        }
        
        // 마나 아이콘 관련 안내
        if (wallBreakable)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("파괴 가능한 벽에는 필요한 마나 아이콘이 자동으로 표시됩니다.\n마나 아이콘들은 WallPrefab에 미리 설정되어 있습니다.", MessageType.Info);
        }
    }

    private ElementalMana GetAutoMana(WallType wallType)
    {
        switch (wallType)
        {
            case WallType.Stone: return new ElementalMana { water = 5 };
            case WallType.Metal: return new ElementalMana { fire = 5 };
            case WallType.Magic: return new ElementalMana { air = 5 };
            case WallType.Breakable: return new ElementalMana { earth = 5 };
            default: return new ElementalMana { earth = 5 };
        }
    }

    private void DrawMapGrid()
    {
        if (!showGrid) return;
        
        EditorGUILayout.LabelField("Map Grid", EditorStyles.boldLabel);
        
        var buttonSize = GUILayout.Width(25);
        var buttonHeight = GUILayout.Height(25);
        
        for (int y = mapHeight - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < mapWidth; x++)
            {
                var coord = new Vector2Int(x, y);
                var hasObject = placedObjects.ContainsKey(coord);
                
                GUI.backgroundColor = GetTileColor(coord, hasObject);
                
                var buttonText = hasObject ? GetObjectSymbol(coord) : "·";
                
                if (GUILayout.Button(buttonText, buttonSize, buttonHeight))
                {
                    HandleTileClick(coord);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.LabelField($"Selected: ({selectedCoord.x}, {selectedCoord.y})", EditorStyles.centeredGreyMiniLabel);
    }

    /// <summary>
    /// 선택된 타일 정보 표시 (간단한 버전)
    /// </summary>
    private void DrawSelectedTileInfo()
    {
        EditorGUILayout.LabelField("Selected Tile Info", EditorStyles.boldLabel);
        
        EditorGUILayout.LabelField($"Coordinate: ({selectedCoord.x}, {selectedCoord.y})");
        
        if (placedObjects.ContainsKey(selectedCoord))
        {
            var obj = placedObjects[selectedCoord];
            EditorGUILayout.LabelField($"Object: {obj.name}");
            
            // 벽 정보
            var wallTile = obj.GetComponent<WallTile>();
            if (wallTile != null)
            {
                EditorGUILayout.LabelField($"Type: {wallTile.Type}");
                EditorGUILayout.LabelField($"Breakable: {wallTile.IsBreakable}");
                EditorGUILayout.LabelField("Required Mana:");
                EditorGUI.indentLevel++;
                if (wallTile.RequiredMana.fire > 0) EditorGUILayout.LabelField($"Fire: {wallTile.RequiredMana.fire}");
                if (wallTile.RequiredMana.water > 0) EditorGUILayout.LabelField($"Water: {wallTile.RequiredMana.water}");
                if (wallTile.RequiredMana.earth > 0) EditorGUILayout.LabelField($"Earth: {wallTile.RequiredMana.earth}");
                if (wallTile.RequiredMana.air > 0) EditorGUILayout.LabelField($"Air: {wallTile.RequiredMana.air}");
                EditorGUI.indentLevel--;
            }
            
            // 스펠홀더 정보
            var spellHolder = obj.GetComponent<SpellHolder>();
            if (spellHolder != null)
            {
                if (spellHolder.spell != null)
                {
                    EditorGUILayout.LabelField($"Spell: {spellHolder.spell.spellName}");
                    EditorGUILayout.LabelField($"Description: {spellHolder.spell.description}");
                }
                else
                {
                    EditorGUILayout.LabelField("Spell: None");
                }
            }
        }
        else
        {
            EditorGUILayout.LabelField("Object: Empty");
        }
    }

    private Color GetTileColor(Vector2Int coord, bool hasObject)
    {
        // 현재 선택된 좌표 하이라이트
        if (coord == selectedCoord)
        {
            return Color.yellow; // 선택된 좌표는 노란색
        }
        
        // 오브젝트가 있는 경우 타입별 색상
        if (hasObject)
        {
            var obj = placedObjects[coord];
            if (obj.name.StartsWith("Wall_")) return Color.red;
            if (obj.name.StartsWith("SpellHolder_")) return Color.blue;
            if (obj.name.StartsWith("PlayerIcon_")) return Color.green;
        }
        
        return Color.white; // 기본 색상
    }

    private string GetObjectSymbol(Vector2Int coord)
    {
        if (!placedObjects.ContainsKey(coord)) return "·";
        
        var obj = placedObjects[coord];
        if (obj.name.StartsWith("Wall_")) 
        {
            var wallTile = obj.GetComponent<WallTile>();
            if (wallTile != null)
            {
                switch (wallTile.Type)
                {
                    case WallType.Stone: return "█";
                    case WallType.Metal: return "▬";
                    case WallType.Magic: return "◊";
                    case WallType.Breakable: return "▣";
                }
            }
            return "█";
        }
        if (obj.name.StartsWith("SpellHolder_")) 
        {
            var spellHolder = obj.GetComponent<SpellHolder>();
            if (spellHolder != null && spellHolder.spell != null)
            {
                // Spell이 할당되어 있으면 다른 심볼 사용
                return "★"; // 할당된 SpellHolder
            }
            return "♦"; // 빈 SpellHolder
        }
        if (obj.name.StartsWith("PlayerIcon_")) return "◉";
        
        return "?";
    }

    private void HandleTileClick(Vector2Int coord)
    {
        selectedCoord = coord;
        
        switch (currentMode)
        {
            case EditMode.Wall:
                PlaceWall(coord);
                break;
            case EditMode.SpellHolder:
                PlaceSpellHolder(coord);
                break;
            case EditMode.PlayerIcon:
                PlacePlayerIcon(coord);
                break;
            case EditMode.Erase:
                EraseObject(coord);
                break;
        }
    }

    private void PlaceWall(Vector2Int coord)
    {
        if (wallUIPrefab == null || mapContainer == null) return;
        
        EraseObject(coord);
        
        var wallObj = PrefabUtility.InstantiatePrefab(wallUIPrefab, mapContainer) as GameObject;
        wallObj.name = $"Wall_{coord.x}_{coord.y}";
        
        // UI 위치 설정
        var rectTransform = wallObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(coord.x * tileSize, coord.y * tileSize);
            rectTransform.sizeDelta = new Vector2(tileSize, tileSize);
        }
        
        // 벽 속성 설정
        var wallTile = wallObj.GetComponent<WallTile>();
        if (wallTile != null)
        {
            wallTile.SetCoordinate(coord);
            wallTile.SetWallType(selectedWallType, wallBreakable);
            wallTile.SetDisplayMode(wallDisplayMode);
            
            if (!useAutoMana)
            {
                wallTile.SetRequiredMana(customMana);
            }
            
            // 커스텀 색상 적용 (컬러 모드에서만)
            if (wallDisplayMode == DisplayMode.Color && useCustomWallColor)
            {
                var image = wallObj.GetComponent<Image>();
                if (image != null)
                {
                    image.color = wallColor;
                }
            }
        }
        
        placedObjects[coord] = wallObj;
        Debug.Log($"{LOG_PREFIX} Placed {selectedWallType} Wall at {coord}");
    }

    private void PlaceSpellHolder(Vector2Int coord)
    {
        if (spellHolderUIPrefab == null || mapContainer == null) return;
        
        EraseObject(coord);
        
        var spellObj = PrefabUtility.InstantiatePrefab(spellHolderUIPrefab, mapContainer) as GameObject;
        spellObj.name = $"SpellHolder_{coord.x}_{coord.y}";
        
        // UI 위치 설정
        var rectTransform = spellObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(coord.x * tileSize, coord.y * tileSize);
            rectTransform.sizeDelta = new Vector2(tileSize, tileSize);
        }
        
        // SpellHolder 속성 설정
        var spellHolder = spellObj.GetComponent<SpellHolder>();
        if (spellHolder != null)
        {
            spellHolder.coord = coord;
            
            // 선택된 Spell 할당
            if (selectedSpell != null)
            {
                spellHolder.SetSpell(selectedSpell);
                Debug.Log($"{LOG_PREFIX} Assigned spell '{selectedSpell.spellName}' to SpellHolder at {coord}");
            }
        }
        
        placedObjects[coord] = spellObj;
        Debug.Log($"{LOG_PREFIX} Placed SpellHolder at {coord}");
    }

    private void PlacePlayerIcon(Vector2Int coord)
    {
        if (playerIconPrefab == null || mapContainer == null) return;
        
        // 기존 플레이어 아이콘 제거
        RemoveAllPlayerIcons();
        EraseObject(coord);
        
        var iconObj = PrefabUtility.InstantiatePrefab(playerIconPrefab, mapContainer) as GameObject;
        iconObj.name = $"PlayerIcon_{coord.x}_{coord.y}";
        
        // UI 위치 설정
        var rectTransform = iconObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(coord.x * tileSize, coord.y * tileSize);
            rectTransform.sizeDelta = new Vector2(tileSize, tileSize);
        }
        
        placedObjects[coord] = iconObj;
        Debug.Log($"{LOG_PREFIX} Placed Player Icon at {coord}");
    }

    private void EraseObject(Vector2Int coord)
    {
        if (placedObjects.ContainsKey(coord))
        {
            DestroyImmediate(placedObjects[coord]);
            placedObjects.Remove(coord);
            Debug.Log($"{LOG_PREFIX} Erased object at {coord}");
        }
    }

    private void RemoveAllPlayerIcons()
    {
        var toRemove = new List<Vector2Int>();
        foreach (var kvp in placedObjects)
        {
            if (kvp.Value.name.StartsWith("PlayerIcon_"))
            {
                toRemove.Add(kvp.Key);
            }
        }
        
        foreach (var coord in toRemove)
        {
            EraseObject(coord);
        }
    }

    private void DrawUtilityButtons()
    {
        EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Refresh Map"))
        {
            RefreshMapFromScene();
        }
        
        if (GUILayout.Button("Clear All"))
        {
            if (EditorUtility.DisplayDialog("Confirm", "모든 오브젝트를 삭제하시겠습니까?", "Yes", "No"))
            {
                ClearAllObjects();
            }
        }
        
        if (GUILayout.Button("Create UI Container"))
        {
            CreateUIMapContainer();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Generate Simple Maze"))
        {
            GenerateSimpleMaze();
        }
        
        if (GUILayout.Button("Apply Wall Settings to All"))
        {
            ApplyWallSettingsToAll();
        }
        
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 모든 벽에 현재 설정 적용
    /// </summary>
    private void ApplyWallSettingsToAll()
    {
        if (!EditorUtility.DisplayDialog("Confirm", "모든 벽에 현재 설정을 적용하시겠습니까?", "Yes", "No"))
            return;
            
        int appliedCount = 0;
        foreach (var kvp in placedObjects)
        {
            if (kvp.Value.name.StartsWith("Wall_"))
            {
                var wallTile = kvp.Value.GetComponent<WallTile>();
                if (wallTile != null)
                {
                    Undo.RecordObject(wallTile, "Apply Wall Settings");
                    wallTile.SetWallType(selectedWallType, wallBreakable);
                    wallTile.SetDisplayMode(wallDisplayMode);
                    
                    if (!useAutoMana)
                    {
                        wallTile.SetRequiredMana(customMana);
                    }
                    
                    EditorUtility.SetDirty(wallTile);
                    appliedCount++;
                }
            }
        }
        
        Debug.Log($"{LOG_PREFIX} Applied settings to {appliedCount} walls");
        EditorUtility.DisplayDialog("Success", $"{appliedCount}개의 벽에 설정이 적용되었습니다.", "OK");
    }

    private void CreateUIMapContainer()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "Canvas가 씬에 없습니다. Canvas를 먼저 생성해주세요.", "OK");
            return;
        }

        var containerObj = new GameObject("EnchantMapContainer");
        containerObj.transform.SetParent(canvas.transform, false);
        
        var rectTransform = containerObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        mapContainer = rectTransform;
        
        Debug.Log($"{LOG_PREFIX} Created UI Map Container");
        EditorUtility.DisplayDialog("Success", "UI Map Container가 생성되었습니다.", "OK");
    }

    private void RefreshMapFromScene()
    {
        placedObjects.Clear();
        
        if (mapContainer == null) 
        {
            Debug.LogWarning($"{LOG_PREFIX} MapContainer is not assigned. Please drag and assign a MapContainer.");
            return;
        }
        
        // 자식 오브젝트들을 placedObjects에 로드
        int loadedCount = 0;
        foreach (RectTransform child in mapContainer)
        {
            Vector2Int coord;
            if (TryParseCoordFromName(child.name, out coord))
            {
                placedObjects[coord] = child.gameObject;
                loadedCount++;
            }
            else if (TryParseCoordFromPosition(child, out coord))
            {
                // 이름으로 파싱 안되면 위치로 파싱 시도
                placedObjects[coord] = child.gameObject;
                child.name = GenerateObjectName(child.gameObject, coord);
                loadedCount++;
                Debug.Log($"{LOG_PREFIX} Generated name for object: {child.name}");
            }
        }
        
        Debug.Log($"{LOG_PREFIX} Refreshed map with {loadedCount} objects from container: {mapContainer.name}");
    }

    private bool TryParseCoordFromName(string objName, out Vector2Int coord)
    {
        coord = Vector2Int.zero;
        
        var parts = objName.Split('_');
        if (parts.Length >= 3)
        {
            if (int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y))
            {
                coord = new Vector2Int(x, y);
                return true;
            }
        }
        
        return false;
    }

    private bool TryParseCoordFromPosition(RectTransform rectTransform, out Vector2Int coord)
    {
        coord = Vector2Int.zero;
        
        var pos = rectTransform.anchoredPosition;
        
        // tileSize로 나누어서 좌표 계산 (절댓값 사용)
        int x = Mathf.RoundToInt(Mathf.Abs(pos.x) / tileSize);
        int y = Mathf.RoundToInt(Mathf.Abs(pos.y) / tileSize);
        
        // 유효한 좌표인지 확인 (맵 크기 범위 내)
        if (x >= 0 && y >= 0 && x < mapWidth && y < mapHeight)
        {
            coord = new Vector2Int(x, y);
            return true;
        }
        
        return false;
    }

    private string GenerateObjectName(GameObject obj, Vector2Int coord)
    {
        if (obj.GetComponent<WallTile>() != null)
            return $"Wall_{coord.x}_{coord.y}";
        else if (obj.GetComponent<SpellHolder>() != null)
            return $"SpellHolder_{coord.x}_{coord.y}";
        else if (obj.name.Contains("Player") || obj.name.Contains("Icon"))
            return $"PlayerIcon_{coord.x}_{coord.y}";
        else
            return $"Object_{coord.x}_{coord.y}";
    }

    private void CalculateMapSize()
    {
        if (placedObjects.Count == 0) return;
        
        int maxX = 0, maxY = 0;
        foreach (var coord in placedObjects.Keys)
        {
            maxX = Mathf.Max(maxX, coord.x);
            maxY = Mathf.Max(maxY, coord.y);
        }
        
        mapWidth = maxX + 3; // 여유분 추가
        mapHeight = maxY + 3;
        
        Debug.Log($"{LOG_PREFIX} Calculated map size: {mapWidth}x{mapHeight}");
    }

    private void SaveChangesToPrefab()
    {
        if (!isEditingExistingMap || mapContainer == null)
        {
            EditorUtility.DisplayDialog("Error", "저장할 기존 맵이 없습니다.", "OK");
            return;
        }
        
        // 프리팹 루트 찾기
        var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(mapContainer.gameObject);
        if (prefabRoot != null)
        {
            // 프리팹에 변경사항 적용
            PrefabUtility.ApplyPrefabInstance(prefabRoot, InteractionMode.UserAction);
            Debug.Log($"{LOG_PREFIX} Saved changes to prefab: {prefabRoot.name}");
            EditorUtility.DisplayDialog("Success", "변경사항이 프리팹에 저장되었습니다!", "OK");
        }
        else
        {
            // Scene 오브젝트인 경우 직접 저장
            EditorUtility.SetDirty(mapContainer.gameObject);
            Debug.Log($"{LOG_PREFIX} Marked scene object as dirty: {mapContainer.name}");
            EditorUtility.DisplayDialog("Success", "변경사항이 Scene에 저장되었습니다!", "OK");
        }
    }

    private void ResetToOriginalMap()
    {
        if (!EditorUtility.DisplayDialog("Confirm", "모든 변경사항을 되돌리고 원본 맵으로 리셋하시겠습니까?", "Yes", "No"))
            return;
            
        if (mapContainer != null)
        {
            var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(mapContainer.gameObject);
            if (prefabRoot != null)
            {
                PrefabUtility.RevertPrefabInstance(prefabRoot, InteractionMode.UserAction);
                Debug.Log($"{LOG_PREFIX} Reverted prefab to original: {prefabRoot.name}");
            }
        }
        
        // 맵 다시 로드
        RefreshMapFromScene();
        EditorUtility.DisplayDialog("Success", "원본 맵으로 리셋되었습니다.", "OK");
    }

    private void DrawSpellSettings()
    {
        EditorGUILayout.LabelField("Spell Settings", EditorStyles.boldLabel);
        
        // Spell 목록 로드 버튼
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Load Available Spells"))
        {
            LoadAvailableSpells();
        }
        if (GUILayout.Button("Refresh Spell List"))
        {
            LoadAvailableSpells();
        }
        EditorGUILayout.EndHorizontal();
        
        // Spell 선택 드롭다운
        if (availableSpells != null && availableSpells.Length > 0)
        {
            EditorGUILayout.LabelField($"Available Spells: {availableSpells.Length}", EditorStyles.helpBox);
            
            selectedSpellIndex = EditorGUILayout.Popup("Select Spell", selectedSpellIndex, spellNames);
            
            if (selectedSpellIndex >= 0 && selectedSpellIndex < availableSpells.Length)
            {
                selectedSpell = availableSpells[selectedSpellIndex];
                
                if (selectedSpell != null)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField($"Spell: {selectedSpell.spellName}");
                    EditorGUILayout.LabelField($"Description: {selectedSpell.description}");
                    
                    // 마나 비용 표시
                    EditorGUILayout.LabelField("Mana Cost:");
                    EditorGUI.indentLevel++;
                    if (selectedSpell.cost.fire > 0) EditorGUILayout.LabelField($"Fire: {selectedSpell.cost.fire}");
                    if (selectedSpell.cost.water > 0) EditorGUILayout.LabelField($"Water: {selectedSpell.cost.water}");
                    if (selectedSpell.cost.earth > 0) EditorGUILayout.LabelField($"Earth: {selectedSpell.cost.earth}");
                    if (selectedSpell.cost.air > 0) EditorGUILayout.LabelField($"Air: {selectedSpell.cost.air}");
                    EditorGUI.indentLevel--;
                    EditorGUI.indentLevel--;
                    
                    // 아이콘 미리보기
                    if (selectedSpell.spellIcon != null)
                    {
                        EditorGUILayout.LabelField("Icon Preview:");
                        var iconRect = GUILayoutUtility.GetRect(50, 50, GUILayout.Width(50), GUILayout.Height(50));
                        EditorGUI.DrawTextureTransparent(iconRect, selectedSpell.spellIcon.texture);
                    }
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No spells loaded. Click 'Load Available Spells' to scan for MagicSpell assets.", MessageType.Info);
        }
        
        // 수동 Spell 할당
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Manual Spell Assignment", EditorStyles.boldLabel);
        selectedSpell = (MagicSpell)EditorGUILayout.ObjectField("Manual Spell", selectedSpell, typeof(MagicSpell), false);
    }

    private void LoadAvailableSpells()
    {
        var guids = AssetDatabase.FindAssets("t:MagicSpell");
        availableSpells = new MagicSpell[guids.Length];
        spellNames = new string[guids.Length];
        
        for (int i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            availableSpells[i] = AssetDatabase.LoadAssetAtPath<MagicSpell>(path);
            
            if (availableSpells[i] != null)
            {
                spellNames[i] = $"{availableSpells[i].spellName} ({availableSpells[i].name})";
            }
            else
            {
                spellNames[i] = "Unknown Spell";
            }
        }
        
        selectedSpellIndex = 0;
        if (availableSpells.Length > 0)
        {
            selectedSpell = availableSpells[0];
        }
        
        Debug.Log($"{LOG_PREFIX} Loaded {availableSpells.Length} spells");
        EditorUtility.DisplayDialog("Success", $"{availableSpells.Length}개의 Spell을 로드했습니다!", "OK");
    }

    private void ClearAllObjects()
    {
        foreach (var obj in placedObjects.Values)
        {
            if (obj != null)
                DestroyImmediate(obj);
        }
        
        placedObjects.Clear();
        Debug.Log($"{LOG_PREFIX} Cleared all objects");
    }
    
    private void GenerateSimpleMaze()
    {
        if (wallUIPrefab == null || mapContainer == null)
        {
            EditorUtility.DisplayDialog("Error", "Wall Prefab과 Map Container가 필요합니다.", "OK");
            return;
        }

        ClearAllObjects();
        
        // 외곽 벽 생성
        for (int x = 0; x < mapWidth; x++)
        {
            PlaceWall(new Vector2Int(x, 0));           // 아래쪽
            PlaceWall(new Vector2Int(x, mapHeight - 1)); // 위쪽
        }
        
        for (int y = 0; y < mapHeight; y++)
        {
            PlaceWall(new Vector2Int(0, y));           // 왼쪽
            PlaceWall(new Vector2Int(mapWidth - 1, y)); // 오른쪽
        }
        
        // 내부 미로 패턴 (간단한 버전)
        for (int x = 2; x < mapWidth - 2; x += 2)
        {
            for (int y = 2; y < mapHeight - 2; y += 2)
            {
                PlaceWall(new Vector2Int(x, y));
                
                // 랜덤하게 한 방향으로 벽 연장
                int direction = Random.Range(0, 4);
                Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                var extendCoord = new Vector2Int(x, y) + dirs[direction];
                
                if (extendCoord.x > 0 && extendCoord.x < mapWidth - 1 && 
                    extendCoord.y > 0 && extendCoord.y < mapHeight - 1)
                {
                    PlaceWall(extendCoord);
                }
            }
        }
        
        Debug.Log($"{LOG_PREFIX} Generated simple maze");
    }
    
    private void OnEnable()
    {
        // Spell 목록은 수동으로 로드
        Debug.Log($"{LOG_PREFIX} Editor opened. Please manually assign Map Container and load spells if needed.");
    }
    
    private void LoadMapFromContainer(RectTransform container)
    {
        if (container == null) return;
        
        mapContainer = container;
        originalMapName = container.name;
        isEditingExistingMap = true;
        
        placedObjects.Clear();
        
        // 기존 오브젝트들 분석 및 로드
        int loadedCount = 0;
        foreach (RectTransform child in container)
        {
            Vector2Int coord;
            if (TryParseCoordFromName(child.name, out coord))
            {
                placedObjects[coord] = child.gameObject;
                loadedCount++;
            }
            else if (TryParseCoordFromPosition(child, out coord))
            {
                // 이름으로 파싱 안되면 위치로 파싱 시도
                placedObjects[coord] = child.gameObject;
                child.name = GenerateObjectName(child.gameObject, coord);
                loadedCount++;
            }
        }
        
        // 맵 크기 자동 계산
        CalculateMapSize();
        
        Debug.Log($"{LOG_PREFIX} Loaded existing map '{originalMapName}' with {loadedCount} objects");
        EditorUtility.DisplayDialog("Success", $"기존 맵 로드 완료!\n오브젝트: {loadedCount}개\n맵 크기: {mapWidth}x{mapHeight}", "OK");
    }
    

} 