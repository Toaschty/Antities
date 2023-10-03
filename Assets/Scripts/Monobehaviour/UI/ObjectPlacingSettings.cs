using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class ObjectPlacingSettings : MonoBehaviour, IMenu
{
    public GameObject[] Objects;

    private Entity entity;
    private EntityQuery terrainObjectsQuery;
    private EntityQuery objectsPlacingQuery;
    private EntityQuery placedObjectsQuery;

    private void Awake()
    {
        entity = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntity();
        terrainObjectsQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(new ComponentType[] { typeof(TerrainObjects) });
        objectsPlacingQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(new ComponentType[] { typeof(ObjectPlacing) });
        placedObjectsQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(new ComponentType[] { typeof(TerrainObject) });
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
        RefRW<TerrainObjects> terrainObjects;
        RefRW<ObjectPlacing> objectPlacing;
        terrainObjectsQuery.TryGetSingletonRW(out terrainObjects);
        objectsPlacingQuery.TryGetSingletonRW(out objectPlacing);
        terrainObjects.ValueRW.CurrentHalo = World.DefaultGameObjectInjectionWorld.EntityManager.Instantiate(terrainObjects.ValueRO.Halo_Colony);
        objectPlacing.ValueRW.Object = global::Objects.COLONY;
    }

    public void SelectFood()
    {
        DeselectAll();

        Objects[1].transform.GetChild(0).gameObject.SetActive(true);

        // Spawn halo
        RefRW<TerrainObjects> terrainObjects;
        RefRW<ObjectPlacing> objectPlacing;
        terrainObjectsQuery.TryGetSingletonRW(out terrainObjects);
        objectsPlacingQuery.TryGetSingletonRW(out objectPlacing);
        terrainObjects.ValueRW.CurrentHalo = World.DefaultGameObjectInjectionWorld.EntityManager.Instantiate(terrainObjects.ValueRO.Halo_Food);
        objectPlacing.ValueRW.Object = global::Objects.FOOD;
    }

    public void SelectTree()
    {
        DeselectAll();
        Objects[2].transform.GetChild(0).gameObject.SetActive(true);

        // Spawn halo
        RefRW<TerrainObjects> terrainObjects;
        RefRW<ObjectPlacing> objectPlacing;
        terrainObjectsQuery.TryGetSingletonRW(out terrainObjects);
        objectsPlacingQuery.TryGetSingletonRW(out objectPlacing);
        terrainObjects.ValueRW.CurrentHalo = World.DefaultGameObjectInjectionWorld.EntityManager.Instantiate(terrainObjects.ValueRO.Halo_Tree);
        objectPlacing.ValueRW.Object = global::Objects.TREE;
    }

    private void DeselectAll()
    {
        foreach (GameObject obj in Objects)
            obj.transform.GetChild(0).gameObject.SetActive(false);

        // Despawn halo entity
        RefRW<TerrainObjects> terrainObjects;
        terrainObjectsQuery.TryGetSingletonRW(out terrainObjects);
        World.DefaultGameObjectInjectionWorld.EntityManager.DestroyEntity(terrainObjects.ValueRW.CurrentHalo);
    }

    public void ResetObjects()
    {
        foreach (Entity placed in placedObjectsQuery.ToEntityArray(Allocator.Temp))
            World.DefaultGameObjectInjectionWorld.EntityManager.DestroyEntity(placed);
    }

    public void OpenMenu()
    {
        World.DefaultGameObjectInjectionWorld.EntityManager.AddComponentData(entity, new ObjectPlacing
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

        World.DefaultGameObjectInjectionWorld.EntityManager.RemoveComponent<ObjectPlacing>(entity);
    }
}
