using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public partial struct ColonySystem : ISystem
{
    private ComponentLookup<Ant> antLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Colony>();
        state.RequireForUpdate<Ant>();

        antLookup = state.GetComponentLookup<Ant>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        antLookup.Update(ref state);

        DepositJob depositJob = new DepositJob
        {
            CollisionWorld = collisionWorld,
            Time = SystemAPI.Time.ElapsedTime,
            AntLookup = antLookup,
            ECB = ecb.AsParallelWriter(),
        };

        JobHandle depositHandle = depositJob.ScheduleParallel(state.Dependency);
        depositHandle.Complete();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();

        state.Dependency = depositHandle;
    }
}

[BurstCompile]
public partial struct DepositJob : IJobEntity
{
    [ReadOnly] public CollisionWorld CollisionWorld;
    [ReadOnly] public double Time;

    [NativeDisableParallelForRestriction] public ComponentLookup<Ant> AntLookup;

    public EntityCommandBuffer.ParallelWriter ECB;

    [BurstCompile]
    public void Execute(ref LocalTransform transform, ref Colony colony)
    {
        NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);
        PointDistanceInput pointDistanceInput = new PointDistanceInput
        {
            Position = transform.Position,
            MaxDistance = colony.DepositRadius,
            Filter = new CollisionFilter
            {
                BelongsTo = 256u, // Colony
                CollidesWith = 512u, // Ant
                GroupIndex = 0
            }
        };

        // Return if hit nothing
        if (!CollisionWorld.CalculateDistance(pointDistanceInput, ref hits))
            return;

        foreach (var hit in hits)
        {
            RefRW<Ant> ant = AntLookup.GetRefRW(hit.Entity);

            // Reset pending pheromone spawning
            ECB.SetComponentEnabled<SpawnPendingPheromones>(0, hit.Entity, true);

            // Check if ant is carrying food
            if (ant.ValueRO.Food != Entity.Null)
            {
                // Start building path
                ECB.SetComponentEnabled<BuildPath>(0, hit.Entity, true);

                // Destroy food
                ECB.DestroyEntity(0, ant.ValueRO.Food);

                // Reset ant
                ant.ValueRW.Food = Entity.Null;
                ant.ValueRW.Target = Entity.Null;
                ant.ValueRW.State = AntState.SearchingFood;

                // Swap targeting
                ECB.SetComponentEnabled<TargetingFood>(0, hit.Entity, true);
                ECB.SetComponentEnabled<TargetingColony>(0, hit.Entity, false);

                // Instant turn around
                float3 newDir = -ant.ValueRO.DesiredDirection;
                ant.ValueRW.DesiredDirection = newDir;
                ant.ValueRW.Velocity = newDir;
                ant.ValueRW.RandomSteerForce = newDir;
            }
        }
    }
}