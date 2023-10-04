using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.EventSystems;

public class RaycastBlocker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private EntityQuery query;

    private void Awake()
    {
        query = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(new ComponentType[] { typeof(CameraData) });
    }

    private void OnApplicationQuit()
    {
        query.Dispose();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        query.GetSingletonRW<CameraData>().ValueRW.OnUI = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        query.GetSingletonRW<CameraData>().ValueRW.OnUI = false;
    }
}
