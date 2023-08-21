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
            foreach (var (ant_transform, ant) in SystemAPI.Query<RefRO<LocalToWorld>, RefRW<Ant>>())
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

                    var leftSensor = state.EntityManager.GetComponentData<Sensor>(ant.ValueRO.LeftSensor);
                    var centerSensor = state.EntityManager.GetComponentData<Sensor>(ant.ValueRO.CenterSensor);
                    var rightSensor = state.EntityManager.GetComponentData<Sensor>(ant.ValueRO.RightSensor);
                    leftSensor.SearchMarker = MarkerType.Food;
                    centerSensor.SearchMarker = MarkerType.Food;
                    rightSensor.SearchMarker = MarkerType.Food;
                    state.EntityManager.SetComponentData(ant.ValueRW.LeftSensor, leftSensor);
                    state.EntityManager.SetComponentData(ant.ValueRW.CenterSensor, centerSensor);
                    state.EntityManager.SetComponentData(ant.ValueRW.RightSensor, rightSensor);
                }
            }
        }

        ecb.Playback(state.EntityManager);
    }
}
