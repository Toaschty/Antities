using Unity.Entities;
using UnityEngine;

public class EditingSettings : MonoBehaviour, IMenu
{
    public GameObject AdditionButton;
    public GameObject RemovalButton;

    private EntityManager manager;
    private EntityQuery query;
    private EntityQuery brushQuery;
    private Entity editing;

    private void Awake()
    {
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        query = manager.CreateEntityQuery(new ComponentType[] { typeof(TerrainEditing) });
        brushQuery = manager.CreateEntityQuery(new ComponentType[] { typeof(BrushData) });
        editing = manager.CreateEntity();
    }

    private void OnApplicationQuit()
    {
        query.Dispose();
        brushQuery.Dispose();
    }

    public void SelectAddition()
    {
        // Activate correct frame
        AdditionButton.transform.GetChild(0).gameObject.SetActive(true);
        RemovalButton.transform.GetChild(0).gameObject.SetActive(false);

        // Switch to addition mode
        query.GetSingletonRW<TerrainEditing>().ValueRW.Mode = EditingModes.ADD;
    }

    public void SelectRemoval()
    {
        // Activate correct frame
        AdditionButton.transform.GetChild(0).gameObject.SetActive(false);
        RemovalButton.transform.GetChild(0).gameObject.SetActive(true);

        // Switch to removal mode
        query.GetSingletonRW<TerrainEditing>().ValueRW.Mode = EditingModes.REMOVE;
    }

    public void OpenMenu()
    {
        manager.AddComponentData(editing, new TerrainEditing
        {
            Mode = EditingModes.ADD
        });

        // Default operation is addition
        SelectAddition();
    }

    public void CloseMenu()
    {
        manager.RemoveComponent<TerrainEditing>(editing);

        // Disable brush
        if (brushQuery.GetSingleton<BrushData>().Instance != Entity.Null)
            manager.SetEnabled(brushQuery.GetSingleton<BrushData>().Instance, false);
    }
}
