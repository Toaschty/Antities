using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

public partial struct AntSpawnerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AntSpawner>();
        state.RequireForUpdate<RunningSimulation>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Only spawn ants once
        state.Enabled = false;

        AntSpawner spawner = SystemAPI.GetSingleton<AntSpawner>();
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (transform, colony) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<Colony>>())
        {
            // Spawn ants
            NativeArray<Entity> ants = new NativeArray<Entity>(colony.ValueRO.AntAmount, Allocator.Temp);
            state.EntityManager.Instantiate(spawner.ant, ants);

            // Set ants to correct position
            foreach (Entity ant in ants)
            {
                SystemAPI.GetComponentRW<LocalTransform>(ant).ValueRW.Position = transform.ValueRO.Position;
                SystemAPI.GetComponentRW<Ant>(ant).ValueRW.ColonyPosition = transform.ValueRO.Position;
            }
        }

        ecb.Playback(state.EntityManager);

        // Random rotation
        Unity.Mathematics.Random random = new Unity.Mathematics.Random(1);

        foreach (var ant in SystemAPI.Query<RefRW<Ant>>())
        {
            ant.ValueRW.LastPheromonePosition = random.NextFloat3Direction();
            ant.ValueRW.DesiredDirection = random.NextFloat3Direction();
            ant.ValueRW.DesiredDirection.y = 0f;
            ant.ValueRW.Velocity = ant.ValueRO.DesiredDirection;
        }

    }
}
