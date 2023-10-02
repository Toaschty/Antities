using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.VisualScripting;
using UnityEngine;

public class EditingSettings : MonoBehaviour, IMenu
{
    public GameObject AdditionButton;
    public GameObject RemovalButton;

    private EntityQuery query;
    private Entity editing;

    private void Awake()
    {
        query = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(new ComponentType[] { typeof(TerrainEditing) });
        editing = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntity();
    }

    public void SelectAddition()
    {
        // Activate correct frame
        AdditionButton.transform.GetChild(0).gameObject.SetActive(true);
        RemovalButton.transform.GetChild(0).gameObject.SetActive(false);

        // Switch to addition mode
        RefRW<TerrainEditing> editingData;
        query.TryGetSingletonRW(out editingData);
        editingData.ValueRW.Mode = EditingModes.ADD;
    }

    public void SelectRemoval()
    {
        // Activate correct frame
        AdditionButton.transform.GetChild(0).gameObject.SetActive(false);
        RemovalButton.transform.GetChild(0).gameObject.SetActive(true);

        // Switch to removal mode
        RefRW<TerrainEditing> editingData;
        query.TryGetSingletonRW(out editingData);
        editingData.ValueRW.Mode = EditingModes.REMOVE;
    }

    public void OpenMenu()
    {
        World.DefaultGameObjectInjectionWorld.EntityManager.AddComponentData(editing, new TerrainEditing
        {
            Mode = EditingModes.ADD
        });

        // Default operation is addition
        SelectAddition();
    }

    public void CloseMenu()
    {
        World.DefaultGameObjectInjectionWorld.EntityManager.RemoveComponent<TerrainEditing>(editing);
    }
}
