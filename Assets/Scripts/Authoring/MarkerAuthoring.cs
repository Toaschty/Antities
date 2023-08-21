using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class MarkerAuthoring : MonoBehaviour
{
    public MarkerType Type;
    public float MaxIntensity;

    class Baker : Baker<MarkerAuthoring>
    {
        public override void Bake(MarkerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Marker
            {
                Type = authoring.Type,
                MaxIntensity = authoring.MaxIntensity,
                Intensity = authoring.MaxIntensity,
            });
        }
    }
}

public enum MarkerType
{
    Home,
    Food
}


public struct Marker : IComponentData
{
    public MarkerType Type;
    public float Intensity;
    public float MaxIntensity;
}

