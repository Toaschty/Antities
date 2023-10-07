using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

public partial struct ResetSimulationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ResetSimulation>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ECB = new EntityCommandBuffer(Allocator.Temp);

        // Remove pending pheromones and ants
        foreach (var (ant, entity) in SystemAPI.Query<RefRW<Ant>>().WithEntityAccess())
        {
            DynamicBuffer<WayPoint> waypoints = SystemAPI.GetBuffer<WayPoint>(entity);

            foreach (WayPoint w in waypoints)
                ECB.DestroyEntity(w.PendingPheromone);

            // Destory carried food
            if (ant.ValueRO.Food != Entity.Null)
                ECB.DestroyEntity(ant.ValueRO.Food);

            ECB.DestroyEntity(entity);
        }

        // Remove path pheromones
        foreach (var (pheromone, entity) in SystemAPI.Query<RefRW<Pheromone>>().WithEntityAccess())
            ECB.DestroyEntity(entity);

        // Respawn food
        foreach (var (food, entity) in SystemAPI.Query<RefRW<Food>>().WithEntityAccess().WithOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IgnoreComponentEnabledState))
        {
            ECB.SetEnabled(entity, true);
            ECB.SetComponentEnabled<Food>(entity, true);
            food.ValueRW.Amount = food.ValueRW.MaxAmount;
        }

        ECB.Playback(state.EntityManager);

        // Reset GUI
        SystemAPI.GetSingletonRW<CameraData>().ValueRW.OnUI = false;


        // Make sure this system only runs once by removing reset component
        Entity simulation;
        SystemAPI.TryGetSingletonEntity<ResetSimulation>(out simulation);
        state.EntityManager.RemoveComponent<ResetSimulation>(simulation);
    }
}
