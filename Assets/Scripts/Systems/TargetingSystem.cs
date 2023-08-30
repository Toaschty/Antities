using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
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
        var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

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

            Entity nearestTarget = Entity.Null;
            float distance = 0f;

            foreach (var (m_transform, entity) in SystemAPI.Query<RefRO<LocalToWorld>>().WithAny<Food>().WithEntityAccess())
            {
                // Check if target is inside 
                if (IsInsideView(transform.ValueRO.Position, m_transform.ValueRO.Position, transform.ValueRO.Forward(), ant.ValueRO.ViewAngle, ant.ValueRO.ViewRadiusSqrt, collisionWorld))
                {
                    var currDist = math.distance(transform.ValueRO.Position, m_transform.ValueRO.Position);

                    // Set entity instantly the first time
                    if (nearestTarget == Entity.Null)
                    {
                        nearestTarget = entity;
                        distance = currDist;
                        continue;
                    }

                    // Check if current entity is closer than the previous ones
                    if (currDist < distance)
                    {
                        nearestTarget = entity;
                        distance = currDist;
                    }
                }
            }

            if (nearestTarget != Entity.Null)
                ant.ValueRW.Target = nearestTarget;
        }

        // Update ants which are looking for colony
        foreach (var (transform, ant) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<Ant>>().WithAny<TargetColony>())
        {
            if (ant.ValueRO.Target != Entity.Null)
                continue;

            foreach (var (c_transform, entity) in SystemAPI.Query<RefRO<LocalToWorld>>().WithAny<Colony>().WithEntityAccess())
            {
                if (IsInsideView(transform.ValueRO.Position, c_transform.ValueRO.Position, transform.ValueRO.Forward(), ant.ValueRO.ViewAngle, ant.ValueRO.ViewRadiusSqrt, collisionWorld))
                {
                    ant.ValueRW.Target = entity;
                    break;
                }
            }
        }
    }

    private bool IsInsideView(float3 position, float3 targetPosition, float3 forward, float viewAngle, float viewRadiusSqrt, CollisionWorld collisionWorld)
    {
        // Check if target is inside range
        if (math.distancesq(position, targetPosition) > viewRadiusSqrt)
            return false;

        // Check if object is blocked by wall
        RaycastInput input = new RaycastInput
        {
            Start = position,
            End = targetPosition,
            Filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = ~0u,
                GroupIndex = 0
            }
        };

        Debug.DrawLine(position, targetPosition, Color.red, 0.5f);

        Unity.Physics.RaycastHit hit = new Unity.Physics.RaycastHit();
        bool haveHit = collisionWorld.CastRay(input, out hit);

        if (haveHit)
            return false;

        var toTarget = targetPosition - position;
        var dot = math.dot(forward, toTarget);
        var angle = math.acos(dot / (math.length(forward) * math.length(toTarget)));

        return angle < viewAngle / 2.0f;
    }
}
