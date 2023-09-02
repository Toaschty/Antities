using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

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
        return;


        var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

        foreach (var (transform, ant) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<Ant>>())
        {
            RaycastInput input = new RaycastInput()
            {
                Start = transform.ValueRO.Position,
                End = transform.ValueRO.Position + transform.ValueRO.Forward() * 0.5f,
                Filter = new CollisionFilter
                {
                    BelongsTo = ~0u,
                    CollidesWith = 128,
                    GroupIndex = 0
                }
            };

            Unity.Physics.RaycastHit hit = new Unity.Physics.RaycastHit();
            bool haveHit = collisionWorld.CastRay(input, out hit);

            // Invert direction on hit with wall
            if (haveHit)
            {
                float3 newDirection = math.reflect(ant.ValueRW.Velocity, hit.SurfaceNormal);

                ant.ValueRW.Velocity = newDirection;
                ant.ValueRW.RandomSteerForce = newDirection;
                ant.ValueRW.DesiredDirection = newDirection;
            }
        }
    }
}
