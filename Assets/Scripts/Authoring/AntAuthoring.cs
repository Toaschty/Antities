using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class AntAuthoring : MonoBehaviour
{
    public float MaxSpeed = 2f;
    public float SteerStrength = 2f;
    public float WanderStrength = 1f;

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
                Velocity = float3.zero,
                ViewAngle = 90,
                ViewRadiusSqrt = 16,
                Target = Entity.Null,
                Food = Entity.Null,
                PickUpRadius = 0.5f,
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

    // Turn around
    public float3 TurnAroundDirection;
    public float TurnAroundStrength;

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