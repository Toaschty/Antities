using System.Diagnostics;
using System.Numerics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine.UI;
using UnityEngine.UIElements;

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
    public void Execute(ref LocalTransform transform, ref Ant ant)
    {
        // Ground Check
        RaycastInput groundCheckInput = new RaycastInput
        {
            Start = transform.Position + new float3(0.0f, 1f, 0.0f),
            End = transform.Position + new float3(0.0f, -1f, 0.0f),
            Filter = new CollisionFilter
            {
                BelongsTo = 512u, // Ant
                CollidesWith = 128u, // Wall
                GroupIndex = 0
            }
        };

        RaycastHit groundHit = new RaycastHit();
        bool hitGround = CollisionWorld.CastRay(groundCheckInput, out groundHit);

        float slopeAngle = math.degrees(math.acos(math.dot(new float3(0.0f, 1.0f, 0.0f), groundHit.SurfaceNormal)));

        // Flip velocity
        if (slopeAngle > ant.MaxSlopeAngle && ant.Velocity.y > 0)
        {
            ant.DesiredDirection = -ant.Velocity;
            ant.RandomSteerForce = -ant.Velocity;
        }

        if (hitGround && groundHit.Fraction <= 0.52f)
        {
            ant.IsGrounded = true;
            ant.GroundNormal = groundHit.SurfaceNormal;

            // Fix position
            if (groundHit.Fraction <= 0.48)
                transform.Position = groundHit.Position;
        }
        else
        {
            ant.IsGrounded = false;
        }

        // Collision with walls
        RaycastInput wallCheckInput = new RaycastInput()
        {
            Start = transform.Position + new float3(0.0f, 0.2f, 0.0f),
            End = transform.Position + new float3(0.0f, 0.2f, 0.0f) + transform.Forward() * 0.5f,
            Filter = new CollisionFilter
            {
                BelongsTo = 512u, // Ant
                CollidesWith = 128u, // Wall
                GroupIndex = 0
            }
        };

        // Invert direction on hit with wall
        RaycastHit wallHit = new RaycastHit();
        if (ant.IsGrounded && CollisionWorld.CastRay(wallCheckInput, out wallHit))
        {
            float wallAngle = math.degrees(math.acos(math.dot(new float3(0.0f, 1.0f, 0.0f), wallHit.SurfaceNormal)));

            if (wallAngle < ant.MaxSlopeAngle)
                return;

            float3 newDirection = math.reflect(ant.Velocity, wallHit.SurfaceNormal);

            ant.Velocity = newDirection;
            ant.RandomSteerForce = newDirection;
            ant.DesiredDirection = newDirection;
        }
    }
}