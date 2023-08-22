using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct SensorSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Ant>();
        state.RequireForUpdate<Sensor>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, sensor) in SystemAPI.Query<RefRO<LocalToWorld>, RefRW<Sensor>>())
        {
            sensor.ValueRW.Intensity = 0f;

            var antState = state.EntityManager.GetComponentData<Ant>(sensor.ValueRO.Ant).State;

            // TODO - Improve Query
            foreach (var (m_transform, marker, entity) in SystemAPI.Query<RefRO<LocalToWorld>, RefRO<Marker>>().WithEntityAccess())
            {
                if ((state.EntityManager.IsComponentEnabled<FoodMarker>(entity) && antState == AntState.SearchingFood) ||
                    (state.EntityManager.IsComponentEnabled<ColonyMarker>(entity) && antState == AntState.GoingHome))
                {
                    // Calculate distance to marker
                    if (math.distance(transform.ValueRO.Position, m_transform.ValueRO.Position) < sensor.ValueRO.Radius)
                    {
                        sensor.ValueRW.Intensity += marker.ValueRO.Intensity;
                    }
                }
            }
        }
    }
}
