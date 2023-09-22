using Unity.Burst;
using Unity.Entities;
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

        // Get Screen to World Ray
        Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
    
        // Save data
        data.ValueRW.Origin = cameraRay.origin;
        data.ValueRW.Direction = cameraRay.direction;
    }
}
