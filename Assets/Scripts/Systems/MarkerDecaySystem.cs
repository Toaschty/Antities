using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

public partial struct MarkerDecaySystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Marker>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        var markerConfig = SystemAPI.GetSingleton<MarkerConfig>();

        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        var decayJob = new DecayJob
        {
            DeltaTime = deltaTime,
            MarkerConfig = markerConfig,
            ECB = ecb.AsParallelWriter(),
        };

        JobHandle decayJobHandle = decayJob.ScheduleParallel(state.Dependency);
        decayJobHandle.Complete();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

[BurstCompile]
public partial struct DecayJob : IJobEntity
{
    [ReadOnly] public float DeltaTime;
    [ReadOnly] public MarkerConfig MarkerConfig;

    public EntityCommandBuffer.ParallelWriter ECB;

    [BurstCompile]
    public void Execute(Entity entity, ref Marker marker, ref LocalTransform transform)
    {
        // Reduce intensity by deltaTime
        marker.Intensity -= DeltaTime;

        // Scale down marker model depending on time
        transform.Scale = (marker.Intensity / MarkerConfig.PheromoneMaxTime) * MarkerConfig.Scale;

        // Destroy marker if necessary
        if (marker.Intensity < 0)
            ECB.DestroyEntity(0, entity);
    }
}
