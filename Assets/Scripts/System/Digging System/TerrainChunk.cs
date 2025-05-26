// TerrainChunk.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static FastNoiseLite;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TerrainChunk : MonoBehaviour
{
    [SerializeField] private int width = 30;
    [SerializeField] private int height = 10;
    [SerializeField] private float resolution = 1f;
    [SerializeField] private float noiseScale = 1f;
    [SerializeField] private float surfaceLevel = 0f;
    [SerializeField] private float groundThreshold = 10f;
    [SerializeField] private bool sealEdges = false;
    [SerializeField] private bool useNoise = true;
    [SerializeField] private bool use3DNoise = false;
    [SerializeField] private bool visualizeNoise = false;

    public Vector3Int chunkCoord;

    private float[,,] heights;
    private List<Vector3> vertices = new();
    private List<int> triangles = new();

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private FastNoiseLite noise;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        noise = new FastNoiseLite();
        noise.SetSeed(1337);
        noise.SetFrequency(noiseScale);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

        GenerateDensityField();
        GenerateMesh();
    }

    void GenerateDensityField()
    {
        heights = new float[width + 1, height + 1, width + 1];

        for (int x = 0; x <= width; x++)
        for (int y = 0; y <= height; y++)
        for (int z = 0; z <= width; z++)
        {
            float worldX = (x + chunkCoord.x * width) * resolution;
            float worldY = (y + chunkCoord.y * height) * resolution;
            float worldZ = (z + chunkCoord.z * width) * resolution;

            float density;

            if (!useNoise)
            {
                density = worldY - height * resolution / 2f;
            }
            else if (worldY >= groundThreshold)
            {
                float heightValue = height * resolution * noise.GetNoise(worldX, worldZ);
                density = worldY - heightValue;
            }
            else
            {
                float val = noise.GetNoise(worldX, worldY, worldZ);
                density = val - 0.5f;
            }

            if (sealEdges && (x == 0 || x == width || y == 0 || y == height || z == 0 || z == width))
            {
                heights[x, y, z] = 10f;
                continue;
            }

            heights[x, y, z] = density;
        }
    }

    void GenerateMesh()
    {
        vertices.Clear();
        triangles.Clear();

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        for (int z = 0; z < width; z++)
        {
            float[] cube = new float[8];
            for (int i = 0; i < 8; i++)
            {
                Vector3Int c = new Vector3Int(x, y, z) + MarchingTable.Corners[i];
                cube[i] = heights[c.x, c.y, c.z];
            }

            Vector3 pos = new Vector3(x, y, z) * resolution;
            MarchCube(pos, cube);
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
    }

    void MarchCube(Vector3 position, float[] cube)
    {
        int configIndex = 0;
        for (int i = 0; i < 8; i++)
            if (cube[i] > surfaceLevel)
                configIndex |= 1 << i;

        if (configIndex == 0 || configIndex == 255) return;

        int edgeIndex = 0;
        for (int t = 0; t < 5; t++)
        for (int v = 0; v < 3; v++)
        {
            int edge = MarchingTable.Triangles[configIndex, edgeIndex];
            if (edge == -1) return;

            Vector3 start = position + MarchingTable.Edges[edge, 0] * resolution;
            Vector3 end = position + MarchingTable.Edges[edge, 1] * resolution;
            Vector3 vertex = (start + end) / 2f;

            vertices.Add(vertex);
            triangles.Add(vertices.Count - 1);
            edgeIndex++;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!visualizeNoise || heights == null) return;

        for (int x = 0; x <= width; x++)
        for (int y = 0; y <= height; y++)
        for (int z = 0; z <= width; z++)
        {
            float d = heights[x, y, z];
            Gizmos.color = new Color(d, d, d);
            Gizmos.DrawSphere(new Vector3(x, y, z) * resolution, 0.1f * resolution);
        }
    }

    public void DigAtWorldPosition(Vector3 worldPos, float radius, float intensity)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPos) / resolution;

        for (int x = 0; x <= width; x++)
        for (int y = 0; y <= height; y++)
        for (int z = 0; z <= width; z++)
        {
            Vector3 point = new Vector3(x, y, z);
            float dist = Vector3.Distance(point, localPos);
            if (dist < radius)
            {
                heights[x, y, z] += intensity * (1f - dist / radius);
                heights[x, y, z] = Mathf.Clamp(heights[x, y, z], -1f, 1f);
            }
        }

        GenerateMesh();
    }
}
