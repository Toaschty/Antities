using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.VisualScripting;

public partial struct MarkerDecaySystem : ISystem
{
    private ComponentLookup<Pheromone> PheromoneLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Pheromone>();

        PheromoneLookup = state.GetComponentLookup<Pheromone>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var PheromoneConfig = SystemAPI.GetSingleton<PheromoneConfig>();
        var ECB = new EntityCommandBuffer(Allocator.TempJob);

        PheromoneLookup.Update(ref state);

        var decayJob = new DecayJob
        {
            Time = (float)SystemAPI.Time.ElapsedTime,
            PheromoneConfig = PheromoneConfig,
            ECB = ECB.AsParallelWriter(),
        };

        JobHandle decayJobHandle = decayJob.ScheduleParallel(state.Dependency);
        decayJobHandle.Complete();
        
        if (!ECB.IsEmpty)
        {
            // Reset path qualities
            foreach (var ant in SystemAPI.Query<RefRW<Ant>>())
                ant.ValueRW.HighestQualityFound = 0f;
        }

        ECB.Playback(state.EntityManager);
        ECB.Dispose();

        state.Dependency = decayJobHandle;
    }
}

[BurstCompile]
public partial struct DecayJob : IJobEntity
{
    [ReadOnly] public float Time;
    [ReadOnly] public PheromoneConfig PheromoneConfig;

    public EntityCommandBuffer.ParallelWriter ECB;

    [BurstCompile]
    public void Execute(Entity entity, ref Pheromone pheromone, ref LocalTransform transform)
    {
        if (pheromone.LifeTime < Time && pheromone.Quality == 0.0001f)
            pheromone.Quality = 0.0001f;

        if (pheromone.LifeTime + 30 < Time)
        {
            ECB.DestroyEntity(0, entity);
            return;
        }

        transform.Scale = (float)((pheromone.LifeTime - Time) / PheromoneConfig.PheromoneMaxTime) * PheromoneConfig.Scale;
    }
}
