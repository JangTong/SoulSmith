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

    [Header("Map Settings")]
    private int mapWidth = 10;
    private int mapHeight = 10;
    private float tileSize = 50f; // UI 타일 크기 (픽셀)
    private RectTransform mapContainer;
    
    [Header("UI Prefabs")]
    private GameObject wallUIPrefab;
    private GameObject spellHolderUIPrefab;
    private GameObject playerIconPrefab;
    
    [Header("Editing")]
    private EditMode currentMode = EditMode.Wall;
    private Vector2Int selectedCoord = Vector2Int.zero;
    private Dictionary<Vector2Int, GameObject> placedObjects = new Dictionary<Vector2Int, GameObject>();
    
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
        
        DrawMapGrid();
        EditorGUILayout.Space();
        
        DrawUtilityButtons();
        
        EditorGUILayout.EndScrollView();
    }

    private void DrawMapSettings()
    {
        EditorGUILayout.LabelField("Map Settings", EditorStyles.boldLabel);
        
        mapContainer = (RectTransform)EditorGUILayout.ObjectField("Map Container (RectTransform)", mapContainer, typeof(RectTransform), true);
        
        EditorGUILayout.BeginHorizontal();
        mapWidth = EditorGUILayout.IntSlider("Width", mapWidth, 5, 30);
        mapHeight = EditorGUILayout.IntSlider("Height", mapHeight, 5, 30);
        EditorGUILayout.EndHorizontal();
        
        tileSize = EditorGUILayout.FloatField("UI Tile Size (px)", tileSize);
        showGrid = EditorGUILayout.Toggle("Show Grid", showGrid);
        
        if (mapContainer != null)
        {
            EditorGUILayout.HelpBox($"Map Container: {mapContainer.name} (UI)", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("RectTransform이 필요합니다 (Canvas 자식)", MessageType.Warning);
        }
    }

    private void DrawPrefabSettings()
    {
        EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);
        
        wallUIPrefab = (GameObject)EditorGUILayout.ObjectField("Wall Prefab", wallUIPrefab, typeof(GameObject), false);
        spellHolderUIPrefab = (GameObject)EditorGUILayout.ObjectField("SpellHolder Prefab", spellHolderUIPrefab, typeof(GameObject), false);
        playerIconPrefab = (GameObject)EditorGUILayout.ObjectField("Player Icon Prefab", playerIconPrefab, typeof(GameObject), false);
        
        EditorGUILayout.HelpBox("모든 프리팹은 UI 요소(Image 등)여야 합니다", MessageType.Info);
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

    private Color GetTileColor(Vector2Int coord, bool hasObject)
    {
        if (coord == selectedCoord)
            return Color.yellow;
        
        if (!hasObject)
            return Color.white;
            
        var obj = placedObjects[coord];
        if (obj.name.StartsWith("Wall_"))
            return Color.red;
        else if (obj.name.StartsWith("SpellHolder_"))
            return Color.blue;
        else if (obj.name.StartsWith("PlayerIcon_"))
            return Color.green;
            
        return Color.gray;
    }

    private string GetObjectSymbol(Vector2Int coord)
    {
        if (!placedObjects.ContainsKey(coord))
            return "·";
            
        var obj = placedObjects[coord];
        if (obj.name.StartsWith("Wall_"))
            return "█";
        else if (obj.name.StartsWith("SpellHolder_"))
            return "◆";
        else if (obj.name.StartsWith("PlayerIcon_"))
            return "●";
            
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
        if (wallUIPrefab == null || mapContainer == null)
        {
            EditorUtility.DisplayDialog("Error", "Wall Prefab이나 Map Container가 설정되지 않았습니다.", "OK");
            return;
        }

        EraseObject(coord); // 기존 오브젝트 제거
        
        var wallObj = PrefabUtility.InstantiatePrefab(wallUIPrefab, mapContainer) as GameObject;
        wallObj.name = $"Wall_{coord.x}_{coord.y}";
        
        // UI 위치 설정 (RectTransform)
        var rectTransform = wallObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(coord.x * tileSize, coord.y * tileSize);
            rectTransform.sizeDelta = new Vector2(tileSize, tileSize);
        }
        
        // WallTile 컴포넌트 설정
        var wallTile = wallObj.GetComponent<WallTile>();
        if (wallTile != null)
        {
            wallTile.SetCoordinate(coord);
        }
        
        placedObjects[coord] = wallObj;
        Debug.Log($"{LOG_PREFIX} Placed wall at {coord}");
    }

    private void PlaceSpellHolder(Vector2Int coord)
    {
        if (spellHolderUIPrefab == null || mapContainer == null)
        {
            EditorUtility.DisplayDialog("Error", "SpellHolder Prefab이나 Map Container가 설정되지 않았습니다.", "OK");
            return;
        }

        EraseObject(coord);
        
        var holderObj = PrefabUtility.InstantiatePrefab(spellHolderUIPrefab, mapContainer) as GameObject;
        holderObj.name = $"SpellHolder_{coord.x}_{coord.y}";
        
        // UI 위치 설정
        var rectTransform = holderObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(coord.x * tileSize, coord.y * tileSize);
            rectTransform.sizeDelta = new Vector2(tileSize, tileSize);
        }
        
        // SpellHolder 컴포넌트 설정
        var spellHolder = holderObj.GetComponent<SpellHolder>();
        if (spellHolder != null)
        {
            spellHolder.SetCoordinate(coord);
        }
        
        placedObjects[coord] = holderObj;
        Debug.Log($"{LOG_PREFIX} Placed SpellHolder at {coord}");
    }

    private void PlacePlayerIcon(Vector2Int coord)
    {
        if (playerIconPrefab == null || mapContainer == null)
        {
            EditorUtility.DisplayDialog("Error", "Player Icon Prefab이나 Map Container가 설정되지 않았습니다.", "OK");
            return;
        }

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
        
        if (GUILayout.Button("Generate Simple Maze"))
        {
            GenerateSimpleMaze();
        }
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
        
        if (mapContainer == null) return;
        
        foreach (RectTransform child in mapContainer)
        {
            Vector2Int coord;
            if (TryParseCoordFromName(child.name, out coord))
            {
                placedObjects[coord] = child.gameObject;
            }
        }
        
        Debug.Log($"{LOG_PREFIX} Refreshed map with {placedObjects.Count} objects");
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
        RefreshMapFromScene();
    }
} 