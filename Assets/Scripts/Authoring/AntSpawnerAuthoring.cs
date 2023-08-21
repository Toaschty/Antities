using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class AntSpawnerAuthoring : MonoBehaviour
{
    public int Count;
    public GameObject Ant;

    class Baker : Baker<AntSpawnerAuthoring>
    {
        public override void Bake(AntSpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new AntSpawner
            {
                count = authoring.Count,
                ant = GetEntity(authoring.Ant, TransformUsageFlags.Dynamic),
            });
        }
    }
}

public struct AntSpawner : IComponentData
{
    public int count;
    public Entity ant;
}