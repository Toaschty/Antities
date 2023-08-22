using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class MarkerAuthoring : MonoBehaviour
{
    class Baker : Baker<MarkerAuthoring>
    {
        public override void Bake(MarkerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Marker
            {
                Intensity = float.MaxValue,
                Scale = authoring.gameObject.transform.localScale[0]
            });
            AddComponent<FoodMarker>(entity);
            SetComponentEnabled<FoodMarker>(entity, false);
            AddComponent<ColonyMarker>(entity);
            SetComponentEnabled<ColonyMarker>(entity, false);
        }
    }
}

public struct Marker : IComponentData
{
    // Current pheromone intensity
    public float Intensity;
    public float Scale;
}

public struct FoodMarker : IComponentData, IEnableableComponent
{
}

public struct ColonyMarker : IComponentData, IEnableableComponent
{
}

