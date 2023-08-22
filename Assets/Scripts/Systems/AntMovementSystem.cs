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

        foreach (var (transform, ant) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<Ant>>())
        {
            // Get sensor data
            float leftSensorIntensity = state.EntityManager.GetComponentData<Sensor>(ant.ValueRO.LeftSensor).Intensity;
            float centerSensorIntensity = state.EntityManager.GetComponentData<Sensor>(ant.ValueRO.CenterSensor).Intensity;
            float rightSensorIntensity = state.EntityManager.GetComponentData<Sensor>(ant.ValueRO.RightSensor).Intensity;

            // Ant is currently turing around
            if (ant.ValueRO.State == AntState.TurningAround)
            {
                var currAngle = math.atan2(ant.ValueRO.DesiredDirection.z, ant.ValueRO.DesiredDirection.x);
                var desAngle = math.atan2(ant.ValueRO.TurnAroundDirection.z, ant.ValueRO.TurnAroundDirection.x);

                var angle = math.lerp(currAngle, desAngle, 0.2f);

                ant.ValueRW.DesiredDirection = math.normalize(new float3(math.cos(angle), 0.0f, math.sin(angle)));

                if (math.abs(currAngle - desAngle) < 0.02f)
                {
                    ant.ValueRW.State = AntState.GoingHome;
                }
            }
            else
            {
                // Ant has target => Move to target
                if (ant.ValueRO.Target != Entity.Null)
                {
                    float3 targetPosition = state.EntityManager.GetComponentData<LocalToWorld>(ant.ValueRO.Target).Position;
                    float3 desiredDirection = math.normalize(targetPosition - transform.ValueRO.Position);
                    desiredDirection.y = 0;
                    ant.ValueRW.DesiredDirection = desiredDirection;
                }
                // Ant has sensor data => Move according to data
                else if (leftSensorIntensity + centerSensorIntensity + rightSensorIntensity > 0.0f)
                {
                    float3 sensorPosition = float3.zero;

                    // Left sensor
                    if (leftSensorIntensity > centerSensorIntensity && leftSensorIntensity > rightSensorIntensity)
                    {
                        sensorPosition = state.EntityManager.GetComponentData<LocalToWorld>(ant.ValueRO.LeftSensor).Position;
                    }

                    // Center
                    if (centerSensorIntensity > leftSensorIntensity && centerSensorIntensity > rightSensorIntensity)
                    {
                        sensorPosition = state.EntityManager.GetComponentData<LocalToWorld>(ant.ValueRO.CenterSensor).Position;
                    }

                    // Right
                    if (rightSensorIntensity > leftSensorIntensity && rightSensorIntensity > centerSensorIntensity)
                    {
                        sensorPosition = state.EntityManager.GetComponentData<LocalToWorld>(ant.ValueRO.RightSensor).Position;
                    }

                    if (sensorPosition.x != 0 && sensorPosition.y != 0 && sensorPosition.z != 0)
                    {
                        float3 desiredDirection = math.normalize(sensorPosition - transform.ValueRO.Position);
                        desiredDirection = math.normalize(desiredDirection + random.NextFloat3Direction() * ant.ValueRO.WanderStrength);
                        desiredDirection.y = 0;
                        ant.ValueRW.DesiredDirection = desiredDirection;
                    }
                }
                // No data => Random movement
                else
                {
                    // Generate new desired direction randomly
                    float3 desiredDirection = math.normalize(ant.ValueRO.DesiredDirection + random.NextFloat3Direction() * ant.ValueRO.WanderStrength);
                    desiredDirection.y = 0;
                    ant.ValueRW.DesiredDirection = desiredDirection;
                }
            }

            // Calculate acceleration
            float3 desiredVelocity = ant.ValueRO.DesiredDirection * ant.ValueRO.MaxSpeed;
            float3 acceleration = (desiredVelocity - ant.ValueRO.Velocity) * ant.ValueRO.SteerStrength;

            if (math.length(acceleration) > ant.ValueRO.SteerStrength)
            {
                acceleration *= ant.ValueRO.SteerStrength / math.length(acceleration);
            }

            // Calculate velocity
            float3 velocity = ant.ValueRO.Velocity + acceleration * deltaTime;

            if (math.length(velocity) > ant.ValueRO.MaxSpeed)
            {
                velocity *= ant.ValueRO.MaxSpeed / math.length(velocity);
            }

            ant.ValueRW.Velocity = velocity;

            // Move ant
            transform.ValueRW.Position += velocity * deltaTime;
            transform.ValueRW.Rotation = quaternion.RotateY(-math.atan2(velocity.z, velocity.x) + math.PI / 2f);
        }
    }
}