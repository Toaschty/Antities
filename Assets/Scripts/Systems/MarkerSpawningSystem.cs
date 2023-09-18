using JetBrains.Annotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[UpdateBefore(typeof(MarkerDecaySystem))]
public partial struct MarkerSpawningSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PheromoneConfig>();
        state.RequireForUpdate<Ant>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        PheromoneConfig PheromoneConfig = SystemAPI.GetSingleton<PheromoneConfig>();
        EntityCommandBuffer ECB = new EntityCommandBuffer(Allocator.TempJob);

        var spawningJob = new SpawningJob
        {
            PheromoneConfig = PheromoneConfig,
            ECB = ECB.AsParallelWriter(),
        };

        JobHandle spawningHandle = spawningJob.ScheduleParallel(state.Dependency);
        spawningHandle.Complete();

        ECB.Playback(state.EntityManager);
        ECB.Dispose();

        state.Dependency = spawningHandle;
    }
}

[BurstCompile]
[WithAny(typeof(SpawnPendingPheromones))]
public partial struct SpawningJob : IJobEntity
{
    [ReadOnly] public PheromoneConfig PheromoneConfig;

    public EntityCommandBuffer.ParallelWriter ECB;

    [BurstCompile]
    public void Execute(Entity entity, in LocalToWorld transform, ref Ant ant)
    {
        // Check distance to last pheronome
        if (math.distance(transform.Position, ant.LastPheromonePosition) < PheromoneConfig.DistanceBetweenPheromones)
            return;

        // Spawn new pending pheromone
        Entity pendingPheromone = ECB.Instantiate(0, PheromoneConfig.PendingPheromone);

        ECB.SetComponent(0, pendingPheromone, new LocalTransform
        {
            Position = transform.Position + new float3(0.0f, 0.2f, 0.0f),
            Rotation = quaternion.identity,
            Scale = PheromoneConfig.Scale
        });

        // Save pheromone as path waypoint
        ECB.AppendToBuffer(0, entity, new WayPoint
        {
            Position = transform.Position + new float3(0.0f, 0.2f, 0.0f),
            PendingPheromone = pendingPheromone,
        });

        ant.LastPheromonePosition = transform.Position;
    }
}