using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct FoodPickupSystem : ISystem
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
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        
        foreach (var (transform, ant, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<Ant>>().WithEntityAccess())
        {
            // Skip if ant has currently no food
            if (ant.ValueRO.Target == Entity.Null)
                continue;

            // Check distance to food
            var foodPosition = state.EntityManager.GetComponentData<LocalToWorld>(ant.ValueRO.Target).Position;
            if (math.distance(transform.ValueRO.Position, foodPosition) < ant.ValueRO.PickUpRadius)
            {
                // Save target as food
                ant.ValueRW.Food = ant.ValueRW.Target;

                // Clear target
                ant.ValueRW.Target = Entity.Null;

                // Parent ant
                ecb.AddComponent(ant.ValueRW.Food, new Parent
                {
                    Value = entity
                });

                ecb.SetComponent(ant.ValueRW.Food, new LocalTransform
                {
                    Position = new float3(0.0f, 0.5f, 0.0f),
                    Rotation = Quaternion.identity,
                    Scale = 0.2f,
                });

                // Switch state of ant
                ant.ValueRW.State = AntState.TurningAround;
                ant.ValueRW.TurnAroundDirection = ant.ValueRO.DesiredDirection * -1;

                var leftSensor = state.EntityManager.GetComponentData<Sensor>(ant.ValueRO.LeftSensor);
                var centerSensor = state.EntityManager.GetComponentData<Sensor>(ant.ValueRO.CenterSensor);
                var rightSensor = state.EntityManager.GetComponentData<Sensor>(ant.ValueRO.RightSensor);
                leftSensor.SearchMarker = MarkerType.Home;
                centerSensor.SearchMarker = MarkerType.Home;
                rightSensor.SearchMarker = MarkerType.Home;
                state.EntityManager.SetComponentData(ant.ValueRW.LeftSensor, leftSensor);
                state.EntityManager.SetComponentData(ant.ValueRW.CenterSensor, centerSensor);
                state.EntityManager.SetComponentData(ant.ValueRW.RightSensor, rightSensor);
            }
        }

        ecb.Playback(state.EntityManager);
    }
}