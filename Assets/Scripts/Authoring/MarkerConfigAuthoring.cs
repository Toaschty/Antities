using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class MarkerConfigAuthoring : MonoBehaviour
{
    public float DistanceBetweenMarkers;
    public float PheromoneMaxTime;

    public GameObject ToHomeMarker;
    public GameObject ToFoodMarker;

    class Baker : Baker<MarkerConfigAuthoring>
    {
        public override void Bake(MarkerConfigAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new MarkerConfig
            {
                DistanceBetweenMarkers = authoring.DistanceBetweenMarkers,
                PheromoneMaxTime = authoring.PheromoneMaxTime,
                ToHomeMarker = GetEntity(authoring.ToHomeMarker, TransformUsageFlags.Renderable),
                ToFoodMarker = GetEntity(authoring.ToFoodMarker, TransformUsageFlags.Renderable),
            });
        }
    }
}

public struct MarkerConfig : IComponentData
{
    public float DistanceBetweenMarkers;
    public float PheromoneMaxTime;

    public Entity ToHomeMarker;
    public Entity ToFoodMarker;
}