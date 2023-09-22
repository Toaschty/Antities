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
    // Screen to World Ray
    public float3 Origin;
    public float3 Direction;
}