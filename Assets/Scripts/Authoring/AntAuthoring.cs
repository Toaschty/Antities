using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

public class AntAuthoring : MonoBehaviour
{
    [Header("Movement Settings")]
    public float MaxSpeed = 2f;
    public float SteerStrength = 2f;
    public float WanderStrength = 1f;
    public float SensorStrength = 0.9f;
    public float RandomDirectionAngle = 90f;
    public float GroundLevel = 0f;

    [Header("Random Movement Settings")]
    public float MaxRandomSteerDuration = 1f;
    public float RandomSteerStrength = 0.8f;

    [Header("Turn Around Settings")]
    [Range(0f, 1f)]
    public float TurnAroundStrength = 1f;

    [Header("Detection Settings")]
    public float ViewAngle = 90f;
    public float ViewRadius = 4f;
    public float PickUpRadius = 0.5f;

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
                MaxSpeed = authoring.MaxSpeed,
                SteerStrength = authoring.SteerStrength,
                WanderStrength = authoring.WanderStrength,
                SensorStength = authoring.SensorStrength,
                RandomDirectionAngle = authoring.RandomDirectionAngle,
                RandomSteerForce = float3.zero,
                RandomSteerStength = authoring.RandomSteerStrength,
                IsGrounded = false,
                GroundNormal = new float3(0.0f, 1.0f, 0.0f),
                MaxRandomSteerDuration = authoring.MaxRandomSteerDuration,
                NextRandomSteerTime = Time.time,
                LastPheromonePosition = float3.zero,
                LeftColony = Time.time,
                LeftFood = 0f,
                TurnAroundStrength = authoring.TurnAroundStrength,
                TurnAroundDirection = float3.zero,
                Velocity = float3.zero,
                ViewAngle = authoring.ViewAngle * Mathf.Deg2Rad,
                ViewRadius = authoring.ViewRadius,
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
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawCube(new float3(0.0f, GroundLevel, 0.0f), new float3(1.0f, 0.01f, 1.0f));
    }
}


public enum AntState
{
    SearchingFood,
    TurningAround,
    GoingHome
}

public struct Ant : IComponentData
{
    // State
    public AntState State;

    // Movement
    public float MaxSpeed;
    public float SteerStrength;
    public float WanderStrength;
    public float SensorStength;
    public float RandomDirectionAngle;
    public float MaxRandomSteerDuration;
    public float RandomSteerStength;
    public float NextRandomSteerTime;
    public float3 RandomSteerForce;

    public bool IsGrounded;
    public bool IsSliding;
    public float3 GroundNormal;

    // Pheromone
    public float3 LastPheromonePosition;

    // Timings
    public double LeftColony;
    public double LeftFood;

    // Turn around
    public float TurnAroundStrength;
    public float3 TurnAroundDirection;

    public float3 Velocity;
    public float3 DesiredDirection;

    // Detection
    public float ViewAngle;
    public float ViewRadius;

    public Entity LeftSensor;
    public Entity CenterSensor;
    public Entity RightSensor;

    // Food
    public Entity Target;
    public Entity Food;
}

public struct SkipMarkerSpawning : IComponentData
{ 
}

public struct TargetingFood : IComponentData, IEnableableComponent
{
}

public struct TargetingColony : IComponentData, IEnableableComponent
{
}