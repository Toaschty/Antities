using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class ObjectPlacingAuthoring : MonoBehaviour
{
    [Header("Objects")]
    public GameObject Colony;
    public GameObject Food;
    public GameObject Tree;

    [Header("Halo Objects")]
    public GameObject Halo_Colony;
    public GameObject Halo_Food;
    public GameObject Halo_Tree;

    class Baker : Baker<ObjectPlacingAuthoring>
    {
        public override void Bake(ObjectPlacingAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new TerrainObjects
            {
                Colony = GetEntity(authoring.Colony, TransformUsageFlags.Renderable),
                Food = GetEntity(authoring.Food, TransformUsageFlags.Renderable),
                Tree = GetEntity(authoring.Tree, TransformUsageFlags.Renderable),
                Halo_Colony = GetEntity(authoring.Halo_Colony, TransformUsageFlags.Renderable),
                Halo_Food = GetEntity(authoring.Halo_Food, TransformUsageFlags.Renderable),
                Halo_Tree = GetEntity(authoring.Halo_Tree, TransformUsageFlags.Renderable),
            });
        }
    }
}

public struct TerrainObjects : IComponentData
{
    public Entity Colony;
    public Entity Food;
    public Entity Tree;

    public Entity Halo_Colony;
    public Entity Halo_Food;
    public Entity Halo_Tree;

    public Entity CurrentHalo;
}

public struct TerrainObject : IComponentData
{
}

public struct ObjectPlacing : IComponentData
{
    public Objects Object;

    // Spawn settings
    public float Angle;
    public float Scale;
}

public enum Objects
{
    COLONY,
    FOOD,
    TREE
}
