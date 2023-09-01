using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct MarkerSpawningSystem : ISystem
{
    NativeParallelMultiHashMap<int, float> ColonyPheromones;
    NativeParallelMultiHashMap<int, float> FoodPheromones;

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

        ColonyPheromones = new NativeParallelMultiHashMap<int, float>(markerCount, Allocator.TempJob);
        FoodPheromones = new NativeParallelMultiHashMap<int, float>(markerCount, Allocator.TempJob);

        var pheromoneHashingJob = new PheromoneHashingJob
        {
            GridSize = hashConfig.GridSize,
            ColonyPhermones = ColonyPheromones.AsParallelWriter(),
            FoodPhermones = FoodPheromones.AsParallelWriter(),
        };

        JobHandle phermoneHashingHandle = pheromoneHashingJob.ScheduleParallel(state.Dependency);
        phermoneHashingHandle.Complete();

        EntityCommandBuffer ECB = new EntityCommandBuffer(Allocator.TempJob);
        var markerSpawnerJob = new MarkerSpawnerJob
        {
            markerConfig = markerConfig,
            hashConfig = hashConfig,
            Time = (float)SystemAPI.Time.ElapsedTime,
            ColonyPheromones = ColonyPheromones,
            FoodPheromones = FoodPheromones,
            ECB = ECB.AsParallelWriter()
        };

        JobHandle spawnJobHandle = markerSpawnerJob.ScheduleParallel(phermoneHashingHandle);
        spawnJobHandle.Complete();

        ECB.Playback(state.EntityManager);
        ECB.Dispose();

        // Cleanup
        ColonyPheromones.Dispose();
        FoodPheromones.Dispose();

        state.Dependency = spawnJobHandle;
    }
}

[BurstCompile]
public partial struct PheromoneHashingJob : IJobEntity
{
    [ReadOnly] public float GridSize;

    public NativeParallelMultiHashMap<int, float>.ParallelWriter ColonyPhermones;
    public NativeParallelMultiHashMap<int, float>.ParallelWriter FoodPhermones;

    [BurstCompile]
    public void Execute(MarkerAspect marker)
    {
        // Generate hash for current marker
        int hash = (int)math.hash(new int3(math.floor(marker.Transform.ValueRO.Position * (1.0f / GridSize))));

        // Add hash to corresponding hashMap
        if (marker.HasFoodMarker)
            FoodPhermones.Add(hash, marker.Marker.ValueRO.Intensity);
        if (marker.HasColonyMarker)
            ColonyPhermones.Add(hash, marker.Marker.ValueRO.Intensity);
    }
}

[BurstCompile]
public partial struct MarkerSpawnerJob : IJobEntity
{
    [ReadOnly] public MarkerConfig markerConfig;
    [ReadOnly] public HashConfig hashConfig;
    [ReadOnly] public float Time;
    [ReadOnly] public NativeParallelMultiHashMap<int, float> ColonyPheromones;
    [ReadOnly] public NativeParallelMultiHashMap<int, float> FoodPheromones;

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

        NativeParallelMultiHashMap<int, float>.Enumerator phermonesValues;
        float maxValue = 0.0f;
        if (ant.State == AntState.SearchingFood)
        {
            phermonesValues = ColonyPheromones.GetValuesForKey(hash);
            maxValue = hashConfig.MaxColonyPheromonePerGrid;
        }
        else if (ant.State == AntState.GoingHome)
        {
            phermonesValues = FoodPheromones.GetValuesForKey(hash);
            maxValue = hashConfig.MaxFoodPheromonePerGrid;
        }
        else
        {
            return;
        }

        float hashIntensity = 0f;
        foreach (var value in phermonesValues)
            hashIntensity += value;

        if (hashIntensity > maxValue)
        {
            // Cleanup array and do nothing
            phermonesValues.Dispose();
            return;
        }

        // Spawn new marker
        Entity pheromoneInstance = Entity.Null;
        float intensity = 0f;

        if (ant.State == AntState.SearchingFood)
        {
            pheromoneInstance = ECB.Instantiate(0, markerConfig.ToHomeMarker);
            ECB.AddComponent<ColonyMarker>(0, pheromoneInstance);
            intensity = 1 - (Time - ant.LeftColony) / markerConfig.PheromoneMaxTime;
            // intensity = 1 - math.pow((Time - ant.LeftColony) / markerConfig.PheromoneMaxTime, 0.25f);
        }
        else
        {
            pheromoneInstance = ECB.Instantiate(0, markerConfig.ToFoodMarker);
            ECB.AddComponent<FoodMarker>(0, pheromoneInstance);
            intensity = 1 - (Time - ant.LeftFood) / markerConfig.PheromoneMaxTime;
            // intensity = 1 - math.pow((Time - ant.LeftFood) / markerConfig.PheromoneMaxTime, 0.25f);
        }

        intensity *= markerConfig.PheromoneMaxTime;

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