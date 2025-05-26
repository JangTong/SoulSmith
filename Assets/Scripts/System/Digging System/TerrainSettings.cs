// TerrainSettings.cs
using UnityEngine;

[CreateAssetMenu(fileName = "TerrainSettings", menuName = "SoulSmith/TerrainSettings")]
public class TerrainSettings : ScriptableObject
{
    [Header("Chunk Parameters")]
    public float chunkSize = 16f;      // Size of one chunk in world units
    [Range(2, 256)]
    public int resolution = 32;        // Number of voxels per side

    [Header("Noise Settings")]
    public float noiseScale = 0.1f;
    public float noiseStrength = 5f;
    public float surfaceLevel = 0f;    // World-space surface height
}