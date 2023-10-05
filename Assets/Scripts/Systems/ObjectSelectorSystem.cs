using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

public partial struct ObjectSelectorSystem : ISystem
{
    private Entity previousSelected;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CameraData>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        CameraData cameraData = SystemAPI.GetSingleton<CameraData>();

        if (cameraData.Intersect && !cameraData.OnUI && Input.GetMouseButtonDown(0))
        {
            // Check if current object is a terrain object
            if (!state.EntityManager.HasComponent<PlacedTerrainObject>(cameraData.Entity))
                return;

            // Deselect previous entity
            state.EntityManager.RemoveComponent<SelectedObject>(previousSelected);

            // Select current entity
            state.EntityManager.AddComponent<SelectedObject>(cameraData.Entity);
            previousSelected = cameraData.Entity;
        }
    }
}
