using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

public partial struct FoodSourceSystem : ISystem
{
    private ComponentLookup<Ant> AntLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Ant>();
        state.RequireForUpdate<Food>();

        AntLookup = state.GetComponentLookup<Ant>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ECB = new EntityCommandBuffer(Allocator.TempJob);
        var CollisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        var PheromoneConfig = SystemAPI.GetSingleton<PheromoneConfig>();

        AntLookup.Update(ref state);

        var sourceJob = new SourceJob
        {
            AntLookup = AntLookup,
            CollisionWorld = CollisionWorld,
            ECB = ECB.AsParallelWriter(),
            Time = SystemAPI.Time.ElapsedTime,
            PheromoneConfig = PheromoneConfig,
        };

        JobHandle sourceHandle = sourceJob.ScheduleParallel(state.Dependency);
        sourceHandle.Complete();

        ECB.Playback(state.EntityManager);
        ECB.Dispose();

        state.Dependency = sourceHandle;
    }
}

[BurstCompile]
public partial struct SourceJob : IJobEntity
{
    [ReadOnly] public CollisionWorld CollisionWorld;
    [ReadOnly] public double Time;
    [ReadOnly] public PheromoneConfig PheromoneConfig;

    [NativeDisableParallelForRestriction] public ComponentLookup<Ant> AntLookup;
    public EntityCommandBuffer.ParallelWriter ECB;

    [BurstCompile]
    public void Execute(Entity entity, ref LocalTransform transform, ref Food food)
    {
        NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);
        PointDistanceInput pointDistanceInput = new PointDistanceInput
        {
            Position = transform.Position,
            MaxDistance = food.PickUpRadius,
            Filter = new CollisionFilter
            {
                BelongsTo = 64u, // Food
                CollidesWith = 512u, // Ant
                GroupIndex = 0
            }
        };

        if (!CollisionWorld.CalculateDistance(pointDistanceInput, ref hits))
            return;

        bool sourceDestroyed = false;

        foreach (var hit in hits)
        {
            RefRW<Ant> ant = AntLookup.GetRefRW(hit.Entity);
            
            // Clear target
            ant.ValueRW.Target = Entity.Null;

            // Skip next steps if source was destroyed or ant has currently food already
            if (sourceDestroyed)
                continue;
            if (ant.ValueRO.Food != Entity.Null)
                continue;

            if (food.Amount > 0)
            {
                // Start path building
                ECB.SetComponentEnabled<BuildPath>(0, hit.Entity, true);

                // Instantiate small food
                Entity carryFood = ECB.Instantiate(0, food.CarryModel);

                // Parent new food model to current ant
                ECB.AddComponent(0, carryFood, new Parent
                {
                    Value = hit.Entity,
                });

                // Move food model above ant
                ECB.SetComponent(0, carryFood, new LocalTransform
                {
                    Position = new float3(0.0f, 0.5f, 0.0f),
                    Rotation = Quaternion.identity,
                    Scale = 0.2f
                });

                ant.ValueRW.Food = carryFood;
                
                // Handle food amount
                food.Amount--;

                if (food.Amount <= 0)
                {
                    // Mark food for deletion
                    ECB.DestroyEntity(1, entity);
                    sourceDestroyed = true;
                }

                // Flip movement direction
                ant.ValueRW.State = AntState.GoingHome;
                ant.ValueRW.Velocity = -ant.ValueRO.Velocity;
                ant.ValueRW.DesiredDirection = ant.ValueRO.Velocity;
                ant.ValueRW.RandomSteerForce = ant.ValueRO.Velocity;

                // Switch target to colony
                ECB.SetComponentEnabled<TargetingFood>(0, hit.Entity, false);
                ECB.SetComponentEnabled<TargetingColony>(0, hit.Entity, true);
                
                // Reset path settings
                ECB.SetComponentEnabled<SpawnPendingPheromones>(0, hit.Entity, true);

                ECB.SetComponent(0, hit.Entity, ant.ValueRO);
            }
        }
    }
}