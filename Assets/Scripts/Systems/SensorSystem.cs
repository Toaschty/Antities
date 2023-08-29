using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct SensorSystem : ISystem
{
    NativeParallelMultiHashMap<int, Entity> HashMap;

    // Lookups
    public ComponentLookup<Ant> AntsLookup;
    public ComponentLookup<LocalToWorld> LocalToWorldLookup;
    public ComponentLookup<Marker> MarkerLookup;
    public ComponentLookup<FoodMarker> FoodMarkerLookup;
    public ComponentLookup<ColonyMarker> ColonyMarkerLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Ant>();
        state.RequireForUpdate<Sensor>();
        state.RequireForUpdate<HashConfig>();

        // Create lookups
        AntsLookup = state.GetComponentLookup<Ant>();
        LocalToWorldLookup = state.GetComponentLookup<LocalToWorld>();
        MarkerLookup = state.GetComponentLookup<Marker>();
        FoodMarkerLookup = state.GetComponentLookup<FoodMarker>();
        ColonyMarkerLookup = state.GetComponentLookup<ColonyMarker>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Get grid config
        float gridSize = SystemAPI.GetSingleton<HashConfig>().GridSize;

        // Create new hashmap for current amount of markers
        int markerCount = SystemAPI.QueryBuilder().WithAll<Marker>().Build().CalculateEntityCount();
        HashMap = new NativeParallelMultiHashMap<int, Entity>(markerCount, Allocator.TempJob);

        // Fill hashmap with marker data
        EntityQuery markerQuery = SystemAPI.QueryBuilder().WithAll<LocalToWorld, Marker>().Build();
        NativeArray<LocalToWorld> markerTransforms = markerQuery.ToComponentDataArray<LocalToWorld>(Allocator.TempJob);
        NativeArray<Entity> markerEntities = markerQuery.ToEntityArray(Allocator.TempJob);
        var hashMarkerJob = new HashMarkerJob
        {
            MarkerTransforms = markerTransforms,
            MarkerEntities = markerEntities,
            GridSize = gridSize,
            HashMap = HashMap.AsParallelWriter()
        };

        JobHandle hashMarkerHandle = hashMarkerJob.Schedule(markerCount, 512, state.Dependency);

        // Update lookups
        AntsLookup.Update(ref state);
        LocalToWorldLookup.Update(ref state);
        MarkerLookup.Update(ref state);
        FoodMarkerLookup.Update(ref state);
        ColonyMarkerLookup.Update(ref state);

        hashMarkerHandle.Complete();

        // Cleanup
        markerTransforms.Dispose();
        markerEntities.Dispose();

        // Calculate intensity for every sensor
        var sensorJob = new SensorJob
        {
            AntsLookup = AntsLookup,
            GridSize = gridSize,
            HashMap = HashMap,
            LocalToWorldLookup = LocalToWorldLookup,
            MarkerLookup = MarkerLookup,
            FoodMarkerLookup = FoodMarkerLookup,
            ColonyMarkerLookup = ColonyMarkerLookup,
        };

        JobHandle sensorHandle = sensorJob.ScheduleParallel(hashMarkerHandle);
        sensorHandle.Complete();

        state.Dependency = sensorHandle;

        // Cleanup
        HashMap.Dispose();
    }

    private void GetEntitiesInRadius(float3 position, float radius, int gridSize, ref NativeList<Entity> entities)
    {
        var gridMultiplier = math.max((int)math.ceil(radius / gridSize), 1);

        var baseCoord = CalculateBaseHashCoord(position, gridSize);

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

    private int CalculateHash(float3 position, int gridSize)
    {
        return (int)math.hash(CalculateBaseHashCoord(position, gridSize));
    }

    private int3 CalculateBaseHashCoord(float3 position, int gridSize)
    {
        return new int3(math.floor(position * (1.0f / gridSize)));
    }
}

public struct HashMarkerJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<LocalToWorld> MarkerTransforms;
    [ReadOnly] public NativeArray<Entity> MarkerEntities;
    [ReadOnly] public float GridSize;

    public NativeParallelMultiHashMap<int, Entity>.ParallelWriter HashMap;

    public void Execute(int index)
    {
        // Generate hash for current position
        int hash = (int)math.hash(new int3(math.floor(MarkerTransforms[index].Position * (1.0f / GridSize))));
        HashMap.Add(hash, MarkerEntities[index]);
    }
}

[BurstCompile]
public partial struct SensorJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<Ant> AntsLookup;
    [ReadOnly] public float GridSize;
    [ReadOnly] public NativeParallelMultiHashMap<int, Entity> HashMap;
    [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
    [ReadOnly] public ComponentLookup<Marker> MarkerLookup;
    [ReadOnly] public ComponentLookup<FoodMarker> FoodMarkerLookup;
    [ReadOnly] public ComponentLookup<ColonyMarker> ColonyMarkerLookup;

    [BurstCompile]
    public void Execute(ref Sensor sensor, in LocalToWorld transform)
    {
        sensor.Intensity = 0f;

        // Get current ant state
        AntState antState = AntsLookup.GetRefRO(sensor.Ant).ValueRO.State;

        // Get marker entities inside nearby hashes
        var gridMultiplier = math.max((int)math.ceil(sensor.Radius / GridSize), 1);
        var baseCoord = new int3(math.floor(transform.Position * (1.0f / GridSize)));

        for (int i = -gridMultiplier; i <= gridMultiplier; i++)
        {
            for (int j = -gridMultiplier; j <= gridMultiplier; j++)
            {
                var hash = (int)math.hash(baseCoord + new int3(i, 0, j));

                foreach (var hashEntity in HashMap.GetValuesForKey(hash))
                {
                    if (math.distancesq(transform.Position, LocalToWorldLookup.GetRefRO(hashEntity).ValueRO.Position) < sensor.RadiusSqrt)
                    {
                        if ((FoodMarkerLookup.IsComponentEnabled(hashEntity) && antState == AntState.SearchingFood) ||
                            (ColonyMarkerLookup.IsComponentEnabled(hashEntity) && antState == AntState.GoingHome))
                        {
                            sensor.Intensity += MarkerLookup.GetRefRO(hashEntity).ValueRO.Intensity;
                        }
                    }
                }
            }
        }
    }
}