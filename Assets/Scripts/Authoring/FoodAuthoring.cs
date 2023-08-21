using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class FoodAuthoring : MonoBehaviour
{
    class Baker : Baker<FoodAuthoring>
    {
        public override void Bake(FoodAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Food
            {
                Targeted = false,
            });
        }
    }
}

public struct Food : IComponentData
{
    public bool Targeted;
}