using Unity.Entities;
using UnityEngine;

public class ColonyAuthoring : MonoBehaviour
{
    public float DepositRadius;

    class Baker : Baker<ColonyAuthoring>
    {
        public override void Bake(ColonyAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(entity, new Colony
            {
                DepositRadius = authoring.DepositRadius,
                AntAmount = 1,
            });
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, DepositRadius);
    }
}

public struct Colony : IComponentData
{
    public float DepositRadius;

    public int AntAmount;

    public static EntityQuery GetQuery()
    {
        return World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(new ComponentType[] { typeof(Colony) });
    }
}