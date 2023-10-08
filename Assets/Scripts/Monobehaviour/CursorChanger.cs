using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class CursorChanger : MonoBehaviour
{
    public Texture2D CursorTexture;

    private EntityManager manager;
    private EntityQuery query;
    private EntityQuery objectQuery;

    private void Awake()
    {
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        query = CameraData.GetQuery();
        objectQuery = PlacedTerrainObject.GetQuery();
    }

    private void OnApplicationQuit()
    {
        query.Dispose();
        objectQuery.Dispose();
    }

    private void Update()
    {
        CameraData cameraData = query.GetSingleton<CameraData>();

        if (cameraData.Intersect && !cameraData.OnUI)
        {
            // Check if current object is a terrain object
            if (manager.HasComponent<PlacedTerrainObject>(cameraData.Entity))
                Cursor.SetCursor(CursorTexture, Vector2.zero, CursorMode.Auto);
            else
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
        else
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}
