using JetBrains.Annotations;
using System.Collections;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Mesh;

public partial struct TerrainSystem : ISystem
{
    private NativeArray<Entity> ChunkEntities;
    private NativeArray<float> TerrainData;
    private ComponentLookup<Chunk> ChunkLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        ChunkLookup = state.GetComponentLookup<Chunk>();
    }

    public void OnUpdate(ref SystemState state)
    {
        // Check if terrain should be generated from scratch
        if (SystemAPI.HasSingleton<GenerateTerrain>())
        {
            StartTerrainGeneration(ref state);
            return;
        }

        // Skip editing step if no terrain exists
        if (ChunkEntities.Length == 0 || !SystemAPI.HasSingleton<TerrainEditing>())
            return;

        NativeArray<Entity> modifiedChunks;
        MeshDataArray meshDataArray = ModifyTerrain(ref state, out modifiedChunks);

        if (meshDataArray.Length > 0)
            UpdateMeshes(ref state, modifiedChunks, meshDataArray);

        modifiedChunks.Dispose();
    }

    public void StartTerrainGeneration(ref SystemState state)
    {
        // Delete old entities if existing
        foreach (Entity chunk in ChunkEntities)
            state.EntityManager.DestroyEntity(chunk);

        // Start generation of new terrain with current settings
        MeshDataArray meshDataArray = TerrainSetup(ref state);
        GenerateTerrainMeshes(ref state, meshDataArray, ChunkEntities);

        // Remove generate component
        state.EntityManager.RemoveComponent<GenerateTerrain>(SystemAPI.GetSingletonEntity<GenerateTerrain>());
    }

    [BurstCompile]
    public MeshDataArray TerrainSetup(ref SystemState state)
    {
        Terrain terrain = SystemAPI.GetSingleton<Terrain>();

        // Setup terrain data array
        int arrayLength = (terrain.Width + 1) * (terrain.Height + 1) * (terrain.Depth + 1);
        TerrainData = new NativeArray<float>(arrayLength, Allocator.Persistent);

        // Generate noise for terrain landscape
        var noiseJob = new NoiseJob
        {
            TerrainSettings = terrain,
            TerrainData = TerrainData,
        };
        noiseJob.Schedule(arrayLength, math.max(1, arrayLength / 8)).Complete();

        // Calculate chunk data
        int chunksX = terrain.Width / terrain.ChunkWidth;
        int chunksY = terrain.Height / terrain.ChunkHeight;
        int chunksZ = terrain.Depth / terrain.ChunkDepth;

        NativeList<float3> ChunkCoords = new NativeList<float3>(Allocator.TempJob);

        for (int cx = 0; cx < chunksX; cx++)
            for (int cy = 0; cy < chunksY; cy++)
                for (int cz = 0; cz < chunksZ; cz++)
                    ChunkCoords.Add(new float3(cx * terrain.ChunkWidth, cy * terrain.ChunkHeight, cz * terrain.ChunkDepth));

        MeshDataArray MeshDataArray = AllocateWritableMeshData(ChunkCoords.Length);

        // Generate mesh data (vertices and triangles) for all chunks
        var generateJob = new GenerateJob
        {
            TerrainData = TerrainData,
            ChunkCoords = ChunkCoords.AsArray(),
            Terrain = terrain,
            MeshDataArray = MeshDataArray,
        };
        generateJob.Schedule(ChunkCoords.Length, math.max(1, ChunkCoords.Length / 8)).Complete();

        // Create chunk entities
        ChunkEntities = new NativeArray<Entity>(ChunkCoords.Length, Allocator.Persistent);

        for (int i = 0; i < ChunkCoords.Length; i++)
        {
            Entity chunkEntity = state.EntityManager.Instantiate(terrain.EmptyChunk);
            state.EntityManager.SetComponentData(chunkEntity, new LocalTransform()
            {
                Position = ChunkCoords[i],
                Rotation = quaternion.identity,
                Scale = 1f
            });

            state.EntityManager.AddComponentData(chunkEntity, new Chunk
            {
                BaseCoords = ChunkCoords[i],
                Width = terrain.ChunkWidth,
                Height = terrain.ChunkHeight,
                Depth = terrain.ChunkDepth,
            });

            Entity chunkBorder = state.EntityManager.Instantiate(terrain.ChunkBorder);

            var boxCollider = Unity.Physics.BoxCollider.Create(new BoxGeometry
            {
                Center = new float3(terrain.ChunkWidth, terrain.ChunkHeight, terrain.ChunkDepth) / 2.0f,
                Size = new float3(terrain.ChunkWidth, terrain.ChunkHeight, terrain.ChunkDepth),
                Orientation = quaternion.identity,
                BevelRadius = 0f,
            }, new CollisionFilter
            {
                BelongsTo = 32768u, // Border
                CollidesWith = ~0u,
                GroupIndex = 0,
            });

            state.EntityManager.SetComponentData(chunkBorder, new PhysicsCollider
            {
                Value = boxCollider
            });

            state.EntityManager.AddComponentData(chunkBorder, new Parent
            {
                Value = chunkEntity
            });

            ChunkEntities[i] = chunkEntity;
        }

        ChunkCoords.Dispose();

        // Spawn brush
        RefRW<BrushData> brushData = SystemAPI.GetSingletonRW<BrushData>();
        brushData.ValueRW.Instance = state.EntityManager.Instantiate(brushData.ValueRO.Prefab);
        state.EntityManager.SetEnabled(brushData.ValueRW.Instance, false);

        return MeshDataArray;
    }

    public void GenerateTerrainMeshes(ref SystemState state, MeshDataArray MeshDataArray, NativeArray<Entity> chunks)
    {
        Mesh[] meshes = new Mesh[chunks.Length];
        for (int i = 0; i < chunks.Length; i++)
            meshes[i] = new Mesh();

        ApplyAndDisposeWritableMeshData(MeshDataArray, meshes);

        // Apply new meshes
        for (int i = 0; i < chunks.Length; i++)
        {
            meshes[i].RecalculateBounds();
            meshes[i].RecalculateNormals();

            // Define mesh rendering components
            RenderMeshDescription renderMeshDescription = new RenderMeshDescription(ShadowCastingMode.On, true);
            RenderMeshArray renderMeshArray = new RenderMeshArray( new[] { Resources.Load<UnityEngine.Material>("Terrain") }, new[] { meshes[i] } );
            MaterialMeshInfo materialMeshInfo = MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0);

            // Add rendering components to entity
            RenderMeshUtility.AddComponents(chunks[i], state.EntityManager, renderMeshDescription, renderMeshArray, materialMeshInfo);

            // Add collider to mesh
            NativeArray<Vector3> vertices = new NativeArray<Vector3>(meshes[i].vertices, Allocator.Temp);
            NativeArray<int> triangles = new NativeArray<int>(meshes[i].triangles, Allocator.Temp);
            NativeArray<int3> triangles3 = new NativeArray<int3>(meshes[i].triangles.Length / 3, Allocator.Temp);
            for (int t = 0; t < triangles3.Length; t++)
                triangles3[t] = new int3(triangles[t * 3], triangles[t * 3 + 1], triangles[t * 3 + 2]);

            BlobAssetReference<Unity.Physics.Collider> meshCollider = Unity.Physics.MeshCollider.Create(vertices.Reinterpret<float3>(), triangles3);

            state.EntityManager.SetComponentData(chunks[i], new PhysicsCollider
            {
                Value = meshCollider
            });
        }
    }

    [BurstCompile]
    public MeshDataArray ModifyTerrain(ref SystemState state, out NativeArray<Entity> chunks)
    {
        ChunkLookup.Update(ref state);

        // Terrain Editing
        CameraData cameraData = SystemAPI.GetSingleton<CameraData>();
        BrushData brushData = SystemAPI.GetSingleton<BrushData>();
        CollisionWorld CollisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        Terrain Terrain = SystemAPI.GetSingleton<Terrain>();

        if (cameraData.Intersects && !Input.GetMouseButton(1))
        {
            if (!state.EntityManager.IsEnabled(brushData.Instance))
                state.EntityManager.SetEnabled(brushData.Instance, true);

            state.EntityManager.SetComponentData(brushData.Instance, new LocalTransform
            {
                Position = cameraData.Intersection,
                Rotation = quaternion.identity,
                Scale = brushData.BrushSize,
            });
        }
        else
        {
            if (state.EntityManager.IsEnabled(brushData.Instance))
                state.EntityManager.SetEnabled(brushData.Instance, false);
        }

        // Handle keyboard input
        if (Input.GetKeyDown(KeyCode.R))
            brushData.BrushSize += 1f;
        if (Input.GetKeyDown(KeyCode.F))
            brushData.BrushSize -= 1f;
        brushData.BrushSize = math.clamp(brushData.BrushSize, 1.0f, 20.0f);

        // Save brush changes
        SystemAPI.GetSingletonRW<BrushData>().ValueRW = brushData;

        // Handle terrain editing
        if (Input.GetMouseButton(0) && !Input.GetMouseButton(1))
        {
            TerrainEditing editingData = SystemAPI.GetSingleton<TerrainEditing>();

            // Get Chunks which needs to be modified
            NativeList<DistanceHit> chunkHits = new NativeList<DistanceHit>(Allocator.Temp);
            PointDistanceInput chunkInput = new PointDistanceInput
            {
                Position = cameraData.Intersection,
                MaxDistance = brushData.BrushSize,
                Filter = new CollisionFilter
                {
                    BelongsTo = ~0u, // Everything
                    CollidesWith = 32768u, // Chunk Borders
                    GroupIndex = 0,
                }
            };

            CollisionWorld.CalculateDistance(chunkInput, ref chunkHits);

            NativeHashSet<Entity> distinctHits = new NativeHashSet<Entity>(chunkHits.Length, Allocator.Temp);
            foreach (DistanceHit cHit in chunkHits)
                if (state.EntityManager.HasComponent<Parent>(cHit.Entity))
                    distinctHits.Add(state.EntityManager.GetComponentData<Parent>(cHit.Entity).Value);

            NativeArray<Entity> chunkHitArray = distinctHits.ToNativeArray(Allocator.TempJob);

            NativeArray<float3> chunkCoords = new NativeArray<float3>(chunkHitArray.Length, Allocator.TempJob);

            // Modify values in terrain data array
            for (int i = 0; i < chunkHitArray.Length; i++)
            {
                ChunkLookup.Update(ref state);

                Chunk chunkData = ChunkLookup[chunkHitArray[i]];

                chunkCoords[i] = chunkData.BaseCoords;

                for (int x = (int)chunkData.BaseCoords.x; x < (int)chunkData.BaseCoords.x + chunkData.Width; x++)
                {
                    for (int y = (int)chunkData.BaseCoords.y; y < (int)chunkData.BaseCoords.y + chunkData.Height; y++)
                    {
                        for (int z = (int)chunkData.BaseCoords.z; z < (int)chunkData.BaseCoords.z + chunkData.Depth; z++)
                        {
                            float distance = math.distance(new float3(x, y, z), cameraData.Intersection);

                            if (distance < brushData.BrushSize / 2)
                            {
                                float value = GetValueAtPosition(TerrainData, new float3(x, y, z), Terrain.Width, Terrain.Depth);

                                if (editingData.Mode == EditingModes.ADD)
                                    value += 0.2f * SystemAPI.Time.DeltaTime;
                                if (editingData.Mode == EditingModes.REMOVE)
                                    value -= 0.2f * SystemAPI.Time.DeltaTime;

                                SetValueAtPosition(TerrainData, new float3(x, y, z), Terrain.Width, Terrain.Depth, value);
                            }
                        }
                    }
                }
            }

            // Generate new meshes
            MeshDataArray meshDataArray = AllocateWritableMeshData(chunkHitArray.Length);

            var generateJob = new GenerateJob
            {
                TerrainData = TerrainData,
                ChunkCoords = chunkCoords,
                Terrain = Terrain,
                MeshDataArray = meshDataArray,
            };
            generateJob.Schedule(chunkHitArray.Length, math.max(1, chunkHitArray.Length / 8)).Complete();

            chunks = new NativeArray<Entity>(chunkHitArray, Allocator.TempJob);

            chunkCoords.Dispose();
            chunkHitArray.Dispose();

            return meshDataArray;
        }
        else
        {
            chunks = new NativeArray<Entity>();
        }

        return new MeshDataArray();
    }

    public void UpdateMeshes(ref SystemState state, NativeArray<Entity> chunks, MeshDataArray meshDataArray)
    {
        NativeArray<BlobAssetReference<Unity.Physics.Collider>> BlobCollider = new NativeArray<BlobAssetReference<Unity.Physics.Collider>>(1, Allocator.TempJob);

        for (int i = 0; i < chunks.Length; i++)
        {
            RenderMeshArray array = state.EntityManager.GetSharedComponentManaged<RenderMeshArray>(chunks[i]);

            array.Meshes[0].Clear();

            // Apply new mesh
            array.Meshes[0].SetVertices(meshDataArray[i].GetVertexData<float3>().Reinterpret<Vector3>().ToArray());
            array.Meshes[0].SetTriangles(meshDataArray[i].GetIndexData<int>().ToArray(), 0);

            // Optimize mesh
            array.Meshes[0].OptimizeIndexBuffers();
            array.Meshes[0].OptimizeReorderVertexBuffer();
            array.Meshes[0].RecalculateBounds();
            array.Meshes[0].RecalculateNormals();

            var Verts = new NativeArray<float3>(meshDataArray[i].GetVertexData<float3>(), Allocator.TempJob);
            var Tris = new NativeArray<int>(meshDataArray[i].GetIndexData<int>(), Allocator.TempJob);

            var applyColliders = new ApplyCollidersJob
            {
                MeshVerts = Verts,
                MeshTris = Tris,
                BlobCollider = BlobCollider,
            };

            applyColliders.Schedule().Complete();

            Verts.Dispose();
            Tris.Dispose();

            state.EntityManager.SetComponentData(chunks[i], new PhysicsCollider { Value = BlobCollider[0] });
        }

        BlobCollider.Dispose();
    }

    [BurstCompile]
    private float GetValueAtPosition(NativeArray<float> TerrainData, float3 coordinate, int width, int depth)
    {
        return TerrainData[(int)(coordinate.x + coordinate.y * (width + 1) * (depth + 1) + coordinate.z * (width + 1))];
    }

    [BurstCompile]
    private void SetValueAtPosition(NativeArray<float> TerrainData, float3 coordinate, int width, int depth, float value)
    {
        TerrainData[(int)(coordinate.x + coordinate.y * (width + 1) * (depth + 1) + coordinate.z * (width + 1))] = value;
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) 
    {
        ChunkEntities.Dispose();
        TerrainData.Dispose();
    }
}

[BurstCompile]
partial struct ApplyCollidersJob : IJob
{
    public NativeArray<float3> MeshVerts;
    public NativeArray<int> MeshTris;

    public NativeArray<BlobAssetReference<Unity.Physics.Collider>> BlobCollider;

    [BurstCompile]
    public void Execute()
    {
        // Calculate collider
        NativeArray<int3> CTris = new NativeArray<int3>(MeshTris.Length / 3, Allocator.Temp);

        int ii = 0;
        for (int t = 0; t < MeshTris.Length; t += 3)
            CTris[ii++] = new int3(MeshTris[t], MeshTris[t + 1], MeshTris[t + 2]);

        BlobCollider[0] = Unity.Physics.MeshCollider.Create(MeshVerts, CTris);
    }
}

[BurstCompile]
public partial struct NoiseJob : IJobParallelFor
{
    public NativeArray<float> TerrainData;

    [ReadOnly] public Terrain TerrainSettings;

    public void Execute(int index)
    {
        // Get x,y,z from index
        float x = index % (TerrainSettings.Width + 1);
        float y = (index / ((TerrainSettings.Width + 1) * (TerrainSettings.Depth + 1))) % (TerrainSettings.Height + 1);
        float z = (index / (TerrainSettings.Width + 1)) % (TerrainSettings.Depth + 1);

        // TODO - And randomness

        // Apply noise scale
        float sampleX = x / TerrainSettings.Width * TerrainSettings.NoiseScale;
        float sampleY = y / TerrainSettings.Width * TerrainSettings.NoiseScale;
        float sampleZ = z / TerrainSettings.Width * TerrainSettings.NoiseScale;

        TerrainData[index] = Perlin3D(sampleX, sampleY, sampleZ);

        if (y > TerrainSettings.NoiseDropOffHeight)
            TerrainData[index] *= 1 - math.pow(((y - TerrainSettings.NoiseDropOffHeight) / (TerrainSettings.Height - TerrainSettings.NoiseDropOffHeight)), 3);
        if (y < TerrainSettings.NoiseDropOffDepth)
            TerrainData[index] = math.lerp(TerrainData[index], TerrainSettings.Threshold + 0.01f, 1 - (y / TerrainSettings.NoiseDropOffDepth));
    }

    private float Perlin3D(float x, float y, float z)
    {
        float xy = Mathf.PerlinNoise(x, y);
        float xz = Mathf.PerlinNoise(x, z);
        float yz = Mathf.PerlinNoise(y, z);

        float yx = Mathf.PerlinNoise(y, x);
        float zx = Mathf.PerlinNoise(z, x);
        float zy = Mathf.PerlinNoise(z, y);

        return (xy + xz + yz + yx + zx + zy) / 6.0f;
    }
}

[BurstCompile]
public partial struct GenerateJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> TerrainData;
    [ReadOnly] public NativeArray<float3> ChunkCoords;
    [ReadOnly] public Terrain Terrain;

    public Mesh.MeshDataArray MeshDataArray;

    [BurstCompile]
    public void Execute(int index)
    {
        NativeList<float3> vertices = new NativeList<float3>(Allocator.Temp);
        NativeList<int> triangles = new NativeList<int>(Allocator.Temp);

        for (int x = 0; x < Terrain.ChunkWidth; x++)
        {
            for (int y = 0; y < Terrain.ChunkHeight; y++)
            {
                for (int z = 0; z < Terrain.ChunkDepth; z++)
                {
                    float3 cubeBaseCoord = new float3(x, y, z);

                    int cubeIndex = 0;
                    int oldVertexCount = vertices.Length;

                    // Cube Corner Configuration
                    if (InsideScalarField(TerrainData, ChunkCoords[index] + cubeBaseCoord + new float3(0.0f, 0.0f, 1.0f), Terrain.Width, Terrain.Depth, Terrain.Threshold)) cubeIndex |= 1;
                    if (InsideScalarField(TerrainData, ChunkCoords[index] + cubeBaseCoord + new float3(1.0f, 0.0f, 1.0f), Terrain.Width, Terrain.Depth, Terrain.Threshold)) cubeIndex |= 2;
                    if (InsideScalarField(TerrainData, ChunkCoords[index] + cubeBaseCoord + new float3(1.0f, 0.0f, 0.0f), Terrain.Width, Terrain.Depth, Terrain.Threshold)) cubeIndex |= 4;
                    if (InsideScalarField(TerrainData, ChunkCoords[index] + cubeBaseCoord + new float3(0.0f, 0.0f, 0.0f), Terrain.Width, Terrain.Depth, Terrain.Threshold)) cubeIndex |= 8;
                    if (InsideScalarField(TerrainData, ChunkCoords[index] + cubeBaseCoord + new float3(0.0f, 1.0f, 1.0f), Terrain.Width, Terrain.Depth, Terrain.Threshold)) cubeIndex |= 16;
                    if (InsideScalarField(TerrainData, ChunkCoords[index] + cubeBaseCoord + new float3(1.0f, 1.0f, 1.0f), Terrain.Width, Terrain.Depth, Terrain.Threshold)) cubeIndex |= 32;
                    if (InsideScalarField(TerrainData, ChunkCoords[index] + cubeBaseCoord + new float3(1.0f, 1.0f, 0.0f), Terrain.Width, Terrain.Depth, Terrain.Threshold)) cubeIndex |= 64;
                    if (InsideScalarField(TerrainData, ChunkCoords[index] + cubeBaseCoord + new float3(0.0f, 1.0f, 0.0f), Terrain.Width, Terrain.Depth, Terrain.Threshold)) cubeIndex |= 128;

                    // Edge vertices
                    int edgeMask = MarchingCubes.EdgeMask[cubeIndex];

                    // Skip next steps if cube is inside or outside surface
                    if (edgeMask == 0)
                        continue;

                    // Add necessary vertices
                    for (int i = 0; i < 12; i++)
                    {
                        if ((MarchingCubes.EdgeMask[cubeIndex] & (1 << i)) != 0)
                        {
                            int[] indices = MarchingCubes.EdgeVertexIndices[i];

                            // Add vertice for later usage
                            float3 v1 = new float3((indices[0] & 1) >> 0, (indices[0] & 2) >> 1, (indices[0] & 4) >> 2);
                            float3 v2 = new float3((indices[1] & 1) >> 0, (indices[1] & 2) >> 1, (indices[1] & 4) >> 2);

                            // Get scalar values of vertices
                            float v1v = GetValueAtPosition(TerrainData, new float3(v1.x + cubeBaseCoord.x + ChunkCoords[index].x, v1.y + cubeBaseCoord.y + ChunkCoords[index].y, v1.z + cubeBaseCoord.z + ChunkCoords[index].z), Terrain.Width, Terrain.Depth);
                            float v2v = GetValueAtPosition(TerrainData, new float3(v2.x + cubeBaseCoord.x + ChunkCoords[index].x, v2.y + cubeBaseCoord.y + ChunkCoords[index].y, v2.z + cubeBaseCoord.z + ChunkCoords[index].z), Terrain.Width, Terrain.Depth);

                            // Swap vertices if necessary
                            if (v2v < v1v)
                            {
                                float tmp = v1v;
                                v1v = v2v;
                                v2v = tmp;

                                float3 vTemp = v1;
                                v1 = v2;
                                v2 = vTemp;
                            }

                            // Add edge vertex to vertives
                            if (math.abs(v1v - v2v) > 0.0001)
                                vertices.Add(cubeBaseCoord + (v1 + (v2 - v1) / (v2v - v1v) * (Terrain.Threshold - v1v)));
                            else
                                vertices.Add(cubeBaseCoord + v1);
                        }
                    }

                    // Create lookup array
                    int[] edgeIndicesLookup = MarchingCubes.TriangleTable[cubeIndex];
                    NativeHashSet<int> uniqueLookup = new NativeHashSet<int>(edgeIndicesLookup.Length, Allocator.Temp);
                    foreach (var edgeIndex in edgeIndicesLookup)
                        uniqueLookup.Add(edgeIndex);
                    NativeArray<int> nativeEdgeIndicesLookup = uniqueLookup.ToNativeArray(Allocator.Temp);

                    // Get triangle indices
                    int[] cubeTriangles = MarchingCubes.TriangleTable[cubeIndex];

                    // Add triangle data to mesh
                    foreach (int i in cubeTriangles)
                        triangles.Add(oldVertexCount + FindIndex(nativeEdgeIndicesLookup, i));
                }
            }
        }

        NativeArray<VertexAttributeDescriptor> meshDescriptors = new NativeArray<VertexAttributeDescriptor>(2, Allocator.Temp);
        meshDescriptors[0] = new VertexAttributeDescriptor(VertexAttribute.Position);
        meshDescriptors[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, stream: 1);

        Mesh.MeshData currentMeshData = MeshDataArray[index];

        currentMeshData.SetVertexBufferParams(vertices.Length, meshDescriptors);
        currentMeshData.SetIndexBufferParams(triangles.Length, IndexFormat.UInt32);
        currentMeshData.subMeshCount = 1;

        // Apply vertices
        NativeArray<float3> meshVertices = currentMeshData.GetVertexData<float3>();
        
        for (int i = 0; i < vertices.Length; i++)
            meshVertices[i] = vertices[i];
    
        // Apply triangles
        NativeArray<int> meshTriangles = currentMeshData.GetIndexData<int>();

        for (int i = 0; i < triangles.Length; i++)
            meshTriangles[i] = triangles[i];

        // Sub mesh data
        currentMeshData.SetSubMesh(0, new SubMeshDescriptor(0, meshTriangles.Length));
    }

    [BurstCompile]
    private int FindIndex(NativeArray<int> array, int item)
    {
        for (int i = 0; i < array.Length; i++)
            if (array[i] == item) return i;
        return 0;
    }

    [BurstCompile]
    private float GetValueAtPosition(NativeArray<float> TerrainData, float3 coordinate, int width, int depth)
    {
        return TerrainData[(int)(coordinate.x + coordinate.y * (width + 1) * (depth + 1) + coordinate.z * (width + 1))];
    }

    [BurstCompile]
    private bool InsideScalarField(NativeArray<float> TerrainData, float3 coordinate, int width, int depth, float Threshold)
    {
        return GetValueAtPosition(TerrainData, coordinate, width, depth) < Threshold;
    }
}

public class MarchingCubes
{
    public static readonly int[][] EdgeVertexIndices = new int[][]
    {
        new int[] {4, 5},
        new int[] {1, 5},
        new int[] {0, 1},
        new int[] {0, 4},
        new int[] {7, 6},
        new int[] {3, 7},
        new int[] {3, 2},
        new int[] {2, 6},
        new int[] {6, 4},
        new int[] {5, 7},
        new int[] {1, 3},
        new int[] {2, 0},
    };

    public static readonly int[] EdgeMask = new int[]
    {
        0x0  , 0x109, 0x203, 0x30a, 0x406, 0x50f, 0x605, 0x70c,
        0x80c, 0x905, 0xa0f, 0xb06, 0xc0a, 0xd03, 0xe09, 0xf00,
        0x190, 0x99 , 0x393, 0x29a, 0x596, 0x49f, 0x795, 0x69c,
        0x99c, 0x895, 0xb9f, 0xa96, 0xd9a, 0xc93, 0xf99, 0xe90,
        0x230, 0x339, 0x33 , 0x13a, 0x636, 0x73f, 0x435, 0x53c,
        0xa3c, 0xb35, 0x83f, 0x936, 0xe3a, 0xf33, 0xc39, 0xd30,
        0x3a0, 0x2a9, 0x1a3, 0xaa , 0x7a6, 0x6af, 0x5a5, 0x4ac,
        0xbac, 0xaa5, 0x9af, 0x8a6, 0xfaa, 0xea3, 0xda9, 0xca0,
        0x460, 0x569, 0x663, 0x76a, 0x66 , 0x16f, 0x265, 0x36c,
        0xc6c, 0xd65, 0xe6f, 0xf66, 0x86a, 0x963, 0xa69, 0xb60,
        0x5f0, 0x4f9, 0x7f3, 0x6fa, 0x1f6, 0xff , 0x3f5, 0x2fc,
        0xdfc, 0xcf5, 0xfff, 0xef6, 0x9fa, 0x8f3, 0xbf9, 0xaf0,
        0x650, 0x759, 0x453, 0x55a, 0x256, 0x35f, 0x55 , 0x15c,
        0xe5c, 0xf55, 0xc5f, 0xd56, 0xa5a, 0xb53, 0x859, 0x950,
        0x7c0, 0x6c9, 0x5c3, 0x4ca, 0x3c6, 0x2cf, 0x1c5, 0xcc ,
        0xfcc, 0xec5, 0xdcf, 0xcc6, 0xbca, 0xac3, 0x9c9, 0x8c0,
        0x8c0, 0x9c9, 0xac3, 0xbca, 0xcc6, 0xdcf, 0xec5, 0xfcc,
        0xcc , 0x1c5, 0x2cf, 0x3c6, 0x4ca, 0x5c3, 0x6c9, 0x7c0,
        0x950, 0x859, 0xb53, 0xa5a, 0xd56, 0xc5f, 0xf55, 0xe5c,
        0x15c, 0x55 , 0x35f, 0x256, 0x55a, 0x453, 0x759, 0x650,
        0xaf0, 0xbf9, 0x8f3, 0x9fa, 0xef6, 0xfff, 0xcf5, 0xdfc,
        0x2fc, 0x3f5, 0xff , 0x1f6, 0x6fa, 0x7f3, 0x4f9, 0x5f0,
        0xb60, 0xa69, 0x963, 0x86a, 0xf66, 0xe6f, 0xd65, 0xc6c,
        0x36c, 0x265, 0x16f, 0x66 , 0x76a, 0x663, 0x569, 0x460,
        0xca0, 0xda9, 0xea3, 0xfaa, 0x8a6, 0x9af, 0xaa5, 0xbac,
        0x4ac, 0x5a5, 0x6af, 0x7a6, 0xaa , 0x1a3, 0x2a9, 0x3a0,
        0xd30, 0xc39, 0xf33, 0xe3a, 0x936, 0x83f, 0xb35, 0xa3c,
        0x53c, 0x435, 0x73f, 0x636, 0x13a, 0x33 , 0x339, 0x230,
        0xe90, 0xf99, 0xc93, 0xd9a, 0xa96, 0xb9f, 0x895, 0x99c,
        0x69c, 0x795, 0x49f, 0x596, 0x29a, 0x393, 0x99 , 0x190,
        0xf00, 0xe09, 0xd03, 0xc0a, 0xb06, 0xa0f, 0x905, 0x80c,
        0x70c, 0x605, 0x50f, 0x406, 0x30a, 0x203, 0x109, 0x0
    };

    public static readonly int[][] TriangleTable = new int[][]
    {
        new int[] {},
        new int[] {0, 8, 3},
        new int[] {0, 1, 9},
        new int[] {1, 8, 3, 9, 8, 1},
        new int[] {1, 2, 10},
        new int[] {0, 8, 3, 1, 2, 10},
        new int[] {9, 2, 10, 0, 2, 9},
        new int[] {2, 8, 3, 2, 10, 8, 10, 9, 8},
        new int[] {3, 11, 2},
        new int[] {0, 11, 2, 8, 11, 0},
        new int[] {1, 9, 0, 2, 3, 11},
        new int[] {1, 11, 2, 1, 9, 11, 9, 8, 11},
        new int[] {3, 10, 1, 11, 10, 3},
        new int[] {0, 10, 1, 0, 8, 10, 8, 11, 10},
        new int[] {3, 9, 0, 3, 11, 9, 11, 10, 9},
        new int[] {9, 8, 10, 10, 8, 11},
        new int[] {4, 7, 8},
        new int[] {4, 3, 0, 7, 3, 4},
        new int[] {0, 1, 9, 8, 4, 7},
        new int[] {4, 1, 9, 4, 7, 1, 7, 3, 1},
        new int[] {1, 2, 10, 8, 4, 7},
        new int[] {3, 4, 7, 3, 0, 4, 1, 2, 10},
        new int[] {9, 2, 10, 9, 0, 2, 8, 4, 7},
        new int[] {2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4},
        new int[] {8, 4, 7, 3, 11, 2},
        new int[] {11, 4, 7, 11, 2, 4, 2, 0, 4},
        new int[] {9, 0, 1, 8, 4, 7, 2, 3, 11},
        new int[] {4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1},
        new int[] {3, 10, 1, 3, 11, 10, 7, 8, 4},
        new int[] {1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4},
        new int[] {4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3},
        new int[] {4, 7, 11, 4, 11, 9, 9, 11, 10},
        new int[] {9, 5, 4},
        new int[] {9, 5, 4, 0, 8, 3},
        new int[] {0, 5, 4, 1, 5, 0},
        new int[] {8, 5, 4, 8, 3, 5, 3, 1, 5},
        new int[] {1, 2, 10, 9, 5, 4},
        new int[] {3, 0, 8, 1, 2, 10, 4, 9, 5},
        new int[] {5, 2, 10, 5, 4, 2, 4, 0, 2},
        new int[] {2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8},
        new int[] {9, 5, 4, 2, 3, 11},
        new int[] {0, 11, 2, 0, 8, 11, 4, 9, 5},
        new int[] {0, 5, 4, 0, 1, 5, 2, 3, 11},
        new int[] {2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5},
        new int[] {10, 3, 11, 10, 1, 3, 9, 5, 4},
        new int[] {4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10},
        new int[] {5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3},
        new int[] {5, 4, 8, 5, 8, 10, 10, 8, 11},
        new int[] {9, 7, 8, 5, 7, 9},
        new int[] {9, 3, 0, 9, 5, 3, 5, 7, 3},
        new int[] {0, 7, 8, 0, 1, 7, 1, 5, 7},
        new int[] {1, 5, 3, 3, 5, 7},
        new int[] {9, 7, 8, 9, 5, 7, 10, 1, 2},
        new int[] {10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3},
        new int[] {8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2},
        new int[] {2, 10, 5, 2, 5, 3, 3, 5, 7},
        new int[] {7, 9, 5, 7, 8, 9, 3, 11, 2},
        new int[] {9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11},
        new int[] {2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7},
        new int[] {11, 2, 1, 11, 1, 7, 7, 1, 5},
        new int[] {9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11},
        new int[] {5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0},
        new int[] {11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0},
        new int[] {11, 10, 5, 7, 11, 5},
        new int[] {10, 6, 5},
        new int[] {0, 8, 3, 5, 10, 6},
        new int[] {9, 0, 1, 5, 10, 6},
        new int[] {1, 8, 3, 1, 9, 8, 5, 10, 6},
        new int[] {1, 6, 5, 2, 6, 1},
        new int[] {1, 6, 5, 1, 2, 6, 3, 0, 8},
        new int[] {9, 6, 5, 9, 0, 6, 0, 2, 6},
        new int[] {5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8},
        new int[] {2, 3, 11, 10, 6, 5},
        new int[] {11, 0, 8, 11, 2, 0, 10, 6, 5},
        new int[] {0, 1, 9, 2, 3, 11, 5, 10, 6},
        new int[] {5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11},
        new int[] {6, 3, 11, 6, 5, 3, 5, 1, 3},
        new int[] {0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6},
        new int[] {3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9},
        new int[] {6, 5, 9, 6, 9, 11, 11, 9, 8},
        new int[] {5, 10, 6, 4, 7, 8},
        new int[] {4, 3, 0, 4, 7, 3, 6, 5, 10},
        new int[] {1, 9, 0, 5, 10, 6, 8, 4, 7},
        new int[] {10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4},
        new int[] {6, 1, 2, 6, 5, 1, 4, 7, 8},
        new int[] {1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7},
        new int[] {8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6},
        new int[] {7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9},
        new int[] {3, 11, 2, 7, 8, 4, 10, 6, 5},
        new int[] {5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11},
        new int[] {0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6},
        new int[] {9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6},
        new int[] {8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6},
        new int[] {5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11},
        new int[] {0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7},
        new int[] {6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9},
        new int[] {10, 4, 9, 6, 4, 10},
        new int[] {4, 10, 6, 4, 9, 10, 0, 8, 3},
        new int[] {10, 0, 1, 10, 6, 0, 6, 4, 0},
        new int[] {8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10},
        new int[] {1, 4, 9, 1, 2, 4, 2, 6, 4},
        new int[] {3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4},
        new int[] {0, 2, 4, 4, 2, 6},
        new int[] {8, 3, 2, 8, 2, 4, 4, 2, 6},
        new int[] {10, 4, 9, 10, 6, 4, 11, 2, 3},
        new int[] {0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6},
        new int[] {3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10},
        new int[] {6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1},
        new int[] {9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3},
        new int[] {8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1},
        new int[] {3, 11, 6, 3, 6, 0, 0, 6, 4},
        new int[] {6, 4, 8, 11, 6, 8},
        new int[] {7, 10, 6, 7, 8, 10, 8, 9, 10},
        new int[] {0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10},
        new int[] {10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0},
        new int[] {10, 6, 7, 10, 7, 1, 1, 7, 3},
        new int[] {1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7},
        new int[] {2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9},
        new int[] {7, 8, 0, 7, 0, 6, 6, 0, 2},
        new int[] {7, 3, 2, 6, 7, 2},
        new int[] {2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7},
        new int[] {2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7},
        new int[] {1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11},
        new int[] {11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1},
        new int[] {8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6},
        new int[] {0, 9, 1, 11, 6, 7},
        new int[] {7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0},
        new int[] {7, 11, 6},
        new int[] {7, 6, 11},
        new int[] {3, 0, 8, 11, 7, 6},
        new int[] {0, 1, 9, 11, 7, 6},
        new int[] {8, 1, 9, 8, 3, 1, 11, 7, 6},
        new int[] {10, 1, 2, 6, 11, 7},
        new int[] {1, 2, 10, 3, 0, 8, 6, 11, 7},
        new int[] {2, 9, 0, 2, 10, 9, 6, 11, 7},
        new int[] {6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8},
        new int[] {7, 2, 3, 6, 2, 7},
        new int[] {7, 0, 8, 7, 6, 0, 6, 2, 0},
        new int[] {2, 7, 6, 2, 3, 7, 0, 1, 9},
        new int[] {1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6},
        new int[] {10, 7, 6, 10, 1, 7, 1, 3, 7},
        new int[] {10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8},
        new int[] {0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7},
        new int[] {7, 6, 10, 7, 10, 8, 8, 10, 9},
        new int[] {6, 8, 4, 11, 8, 6},
        new int[] {3, 6, 11, 3, 0, 6, 0, 4, 6},
        new int[] {8, 6, 11, 8, 4, 6, 9, 0, 1},
        new int[] {9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6},
        new int[] {6, 8, 4, 6, 11, 8, 2, 10, 1},
        new int[] {1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6},
        new int[] {4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9},
        new int[] {10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3},
        new int[] {8, 2, 3, 8, 4, 2, 4, 6, 2},
        new int[] {0, 4, 2, 4, 6, 2},
        new int[] {1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8},
        new int[] {1, 9, 4, 1, 4, 2, 2, 4, 6},
        new int[] {8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1},
        new int[] {10, 1, 0, 10, 0, 6, 6, 0, 4},
        new int[] {4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3},
        new int[] {10, 9, 4, 6, 10, 4},
        new int[] {4, 9, 5, 7, 6, 11},
        new int[] {0, 8, 3, 4, 9, 5, 11, 7, 6},
        new int[] {5, 0, 1, 5, 4, 0, 7, 6, 11},
        new int[] {11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5},
        new int[] {9, 5, 4, 10, 1, 2, 7, 6, 11},
        new int[] {6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5},
        new int[] {7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2},
        new int[] {3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6},
        new int[] {7, 2, 3, 7, 6, 2, 5, 4, 9},
        new int[] {9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7},
        new int[] {3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0},
        new int[] {6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8},
        new int[] {9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7},
        new int[] {1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4},
        new int[] {4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10},
        new int[] {7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10},
        new int[] {6, 9, 5, 6, 11, 9, 11, 8, 9},
        new int[] {3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5},
        new int[] {0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11},
        new int[] {6, 11, 3, 6, 3, 5, 5, 3, 1},
        new int[] {1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6},
        new int[] {0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10},
        new int[] {11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5},
        new int[] {6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3},
        new int[] {5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2},
        new int[] {9, 5, 6, 9, 6, 0, 0, 6, 2},
        new int[] {1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8},
        new int[] {1, 5, 6, 2, 1, 6},
        new int[] {1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6},
        new int[] {10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0},
        new int[] {0, 3, 8, 5, 6, 10},
        new int[] {10, 5, 6},
        new int[] {11, 5, 10, 7, 5, 11},
        new int[] {11, 5, 10, 11, 7, 5, 8, 3, 0},
        new int[] {5, 11, 7, 5, 10, 11, 1, 9, 0},
        new int[] {10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1},
        new int[] {11, 1, 2, 11, 7, 1, 7, 5, 1},
        new int[] {0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11},
        new int[] {9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7},
        new int[] {7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2},
        new int[] {2, 5, 10, 2, 3, 5, 3, 7, 5},
        new int[] {8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5},
        new int[] {9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2},
        new int[] {9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2},
        new int[] {1, 3, 5, 3, 7, 5 },
        new int[] {0, 8, 7, 0, 7, 1, 1, 7, 5},
        new int[] {9, 0, 3, 9, 3, 5, 5, 3, 7},
        new int[] {9, 8, 7, 5, 9, 7},
        new int[] {5, 8, 4, 5, 10, 8, 10, 11, 8},
        new int[] {5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0},
        new int[] {0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5},
        new int[] {10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4},
        new int[] {2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8},
        new int[] {0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11},
        new int[] {0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5},
        new int[] {9, 4, 5, 2, 11, 3},
        new int[] {2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4},
        new int[] {5, 10, 2, 5, 2, 4, 4, 2, 0},
        new int[] {3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9},
        new int[] {5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2},
        new int[] {8, 4, 5, 8, 5, 3, 3, 5, 1},
        new int[] {0, 4, 5, 1, 0, 5},
        new int[] {8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5},
        new int[] {9, 4, 5},
        new int[] {4, 11, 7, 4, 9, 11, 9, 10, 11},
        new int[] {0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11},
        new int[] {1, 10, 11, 1, 11, 4, 1, 4, 0, 7, 4, 11},
        new int[] {3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4},
        new int[] {4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2},
        new int[] {9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3},
        new int[] {11, 7, 4, 11, 4, 2, 2, 4, 0},
        new int[] {11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4},
        new int[] {2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9},
        new int[] {9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7},
        new int[] {3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10},
        new int[] {1, 10, 2, 8, 7, 4},
        new int[] {4, 9, 1, 4, 1, 7, 7, 1, 3},
        new int[] {4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1},
        new int[] {4, 0, 3, 7, 4, 3},
        new int[] {4, 8, 7},
        new int[] {9, 10, 8, 10, 11, 8},
        new int[] {3, 0, 9, 3, 9, 11, 11, 9, 10},
        new int[] {0, 1, 10, 0, 10, 8, 8, 10, 11},
        new int[] {3, 1, 10, 11, 3, 10},
        new int[] {1, 2, 11, 1, 11, 9, 9, 11, 8},
        new int[] {3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9},
        new int[] {0, 2, 11, 8, 0, 11},
        new int[] {3, 2, 11},
        new int[] {2, 3, 8, 2, 8, 10, 10, 8, 9},
        new int[] {9, 10, 2, 0, 9, 2},
        new int[] {2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8},
        new int[] {1, 10, 2},
        new int[] {1, 3, 8, 9, 1, 8},
        new int[] {0, 9, 1},
        new int[] {0, 3, 8},
        new int[] {}
    };
}