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
            AddComponent(entity, new Pheromone());
        }
    }
}

public struct Pheromone : IComponentData
{
    public float Quality;
    public float LifeTime;
}