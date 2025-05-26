// ChunkManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class ChunkManager : MonoBehaviour
{
    public static ChunkManager Instance { get; private set; }

    [Header("설정")]
    public Transform player;
    public GameObject chunkPrefab;
    public int chunkWidth = 30;
    public int height = 10;
    public float resolution = 1f;
    public int viewRadius = 2;
    public int verticalRadius = 1;
    public int maxRadius = 4;
    public bool centerPivot = true;
    public int prewarmChunkCount = 100;
    public int chunksPerFrame = 5;
    public int maxChunkPoolSize = 300;
    public float unloadDistance = 400f;

    private Dictionary<Vector3Int, TerrainChunk> loadedChunks = new();
    private Queue<TerrainChunk> chunkPool = new();
    private LinkedList<Vector3Int> lruList = new();
    private float chunkWorldSize;
    private float chunkHeight;
    private Vector3Int previousChunkCoord;
    private Coroutine currentLoader;

    private void Awake()
    {
        Instance = this;

        if (player == null && Camera.main != null)
            player = Camera.main.transform;
        if (player == null)
            Debug.LogError("❌ 플레이어 또는 카메라를 찾을 수 없습니다.");

        chunkWorldSize = chunkWidth * resolution;
        chunkHeight = height * resolution;
        previousChunkCoord = GetChunkCoord(player.position);
        PrewarmPool();
    }

    private void Start()
    {
        UpdateVisibleChunks();
    }

    private void Update()
    {
        Vector3Int currentCoord = GetChunkCoord(player.position);
        if (currentCoord != previousChunkCoord)
        {
            previousChunkCoord = currentCoord;
            UpdateVisibleChunks();
        }

        UnloadFarChunks();
    }

    Vector3Int GetChunkCoord(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(worldPos.x / chunkWorldSize);
        int y = Mathf.FloorToInt(worldPos.y / chunkHeight);
        int z = Mathf.FloorToInt(worldPos.z / chunkWorldSize);
        return new Vector3Int(x, y, z);
    }

    void UpdateVisibleChunks()
    {
        Vector3Int center = GetChunkCoord(player.position);
        HashSet<Vector3Int> needed = new();
        List<Vector3Int> nearChunks = new();
        List<Vector3Int> farChunks = new();

        for (int dx = -maxRadius; dx <= maxRadius; dx++)
        for (int dy = -verticalRadius; dy <= verticalRadius; dy++)
        for (int dz = -maxRadius; dz <= maxRadius; dz++)
        {
            Vector3Int coord = new Vector3Int(center.x + dx, center.y + dy, center.z + dz);
            float distXZ = new Vector2(dx, dz).magnitude;

            needed.Add(coord);
            if (!loadedChunks.ContainsKey(coord))
            {
                if (distXZ <= viewRadius)
                    nearChunks.Add(coord);
                else if (distXZ <= maxRadius)
                    farChunks.Add(coord);
            }
        }

        nearChunks.Sort((a, b) => Vector3Int.Distance(a, center).CompareTo(Vector3Int.Distance(b, center)));
        farChunks.Sort((a, b) => Vector3Int.Distance(a, center).CompareTo(Vector3Int.Distance(b, center)));

        if (currentLoader != null)
            StopCoroutine(currentLoader);
        currentLoader = StartCoroutine(LoadChunksInOrder(nearChunks, farChunks));
    }

    void UnloadFarChunks()
    {
        Vector3 playerPos = player.position;
        LinkedListNode<Vector3Int> node = lruList.First;
        while (node != null)
        {
            Vector3Int coord = node.Value;
            Vector3 chunkPos = new Vector3(coord.x * chunkWorldSize, coord.y * chunkHeight, coord.z * chunkWorldSize);
            if (centerPivot)
                chunkPos += new Vector3(chunkWorldSize, chunkHeight, chunkWorldSize) * 0.5f;

            float dist = Vector3.Distance(playerPos, chunkPos);
            if (dist > unloadDistance && loadedChunks.ContainsKey(coord))
            {
                var next = node.Next;
                TerrainChunk chunk = loadedChunks[coord];
                chunk.gameObject.SetActive(false);
                if (chunkPool.Count < maxChunkPoolSize)
                    chunkPool.Enqueue(chunk);
                else
                    Destroy(chunk.gameObject);

                loadedChunks.Remove(coord);
                lruList.Remove(node);
                node = next;
            }
            else
            {
                node = node.Next;
            }
        }
    }

    IEnumerator LoadChunksInOrder(List<Vector3Int> near, List<Vector3Int> far)
    {
        int count = 0;
        foreach (var coord in near)
        {
            if (loadedChunks.ContainsKey(coord)) continue;
            LoadChunk(coord);
            count++;
            if (count >= chunksPerFrame)
            {
                count = 0;
                yield return null;
            }
        }

        foreach (var coord in far)
        {
            if (loadedChunks.ContainsKey(coord)) continue;
            LoadChunk(coord);
            count++;
            if (count >= chunksPerFrame)
            {
                count = 0;
                yield return null;
            }
        }
    }

    void LoadChunk(Vector3Int coord)
    {
        TerrainChunk chunk = GetChunkFromPool();
        Vector3 pos = new Vector3(coord.x * chunkWorldSize, coord.y * chunkHeight, coord.z * chunkWorldSize);
        if (centerPivot)
            pos += new Vector3(chunkWorldSize, chunkHeight, chunkWorldSize) * 0.5f;

        chunk.transform.position = pos;
        chunk.chunkCoord = coord;
        chunk.gameObject.SetActive(true);

        loadedChunks.Add(coord, chunk);
        lruList.AddLast(coord);
    }

    TerrainChunk GetChunkFromPool()
    {
        if (chunkPool.Count > 0)
        {
            return chunkPool.Dequeue();
        }
        else
        {
            Debug.LogWarning("[ChunkManager] ChunkPool exhausted! Instantiating new chunk.");
            GameObject go = Instantiate(chunkPrefab, Vector3.zero, Quaternion.identity, transform);
            return go.GetComponent<TerrainChunk>();
        }
    }

    void PrewarmPool()
    {
        for (int i = 0; i < prewarmChunkCount; i++)
        {
            GameObject go = Instantiate(chunkPrefab, Vector3.one * 9999f, Quaternion.identity, transform);
            TerrainChunk chunk = go.GetComponent<TerrainChunk>();
            chunk.gameObject.SetActive(false);
            chunkPool.Enqueue(chunk);
        }
    }
}
