using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;

public class ObjectDetailsSettings : MonoBehaviour
{
    [Header("Detail View")]
    public GameObject Details;
    public TMPro.TMP_Text Name;
    public InfoDisplay NameInfo;
    public TMPro.TMP_Text Detail;
    public InfoDisplay DetailInfo;
    public TMPro.TMP_InputField InputField;

    [Header("Detail View Offset")]
    public Vector3 Offset;

    private EntityManager manager;
    private EntityQuery query;
    private EntityQuery cameraQuery;
    private EntityQuery colonyQuery;
    private Entity entity;
    private Vector3 objectPosition;

    private void Awake()
    {
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        query = manager.CreateEntityQuery(new ComponentType[] { typeof(SelectedObject), typeof(LocalTransform), typeof(ObjectDetailInfo) });
        cameraQuery = CameraData.GetQuery();
        colonyQuery = Colony.GetQuery();
    }

    private void OnApplicationQuit()
    {
        query.Dispose();
        cameraQuery.Dispose();
        colonyQuery.Dispose();
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

            if (screenPoint.z > 0)
                Details.transform.position = screenPoint;
        }
    }

    private void GetObjectData()
    {
        entity = query.ToEntityArray(Allocator.Temp)[0];
        objectPosition = query.GetSingleton<LocalTransform>().Position;

        // Display Info
        ObjectDetailInfo info = query.GetSingleton<ObjectDetailInfo>();
        Name.text = info.Name.ToString();
        NameInfo.InfoText = info.NameInfo.ToString();
        Detail.text = info.Details.ToString();
        DetailInfo.InfoText = info.DetailsInfo.ToString();

        // Disable options if not data is given
        if (Detail.text == "")
            Detail.transform.parent.gameObject.SetActive(false);
        else
            Detail.transform.parent.gameObject.SetActive(true);

        // Get value inside colony or food
        if (manager.HasComponent<Colony>(entity))
            InputField.text = manager.GetComponentData<Colony>(entity).AntAmount.ToString();
        if (manager.HasComponent<Food>(entity))
            InputField.text = manager.GetComponentData<Food>(entity).Amount.ToString();
    }

    public void SetData(string value)
    {
        if (value == "")
            return;

        // Set value inside colony or food
        if (manager.HasComponent<Colony>(entity))
        {
            Colony old = manager.GetComponentData<Colony>(entity);
            old.AntAmount = int.Parse(value);
            manager.SetComponentData(entity, old);
        }
        if (manager.HasComponent<Food>(entity))
        {
            Food old = manager.GetComponentData<Food>(entity);
            old.Amount = int.Parse(value);
            old.MaxAmount = int.Parse(value);
            manager.SetComponentData(entity, old);
        }
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
