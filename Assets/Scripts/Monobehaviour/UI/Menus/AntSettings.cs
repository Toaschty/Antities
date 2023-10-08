using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class AntSettings : MonoBehaviour, IMenu
{
    private EntityManager manager;
    private EntityQuery query;

    private void Awake()
    {
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        query = AntConfig.GetQuery();
    }

    private void OnApplicationQuit()
    {
        query.Dispose();
    }

    public void SetMaxSpeed(string value)
    {
        if (!CheckInput(value)) return;
        query.GetSingletonRW<AntConfig>().ValueRW.MaxSpeed = float.Parse(value);
    }

    public void SetSteerStrength(string value)
    {
        if (!CheckInput(value)) return;
        query.GetSingletonRW<AntConfig>().ValueRW.SteerStrength = float.Parse(value);
    }

    public void SetSensorStrength(string value)
    {
        if (!CheckInput(value)) return;
        query.GetSingletonRW<AntConfig>().ValueRW.SensorStrength = float.Parse(value);
    }

    public void SetSlopeAngle(string value)
    {
        if (!CheckInput(value)) return;
        query.GetSingletonRW<AntConfig>().ValueRW.MaxSlopeAngle = float.Parse(value);
    }

    public void SetRandomRange(Single value)
    {
        if (!CheckInput(value.ToString())) return;
        query.GetSingletonRW<AntConfig>().ValueRW.RandomDirectionAngle = float.Parse(value.ToString());
    }

    public void SetRandomSteerDuration(string value)
    {
        if (!CheckInput(value)) return;
        query.GetSingletonRW<AntConfig>().ValueRW.RandomSteerDuration = float.Parse(value);
    }

    public void SetRandomSteerStrength(string value)
    {
        if (!CheckInput(value)) return;
        query.GetSingletonRW<AntConfig>().ValueRW.RandomSteerStrength = float.Parse(value);
    }

    public void SetViewAngle(Single value)
    {
        if (!CheckInput(value.ToString())) return;
        query.GetSingletonRW<AntConfig>().ValueRW.ViewAngle = float.Parse(value.ToString());
    }

    public void SetViewDistance(string value)
    {
        if (!CheckInput(value)) return;
        query.GetSingletonRW<AntConfig>().ValueRW.ViewDistance = float.Parse(value);
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
