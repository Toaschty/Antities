using System;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class TerrainSettings : MonoBehaviour, IMenu
{
    private EntityManager manager;
    private EntityQuery query;
    private Entity generateTerrain;

    private void Awake()
    {
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        query = Terrain.GetQuery();
        generateTerrain = manager.CreateEntity();
    }

    private void OnApplicationQuit()
    {
        query.Dispose();
    }

    public void SetTerrainWidth(string value)
    {
        query.GetSingletonRW<Terrain>().ValueRW.Width = int.Parse(value);
    }

    public void SetTerrainHeight(string value)
    {
        query.GetSingletonRW<Terrain>().ValueRW.Height = int.Parse(value);
    }

    public void SetTerrainDepth(string value)
    {
        query.GetSingletonRW<Terrain>().ValueRW.Depth = int.Parse(value);
    }

    public void SetTerrainSeed(string value)
    {
        UnityEngine.Random.InitState(int.Parse(value));

        float3 random = new float3(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);

        query.GetSingletonRW<Terrain>().ValueRW.Seed = random * 5000f;
    }

    public void SetChunkWidth(string value)
    {
        query.GetSingletonRW<Terrain>().ValueRW.ChunkWidth = int.Parse(value);
    }

    public void SetChunkHeight(string value)
    {
        query.GetSingletonRW<Terrain>().ValueRW.ChunkHeight = int.Parse(value);
    }

    public void SetChunkDepth(string value)
    {
        query.GetSingletonRW<Terrain>().ValueRW.ChunkDepth = int.Parse(value);
    }

    public void SetNoiseScale(string value)
    {
        query.GetSingletonRW<Terrain>().ValueRW.NoiseScale = float.Parse(value.ToString());
    }

    public void SetThreshold(Single value)
    {
        query.GetSingletonRW<Terrain>().ValueRW.Threshold = float.Parse(value.ToString());
    }

    public void SetNoiseDropOffHeight(string value)
    {
        query.GetSingletonRW<Terrain>().ValueRW.NoiseDropOffHeight = int.Parse(value);
    }

    public void SetNoiseDropOffDepth(string value)
    {
        query.GetSingletonRW<Terrain>().ValueRW.NoiseDropOffDepth = int.Parse(value);
    }

    public void GenerateTerrain()
    {
        manager.AddComponent<GenerateTerrain>(generateTerrain);
        GameObject.FindGameObjectWithTag("MenuManager").GetComponent<MenuManager>().ToggleMenu(0);
        GameObject.FindGameObjectWithTag("InfoDisplay").GetComponent<TMPro.TMP_Text>().text = "";
        GameObject.FindGameObjectWithTag("Controller").GetComponent<Controller>().MoveToTerrainCenter(query.GetSingleton<Terrain>());
    }

    public void OpenMenu()
    {
    }

    public void CloseMenu()
    {
    }
}
