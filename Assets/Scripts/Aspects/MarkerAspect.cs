using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public readonly partial struct MarkerAspect : IAspect
{
    public readonly Entity Entity;

    public readonly RefRW<LocalTransform> Transform;
    public readonly RefRW<Marker> Marker;

    [Optional]
    private readonly RefRO<FoodMarker> FoodMarker;
    
    [Optional]
    private readonly RefRO<ColonyMarker> ColonyMarker;

    public bool HasFoodMarker => FoodMarker.IsValid;

    public bool HasColonyMarker => ColonyMarker.IsValid;
}
