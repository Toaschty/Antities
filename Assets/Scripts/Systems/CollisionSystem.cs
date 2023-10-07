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
        CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        Terrain terrain = SystemAPI.GetSingleton<Terrain>();
        AntConfig antConfig = SystemAPI.GetSingleton<AntConfig>();

        CollisionJob collisionJob = new CollisionJob
        {
            CollisionWorld = collisionWorld,
            Terrain = terrain,
            AntConfig = antConfig
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
    [ReadOnly] public Terrain Terrain;
    [ReadOnly] public AntConfig AntConfig;

    [BurstCompile]
    public void Execute(ref LocalTransform transform, ref Ant ant)
    {
        // Safety check
        if (math.any(math.isnan(transform.Position)))
            return;

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
        if (slopeAngle > AntConfig.MaxSlopeAngle && ant.Velocity.y > 0)
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

            if (wallAngle < AntConfig.MaxSlopeAngle)
                return;

            float3 newDirection = math.reflect(ant.Velocity, wallHit.SurfaceNormal);

            ant.Velocity = newDirection;
            ant.RandomSteerForce = newDirection;
            ant.DesiredDirection = newDirection;
        }

        // Collision with outer terrain borders
        bool flipDirection = false;
        float3 reflectDirection = float3.zero;
        
        if (transform.Position.x < 1.5f && ant.Velocity.x < 0)
        {
            flipDirection = true;
            reflectDirection = new float3(1.0f, 0.0f, 0.0f);
        }
        if (transform.Position.z < 1.5f && ant.Velocity.z < 0)
        {
            flipDirection = true;
            reflectDirection = new float3(0.0f, 0.0f, 1.0f);
        }
        if (transform.Position.x > Terrain.Width - 1.5f && ant.Velocity.x > 0)
        {
            flipDirection = true;
            reflectDirection = new float3(-1.0f, 0.0f, 0.0f);
        }
        if (transform.Position.z > Terrain.Depth - 1.5f && ant.Velocity.z > 0)
        {
            flipDirection = true;
            reflectDirection = new float3(0.0f, 0.0f, -1.0f);
        }

        if (flipDirection)
        {
            float3 newDir = math.reflect(ant.Velocity, reflectDirection);

            ant.DesiredDirection = newDir;
            ant.RandomSteerForce = newDir;
            ant.Velocity = newDir;
        }
    }
}