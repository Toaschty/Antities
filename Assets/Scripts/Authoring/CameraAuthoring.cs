using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class CameraAuthoring : MonoBehaviour
{
    class Baker : Baker<CameraAuthoring>
    {
        public override void Bake(CameraAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent<CameraData>(entity);
        }
    }
}

public struct CameraData : IComponentData
{
    // UI
    public bool OnUI;

    // Screen to World Ray
    public float3 Origin;
    public float3 Direction;

    // Terrain Hit position
    public bool TerrainIntersect;
    public float3 TerrainIntersection;

    // General Hit
    public bool Intersect;
    public float3 Intersection;
    public Entity Entity;

    public static EntityQuery GetQuery()
    {
        return World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(new ComponentType[] { typeof(CameraData) });
    }
}