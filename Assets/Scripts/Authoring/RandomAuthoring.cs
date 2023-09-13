using Unity.Entities;
using UnityEngine;

public class RandomAuthoring : MonoBehaviour
{
    class Baker : Baker<RandomAuthoring>
    {
        public override void Bake(RandomAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new RandomComponent
            {
                //Random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks)
                Random = Unity.Mathematics.Random.CreateFromIndex((uint)System.DateTime.Now.Ticks)
            });
        }
    }
}

public struct RandomComponent : IComponentData
{
    public Unity.Mathematics.Random Random;
}