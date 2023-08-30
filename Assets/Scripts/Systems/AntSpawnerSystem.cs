using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct AntSpawnerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AntSpawner>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Only spawn ants once
        state.Enabled = false;

        var spawner = SystemAPI.GetSingleton<AntSpawner>();

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        var ants = new NativeArray<Entity>(spawner.count, Allocator.Temp);
        ecb.Instantiate(spawner.ant, ants);

        ecb.Playback(state.EntityManager);

        // Random rotation
        Unity.Mathematics.Random random = new Unity.Mathematics.Random(1);

        foreach (var ant in SystemAPI.Query<RefRW<Ant>>())
        {
            ant.ValueRW.DesiredDirection = random.NextFloat3Direction();
            ant.ValueRW.DesiredDirection.y = 0f;
            ant.ValueRW.Velocity = ant.ValueRO.DesiredDirection;
        }
    }
}
