using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
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

            foreach (var (m_transform, marker) in SystemAPI.Query<RefRO<LocalToWorld>, RefRO<Marker>>())
            {
                // Skip all markers which are not needed
                if (sensor.ValueRO.SearchMarker != marker.ValueRO.Type)
                    continue;

                // Calculate distance to marker
                if (math.distance(transform.ValueRO.Position, m_transform.ValueRO.Position) < sensor.ValueRO.Radius)
                {
                    sensor.ValueRW.Intensity += marker.ValueRO.Intensity;
                }
            }
        }
    }
}
