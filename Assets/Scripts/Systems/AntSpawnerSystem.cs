using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
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
    }
}
