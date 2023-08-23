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
    public int GridSize;
    NativeParallelMultiHashMap<int, Entity> HashMap;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Ant>();
        state.RequireForUpdate<Sensor>();

        GridSize = 1;
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        int markerCount = SystemAPI.QueryBuilder().WithAll<Marker>().Build().CalculateEntityCount();

        HashMap = new NativeParallelMultiHashMap<int, Entity>(markerCount, Allocator.Persistent);

        foreach (var (transform, entity) in SystemAPI.Query<RefRO<LocalToWorld>>().WithAny<Marker>().WithEntityAccess())
        {
            // Generate hash for current position
            int hash = CalculateHash(transform.ValueRO.Position);
            HashMap.Add(hash, entity);
        }

        foreach (var (transform, sensor) in SystemAPI.Query<RefRO<LocalToWorld>, RefRW<Sensor>>())
        {
            sensor.ValueRW.Intensity = 0f;

            var antState = state.EntityManager.GetComponentData<Ant>(sensor.ValueRO.Ant).State;

            NativeList<Entity> entities = new NativeList<Entity>(Allocator.Persistent);
            GetEntitiesInRadius(transform.ValueRO.Position, sensor.ValueRO.Radius, ref entities);

            foreach (Entity m_entity in entities)
            {
                var m_transform = state.EntityManager.GetComponentData<LocalToWorld>(m_entity);
                var m_marker = state.EntityManager.GetComponentData<Marker>(m_entity);

                if ((state.EntityManager.IsComponentEnabled<FoodMarker>(m_entity) && antState == AntState.SearchingFood) ||
                    (state.EntityManager.IsComponentEnabled<ColonyMarker>(m_entity) && antState == AntState.GoingHome))
                {
                    // Calculate distance to marker
                    if (math.distance(transform.ValueRO.Position, m_transform.Position) < sensor.ValueRO.Radius)
                    {
                        sensor.ValueRW.Intensity += m_marker.Intensity;
                    }
                }
            }

            entities.Dispose();
        }

        HashMap.Dispose();
    }

    private void GetEntitiesInRadius(float3 position, float radius, ref NativeList<Entity> entities)
    {
        var gridMultiplier = math.max((int)math.ceil(radius / GridSize), 1);

        var baseCoord = CalculateBaseHashCoord(position);

        for (int i = -gridMultiplier; i <= gridMultiplier; i++)
        {
            for (int j = -gridMultiplier; j <= gridMultiplier; j++)
            {
                var hash = (int)math.hash(baseCoord + new int3(i, 0, j));

                foreach (var entity in HashMap.GetValuesForKey(hash))
                {
                    entities.Add(entity);
                }
            }
        }
    }

    private int CalculateHash(float3 position)
    {
        return (int)math.hash(CalculateBaseHashCoord(position));
    }

    private int3 CalculateBaseHashCoord(float3 position)
    {
        return new int3(math.floor(position * (1.0f / GridSize)));
    }
}
