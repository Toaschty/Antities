using Unity.Entities;
using UnityEngine;

public class AntConfigAuthoring : MonoBehaviour
{
    [Header("Movement Settings")]
    public float MaxSpeed;
    public float SteerStrength;
    public float SensorStrength;
    public float MaxSlopeAngle;

    [Header("Random Settings")]
    public float RandomDirectionAngle;
    public float RandomSteerDuration;
    public float RandomSteerStrength;

    [Header("Detection Settings")]
    public float ViewAngle;
    public float ViewDistance;

    class Baker : Baker<AntConfigAuthoring>
    {
        public override void Bake(AntConfigAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new AntConfig
            {
                MaxSpeed = authoring.MaxSpeed,
                SteerStrength = authoring.SteerStrength,
                SensorStrength = authoring.SensorStrength,
                MaxSlopeAngle = authoring.MaxSlopeAngle,
                RandomDirectionAngle = authoring.RandomDirectionAngle,
                RandomSteerDuration = authoring.RandomSteerDuration,
                RandomSteerStrength = authoring.RandomSteerStrength,
                ViewAngle = authoring.ViewAngle,
                ViewDistance = authoring.ViewDistance,
            });
        }
    }
}

public struct AntConfig : IComponentData
{
    // Movement
    public float MaxSpeed;
    public float SteerStrength;
    public float SensorStrength;
    public float MaxSlopeAngle;

    // Random
    public float RandomDirectionAngle;
    public float RandomSteerDuration;
    public float RandomSteerStrength;

    // Detection
    public float ViewAngle;
    public float ViewDistance;

    public static EntityQuery GetQuery()
    {
        return World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(new ComponentType[] { typeof(AntConfig) });
    }
}