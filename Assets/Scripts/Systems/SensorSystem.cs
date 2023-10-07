using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

public partial struct SensorSystem : ISystem
{
    private ComponentLookup<Ant> AntLookup;
    private ComponentLookup<Pheromone> PheromoneLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Ant>();
        state.RequireForUpdate<Sensor>();

        // Create lookups
        AntLookup = state.GetComponentLookup<Ant>();
        PheromoneLookup = state.GetComponentLookup<Pheromone>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        CollisionWorld CollisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

        AntLookup.Update(ref state);
        PheromoneLookup.Update(ref state);

        PhysicsSensorJob sensorJob = new PhysicsSensorJob
        {
            AntsLookup = AntLookup,
            PheromoneLookup = PheromoneLookup,
            CollisionWorld = CollisionWorld,
        };

        state.Dependency = sensorJob.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
public partial struct PhysicsSensorJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<Pheromone> PheromoneLookup;
    [ReadOnly] public CollisionWorld CollisionWorld;

    [NativeDisableParallelForRestriction] public ComponentLookup<Ant> AntsLookup;

    [BurstCompile]
    public void Execute(ref Sensor sensor, in LocalToWorld transform)
    {
        RefRW<Ant> ant = AntsLookup.GetRefRW(sensor.Ant);

        // Get current ant state > Generate search mask
        NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);
        PointDistanceInput pointDistanceInput = new PointDistanceInput
        {
            Position = transform.Position,
            MaxDistance = sensor.Radius,
            Filter = new CollisionFilter
            {
                BelongsTo = 8192u, // Sensor
                CollidesWith = 16384u, //mask,
                GroupIndex = 0
            }
        };

        CollisionWorld.CalculateDistance(pointDistanceInput, ref hits);

        // Calculate intensity of sensor
        float maxQuality = 0f;

        // Find highest quality path
        foreach (var hit in hits)
        {
            if (PheromoneLookup.GetRefRO(hit.Entity).ValueRO.Quality > maxQuality)
                maxQuality = PheromoneLookup.GetRefRO(hit.Entity).ValueRO.Quality;
        }

        sensor.Intensity = maxQuality;

        if (ant.ValueRO.HighestQualityFound < maxQuality)
        {
            ant.ValueRW.HighestQualityFound = maxQuality;
        }
    }
}