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
            HandleRandomSteering(ant);

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
                        float3 desiredDirection = math.normalize(sensorPosition - transform.ValueRO.Position) * ant.ValueRO.SensorStength;
                        desiredDirection.y = 0;
                        ant.ValueRW.DesiredDirection = desiredDirection;
                    }
                }
                // No data => Random movement
                else
                {
                    ant.ValueRW.DesiredDirection = ant.ValueRO.RandomSteerForce;
                }
            }

            // Calculate acceleration
            float3 desiredVelocity = math.normalize(ant.ValueRO.RandomSteerForce + ant.ValueRO.DesiredDirection) * ant.ValueRO.MaxSpeed;
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

    private void HandleRandomSteering(RefRW<Ant> ant)
    {
        // No random steering if ant has target
        if (ant.ValueRO.Target != Entity.Null || ant.ValueRO.State == AntState.TurningAround)
        {
            ant.ValueRW.RandomSteerForce = float3.zero;
            return;
        }

        if (Time.time > ant.ValueRO.NextRandomSteerTime)
        {
            ant.ValueRW.NextRandomSteerTime = Time.time + random.NextFloat(ant.ValueRO.MaxRandomSteerDuration / 2f, ant.ValueRO.MaxRandomSteerDuration);
            ant.ValueRW.RandomSteerForce = GetRandomDirection(ant.ValueRO.DesiredDirection, ant.ValueRO.RandomDirectionAngle) * ant.ValueRO.RandomSteerStength;
            ant.ValueRW.RandomSteerForce.y = 0f;
        }
    }

    private float3 GetRandomDirection(float3 currentDirection, float maxAllowedAngle)
    {
        var angle = math.atan2(currentDirection.z, currentDirection.x);
        var minAngle = angle - math.radians(maxAllowedAngle) / 2f;
        var maxAngle = angle + math.radians(maxAllowedAngle) / 2f;

        var randomAngle = random.NextFloat(minAngle, maxAngle);
        return math.normalize(new float3(math.cos(randomAngle), 0f, math.sin(randomAngle)));
    }
}