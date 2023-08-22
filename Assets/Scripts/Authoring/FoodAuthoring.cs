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
            AddComponent<Food>(entity);
        }
    }
}

public struct Food : IComponentData, IEnableableComponent
{
}