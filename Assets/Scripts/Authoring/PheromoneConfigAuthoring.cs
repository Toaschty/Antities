using Unity.Entities;
using UnityEngine;

public class PheromoneConfigAuthoring : MonoBehaviour
{
    [Header("Path Settings")]
    public float DistanceBetweenPheromones;
    public float PheromoneMaxTime;
    public int MaxPathLength;

    [Header("Prefabs")]
    public GameObject PendingPheromone;
    public GameObject PathPheromone;

    class Baker : Baker<PheromoneConfigAuthoring>
    {
        public override void Bake(PheromoneConfigAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new PheromoneConfig
            {
                DistanceBetweenPheromones = authoring.DistanceBetweenPheromones,
                PheromoneMaxTime = authoring.PheromoneMaxTime,
                MaxPathLength = authoring.MaxPathLength,
                PendingPheromone = GetEntity(authoring.PendingPheromone, TransformUsageFlags.None),
                PathPheromone = GetEntity(authoring.PathPheromone, TransformUsageFlags.None),
                Scale = authoring.PendingPheromone.transform.localScale[0]
            });
        }
    }
}

public struct PheromoneConfig : IComponentData
{
    public float DistanceBetweenPheromones;
    public double PheromoneMaxTime;
    public int MaxPathLength;

    public Entity PendingPheromone;
    public Entity PathPheromone;

    public float Scale;

    public static EntityQuery GetQuery()
    {
        return World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(new ComponentType[] { typeof(PheromoneConfig) });
    }
}