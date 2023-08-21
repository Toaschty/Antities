using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(SensorSystem))]
public partial struct AntMovementSystem : ISystem
{
    Unity.Mathematics.Random random;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Ant>();
        random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (transform, movement) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<Ant>>())
        {
            // Get sensor data
            float leftSensorIntensity = state.EntityManager.GetComponentData<Sensor>(movement.ValueRO.LeftSensor).Intensity;
            float centerSensorIntensity = state.EntityManager.GetComponentData<Sensor>(movement.ValueRO.CenterSensor).Intensity;
            float rightSensorIntensity = state.EntityManager.GetComponentData<Sensor>(movement.ValueRO.RightSensor).Intensity;

            // Ant is currently turing around
            if (movement.ValueRO.State == AntState.TurningAround)
            {
                var currAngle = math.atan2(movement.ValueRO.DesiredDirection.z, movement.ValueRO.DesiredDirection.x);
                var desAngle = math.atan2(movement.ValueRO.TurnAroundDirection.z, movement.ValueRO.TurnAroundDirection.x);

                var angle = math.lerp(currAngle, desAngle, 0.2f);

                movement.ValueRW.DesiredDirection = math.normalize(new float3(math.cos(angle), 0.0f, math.sin(angle)));

                if (math.abs(currAngle - desAngle) < 0.03f)
                {
                    movement.ValueRW.State = AntState.GoingHome;
                }
            }
            else
            {
                // Ant has target => Move to target
                if (movement.ValueRO.Target != Entity.Null)
                {
                    float3 targetPosition = state.EntityManager.GetComponentData<LocalToWorld>(movement.ValueRO.Target).Position;
                    float3 desiredDirection = math.normalize(targetPosition - transform.ValueRO.Position);
                    desiredDirection.y = 0;
                    movement.ValueRW.DesiredDirection = desiredDirection;
                }
                // Ant has sensor data => Move according to data
                else if (leftSensorIntensity + centerSensorIntensity + rightSensorIntensity > 0.0f)
                {
                    float3 sensorPosition = float3.zero;

                    // Left sensor
                    if (leftSensorIntensity > centerSensorIntensity && leftSensorIntensity > rightSensorIntensity)
                    {
                        sensorPosition = state.EntityManager.GetComponentData<LocalToWorld>(movement.ValueRO.LeftSensor).Position;
                    }

                    // Center
                    if (centerSensorIntensity > leftSensorIntensity && centerSensorIntensity > rightSensorIntensity)
                    {
                        sensorPosition = state.EntityManager.GetComponentData<LocalToWorld>(movement.ValueRO.CenterSensor).Position;
                    }

                    // Right
                    if (rightSensorIntensity > leftSensorIntensity && rightSensorIntensity > centerSensorIntensity)
                    {
                        sensorPosition = state.EntityManager.GetComponentData<LocalToWorld>(movement.ValueRO.RightSensor).Position;
                    }

                    if (sensorPosition.x != 0 && sensorPosition.y != 0 && sensorPosition.z != 0)
                    {
                        float3 desiredDirection = math.normalize(sensorPosition - transform.ValueRO.Position);
                        desiredDirection.y = 0;
                        movement.ValueRW.DesiredDirection = desiredDirection;
                    }
                }
                // No data => Random movement
                else
                {
                    // Generate new desired direction randomly
                    float3 desiredDirection = math.normalize(movement.ValueRO.DesiredDirection + random.NextFloat3Direction() * movement.ValueRO.WanderStrength);
                    desiredDirection.y = 0;
                    movement.ValueRW.DesiredDirection = desiredDirection;
                }
            }

            // Calculate acceleration
            float3 desiredVelocity = movement.ValueRO.DesiredDirection * movement.ValueRO.MaxSpeed;
            float3 acceleration = (desiredVelocity - movement.ValueRO.Velocity) * movement.ValueRO.SteerStrength;

            if (math.length(acceleration) > movement.ValueRO.SteerStrength)
            {
                acceleration *= movement.ValueRO.SteerStrength / math.length(acceleration);
            }

            // Calculate velocity
            float3 velocity = movement.ValueRO.Velocity + acceleration * deltaTime;

            if (math.length(velocity) > movement.ValueRO.MaxSpeed)
            {
                velocity *= movement.ValueRO.MaxSpeed / math.length(velocity);
            }

            movement.ValueRW.Velocity = velocity;

            // Move ant
            transform.ValueRW.Position += velocity * deltaTime;
            transform.ValueRW.Rotation = quaternion.RotateY(-math.atan2(velocity.z, velocity.x) + math.PI / 2f);
        }
    }
}