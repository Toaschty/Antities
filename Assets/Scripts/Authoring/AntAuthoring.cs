using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class AntAuthoring : MonoBehaviour
{
    [Header("Sensors")]
    public GameObject LeftSensor;
    public GameObject CenterSensor;
    public GameObject RightSensor;

    class Baker : Baker<AntAuthoring>
    {
        public override void Bake(AntAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Ant
            {
                State = AntState.SearchingFood,
                RandomSteerForce = float3.zero,
                IsGrounded = false,
                HighestQualityFound = 0f,
                GroundNormal = new float3(0.0f, 1.0f, 0.0f),
                NextRandomSteerTime = Time.time,
                LastPheromonePosition = float3.zero,
                Velocity = float3.zero,
                Target = Entity.Null,
                Food = Entity.Null,
                LeftSensor = GetEntity(authoring.LeftSensor, TransformUsageFlags.None),
                CenterSensor = GetEntity(authoring.CenterSensor, TransformUsageFlags.None),
                RightSensor = GetEntity(authoring.RightSensor, TransformUsageFlags.None),
            });
            AddComponent<TargetingFood>(entity);
            SetComponentEnabled<TargetingFood>(entity, true);
            AddComponent<TargetingColony>(entity);
            SetComponentEnabled<TargetingColony>(entity, false);
            AddBuffer<WayPoint>(entity);
            AddComponent<BuildPath>(entity);
            SetComponentEnabled<BuildPath>(entity, false);
            AddComponent<SpawnPendingPheromones>(entity);
        }
    }
}


public enum AntState
{
    SearchingFood,
    GoingHome
}

public struct Ant : IComponentData
{
    // State
    public AntState State;

    // Colony
    public float3 ColonyPosition;

    // Movement
    public float NextRandomSteerTime;
    public float3 RandomSteerForce;

    public bool IsGrounded;
    public float3 GroundNormal;

    // Path
    public float3 LastPheromonePosition;
    public float HighestQualityFound;

    public float3 Velocity;
    public float3 DesiredDirection;

    // Sensors
    public Entity LeftSensor;
    public Entity CenterSensor;
    public Entity RightSensor;

    // Food
    public Entity Target;
    public Entity Food;

    public static EntityQuery GetQuery()
    {
        return World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(new ComponentType[] { typeof(Ant) });
    }
}

[InternalBufferCapacity(512)]
public struct WayPoint : IBufferElementData
{
    public float3 Position;
    public Entity PendingPheromone;
}

public struct BuildPath : IComponentData, IEnableableComponent
{
}

public struct SpawnPendingPheromones : IComponentData, IEnableableComponent
{
}

public struct TargetingFood : IComponentData, IEnableableComponent
{
}

public struct TargetingColony : IComponentData, IEnableableComponent
{
}