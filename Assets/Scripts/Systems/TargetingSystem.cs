using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

public partial struct TargetingSystem : ISystem
{
    ComponentLookup<Food> foodLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Ant>();

        foodLookup = state.GetComponentLookup<Food>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        foodLookup.Update(ref state);

        var targetingJob = new TargetingJob
        {
            FoodLookup = foodLookup,
            CollisionWorld = collisionWorld,
            ECB = ecb.AsParallelWriter(),
        };

        JobHandle handle = targetingJob.ScheduleParallel(state.Dependency);
        handle.Complete();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();

        state.Dependency = handle;
    }
}

[BurstCompile]
[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
public partial struct TargetingJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<Food> FoodLookup;
    [ReadOnly] public CollisionWorld CollisionWorld;

    public EntityCommandBuffer.ParallelWriter ECB;

    [BurstCompile]
    public void Execute(TargetAspect aspect)
    {
        if (aspect.Ant.ValueRO.Target != Entity.Null)
        {
            if (aspect.HasTargetFood && !FoodLookup.HasComponent(aspect.Ant.ValueRO.Target))
                aspect.Ant.ValueRW.Target = Entity.Null;
            else
                return;
        }

        uint mask = aspect.HasTargetFood ? (uint)64 : (uint)256; // Food : Colony

        NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);
        PointDistanceInput pointDistanceInput = new PointDistanceInput
        {
            Position = aspect.Transform.ValueRO.Position,
            MaxDistance = aspect.Ant.ValueRO.ViewRadius,
            Filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = mask,
                GroupIndex = 0
            }
        };

        bool result = CollisionWorld.CalculateDistance(pointDistanceInput, ref hits);

        if (!result)
            return;

        DistanceHit target = new DistanceHit();
        float distance = float.MaxValue;

        foreach (var hit in hits)
        {
            // Check angle to target
            var toTarget = hit.Position - aspect.Transform.ValueRO.Position;
            var dot = math.dot(aspect.Transform.ValueRO.Forward(), toTarget);
            var angle = math.acos(dot / (math.length(aspect.Transform.ValueRO.Forward()) * math.length(toTarget)));

            if (angle > aspect.Ant.ValueRO.ViewAngle / 2.0f)
                continue;

            // Check if object is blocked by wall
            //RaycastInput input = new RaycastInput
            //{
            //    Start = aspect.Transform.ValueRO.Position,
            //    End = hit.Position,
            //    Filter = new CollisionFilter
            //    {
            //        BelongsTo = ~0u,
            //        CollidesWith = 128, // Wall
            //        GroupIndex = 0
            //    }
            //};

            //Unity.Physics.RaycastHit rayCastHit = new Unity.Physics.RaycastHit();
            //bool haveHit = CollisionWorld.CastRay(input, out rayCastHit);

            //// Skip if hit by a wall
            //if (haveHit)
            //    continue;

            if (hit.Distance < distance)
            {
                distance = hit.Distance;
                target = hit;
            }
        }

        if (target.Entity != Entity.Null)
        {
            aspect.Ant.ValueRW.Target = target.Entity;
        }
    }
}
