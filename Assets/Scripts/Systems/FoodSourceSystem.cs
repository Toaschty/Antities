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
    ComponentLookup<Ant> antLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Ant>();
        state.RequireForUpdate<Food>();

        antLookup = state.GetComponentLookup<Ant>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

        antLookup.Update(ref state);

        var sourceJob = new SourceJob
        {
            AntLookup = antLookup,
            CollisionWorld = collisionWorld,
            ECB = ecb.AsParallelWriter(),
            Time = SystemAPI.Time.ElapsedTime,
        };

        JobHandle sourceHandle = sourceJob.ScheduleParallel(state.Dependency);
        sourceHandle.Complete();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();

        state.Dependency = sourceHandle;
    }
}

[BurstCompile]
public partial struct SourceJob : IJobEntity
{
    [ReadOnly] public CollisionWorld CollisionWorld;
    [ReadOnly] public double Time;

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

        bool result = CollisionWorld.CalculateDistance(pointDistanceInput, ref hits);

        if (!result)
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

                // Switch target to colony
                ECB.SetComponentEnabled<TargetingFood>(0, hit.Entity, false);
                ECB.SetComponentEnabled<TargetingColony>(0, hit.Entity, true);

                // Switch state of ant
                ant.ValueRW.State = AntState.TurningAround;
                ant.ValueRW.TurnAroundDirection = -ant.ValueRO.DesiredDirection;
                ant.ValueRW.LeftFood = Time;

                ant.ValueRW.Food = carryFood;
                ECB.SetComponent<Ant>(0, hit.Entity, ant.ValueRO);

                food.Amount--;

                if (food.Amount <= 0)
                {
                    // Mark food for deletion
                    ECB.DestroyEntity(1, entity);
                    sourceDestroyed = true;
                }
            }
        }

        hits.Dispose();
    }
}