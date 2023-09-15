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
    private BufferLookup<MarkerData> MarkerLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Marker>();

        MarkerLookup = state.GetBufferLookup<MarkerData>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        var markerConfig = SystemAPI.GetSingleton<MarkerConfig>();

        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        MarkerLookup.Update(ref state);

        var decayJob = new DecayJob
        {
            DeltaTime = deltaTime,
            MarkerConfig = markerConfig,
            ECB = ecb.AsParallelWriter(),
            MarkerLookup = MarkerLookup,
        };

        JobHandle decayJobHandle = decayJob.ScheduleParallel(state.Dependency);
        decayJobHandle.Complete();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();

        state.Dependency = decayJobHandle;
    }
}

[BurstCompile]
public partial struct DecayJob : IJobEntity
{
    [ReadOnly] public float DeltaTime;
    [ReadOnly] public MarkerConfig MarkerConfig;

    [NativeDisableParallelForRestriction]
    public BufferLookup<MarkerData> MarkerLookup;
    public EntityCommandBuffer.ParallelWriter ECB;

    [BurstCompile]
    public void Execute(Entity entity, Marker marker, ref LocalTransform transform)
    {
        DynamicBuffer<MarkerData> data = MarkerLookup[entity];

        float completeIntensity = 0f;

        for (int i = 0; i < data.Length; i++)
        {
            data.ElementAt(i).Intensity -= DeltaTime;

            completeIntensity += data.ElementAt(i).Intensity;

            if (data.ElementAt(i).Intensity < 0)
            {
                data.RemoveAt(i);

                if (data.IsEmpty)
                {
                    ECB.DestroyEntity(0, entity);
                    return;
                }
            }
        }
        
        transform.Scale = (float)((completeIntensity / data.Length) / MarkerConfig.PheromoneMaxTime) * MarkerConfig.Scale;
    }
}
