using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class ObjectPlacingSettings : MonoBehaviour, IMenu
{
    public GameObject[] Objects;

    private EntityManager manager;
    private Entity entity;
    private EntityQuery terrainObjectsQuery;
    private EntityQuery objectsPlacingQuery;
    private EntityQuery placedObjectsQuery;

    private void Awake()
    {
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        entity = manager.CreateEntity();
        terrainObjectsQuery = TerrainObjects.GetQuery();
        objectsPlacingQuery = PlacementSettings.GetQuery();
        placedObjectsQuery = PlacedTerrainObject.GetQuery();
    }

    private void OnApplicationQuit()
    {
        terrainObjectsQuery.Dispose();
        objectsPlacingQuery.Dispose();
        placedObjectsQuery.Dispose();
    }

    public void SelectColony()
    {
        DeselectAll();

        Objects[0].transform.GetChild(0).gameObject.SetActive(true);

        // Spawn halo
        terrainObjectsQuery.GetSingletonRW<TerrainObjects>().ValueRW.CurrentHalo = manager.Instantiate(terrainObjectsQuery.GetSingletonRW<TerrainObjects>().ValueRO.Halo_Colony);
        objectsPlacingQuery.GetSingletonRW<PlacementSettings>().ValueRW.Object = global::Objects.COLONY;
    }

    public void SelectFood()
    {
        DeselectAll();

        Objects[1].transform.GetChild(0).gameObject.SetActive(true);

        // Spawn halo
        terrainObjectsQuery.GetSingletonRW<TerrainObjects>().ValueRW.CurrentHalo = manager.Instantiate(terrainObjectsQuery.GetSingletonRW<TerrainObjects>().ValueRO.Halo_Food);
        objectsPlacingQuery.GetSingletonRW<PlacementSettings>().ValueRW.Object = global::Objects.FOOD;
    }

    public void SelectTree()
    {
        DeselectAll();
        Objects[2].transform.GetChild(0).gameObject.SetActive(true);

        // Spawn halo
        terrainObjectsQuery.GetSingletonRW<TerrainObjects>().ValueRW.CurrentHalo = manager.Instantiate(terrainObjectsQuery.GetSingletonRW<TerrainObjects>().ValueRO.Halo_Tree);
        objectsPlacingQuery.GetSingletonRW<PlacementSettings>().ValueRW.Object = global::Objects.TREE;
    }

    private void DeselectAll()
    {
        foreach (GameObject obj in Objects)
            obj.transform.GetChild(0).gameObject.SetActive(false);

        // Despawn halo entity
        manager.DestroyEntity(terrainObjectsQuery.GetSingleton<TerrainObjects>().CurrentHalo);
    }

    public void ResetObjects()
    {
        foreach (Entity placed in placedObjectsQuery.ToEntityArray(Allocator.Temp))
            manager.DestroyEntity(placed);
    }

    public void OpenMenu()
    {
        manager.AddComponentData(entity, new PlacementSettings
        {
            Object = global::Objects.COLONY,
            Angle = 0,
            Scale = 1f,
        });

        // Set colony as default object 
        SelectColony();
    }

    public void CloseMenu()
    {
        DeselectAll();

        manager.RemoveComponent<PlacementSettings>(entity);
    }
}
