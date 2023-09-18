using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

public partial struct PathBuildingSystem : ISystem
{
    private BufferLookup<WayPoint> WaypointLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Ant>();

        WaypointLookup = state.GetBufferLookup<WayPoint>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ECB = new EntityCommandBuffer(Allocator.TempJob);
        var PheromoneConfig = SystemAPI.GetSingletonRW<PheromoneConfig>();

        WaypointLookup.Update(ref state);

        var pathValidationJob = new PathValidationJob
        {
            PheromoneConfig = PheromoneConfig.ValueRO,
            WaypointLookup = WaypointLookup,
            ECB = ECB.AsParallelWriter(),
        };

        JobHandle pathValidationHandle = pathValidationJob.ScheduleParallel(state.Dependency);
        pathValidationHandle.Complete();

        var pathJob = new PathBuildingJob
        {
            PheromoneConfig = PheromoneConfig,
            Time = SystemAPI.Time.ElapsedTime,
            WaypointLookup = WaypointLookup,
            ECB = ECB.AsParallelWriter(),
        };

        JobHandle pathHandle = pathJob.ScheduleParallel(pathValidationHandle);
        pathHandle.Complete();

        ECB.Playback(state.EntityManager);
        ECB.Dispose();

        state.Dependency = pathHandle;
    }
}

[BurstCompile]
[WithAny(typeof(Ant))]
public partial struct PathValidationJob : IJobEntity
{
    [ReadOnly] public PheromoneConfig PheromoneConfig;

    [NativeDisableParallelForRestriction] public BufferLookup<WayPoint> WaypointLookup;

    public EntityCommandBuffer.ParallelWriter ECB;

    [BurstCompile]
    public void Execute(Entity entity)
    {
        // Stop spawning pheromones if path length exeeds max path length
        if (WaypointLookup[entity].Length > PheromoneConfig.MaxPathLength)
        {
            // Delete all pending waypoints
            for (int i = 0; i < WaypointLookup[entity].Length; i++)
            {
                // Delete old pending Pheromone
                ECB.DestroyEntity(0, WaypointLookup[entity].ElementAt(i).PendingPheromone);
            }

            WaypointLookup[entity].Clear();

            ECB.SetComponentEnabled<SpawnPendingPheromones>(0, entity, false);
        }
    }
}

[BurstCompile]
[WithAny(typeof(BuildPath))]
public partial struct PathBuildingJob : IJobEntity
{
    [ReadOnly] public double Time;

    [NativeDisableParallelForRestriction] public BufferLookup<WayPoint> WaypointLookup;

    [NativeDisableUnsafePtrRestriction] public RefRW<PheromoneConfig> PheromoneConfig;
    public EntityCommandBuffer.ParallelWriter ECB;

    [BurstCompile]
    public void Execute(Entity entity, ref Ant ant)
    {
        DynamicBuffer<WayPoint> path = WaypointLookup[entity];

        if (path.Length <= 0)
            return;

        float quality = 1.0f / path.Length;

        for (int i = 0; i < path.Length; i++)
        {
            // Delete old pending pheromone
            ECB.DestroyEntity(0, path.ElementAt(i).PendingPheromone);

            // Instantiate new path pheromone
            if (ant.HighestQualityFound < quality)
            {
                Entity pathPheromone = ECB.Instantiate(0, PheromoneConfig.ValueRO.PathPheromone);

                ECB.SetComponent(0, pathPheromone, new LocalTransform
                {
                    Position = path.ElementAt(i).Position,
                    Rotation = Quaternion.identity,
                    Scale = PheromoneConfig.ValueRO.Scale,
                });

                ECB.AddComponent(0, pathPheromone, new Pheromone
                {
                    Quality = quality,
                    LifeTime = (float)(Time + PheromoneConfig.ValueRO.PheromoneMaxTime),
                });
            }
        }

        path.Clear();

        ECB.SetComponentEnabled<BuildPath>(0, entity, false);
    }
}
