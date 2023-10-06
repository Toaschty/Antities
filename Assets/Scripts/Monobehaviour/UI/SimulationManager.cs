using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    public GameObject ResetSimulationButton;

    private Entity simulation;
    private EntityManager manager;
    private EntityQuery query;
    private EntityQuery colonyQuery;

    private void Awake()
    {
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        query = manager.CreateEntityQuery(new ComponentType[] { typeof(RunningSimulation) });
        colonyQuery = Colony.GetQuery();

        simulation = manager.CreateEntity();
    }

    private void OnApplicationQuit()
    {
        query.Dispose();
        colonyQuery.Dispose();
    }

    public void StartSim()
    {
        // Only start simulation if colonies are placed
        if (colonyQuery.IsEmpty)
        {
            GameObject.FindGameObjectWithTag("InfoDisplay").GetComponent<TMPro.TMP_Text>().text = "! Missing Colony !";
            return;
        }

        if (!query.HasSingleton<RunningSimulation>())
        {
            manager.AddComponent<RunningSimulation>(simulation);

            // Enable "Reset Simulation" Button
            ResetSimulationButton.SetActive(true);
        }
    }

    public void ResetSim()
    {
        // Disable "Reset Simulation" Button
        ResetSimulationButton.SetActive(false);

        // Remove Running Simulation entity
        manager.RemoveComponent<RunningSimulation>(simulation);
        manager.AddComponent<ResetSimulation>(simulation);
    }
}
