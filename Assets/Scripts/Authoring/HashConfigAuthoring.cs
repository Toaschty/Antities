using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class HashConfigAuthoring : MonoBehaviour
{
    public float GridSize = 1f;
    public float MaxColonyPheromonePerGrid = 500f;
    public float MaxFoodPheromonePerGrid = 500f;

    class Baker : Baker<HashConfigAuthoring>
    {
        public override void Bake(HashConfigAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new HashConfig
            {
                GridSize = authoring.GridSize,
                MaxColonyPheromonePerGrid = authoring.MaxColonyPheromonePerGrid,
                MaxFoodPheromonePerGrid = authoring.MaxFoodPheromonePerGrid,
            });
        }
    }
}

public struct HashConfig : IComponentData
{
    public float GridSize;
    public float MaxColonyPheromonePerGrid;
    public float MaxFoodPheromonePerGrid;
}