using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public readonly partial struct TargetAspect : IAspect
{
    public readonly Entity Entity;

    public readonly RefRW<LocalTransform> Transform;
    public readonly RefRW<Ant> Ant;

    private readonly EnabledRefRO<TargetingFood> TargetFood;
    private readonly EnabledRefRO<TargetingColony> TargetColony;

    public bool HasTargetFood => TargetFood.ValueRO;
    public bool HasTargetColony => TargetColony.ValueRO;
}
