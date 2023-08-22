using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct TargetingSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Ant>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Update ants which are looking for food
        foreach (var (transform, ant) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<Ant>>().WithAny<TargetFood>())
        {
            // Check if ant has already a target
            if (ant.ValueRO.Target != Entity.Null)
            {
                // Check if target was not picked up already => Remove target if this happened
                if (!state.EntityManager.IsComponentEnabled<Food>(ant.ValueRO.Target))
                {
                    ant.ValueRW.Target = Entity.Null;
                }
                else
                {
                    continue;
                }
            }

            foreach (var (m_transform, entity) in SystemAPI.Query<RefRO<LocalToWorld>>().WithAny<Food>().WithEntityAccess())
            {
                // Check if target is inside 
                if (IsInsideView(transform.ValueRO.Position, m_transform.ValueRO.Position, transform.ValueRO.Forward(), ant.ValueRO.ViewAngle, ant.ValueRO.ViewRadiusSqrt))
                {
                    ant.ValueRW.Target = entity;
                    break;
                }
            }
        }

        // Update ants which are looking for colony
        foreach (var (transform, ant) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<Ant>>().WithAny<TargetColony>())
        {
            if (ant.ValueRO.Target != Entity.Null)
                continue;

            foreach (var (c_transform, entity) in SystemAPI.Query<RefRO<LocalToWorld>>().WithAny<Colony>().WithEntityAccess())
            {
                if (IsInsideView(transform.ValueRO.Position, c_transform.ValueRO.Position, transform.ValueRO.Forward(), ant.ValueRO.ViewAngle, ant.ValueRO.ViewRadiusSqrt))
                {
                    ant.ValueRW.Target = entity;
                    break;
                }
            }
        }
    }

    private bool IsInsideView(float3 position, float3 targetPosition, float3 forward, float viewAngle, float viewRadiusSqrt)
    {
        // Check if target is inside range
        if (math.distancesq(position, targetPosition) > viewRadiusSqrt)
            return false;

        var toTarget = targetPosition - position;
        var dot = math.dot(forward, toTarget);
        var angle = math.acos(dot / (math.length(forward) * math.length(toTarget)));

        return angle < viewAngle / 2.0f;
    }
}
