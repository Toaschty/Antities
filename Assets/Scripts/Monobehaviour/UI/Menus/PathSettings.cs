using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class PathSettings : MonoBehaviour, IMenu
{
    private EntityManager manager;
    private EntityQuery query;

    private void Awake()
    {
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        query = PheromoneConfig.GetQuery();
    }

    private void OnApplicationQuit()
    {
        query.Dispose();
    }

    public void SetDistance(string value)
    {
        if (!CheckInput(value)) return;
        query.GetSingletonRW<PheromoneConfig>().ValueRW.DistanceBetweenPheromones = float.Parse(value);
    }

    public void SetPathTime(string value)
    {
        if (!CheckInput(value)) return;
        query.GetSingletonRW<PheromoneConfig>().ValueRW.PheromoneMaxTime = double.Parse(value);
    }

    public void SetPathLength(string value)
    {
        if (!CheckInput(value)) return;
        query.GetSingletonRW<PheromoneConfig>().ValueRW.MaxPathLength = int.Parse(value);
    }

    public bool CheckInput(string value)
    {
        if (value == "")
            return false;
        return true;
    }

    public void OpenMenu()
    {
    }

    public void CloseMenu()
    {
    }
}
