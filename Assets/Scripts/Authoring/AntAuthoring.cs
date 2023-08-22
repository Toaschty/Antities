using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class AntAuthoring : MonoBehaviour
{
    [Header("Movement Settings")]
    public float MaxSpeed = 2f;
    public float SteerStrength = 2f;
    public float WanderStrength = 1f;
    public float SensorStrength = 0.9f;
    public float RandomDirectionAngle = 90f;

    [Header("Random Movement Settings")]
    public float MaxRandomSteerDuration = 1f;
    public float RandomSteerStrength = 0.8f;

    [Header("Turn Around Settings")]
    public float TurnAroundStrength = 1f;

    [Header("Detection Settings")]
    public float ViewAngle = 90f;
    public float ViewRadiusSqrt = 16f;
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
                MaxRandomSteerDuration = authoring.MaxRandomSteerDuration,
                NextRandomSteerTime = Time.time,
                LastPheromonePosition = float3.zero,
                LeftColony = Time.time,
                LeftFood = 0f,
                TurnAroundDirection = authoring.TurnAroundStrength,
                Velocity = float3.zero,
                ViewAngle = authoring.ViewAngle,
                ViewRadiusSqrt = authoring.ViewRadiusSqrt,
                Target = Entity.Null,
                Food = Entity.Null,
                PickUpRadius = authoring.PickUpRadius,
                LeftSensor = GetEntity(authoring.LeftSensor, TransformUsageFlags.Dynamic),
                CenterSensor = GetEntity(authoring.CenterSensor, TransformUsageFlags.Dynamic),
                RightSensor = GetEntity(authoring.RightSensor, TransformUsageFlags.Dynamic),
            });
            AddComponent<TargetFood>(entity);
            SetComponentEnabled<TargetFood>(entity, true);
            AddComponent<TargetColony>(entity);
            SetComponentEnabled<TargetColony>(entity, false);
        }
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

    // Pheromone
    public float3 LastPheromonePosition;

    // Timings
    public float LeftColony;
    public float LeftFood;

    // Turn around
    public float TurnAroundStrength;
    public float3 TurnAroundDirection;

    public float3 Velocity;
    public float3 DesiredDirection;

    // Detection
    public float ViewAngle;
    public float ViewRadiusSqrt;

    public Entity LeftSensor;
    public Entity CenterSensor;
    public Entity RightSensor;

    // Food
    public Entity Target;
    public Entity Food;
    public float PickUpRadius;
}

public struct TargetFood : IComponentData, IEnableableComponent
{
}

public struct TargetColony : IComponentData, IEnableableComponent
{
}