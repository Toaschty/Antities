using System.Diagnostics;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine.UIElements;

public partial struct MarkerSpawningSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MarkerConfig>();
        state.RequireForUpdate<Ant>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var markerConfig = SystemAPI.GetSingleton<MarkerConfig>();
        EntityCommandBuffer ECB = new EntityCommandBuffer(Allocator.TempJob);

        var spawningJob = new SpawningJob
        {
            Time = SystemAPI.Time.ElapsedTime,
            ECB = ECB.AsParallelWriter(),
            MarkerConfig = markerConfig,
        };

        JobHandle spawningHandle = spawningJob.ScheduleParallel(state.Dependency);
        spawningHandle.Complete();

        ECB.Playback(state.EntityManager);
        ECB.Dispose();

        state.Dependency = spawningHandle;
    }
}

[BurstCompile]
public partial struct SpawningJob : IJobEntity
{
    [ReadOnly] public MarkerConfig MarkerConfig;
    [ReadOnly] public double Time;

    public EntityCommandBuffer.ParallelWriter ECB;

    [BurstCompile]
    public void Execute(in LocalToWorld transform, ref Ant ant)
    {
        // Place first pheromone without checking the distance => No previous pheromone here
        if (math.lengthsq(ant.LastPheromonePosition) > 0.0f)
        {
            // Check distance to last pheronome
            if (math.distance(transform.Position, ant.LastPheromonePosition) < MarkerConfig.DistanceBetweenMarkers)
                return;
        }

        // Spawn new marker
        Entity pheromoneInstance = Entity.Null;
        double intensity = 0f;

        if (ant.State == AntState.SearchingFood)
        {
            pheromoneInstance = ECB.Instantiate(0, MarkerConfig.ToHomeMarker);
            ECB.AddComponent<ColonyMarker>(0, pheromoneInstance);
            intensity = MarkerConfig.PheromoneMaxTime - Time + ant.LeftColony;
        }
        else
        {
            pheromoneInstance = ECB.Instantiate(0, MarkerConfig.ToFoodMarker);
            ECB.AddComponent<FoodMarker>(0, pheromoneInstance);
            intensity = MarkerConfig.PheromoneMaxTime - Time + ant.LeftFood;
        }

        ECB.SetComponent(0, pheromoneInstance, new Marker
        {
            Intensity = (float)intensity,
        });

        ECB.SetComponent(0, pheromoneInstance, new LocalTransform
        {
            Position = transform.Position + new float3(0.0f, 0.2f, 0.0f),
            Rotation = quaternion.identity,
            Scale = MarkerConfig.Scale
        });

        ant.LastPheromonePosition = transform.Position;
    }
}