using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(SensorSystem))]
public partial struct AntMovementSystem : ISystem
{
    public ComponentLookup<Sensor> SensorLookup;
    public ComponentLookup<LocalToWorld> LocalToWorldLookup;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Ant>();

        SensorLookup = state.GetComponentLookup<Sensor>();
        LocalToWorldLookup = state.GetComponentLookup<LocalToWorld>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        var randomComponent = SystemAPI.GetSingletonRW<RandomComponent>();
        var entityExistance = state.EntityManager.UniversalQuery.GetEntityQueryMask();
        SensorLookup.Update(ref state);
        LocalToWorldLookup.Update(ref state);

        var movementJob = new MovementJob
        {
            DeltaTime = deltaTime,
            Random = randomComponent,
            SensorLookup = SensorLookup,
            LocalToWorldLookup = LocalToWorldLookup,
            Time = Time.time,
            EntityExistance = entityExistance,
        };

        state.Dependency = movementJob.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
public partial struct MovementJob : IJobEntity
{
    [ReadOnly] public float DeltaTime;
    [ReadOnly] public float Time;
    [ReadOnly] public ComponentLookup<Sensor> SensorLookup;
    [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
    [ReadOnly] public EntityQueryMask EntityExistance;

    [NativeDisableUnsafePtrRestriction]
    public RefRW<RandomComponent> Random;

    [BurstCompile]
    public void Execute(ref LocalTransform transform, ref Ant ant)
    {
        HandleRandomSteering(ref ant, Time);
        
        // Get sensor data
        float leftSensorIntensity = SensorLookup.GetRefRO(ant.LeftSensor).ValueRO.Intensity;
        float centerSensorIntensity = SensorLookup.GetRefRO(ant.CenterSensor).ValueRO.Intensity;
        float rightSensorIntensity = SensorLookup.GetRefRO(ant.RightSensor).ValueRO.Intensity;

        // Check if ant needs to be reset to colony
        if (transform.Position.y < -5.0f)
            transform.Position = ant.ColonyPosition;

        // Ant is currently turing around
        if (ant.State == AntState.TurningAround)
        {
            var currAngle = math.atan2(ant.Velocity.z, ant.Velocity.x);
            var desAngle = math.atan2(ant.TurnAroundDirection.z, ant.TurnAroundDirection.x);

            var angle = math.lerp(currAngle, desAngle, ant.TurnAroundStrength);

            ant.DesiredDirection = math.normalize(new float3(math.cos(angle), 0.0f, math.sin(angle)));

            if (math.abs(currAngle - desAngle) < 0.2f)
            {
                ant.State = AntState.GoingHome;
            }
        }
        else
        {
            // Ant has target => Move to target
            if (ant.Target != Entity.Null)
            {
                // Check if target entity still exists inside world
                if (EntityExistance.MatchesIgnoreFilter(ant.Target))
                {
                    float3 targetPosition = LocalToWorldLookup.GetRefRO(ant.Target).ValueRO.Position;
                    float3 dir = math.normalize(targetPosition - transform.Position);
                    dir.y = 0;
                    ant.DesiredDirection = dir;
                }
                else
                {
                    ant.Target = Entity.Null;

                    // Force new random direction
                    ant.RandomSteerForce = GetRandomDirection(ant.DesiredDirection, ant.RandomDirectionAngle) * ant.RandomSteerStength;
                    ant.RandomSteerForce.y = 0f;
                }
            }
            // Ant has sensor data => Move according to data
            else if (leftSensorIntensity + centerSensorIntensity + rightSensorIntensity > 0.0f)
            {
                float3 sensorPosition = float3.zero;

                // Left sensor
                if (leftSensorIntensity > centerSensorIntensity && leftSensorIntensity >= rightSensorIntensity)
                    sensorPosition = LocalToWorldLookup.GetRefRO(ant.LeftSensor).ValueRO.Position;

                // Right
                if (rightSensorIntensity >= leftSensorIntensity && rightSensorIntensity > centerSensorIntensity)
                    sensorPosition = LocalToWorldLookup.GetRefRO(ant.RightSensor).ValueRO.Position;

                // Center
                if (centerSensorIntensity >= leftSensorIntensity && centerSensorIntensity >= rightSensorIntensity)
                    sensorPosition = LocalToWorldLookup.GetRefRO(ant.CenterSensor).ValueRO.Position;

                if (sensorPosition.x != 0 || sensorPosition.y != 0 || sensorPosition.z != 0)
                {
                    float3 dir = math.normalize(sensorPosition - transform.Position) * ant.SensorStength;
                    dir.y = 0;
                    ant.DesiredDirection = dir;
                }
            }
            // No data => Random movement
            else
            {
                ant.DesiredDirection = ant.RandomSteerForce;

                if (math.length(ant.DesiredDirection) == 0.0f)
                {
                    // Force new random direction
                    ant.RandomSteerForce = GetRandomDirection(ant.DesiredDirection, ant.RandomDirectionAngle) * ant.RandomSteerStength;
                    ant.RandomSteerForce.y = 0f;
                }
            }
        }

        // Calculate acceleration
        float3 desiredDirection = math.normalize(ant.RandomSteerForce + ant.DesiredDirection) * ant.MaxSpeed;

        // Safety check for NaN
        if (math.any(math.isnan(desiredDirection)))
            return;

        if (ant.IsGrounded)
        {
            float3 projectedDesiredDirection = math.normalize(desiredDirection - math.dot(desiredDirection, ant.GroundNormal) * ant.GroundNormal) * math.length(desiredDirection);

            float3 acceleration = (projectedDesiredDirection - ant.Velocity) * ant.SteerStrength;

            if (math.length(acceleration) > ant.SteerStrength)
                acceleration *= ant.SteerStrength / math.length(acceleration);

            // Calculate velocity
            float3 velocity = ant.Velocity + acceleration * DeltaTime;

            if (math.length(velocity) > ant.MaxSpeed)
                velocity *= ant.MaxSpeed / math.length(velocity);

            ant.Velocity = velocity - math.dot(velocity, ant.GroundNormal) * ant.GroundNormal;
        }
        else
        {
            // Gravity
            ant.Velocity.y += -10f * DeltaTime;
        }

        // Move ant
        transform.Position += ant.Velocity * DeltaTime;

        // transform.Rotation = quaternion.RotateY(-math.atan2(ant.Velocity.z, ant.Velocity.x) + math.PI / 2f);
        if (math.length(ant.Velocity) > 0.001f)
            transform.Rotation = Quaternion.LookRotation(ant.Velocity, ant.GroundNormal);
    }

    [BurstCompile]
    private void HandleRandomSteering(ref Ant ant, float Time)
    {
        // No random steering if ant has target
        if (ant.Target != Entity.Null || ant.State == AntState.TurningAround)
        {
            ant.RandomSteerForce = float3.zero;
            return;
        }

        if (Time > ant.NextRandomSteerTime)
        {
            ant.NextRandomSteerTime = Time + Random.ValueRW.Random.NextFloat(ant.MaxRandomSteerDuration / 2f, ant.MaxRandomSteerDuration);
            ant.RandomSteerForce = GetRandomDirection(ant.DesiredDirection, ant.RandomDirectionAngle) * ant.RandomSteerStength;
            ant.RandomSteerForce.y = 0f;
        }
    }

    [BurstCompile]
    private float3 GetRandomDirection(float3 currentDirection, float maxAllowedAngle)
    {
        var angle = math.atan2(currentDirection.z, currentDirection.x);
        var minAngle = angle - math.radians(maxAllowedAngle) / 2f;
        var maxAngle = angle + math.radians(maxAllowedAngle) / 2f;

        var randomAngle = Random.ValueRW.Random.NextFloat(minAngle, maxAngle);
        return math.normalize(new float3(math.cos(randomAngle), 0f, math.sin(randomAngle)));
    }
}

