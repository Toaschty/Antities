using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public partial struct TargetingSystem : ISystem
{
    public ComponentLookup<Food> FoodLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Ant>();

        FoodLookup = state.GetComponentLookup<Food>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

        FoodLookup.Update(ref state);

        var targetingJob = new TargetingJob
        {
            FoodLookup = FoodLookup,
            CollisionWorld = collisionWorld,
        };

        JobHandle targetingHandle = targetingJob.ScheduleParallel(state.Dependency);
        targetingHandle.Complete();

        state.Dependency = targetingHandle;
    }
}


[BurstCompile]
[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
public partial struct TargetingJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<Food> FoodLookup;
    [ReadOnly] public CollisionWorld CollisionWorld;

    [BurstCompile]
    public void Execute(in LocalTransform transform, ref Ant ant, EnabledRefRO<TargetingFood> tf)
    {
        if (ant.Target != Entity.Null)
        {
            if (tf.ValueRO && !FoodLookup.HasComponent(ant.Target))
                ant.Target = Entity.Null;
            else
                return;
        }

        uint mask = tf.ValueRO ? 64u : 256u; // Food : Colony

        NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);
        PointDistanceInput pointDistanceInput = new PointDistanceInput
        {
            Position = transform.Position,
            MaxDistance = ant.ViewRadius,
            Filter = new CollisionFilter
            {
                BelongsTo = 512u, // Ant
                CollidesWith = mask, // Food : Colony
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
            var toTarget = hit.Position - transform.Position;
            var dot = math.dot(transform.Forward(), toTarget);
            var angle = math.acos(dot / (math.length(transform.Forward()) * math.length(toTarget)));

            if (angle > ant.ViewAngle / 2.0f)
                continue;

            // Check if object is blocked by wall
            RaycastInput input = new RaycastInput
            {
                Start = transform.Position,
                End = hit.Position,
                Filter = new CollisionFilter
                {
                    BelongsTo = 512u, // Ant
                    CollidesWith = 128u, // Wall
                    GroupIndex = 0
                }
            };

            RaycastHit rayCastHit = new RaycastHit();

            // Skip if hit by a wall
            if (CollisionWorld.CastRay(input, out rayCastHit))
                continue;

            if (hit.Distance < distance)
            {
                distance = hit.Distance;
                target = hit;
            }
        }

        if (distance != float.MaxValue)
            ant.Target = target.Entity;

        hits.Dispose();
    }
}
