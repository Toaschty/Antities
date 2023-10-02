using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class TerrainSettings : MonoBehaviour, IMenu
{
    private EntityQuery query;
    private Entity generateTerrain;

    private void Awake()
    {
        query = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(new ComponentType[] { typeof(Terrain) });
        generateTerrain = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntity();
    }

    private void OnApplicationQuit()
    {
        query.Dispose();
    }

    public void SetTerrainWidth(string value)
    {
        RefRW<Terrain> terrain;
        query.TryGetSingletonRW(out terrain);
        terrain.ValueRW.Width = int.Parse(value);
    }

    public void SetTerrainHeight(string value)
    {
        RefRW<Terrain> terrain;
        query.TryGetSingletonRW(out terrain);
        terrain.ValueRW.Height = int.Parse(value);
    }

    public void SetTerrainDepth(string value)
    {
        RefRW<Terrain> terrain;
        query.TryGetSingletonRW(out terrain);
        terrain.ValueRW.Depth = int.Parse(value);
    }

    public void SetChunkWidth(string value)
    {
        RefRW<Terrain> terrain;
        query.TryGetSingletonRW(out terrain);
        terrain.ValueRW.ChunkWidth = int.Parse(value);
    }

    public void SetChunkHeight(string value)
    {
        RefRW<Terrain> terrain;
        query.TryGetSingletonRW(out terrain);
        terrain.ValueRW.ChunkHeight = int.Parse(value);
    }

    public void SetChunkDepth(string value)
    {
        RefRW<Terrain> terrain;
        query.TryGetSingletonRW(out terrain);
        terrain.ValueRW.ChunkDepth = int.Parse(value);
    }

    public void SetNoiseScale(string value)
    {
        RefRW<Terrain> terrain;
        query.TryGetSingletonRW(out terrain);

        terrain.ValueRW.NoiseScale = float.Parse(value.ToString());
    }

    public void SetThreshold(Single value)
    {
        RefRW<Terrain> terrain;
        query.TryGetSingletonRW(out terrain);

        terrain.ValueRW.Threshold = float.Parse(value.ToString());
    }

    public void SetNoiseDropOffHeight(string value)
    {
        RefRW<Terrain> terrain;
        query.TryGetSingletonRW(out terrain);
        terrain.ValueRW.NoiseDropOffHeight = int.Parse(value);
    }

    public void SetNoiseDropOffDepth(string value)
    {
        RefRW<Terrain> terrain;
        query.TryGetSingletonRW(out terrain);
        terrain.ValueRW.NoiseDropOffDepth = int.Parse(value);
    }

    public void GenerateTerrain()
    {
        World.DefaultGameObjectInjectionWorld.EntityManager.AddComponent<GenerateTerrain>(generateTerrain);
        GameObject.FindGameObjectWithTag("MenuManager").GetComponent<MenuManager>().ToggleMenu(0);
        GameObject.FindGameObjectWithTag("InfoDisplay").GetComponent<TMPro.TMP_Text>().text = "";
    }

    public void OpenMenu()
    {
    }

    public void CloseMenu()
    {
    }
}
