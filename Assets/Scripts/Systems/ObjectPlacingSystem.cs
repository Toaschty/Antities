using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.Mesh;

public partial struct ObjectPlacingSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TerrainObjects>();
        state.RequireForUpdate<ObjectPlacing>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        CameraData cameraData = SystemAPI.GetSingleton<CameraData>();
        TerrainObjects objects = SystemAPI.GetSingleton<TerrainObjects>();
        ObjectPlacing objPlacing = SystemAPI.GetSingleton<ObjectPlacing>();

        // Handle keyboard input
        if (Input.GetKeyDown(KeyCode.R))
            objPlacing.Scale += 0.1f;
        if (Input.GetKeyDown(KeyCode.F))
            objPlacing.Scale -= 0.1f;
        objPlacing.Scale = math.clamp(objPlacing.Scale, 0.5f, 2.0f);

        // Handle rotation input
        if (Input.GetKey(KeyCode.LeftShift))
        {
            objPlacing.Angle -= Input.mouseScrollDelta.y * 6f;
            if (objPlacing.Angle > 360)
                objPlacing.Angle -= 360;
            if (objPlacing.Angle < 0)
                objPlacing.Angle += 360;
        }

        // Handle object halo
        if (cameraData.Intersects && !Input.GetMouseButton(1))
        {
            state.EntityManager.SetComponentData(objects.CurrentHalo, new LocalTransform
            {
                Position = cameraData.Intersection,
                Rotation = quaternion.RotateY(math.radians(objPlacing.Angle)),
                Scale = objPlacing.Scale,
            });
            
            if (!state.EntityManager.IsEnabled(objects.CurrentHalo))
                state.EntityManager.SetEnabled(objects.CurrentHalo, true);
        }
        else
        {
            if (state.EntityManager.IsEnabled(objects.CurrentHalo))
                state.EntityManager.SetEnabled(objects.CurrentHalo, false);
        }

        SystemAPI.SetSingleton(objPlacing);

        // Handle object placing
        if (cameraData.Intersects && Input.GetMouseButtonDown(0))
        {
            Entity objectToSpawn = Entity.Null;

            switch (objPlacing.Object)
            {
                case Objects.COLONY:
                    objectToSpawn = objects.Colony;
                    break;
                case Objects.FOOD:
                    objectToSpawn = objects.Food;
                    break;
                case Objects.TREE:
                    objectToSpawn = objects.Tree;
                    break;
            }

            // Spawn entity
            Entity instance = state.EntityManager.Instantiate(objectToSpawn);
            LocalTransform lt = state.EntityManager.GetComponentData<LocalTransform>(instance);
            state.EntityManager.SetComponentData(instance, new LocalTransform
            {
                Position = cameraData.Intersection,
                Rotation = lt.TransformRotation(quaternion.RotateY(math.radians(objPlacing.Angle))),
                Scale = objPlacing.Scale,
            });
            state.EntityManager.AddComponent<TerrainObject>(instance);
        }
    }
}
