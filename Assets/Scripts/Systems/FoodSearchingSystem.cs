using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct FoodSearchingSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Ant>();
        state.RequireForUpdate<Food>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, ant) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<Ant>>())
        {
            // If ant has already food
            if (ant.ValueRO.Target != Entity.Null || ant.ValueRO.State != AntState.SearchingFood)
                continue;

            // Check all food on map
            foreach (var (m_transform, food, entity) in SystemAPI.Query<RefRO<LocalToWorld>, RefRW<Food>>().WithEntityAccess())
            {
                // Skip food which is already targeted by other ants
                if (food.ValueRO.Targeted)
                    continue;

                // Check distance to every food
                if (math.distancesq(transform.ValueRO.Position, m_transform.ValueRO.Position) < ant.ValueRO.ViewRadiusSqrt)
                {
                    // Calculate angle to food
                    var toFood = m_transform.ValueRO.Position - transform.ValueRO.Position;
                    var dot = math.dot(transform.ValueRO.Forward(), toFood);
                    var angle = math.acos(dot / (math.length(transform.ValueRO.Forward()) * math.length(toFood)));

                    // Check if food is inside view angle
                    if (angle < ant.ValueRO.ViewAngle / 2)
                    {
                        ant.ValueRW.Target = entity;
                        food.ValueRW.Targeted = true;
                    }
                }
            }
        }
    }
}
