using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct ColonySearchingSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Ant>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, ant) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<Ant>>())
        {
            // If ant has already food
            if (ant.ValueRO.Target != Entity.Null || ant.ValueRO.State != AntState.GoingHome)
                continue;

            // Check all colonies on map
            foreach (var (c_transform, entity) in SystemAPI.Query<RefRO<LocalToWorld>>().WithAny<Colony>().WithEntityAccess())
            {
                // Check distance to every colony
                if (math.distancesq(transform.ValueRO.Position, c_transform.ValueRO.Position) < ant.ValueRO.ViewRadiusSqrt)
                {
                    // Calculate angle to food
                    var toFood = c_transform.ValueRO.Position - transform.ValueRO.Position;
                    var dot = math.dot(transform.ValueRO.Forward(), toFood);
                    var angle = math.acos(dot / (math.length(transform.ValueRO.Forward()) * math.length(toFood)));

                    // Check if food is inside view angle
                    if (angle < ant.ValueRO.ViewAngle / 2)
                    {
                        ant.ValueRW.Target = entity;
                    }
                }
            }
        }
    }
}
