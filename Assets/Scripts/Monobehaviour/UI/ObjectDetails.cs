using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class ObjectDetails : MonoBehaviour
{
    public GameObject Details;
    public TMPro.TMP_Text Name;
    public InfoDisplay NameInfo;
    public TMPro.TMP_Text Detail;
    public InfoDisplay DetailInfo;

    public Vector3 Offset;

    private EntityQuery query;
    private Entity entity;
    private Vector3 objectPosition;

    private void Awake()
    {
        query = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(new ComponentType[] { typeof(SelectedObject), typeof(LocalTransform), typeof(ObjectDetailInfo) });
    }

    private void OnApplicationQuit()
    {
        query.Dispose();
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
        World.DefaultGameObjectInjectionWorld.EntityManager.RemoveComponent<SelectedObject>(entity);
        Details.SetActive(false);
        entity = Entity.Null;
    }

    public void DeleteObject()
    {
        World.DefaultGameObjectInjectionWorld.EntityManager.DestroyEntity(entity);
        Details.SetActive(false);
        entity = Entity.Null;
    }
}
