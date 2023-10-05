using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class StartSimulation : MonoBehaviour
{
    private EntityManager manager;
    private EntityQuery query;
    private EntityQuery colonyQuery;

    private void Awake()
    {
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        query = manager.CreateEntityQuery(new ComponentType[] { typeof(RunningSimulation) });
        colonyQuery = Colony.GetQuery();
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
            Entity simulation = manager.CreateEntity();
            manager.AddComponent<RunningSimulation>(simulation);
        }
    }
}
