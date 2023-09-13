using Unity.Entities;
using UnityEngine;

public class FoodAuthoring : MonoBehaviour
{
    public int FoodAmount = 10;
    public float PickUpRadius = 1f;

    public GameObject CarryModel;

    class Baker : Baker<FoodAuthoring>
    {
        public override void Bake(FoodAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Food
            {
                MaxAmount = authoring.FoodAmount,
                Amount = authoring.FoodAmount,
                PickUpRadius = authoring.PickUpRadius,
                CarryModel = GetEntity(authoring.CarryModel, TransformUsageFlags.None),
            });
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, PickUpRadius);
    }
}

public struct Food : IComponentData
{
    public int MaxAmount;
    public int Amount;

    public float PickUpRadius;

    public Entity CarryModel;
}