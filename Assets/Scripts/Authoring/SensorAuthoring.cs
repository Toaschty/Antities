using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class SensorAuthoring : MonoBehaviour
{
    public float Radius = 1f;

    class Baker : Baker<SensorAuthoring>
    {
        public override void Bake(SensorAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new Sensor
            {
                Radius = authoring.Radius,
                SearchMarker = MarkerType.Food,
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
    // Search settings
    public float Radius;
    public MarkerType SearchMarker;

    // Queried data
    public float Intensity;
}
