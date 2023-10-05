using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

public partial struct CameraSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CameraData>();
    }

    public void OnUpdate(ref SystemState state)
    {
        RefRW<CameraData> data = SystemAPI.GetSingletonRW<CameraData>();
        CollisionWorld CollisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

        GetCameraRay(data);
        CalculateIntersections(data, CollisionWorld);
    }

    public void GetCameraRay(RefRW<CameraData> data)
    {
        // Get Screen to World Ray
        UnityEngine.Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Save data
        data.ValueRW.Origin = cameraRay.origin;
        data.ValueRW.Direction = cameraRay.direction;
    }

    [BurstCompile]
    public void CalculateIntersections(RefRW<CameraData> data, CollisionWorld CollisionWorld)
    {
        // Handle mouse input
        RaycastInput terrainRayCast = new RaycastInput
        {
            Start = data.ValueRO.Origin,
            End = data.ValueRO.Origin + 500f * data.ValueRO.Direction,
            Filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 128u,
                GroupIndex = 0,
            }
        };

        RaycastInput generalRayCast = new RaycastInput
        {
            Start = data.ValueRO.Origin,
            End = data.ValueRO.Origin + 500f * data.ValueRO.Direction,
            Filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 960u,
                GroupIndex = 0,
            }
        };

        // Handle terrain raycast
        Unity.Physics.RaycastHit terrainHit;
        if (CollisionWorld.CastRay(terrainRayCast, out terrainHit))
        {
            data.ValueRW.TerrainIntersection = terrainHit.Position;
            data.ValueRW.TerrainNormal = terrainHit.SurfaceNormal;
            data.ValueRW.TerrainIntersect = true;
        }
        else
        {
            data.ValueRW.TerrainIntersection = float3.zero;
            data.ValueRW.TerrainNormal = float3.zero;
            data.ValueRW.TerrainIntersect = false;
        }

        // Handle general raycast
        Unity.Physics.RaycastHit generalHit;
        if (CollisionWorld.CastRay(generalRayCast, out generalHit))
        {
            data.ValueRW.Intersection = generalHit.Position;
            data.ValueRW.Intersect = true;
            data.ValueRW.Entity = generalHit.Entity;
        }
        else
        {
            data.ValueRW.Intersection = float3.zero;
            data.ValueRW.Intersect = false;
            data.ValueRW.Entity = Entity.Null;
        }
    }
}
