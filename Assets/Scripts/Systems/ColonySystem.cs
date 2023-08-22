using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct ColonySystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Ant>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (colony_transform, colony) in SystemAPI.Query<RefRO<LocalToWorld>, RefRO<Colony>>())
        {
            foreach (var (ant_transform, ant, entity) in SystemAPI.Query<RefRO<LocalToWorld>, RefRW<Ant>>().WithEntityAccess())
            {
                // Check if ant is carrying food
                if (ant.ValueRO.Food == Entity.Null)
                    continue;

                // Calculate distance to ant > Deposit food
                if (math.distance(colony_transform.ValueRO.Position, ant_transform.ValueRO.Position) < colony.ValueRO.DepositRadius)
                {
                    // Destroy food
                    ecb.DestroyEntity(ant.ValueRW.Food);
                    
                    // Reset ant
                    ant.ValueRW.Food = Entity.Null;
                    ant.ValueRW.Target = Entity.Null;
                    ant.ValueRW.State = AntState.SearchingFood;

                    // Swap search target
                    state.EntityManager.SetComponentEnabled<TargetFood>(entity, true);
                    state.EntityManager.SetComponentEnabled<TargetColony>(entity, false);

                    // Reset timings
                    ant.ValueRW.LeftColony = Time.time;
                    ant.ValueRW.LeftFood = 0f;

                    // Instantly turn around
                    ant.ValueRW.Velocity = -ant.ValueRO.DesiredDirection;
                    ant.ValueRW.DesiredDirection = -ant.ValueRO.DesiredDirection;
                }
            }
        }

        ecb.Playback(state.EntityManager);
    }
}
