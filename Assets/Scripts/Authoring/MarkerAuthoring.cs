using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class MarkerAuthoring : MonoBehaviour
{
    class Baker : Baker<MarkerAuthoring>
    {
        public override void Bake(MarkerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(entity, new Marker
            {
                Intensity = float.MaxValue,
            });
        }
    }
}

public struct Marker : IComponentData
{
    // Current pheromone intensity
    public float Intensity;
}

public struct SubMarker : IBufferElementData
{
    public float Intensity;

}

public struct FoodMarker : IComponentData
{
    // Component needs value for "IsValid" to work
    public bool Value;
}

public struct ColonyMarker : IComponentData
{
    // Component needs value for "IsValid" to work
    public bool Value;
}

