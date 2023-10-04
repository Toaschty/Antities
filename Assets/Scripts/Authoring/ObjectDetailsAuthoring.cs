using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class ObjectDetailsAuthoring : MonoBehaviour
{
    [TextArea]
    public string Name;
    [TextArea]
    public string NameInfo;

    [TextArea]
    public string Details;
    [TextArea] 
    public string DetailsInfo;

    class Baker : Baker<ObjectDetailsAuthoring>
    {
        public override void Bake(ObjectDetailsAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new ObjectDetailInfo
            {
                Name = authoring.Name,
                NameInfo = authoring.NameInfo,
                Details = authoring.Details,
                DetailsInfo = authoring.DetailsInfo,
            });
        }
    }
}

public struct ObjectDetailInfo : IComponentData
{
    public FixedString64Bytes Name;
    public FixedString64Bytes NameInfo;
    public FixedString64Bytes Details;
    public FixedString64Bytes DetailsInfo;
}