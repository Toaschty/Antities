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
        CalculateTerrainIntersection(data, CollisionWorld);
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
    public void CalculateTerrainIntersection(RefRW<CameraData> data, CollisionWorld CollisionWorld)
    {
        // Handle mouse input
        RaycastInput mouseRayInput = new RaycastInput
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

        // Handle state of brush
        Unity.Physics.RaycastHit hit;
        if (CollisionWorld.CastRay(mouseRayInput, out hit))
        {
            data.ValueRW.Intersection = hit.Position;
            data.ValueRW.Intersects = true;
        }
        else
        {
            data.ValueRW.Intersection = float3.zero;
            data.ValueRW.Intersects = false;
        }
    }
}
