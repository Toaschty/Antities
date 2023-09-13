using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public partial struct CollisionSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Ant>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

        var collisionJob = new CollisionJob
        {
            CollisionWorld = collisionWorld,
        };

        var handle = collisionJob.ScheduleParallel(state.Dependency);
        handle.Complete();

        state.Dependency = handle;
    }
}

[BurstCompile]
public partial struct CollisionJob : IJobEntity
{
    [ReadOnly] public CollisionWorld CollisionWorld;

    [BurstCompile]
    public void Execute(in LocalTransform transform, ref Ant ant)
    {
        RaycastInput input = new RaycastInput()
        {
            Start = transform.Position,
            End = transform.Position + transform.Forward() * 0.5f,
            Filter = new CollisionFilter
            {
                BelongsTo = 512u, // Ant
                CollidesWith = 128u, // Wall
                GroupIndex = 0
            }
        };

        RaycastHit hit = new RaycastHit();
        bool haveHit = CollisionWorld.CastRay(input, out hit);

        // Invert direction on hit with wall
        if (haveHit)
        {
            float3 newDirection = math.reflect(ant.Velocity, hit.SurfaceNormal);

            ant.Velocity = newDirection;
            ant.RandomSteerForce = newDirection;
            ant.DesiredDirection = newDirection;
        }
    }
}
