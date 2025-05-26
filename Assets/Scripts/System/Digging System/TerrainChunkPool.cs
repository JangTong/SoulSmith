// TerrainChunkPool.cs
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunkPool : MonoBehaviour
{
    public static TerrainChunkPool Instance { get; private set; }
    [SerializeField] private TerrainChunk chunkPrefab;
    [SerializeField] private int initialPoolSize = 10;

    private readonly Queue<TerrainChunk> pool = new Queue<TerrainChunk>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        for (int i = 0; i < initialPoolSize; i++)
        {
            var inst = Instantiate(chunkPrefab, transform);
            inst.gameObject.SetActive(false);
            pool.Enqueue(inst);
        }
    }

    public TerrainChunk Get()
    {
        if (pool.Count == 0)
        {
            var inst = Instantiate(chunkPrefab, transform);
            pool.Enqueue(inst);
        }
        var chunk = pool.Dequeue();
        chunk.gameObject.SetActive(true);
        return chunk;
    }

    public void Return(TerrainChunk chunk)
    {
        //chunk.Disable();
        chunk.gameObject.SetActive(false);
        pool.Enqueue(chunk);
    }
}