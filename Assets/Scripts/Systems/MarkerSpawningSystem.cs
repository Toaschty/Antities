using System.Diagnostics;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UI;
using UnityEngine.UIElements;

[UpdateBefore(typeof(MarkerDecaySystem))]
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
        var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

        var markerConfig = SystemAPI.GetSingleton<MarkerConfig>();
        EntityCommandBuffer ECB = new EntityCommandBuffer(Allocator.TempJob);

        var spawningJob = new SpawningJob
        {
            Time = SystemAPI.Time.ElapsedTime,
            ECB = ECB.AsParallelWriter(),
            MarkerConfig = markerConfig,
            CollisionWorld = collisionWorld,
        };

        JobHandle spawningHandle = spawningJob.ScheduleParallel(state.Dependency);
        spawningHandle.Complete();

        ECB.Playback(state.EntityManager);
        ECB.Dispose();

        state.Dependency = spawningHandle;
    }
}

[BurstCompile]
[WithAbsent(typeof(SkipMarkerSpawning))]
public partial struct SpawningJob : IJobEntity
{
    [ReadOnly] public MarkerConfig MarkerConfig;
    [ReadOnly] public double Time;
    [ReadOnly] public CollisionWorld CollisionWorld;

    public EntityCommandBuffer.ParallelWriter ECB;

    [BurstCompile]
    public void Execute(Entity entity, in LocalToWorld transform, ref Ant ant)
    {
        // Skip if ant is currently falling
        if (!ant.IsGrounded)
            return;

        // Place first pheromone without checking the distance => No previous pheromone here
        if (math.lengthsq(ant.LastPheromonePosition) > 0.0f)
        {
            // Check distance to last pheronome
            if (math.distance(transform.Position, ant.LastPheromonePosition) < MarkerConfig.DistanceBetweenMarkers)
                return;
        }

        // Check nearby pheromones (Spawn new one or update existing ones)
        uint mask = ant.State == AntState.SearchingFood ? 2048u : 1024u; // Colony : Food Pheromones

        NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);
        PointDistanceInput pointDistanceInput = new PointDistanceInput
        {
            Position = transform.Position,
            MaxDistance = MarkerConfig.DistanceBetweenMarkers / 2.0f,
            Filter = new CollisionFilter
            {
                BelongsTo = ~0u, // Everthing
                CollidesWith = mask, // Colony : Food Pheromones
                GroupIndex = 0
            }
        };

        double intensity;

        if (ant.State == AntState.SearchingFood)
        {
            intensity = MarkerConfig.PheromoneMaxTime + 2 * (ant.LeftColony - Time);
            // intensity = MarkerConfig.PheromoneMaxTime * (1 - math.pow((Time - ant.LeftColony) / MarkerConfig.PheromoneMaxTime, 0.5f));
        }
        else
        { 
            intensity = MarkerConfig.PheromoneMaxTime + 2 * (ant.LeftFood - Time);
            // intensity = MarkerConfig.PheromoneMaxTime * (1 - math.pow((Time - ant.LeftFood) / MarkerConfig.PheromoneMaxTime, 0.5f));
        }

        // Add skip component
        if (intensity < 0f)
        {
            ECB.AddComponent<SkipMarkerSpawning>(0, entity);
            return;
        }

        if (CollisionWorld.CalculateDistance(pointDistanceInput, ref hits))
        {
            foreach (DistanceHit hit in hits)
            {
                ECB.AppendToBuffer(0, hit.Entity, new MarkerData
                {
                    Intensity = (float)intensity,
                });
            }
        }
        else
        {
            // Spawn new marker
            Entity pheromoneInstance = Entity.Null;

            if (ant.State == AntState.SearchingFood)
            {
                pheromoneInstance = ECB.Instantiate(0, MarkerConfig.ToHomeMarker);
                ECB.AddComponent<ColonyMarker>(0, pheromoneInstance);
            }
            else
            {
                pheromoneInstance = ECB.Instantiate(0, MarkerConfig.ToFoodMarker);
                ECB.AddComponent<FoodMarker>(0, pheromoneInstance);
            }

            ECB.SetComponent(0, pheromoneInstance, new Marker());

            DynamicBuffer<MarkerData> markers = ECB.AddBuffer<MarkerData>(0, pheromoneInstance);
            markers.Add(new MarkerData
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
}