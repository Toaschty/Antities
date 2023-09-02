using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Transforms;

public partial struct SensorSystem : ISystem
{
    // Lookups
    public ComponentLookup<Ant> AntsLookup;
    public ComponentLookup<Marker> MarkerLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Ant>();
        state.RequireForUpdate<Sensor>();
        state.RequireForUpdate<HashConfig>();

        // Create lookups
        AntsLookup = state.GetComponentLookup<Ant>();
        MarkerLookup = state.GetComponentLookup<Marker>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

        AntsLookup.Update(ref state);
        MarkerLookup.Update(ref state);

        var sensorJob = new PhysicsSensorJob
        {
            AntsLookup = AntsLookup,
            MarkerLookup = MarkerLookup,
            CollisionWorld = collisionWorld,
        };

        JobHandle sensorHandle = sensorJob.ScheduleParallel(state.Dependency);
        sensorHandle.Complete();

        state.Dependency = sensorHandle;
    }
}

[BurstCompile]
public partial struct PhysicsSensorJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<Ant> AntsLookup;
    [ReadOnly] public ComponentLookup<Marker> MarkerLookup;
    [ReadOnly] public CollisionWorld CollisionWorld;

    [BurstCompile]
    public void Execute(ref Sensor sensor, in LocalToWorld transform)
    {
        sensor.Intensity = 0f;

        // Get current ant state > Generate search mask
        AntState antState = AntsLookup.GetRefRO(sensor.Ant).ValueRO.State;
        uint mask = antState == AntState.SearchingFood ? (uint)1024 : (uint)2048; // Food Pheromone : Colony Pheromone

        NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);
        PointDistanceInput pointDistanceInput = new PointDistanceInput
        {
            Position = transform.Position,
            MaxDistance = sensor.Radius,
            Filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = mask,
                GroupIndex = 0
            }
        };

        bool result = CollisionWorld.CalculateDistance(pointDistanceInput, ref hits);

        if (!result)
            return;

        // Calculate intensity of sensor
        foreach (var hit in hits)
        {
            if (MarkerLookup.HasComponent(hit.Entity))
                sensor.Intensity += MarkerLookup.GetRefRO(hit.Entity).ValueRO.Intensity;
        }
    }
}