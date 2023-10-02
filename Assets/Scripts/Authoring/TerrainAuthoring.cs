using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class TerrainAuthoring : MonoBehaviour
{
    [Header("Terrain Size")]
    public int Width;
    public int Height;
    public int Depth;

    [Header("Chunk Size")]
    public int ChunkWidth;
    public int ChunkHeight;
    public int ChunkDepth;

    [Header("Noise Settings")]
    public float NoiseScale;
    public float Threshold;
    public int NoiseDropOffHeight;
    public int NoiseDropOffDepth;

    [Header("Empty Chunk")]
    public GameObject EmptyChunk;
    public GameObject ChunkBorder;

    [Header("Brush")]
    public GameObject Brush;

    class Baker : Baker<TerrainAuthoring>
    {
        public override void Bake(TerrainAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Terrain
            {
                Width = authoring.Width,
                Height = authoring.Height,
                Depth = authoring.Depth,
                ChunkWidth = authoring.ChunkWidth,
                ChunkHeight = authoring.ChunkHeight,
                ChunkDepth = authoring.ChunkDepth,
                NoiseScale = authoring.NoiseScale,
                Threshold = authoring.Threshold,
                NoiseDropOffDepth = authoring.NoiseDropOffDepth,
                NoiseDropOffHeight = authoring.NoiseDropOffHeight,
                EmptyChunk = GetEntity(authoring.EmptyChunk, TransformUsageFlags.None),
                ChunkBorder = GetEntity(authoring.ChunkBorder, TransformUsageFlags.None),
            });
            AddComponent(entity, new BrushData
            {
                Prefab = GetEntity(authoring.Brush, TransformUsageFlags.Dynamic),
                BrushSize = 1
            });
        }
    }
}

public struct Terrain : IComponentData
{
    // Size Settings
    public int Width;
    public int Height;
    public int Depth;

    // Chunk Settings
    public int ChunkWidth;
    public int ChunkHeight;
    public int ChunkDepth;

    // Noise Settings
    public float NoiseScale;
    public float Threshold;
    public int NoiseDropOffHeight;
    public int NoiseDropOffDepth;

    public Entity EmptyChunk;
    public Entity ChunkBorder;
}

public struct Chunk : IComponentData
{
    public float3 BaseCoords;

    public float Width;
    public float Height;
    public float Depth;
}

public struct BrushData : IComponentData
{
    public Entity Prefab;
    public Entity Instance;
    public float BrushSize;
}

public struct GenerateTerrain : IComponentData { }

public struct TerrainEditing : IComponentData
{
    public EditingModes Mode;
}

public enum EditingModes
{
    ADD,
    REMOVE
}