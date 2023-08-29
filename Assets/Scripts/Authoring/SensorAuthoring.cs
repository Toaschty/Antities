using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class SensorAuthoring : MonoBehaviour
{
    public float Radius = 1f;
    public GameObject Ant;

    class Baker : Baker<SensorAuthoring>
    {
        public override void Bake(SensorAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new Sensor
            {
                Ant = GetEntity(authoring.Ant, TransformUsageFlags.None),
                Radius = authoring.Radius,
                RadiusSqrt = authoring.Radius * authoring.Radius,
                Intensity = 0f
            });
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, Radius);
    }
}

public struct Sensor : IComponentData
{
    public Entity Ant;

    // Search settings
    public float Radius;
    public float RadiusSqrt;

    // Queried data
    public float Intensity;
}
