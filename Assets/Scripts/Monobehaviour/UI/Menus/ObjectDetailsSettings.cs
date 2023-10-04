using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class ObjectDetailsSettings : MonoBehaviour
{
    public GameObject Details;
    public TMPro.TMP_Text Name;
    public InfoDisplay NameInfo;
    public TMPro.TMP_Text Detail;
    public InfoDisplay DetailInfo;

    public Vector3 Offset;

    private EntityManager manager;
    private EntityQuery query;
    private EntityQuery cameraQuery;
    private Entity entity;
    private Vector3 objectPosition;

    private void Awake()
    {
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        query = manager.CreateEntityQuery(new ComponentType[] { typeof(SelectedObject), typeof(LocalTransform), typeof(ObjectDetailInfo) });
        cameraQuery = CameraData.GetQuery();
    }

    private void OnApplicationQuit()
    {
        query.Dispose();
        cameraQuery.Dispose();
    }

    void Update()
    {
        // Check if selected object exists
        if (!query.IsEmpty && !Details.activeSelf)
        {
            GetObjectData();
            OpenDetailMenu();
        }

        // Move window to correct position
        if (Details.activeSelf)
        {
            // Check if different object was selected => Change to new one
            if (objectPosition != (Vector3)query.GetSingleton<LocalTransform>().Position)
                GetObjectData();

            // Get screen point
            Vector3 screenPoint = Camera.main.WorldToScreenPoint(objectPosition + Offset);
            Details.transform.position = screenPoint;
        }
    }

    private void GetObjectData()
    {
        entity = query.ToEntityArray(Allocator.Temp)[0];
        objectPosition = query.GetSingleton<LocalTransform>().Position;

        ObjectDetailInfo info = query.GetSingleton<ObjectDetailInfo>();
        Name.text = info.Name.ToString();
        NameInfo.InfoText = info.NameInfo.ToString();
        Detail.text = info.Details.ToString();
        DetailInfo.InfoText = info.DetailsInfo.ToString();
    }

    public void OpenDetailMenu()
    {
        Details.SetActive(true);
    }

    public void CloseDetailMenu()
    {
        manager.RemoveComponent<SelectedObject>(entity);
        cameraQuery.GetSingletonRW<CameraData>().ValueRW.OnUI = false;
        Details.SetActive(false);
        entity = Entity.Null;
    }

    public void DeleteObject()
    {
        manager.DestroyEntity(entity);
        cameraQuery.GetSingletonRW<CameraData>().ValueRW.OnUI = false;
        Details.SetActive(false);
        entity = Entity.Null;
    }
}
