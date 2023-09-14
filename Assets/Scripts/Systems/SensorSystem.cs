using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Transforms;

public partial struct SensorSystem : ISystem
{
    // Lookups
    public ComponentLookup<Ant> AntLookup;
    public ComponentLookup<Marker> MarkerLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Ant>();
        state.RequireForUpdate<Sensor>();
        state.RequireForUpdate<Marker>();

        // Create lookups
        AntLookup = state.GetComponentLookup<Ant>();
        MarkerLookup = state.GetComponentLookup<Marker>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        var ECB = new EntityCommandBuffer(Allocator.TempJob);

        AntLookup.Update(ref state);
        MarkerLookup.Update(ref state);

        var sensorJob = new PhysicsSensorJob
        {
            AntsLookup = AntLookup,
            MarkerLookup = MarkerLookup,
            CollisionWorld = collisionWorld,
            ECB = ECB.AsParallelWriter(),
        };

        JobHandle sensorHandle = sensorJob.ScheduleParallel(state.Dependency);
        sensorHandle.Complete();

        ECB.Playback(state.EntityManager);
        ECB.Dispose();

        state.Dependency = sensorHandle;
    }
}

[BurstCompile]
public partial struct PhysicsSensorJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<Ant> AntsLookup;
    [ReadOnly] public ComponentLookup<Marker> MarkerLookup;
    [ReadOnly] public CollisionWorld CollisionWorld;

    public EntityCommandBuffer.ParallelWriter ECB;

    [BurstCompile]
    public void Execute(ref Sensor sensor, in LocalToWorld transform)
    {
        // Get current ant state > Generate search mask
        AntState antState = AntsLookup.GetRefRO(sensor.Ant).ValueRO.State;
        uint mask = antState == AntState.SearchingFood ? 1024u : 2048u; // Food Pheromone : Colony Pheromone

        NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);
        PointDistanceInput pointDistanceInput = new PointDistanceInput
        {
            Position = transform.Position,
            MaxDistance = sensor.Radius,
            Filter = new CollisionFilter
            {
                BelongsTo = 8192u, // Sensor
                CollidesWith = mask,
                GroupIndex = 0
            }
        };

        CollisionWorld.CalculateDistance(pointDistanceInput, ref hits);

        // Calculate intensity of sensor
        float sIntensity = 0f;
        foreach (var hit in hits)
        {
            if (MarkerLookup.HasComponent(hit.Entity))
                sIntensity += MarkerLookup.GetRefRO(hit.Entity).ValueRO.Intensity;
        }
        sensor.Intensity = sIntensity;

        //if (hits.Length > 50)
        //    ECB.DestroyEntity(0, hits[0].Entity);

        hits.Dispose();
    }
}