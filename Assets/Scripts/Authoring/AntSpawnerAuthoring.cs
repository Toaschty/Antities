using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class AntSpawnerAuthoring : MonoBehaviour
{
    public GameObject Ant;

    class Baker : Baker<AntSpawnerAuthoring>
    {
        public override void Bake(AntSpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new AntSpawner
            {
                ant = GetEntity(authoring.Ant, TransformUsageFlags.None),
            });
        }
    }
}

public struct AntSpawner : IComponentData
{
    public Entity ant;
}

public struct StartSimulation : IComponentData
{ 
}

public struct RunningSimulation : IComponentData
{
}

public struct ResetSimulation : IComponentData
{
}