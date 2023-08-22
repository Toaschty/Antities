using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct MarkerSpawningSysten : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MarkerConfig>();
        state.RequireForUpdate<Ant>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var markerConfig = SystemAPI.GetSingleton<MarkerConfig>();

        foreach (var (transform, ant) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<Ant>>())
        {
            // Place first pheromone without checking the distance => No previous pheromone here
            if (math.lengthsq(ant.ValueRO.LastPheromonePosition) > 0.0f)
            {
                // Check distance to last pheronome
                if (math.distance(transform.ValueRO.Position, ant.ValueRO.LastPheromonePosition) < markerConfig.DistanceBetweenMarkers)
                    continue;
            }

            Entity pheromoneInstance = Entity.Null;
            float intensity = 0f;

            if (ant.ValueRO.State == AntState.SearchingFood)
            {
                pheromoneInstance = state.EntityManager.Instantiate(markerConfig.ToHomeMarker);
                state.EntityManager.SetComponentEnabled<ColonyMarker>(pheromoneInstance, true);
                intensity = 1 - (Time.time - ant.ValueRO.LeftColony) / markerConfig.PheromoneMaxTime;
            }
            else
            {
                pheromoneInstance = state.EntityManager.Instantiate(markerConfig.ToFoodMarker);
                state.EntityManager.SetComponentEnabled<FoodMarker>(pheromoneInstance, true);
                intensity = 1 - (Time.time - ant.ValueRO.LeftFood) / markerConfig.PheromoneMaxTime;
            }

            intensity = math.lerp(markerConfig.PheromoneMaxTime / 4f, markerConfig.PheromoneMaxTime, intensity);
            state.EntityManager.SetComponentData(pheromoneInstance, new Marker
            {
                Intensity = intensity,
                Scale = state.EntityManager.GetComponentData<Marker>(pheromoneInstance).Scale
            });

            state.EntityManager.SetComponentData(pheromoneInstance, new LocalTransform
            {
                Position = transform.ValueRO.Position,
                Rotation = quaternion.identity,
                Scale = SystemAPI.GetComponent<Marker>(pheromoneInstance).Scale
            });

            ant.ValueRW.LastPheromonePosition = transform.ValueRO.Position;
        }
    }
}
