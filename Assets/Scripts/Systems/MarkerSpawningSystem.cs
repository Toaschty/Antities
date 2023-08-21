using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct MarkerSpawningSysten : ISystem
{
    public float elapsedTime;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MarkerConfig>();
        state.RequireForUpdate<Ant>();

        elapsedTime = 0f;
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var markerConfig = SystemAPI.GetSingleton<MarkerConfig>();

        elapsedTime += SystemAPI.Time.DeltaTime;

        if (elapsedTime < markerConfig.TimeBetweenMarkers)
        {
            return;
        }

        foreach (var (transform, ant) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<Ant>>())
        {
            Entity instance = Entity.Null;

            if (ant.ValueRO.State == AntState.SearchingFood)
            {
                instance = state.EntityManager.Instantiate(markerConfig.ToHomeMarker);
            }
            else
            {
                instance = state.EntityManager.Instantiate(markerConfig.ToFoodMarker);
            }

            state.EntityManager.SetComponentData(instance, new LocalTransform
            {
                Position = transform.ValueRO.Position,
                Rotation = quaternion.identity,
                Scale = SystemAPI.GetComponent<LocalTransform>(markerConfig.ToHomeMarker).Scale
            });
        }

        elapsedTime = 0f;
    }
}
