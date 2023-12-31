using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public Camera Camera;

    [Header("Movement Settings")]
    public float NormalSpeed = 10f;
    public float BoostSpeed = 50;
    public float SmoothTime = 1.0f;

    [Header("Camera Settings")]
    public float LookSpeed = 1f;
    public float MaxVerticalAngle = 70f;
    public float MinZoom = 0f;
    public float MaxZoom = 30f;

    private Vector3 movementInput = Vector3.zero;
    private Vector3 velocity = Vector2.zero;
    private float horizontalAngle = 0f;
    private float verticalAngle = 0f;
    private float cameraZoom = 3f;
    private bool speedBoost = false;

    void Update()
    {
        HandleInput();
        HandlePivotPosition();
        HandleCameraRotation();
    }

    void HandleInput()
    {
        // Handle movement inputs
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.z = Input.GetAxisRaw("Vertical");
        movementInput.y = Input.GetAxisRaw("UpDown");

        // Handle shift input
        speedBoost = Input.GetKey(KeyCode.LeftShift);

        // Handle rotation inputs
        if (Input.GetMouseButton(1))
        {
            horizontalAngle += Input.GetAxis("Mouse X") * LookSpeed * Time.unscaledDeltaTime;
            verticalAngle -= Input.GetAxis("Mouse Y") * LookSpeed * Time.unscaledDeltaTime;

            verticalAngle = Mathf.Clamp(verticalAngle, -MaxVerticalAngle, MaxVerticalAngle);
        }

        if (Input.GetMouseButtonDown(1)) { Cursor.lockState = CursorLockMode.Locked; }
        if (Input.GetMouseButtonUp(1)) { Cursor.lockState= CursorLockMode.None; }

        // Handle zoom input
        if (Input.GetKey(KeyCode.LeftShift)) return;
        cameraZoom -= Input.mouseScrollDelta.y;
        cameraZoom = Mathf.Clamp(cameraZoom, MinZoom, MaxZoom);
    }

    void HandlePivotPosition()
    {
        // Calculate movement according to camera orientation
        Vector3 movement = Camera.transform.forward * movementInput.z + Camera.transform.right * movementInput.x;
        movement.y = 0;
        movement.y += movementInput.y;
        movement.Normalize();

        // Select correct speed
        float speed = speedBoost ? BoostSpeed : NormalSpeed;

        // Smoothly accelartion to desired movement
        Vector3.SmoothDamp(velocity, movement * speed, ref velocity, SmoothTime, float.PositiveInfinity, Time.unscaledDeltaTime);
        transform.position += velocity * Time.unscaledDeltaTime;
    }

    void HandleCameraRotation()
    {
        // Set Camera position and rotation
        Vector3 rotation = new Vector3(verticalAngle, horizontalAngle, 0);
        Quaternion rotationQuaternion = Quaternion.Euler(rotation);
        Camera.transform.position = rotationQuaternion * new Vector3(0, 0, -cameraZoom) + transform.position;
        Camera.transform.rotation = rotationQuaternion;
    }

    public void MoveToTerrainCenter(Terrain terrain)
    {
        StartCoroutine(DelayedMove(terrain));
    }

    private IEnumerator DelayedMove(Terrain terrain)
    {
        yield return new WaitForSeconds(0.4f);
        Move(terrain);
    }

    private void Move(Terrain terrain)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp).WithAll<PhysicsWorldSingleton>();
        EntityQuery singeltonQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(builder);
        CollisionWorld collisionWorld = singeltonQuery.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

        // Get top center of terrain
        float3 terrainCenter = new float3(terrain.Width / 2.0f, terrain.Height, terrain.Depth / 2.0f);

        // Shoot raycast from top to get new camera pivot point
        RaycastInput input = new RaycastInput
        {
            Start = terrainCenter,
            End = new float3(terrainCenter.x, 0.0f, terrainCenter.z),
            Filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 128u,
                GroupIndex = 0
            }
        };

        Unity.Physics.RaycastHit hit;
        if (collisionWorld.CastRay(input, out hit))
            transform.position = hit.Position;
        else
            transform.position = terrainCenter;

        builder.Dispose();
        singeltonQuery.Dispose();
    }
}
