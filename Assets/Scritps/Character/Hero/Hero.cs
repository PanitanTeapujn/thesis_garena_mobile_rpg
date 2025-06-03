using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Hero : Character
{
    // ========== Network Properties ==========
    [Networked] public Vector3 NetworkedPosition { get; set; }
    [Networked] public Vector3 NetworkedVelocity { get; set; }
    [Networked] public bool NetworkedFlipX { get; set; }
    [Networked] public float NetworkedYRotation { get; set; }
    [Networked] public Vector3 NetworkedScale { get; set; }

    // Network input data
    protected NetworkInputData networkInputData;
    protected float currentCameraAngle = 0f;

    // ========== Camera Properties ==========
    [Header("Camera")]
    public Vector3 moveDirection;
    public Transform cameraTransform;
    public float cameraRotationSpeed = 50f;
    public Vector3 cameraOffset = new Vector3(0, 12, -13);

    [Header("Move")]
    public float moveInputX;
    public float moveInputZ;

    protected override void Start()
    {
        base.Start();

        Debug.Log($"[SPAWNED] {gameObject.name} - Input: {HasInputAuthority}, State: {HasStateAuthority}");

        // กำหนดค่าเริ่มต้นของ scale
        NetworkedScale = transform.localScale;

        // Setup camera เฉพาะ local player
        if (HasInputAuthority)
        {
            cameraTransform = Camera.main?.transform;
        }
    }

    // ========== Network Update ==========
    public override void FixedUpdateNetwork()
    {
        // Client ที่มี InputAuthority แต่ไม่มี StateAuthority
        if (HasInputAuthority && !HasStateAuthority)
        {
            if (GetInput(out networkInputData))
            {
                ProcessMovement();
                ProcessCameraRotation();
                ProcessCharacterFacing();

                // ส่ง RPC ไปให้ Host อัพเดท position
                RPC_UpdatePosition(transform.position, rb.velocity, transform.localScale, transform.eulerAngles.y);
            }
        }
        // Host หรือ Server ที่มี StateAuthority
        else if (HasStateAuthority)
        {
            // อัพเดท Network Properties
            NetworkedPosition = transform.position;
            NetworkedScale = transform.localScale;
            NetworkedYRotation = transform.eulerAngles.y;
            NetworkedFlipX = transform.localScale.x < 0;

            if (rb != null)
            {
                NetworkedVelocity = rb.velocity;
            }

            // Process input ถ้าเป็น local player
            if (HasInputAuthority)
            {
                if (GetInput(out networkInputData))
                {
                    ProcessMovement();
                    ProcessCameraRotation();
                    ProcessCharacterFacing();
                }
            }
        }
        // Remote player - apply network state
        else
        {
            ApplyNetworkState();
        }

        // เรียก virtual method สำหรับ abilities เฉพาะของแต่ละ class
        ProcessClassSpecificAbilities();
    }

    // ========== Virtual Methods for Inheritance ==========
    protected virtual void ProcessClassSpecificAbilities()
    {
        // Override ใน class ลูกสำหรับ abilities เฉพาะ
    }

    // ========== RPC Methods ==========
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_UpdatePosition(Vector3 position, Vector3 velocity, Vector3 scale, float yRotation)
    {
        transform.position = position;
        if (rb != null) rb.velocity = velocity;
        transform.localScale = scale;
        transform.eulerAngles = new Vector3(0, yRotation, 0);
    }

    // ========== Network State Application ==========
    protected virtual void ApplyNetworkState()
    {
        float positionDistance = Vector3.Distance(transform.position, NetworkedPosition);

        if (positionDistance > 0.1f)
        {
            if (rb != null)
            {
                rb.velocity = NetworkedVelocity;
            }

            float lerpRate = positionDistance > 2f ? 50f : 20f;
            transform.position = Vector3.Lerp(
                transform.position,
                NetworkedPosition,
                Runner.DeltaTime * lerpRate
            );
        }

        // Scale synchronization
        float scaleDistance = Vector3.Distance(transform.localScale, NetworkedScale);
        if (scaleDistance > 0.01f)
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                NetworkedScale,
                Runner.DeltaTime * 15f
            );
        }

        // Rotation synchronization
        float targetYRotation = NetworkedYRotation;
        float currentYRotation = transform.eulerAngles.y;
        float rotationDifference = Mathf.DeltaAngle(currentYRotation, targetYRotation);

        if (Mathf.Abs(rotationDifference) > 1f)
        {
            Quaternion targetRotation = Quaternion.Euler(0, targetYRotation, 0);
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                Runner.DeltaTime * 10f
            );
        }

        // Flip synchronization
        Vector3 currentScale = transform.localScale;
        bool shouldFlipX = NetworkedFlipX;
        bool isCurrentlyFlipped = currentScale.x < 0;

        if (shouldFlipX != isCurrentlyFlipped)
        {
            currentScale.x = shouldFlipX ? -Mathf.Abs(currentScale.x) : Mathf.Abs(currentScale.x);
            transform.localScale = currentScale;
        }
    }

    // ========== Movement Processing ==========
    protected virtual void ProcessMovement()
    {
        if (!HasInputAuthority) return;

        Vector3 moveDirection = Vector3.zero;

        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            moveDirection = camForward * networkInputData.movementInput.y +
                           camRight * networkInputData.movementInput.x;
        }

        if (rb != null)
        {
            if (moveDirection.magnitude > 0.1f)
            {
                Vector3 newVelocity = new Vector3(
                    moveDirection.x * MoveSpeed,
                    rb.velocity.y,
                    moveDirection.z * MoveSpeed
                );

                rb.velocity = newVelocity;
                FlipCharacterNetwork(networkInputData.movementInput.x);
            }
            else
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
            }
        }
    }

    protected virtual void ProcessCameraRotation()
    {
        if (!HasInputAuthority || cameraTransform == null) return;

        if (Mathf.Abs(networkInputData.cameraRotationInput) > 0.1f)
        {
            currentCameraAngle += networkInputData.cameraRotationInput * cameraRotationSpeed * Runner.DeltaTime;

            Quaternion rotation = Quaternion.AngleAxis(currentCameraAngle, Vector3.up);
            Vector3 rotatedOffset = rotation * new Vector3(0, 10, -10);

            cameraTransform.position = transform.position + rotatedOffset;
            cameraTransform.LookAt(transform.position);
        }
    }

    protected virtual void ProcessCharacterFacing()
    {
        if (!HasInputAuthority || cameraTransform == null) return;

        Vector3 lookDir = cameraTransform.forward;
        lookDir.y = 0;

        if (lookDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDir);
            NetworkedYRotation = transform.eulerAngles.y;
        }
    }

    protected void FlipCharacterNetwork(float horizontalInput)
    {
        Vector3 newScale = transform.localScale;

        if (horizontalInput > 0.1f)
        {
            newScale.x = Mathf.Abs(newScale.x);
            NetworkedFlipX = false;
        }
        else if (horizontalInput < -0.1f)
        {
            newScale.x = -Mathf.Abs(newScale.x);
            NetworkedFlipX = true;
        }

        transform.localScale = newScale;
    }

    // ========== Original Methods (Non-Network) ==========
    protected override void Update()
    {
        base.Update();

        // Camera follow สำหรับ local player
        if (HasInputAuthority && cameraTransform != null)
        {
            Vector3 desiredPosition = transform.position + Quaternion.AngleAxis(currentCameraAngle, Vector3.up) * cameraOffset;
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPosition, Time.deltaTime * 5f);
            cameraTransform.LookAt(transform.position);
        }
    }

    public void Move(Vector3 moveDirection)
    {
        // เก็บไว้สำหรับ non-network movement ถ้าจำเป็น
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();
        Vector3 adjustedMoveDirection = camForward * moveInputZ + camRight * moveInputX;
        rb.velocity = new Vector3(adjustedMoveDirection.x * MoveSpeed, rb.velocity.y, adjustedMoveDirection.z * MoveSpeed);
    }

    protected virtual void FlipCharacter()
    {
        if (moveInputX > 0)
        {
            transform.localScale = new Vector3(1f, 1f, 1f);
        }
        else if (moveInputX < 0)
        {
            transform.localScale = new Vector3(-1f, 1f, 1f);
        }
    }

    public void FollowCamera()
    {
        if (cameraTransform == null)
            return;
        cameraTransform.position = transform.position + cameraOffset;
        cameraTransform.LookAt(transform.position);
    }

    protected void RotateCharacterToCamera()
    {
        if (cameraTransform == null)
            return;
        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0;
        transform.forward = cameraForward;
    }

    protected void RotateCamera(float input)
    {
        if (cameraTransform == null)
            return;
        if (input != 0)
        {
            Quaternion rotation = Quaternion.AngleAxis(input * cameraRotationSpeed * Time.deltaTime, Vector3.up);
            cameraOffset = rotation * cameraOffset;
            cameraTransform.position = transform.position + cameraOffset;
            cameraTransform.LookAt(transform.position);
        }
    }

    // ========== Fusion Methods ==========
    public override void Spawned()
    {
        Debug.Log($"[NETWORK SPAWNED] {gameObject.name} on {Runner.LocalPlayer}, Object ID: {Object.Id}");
    }

    public override void Render()
    {
        // Visual interpolation สำหรับ remote players
        if (!HasInputAuthority)
        {
            float alpha = Runner.DeltaTime * 20f;

            if (Vector3.Distance(transform.position, NetworkedPosition) > 0.01f)
            {
                transform.position = Vector3.Lerp(transform.position, NetworkedPosition, alpha);
            }

            if (Vector3.Distance(transform.localScale, NetworkedScale) > 0.001f)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, NetworkedScale, alpha);
            }
        }
    }
}