using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct MarkerSpawningSysten : ISystem
{
    NativeParallelMultiHashMap<int, float> RawPheromoneHashMap;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MarkerConfig>();
        state.RequireForUpdate<Ant>();
        state.RequireForUpdate<HashConfig>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var markerConfig = SystemAPI.GetSingleton<MarkerConfig>();
        var hashConfig = SystemAPI.GetSingleton<HashConfig>();

        EntityQuery markerQuery = SystemAPI.QueryBuilder().WithAll<LocalToWorld, Marker>().Build();
        int markerCount = markerQuery.CalculateEntityCount();

        RawPheromoneHashMap = new NativeParallelMultiHashMap<int, float>(markerCount, Allocator.TempJob);
        var hashRawPheromoneJob = new HashRawPheromoneJob
        {
            MarkerTransforms = markerQuery.ToComponentDataArray<LocalToWorld>(Allocator.TempJob),
            Markers = markerQuery.ToComponentDataArray<Marker>(Allocator.TempJob),
            GridSize = hashConfig.GridSize,
            HashMap = RawPheromoneHashMap.AsParallelWriter()
        };

        JobHandle rawHandle = hashRawPheromoneJob.Schedule(markerCount, 2048);
        rawHandle.Complete();

        EntityCommandBuffer ECB = new EntityCommandBuffer(Allocator.TempJob);
        var markerSpawnerJob = new MarkerSpawnerJob
        {
            markerConfig = markerConfig,
            hashConfig = hashConfig,
            RawPheromoneHashMap = RawPheromoneHashMap,
            Time = (float)SystemAPI.Time.ElapsedTime,
            ECB = ECB.AsParallelWriter()
        };

        JobHandle spawnJobHandle = markerSpawnerJob.ScheduleParallel(rawHandle);
        spawnJobHandle.Complete();

        ECB.Playback(state.EntityManager);
    }
}

public struct HashRawPheromoneJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<LocalToWorld> MarkerTransforms;
    [ReadOnly] public NativeArray<Marker> Markers;
    [ReadOnly] public float GridSize;

    public NativeParallelMultiHashMap<int, float>.ParallelWriter HashMap;

    public void Execute(int index)
    {
        // Generate hash for current position
        int hash = (int)math.hash(new int3(math.floor(MarkerTransforms[index].Position * (1.0f / GridSize))));
        HashMap.Add(hash, Markers[index].Intensity);
    }
}

[BurstCompile]
public partial struct MarkerSpawnerJob : IJobEntity
{
    [ReadOnly] public MarkerConfig markerConfig;
    [ReadOnly] public HashConfig hashConfig;
    [ReadOnly] public NativeParallelMultiHashMap<int, float> RawPheromoneHashMap;
    [ReadOnly] public float Time;

    public EntityCommandBuffer.ParallelWriter ECB;

    [BurstCompile]
    public void Execute(in LocalToWorld transform, ref Ant ant)
    {
        // Place first pheromone without checking the distance => No previous pheromone here
        if (math.lengthsq(ant.LastPheromonePosition) > 0.0f)
        {
            // Check distance to last pheronome
            if (math.distance(transform.Position, ant.LastPheromonePosition) < markerConfig.DistanceBetweenMarkers)
                return;
        }

        // Check if pheromone limit is reached
        int hash = (int)math.hash(new int3(math.floor(transform.Position * (1.0f / hashConfig.GridSize))));
        var values = RawPheromoneHashMap.GetValuesForKey(hash);

        float hashIntensity = 0f;
        foreach (var v in values)
            hashIntensity += v;

        if (hashIntensity > hashConfig.MaxPheromonePerGrid)
            return;

        // Spawn new marker
        Entity pheromoneInstance = Entity.Null;
        float intensity = 0f;

        if (ant.State == AntState.SearchingFood)
        {
            pheromoneInstance = ECB.Instantiate(0, markerConfig.ToHomeMarker);
            ECB.SetComponentEnabled<ColonyMarker>(0, pheromoneInstance, true);
            intensity = 1 - (Time - ant.LeftColony) / markerConfig.PheromoneMaxTime;
        }
        else
        {
            pheromoneInstance = ECB.Instantiate(0, markerConfig.ToFoodMarker);
            ECB.SetComponentEnabled<FoodMarker>(0, pheromoneInstance, true);
            intensity = 1 - (Time - ant.LeftFood) / markerConfig.PheromoneMaxTime;
        }

        intensity = math.lerp(markerConfig.PheromoneMaxTime / 4f, markerConfig.PheromoneMaxTime, intensity);
        ECB.SetComponent(0, pheromoneInstance, new Marker
        {
            Intensity = intensity,
        });

        ECB.SetComponent(0, pheromoneInstance, new LocalTransform
        {
            Position = transform.Position,
            Rotation = quaternion.identity,
            Scale = markerConfig.Scale
        });

        ant.LastPheromonePosition = transform.Position;
    }
}