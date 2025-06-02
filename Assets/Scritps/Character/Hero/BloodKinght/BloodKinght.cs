using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class BloodKnight : Hero
{
    // Network Properties สำหรับซิงค์
    [Networked] public Vector3 NetworkedPosition { get; set; }
    [Networked] public Vector3 NetworkedVelocity { get; set; } // เพิ่มการซิงค์ velocity
    [Networked] public bool NetworkedFlipX { get; set; }
    [Networked] public float NetworkedYRotation { get; set; }

    // เก็บ input ที่ได้รับจาก network
    private NetworkInputData networkInputData;
    private float currentCameraAngle = 0f;

    protected override void Start()
    {
        base.Start();

        // Setup camera เฉพาะ local player
        if (HasInputAuthority)
        {
            cameraTransform = Camera.main?.transform;
            if (cameraTransform != null)
            {
                Debug.Log("Camera assigned to local player");
            }
        }

        Debug.Log($"BloodKnight spawned - HasInputAuthority: {HasInputAuthority}");
    }

    // *** สำคัญมาก: FixedUpdateNetwork ทำงานกับ Fusion's tick rate ***
    public override void FixedUpdateNetwork()
    {
        if (HasInputAuthority)
        {
            // Local player - process input and update network properties
            if (GetInput(out networkInputData))
            {
                ProcessMovement();
                ProcessCameraRotation();
                ProcessCharacterFacing();
            }

            // อัพเดท network properties ทุก tick
            NetworkedPosition = transform.position;
            if (rb != null)
            {
                NetworkedVelocity = rb.velocity;
            }
        }
        else
        {
            // Remote player - apply network properties
            // ใช้ velocity สำหรับการเคลื่อนที่ที่ smooth
            if (rb != null)
            {
                rb.velocity = NetworkedVelocity;
            }

            // Interpolate position for extra smoothness
            transform.position = Vector3.Lerp(
                transform.position,
                NetworkedPosition,
                Runner.DeltaTime * 15f
            );

            // Apply flip
            if (NetworkedFlipX)
            {
                transform.localScale = new Vector3(-1f, 1f, 1f);
            }
            else
            {
                transform.localScale = new Vector3(1f, 1f, 1f);
            }

            // Apply rotation
            transform.rotation = Quaternion.Euler(0, NetworkedYRotation, 0);
        }
    }

    private void ProcessMovement()
    {
        if (!HasInputAuthority) return;

        // คำนวณทิศทางการเคลื่อนที่ตาม camera
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

        // เคลื่อนที่ด้วย Rigidbody
        if (rb != null)
        {
            if (moveDirection.magnitude > 0.1f)
            {
                rb.velocity = new Vector3(
                    moveDirection.x * MoveSpeed,
                    rb.velocity.y,
                    moveDirection.z * MoveSpeed
                );

                // Flip ตัวละครตามทิศทางที่เดิน
                FlipCharacter(networkInputData.movementInput.x);
            }
            else
            {
                // หยุดเคลื่อนที่
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
            }
        }
    }

    private void ProcessCameraRotation()
    {
        if (!HasInputAuthority || cameraTransform == null) return;

        // หมุนกล้องตาม input
        if (Mathf.Abs(networkInputData.cameraRotationInput) > 0.1f)
        {
            currentCameraAngle += networkInputData.cameraRotationInput * cameraRotationSpeed * Runner.DeltaTime;

            // คำนวณตำแหน่งกล้องใหม่
            Quaternion rotation = Quaternion.AngleAxis(currentCameraAngle, Vector3.up);
            Vector3 rotatedOffset = rotation * new Vector3(0, 10, -10);

            cameraTransform.position = transform.position + rotatedOffset;
            cameraTransform.LookAt(transform.position);
        }
    }

    private void ProcessCharacterFacing()
    {
        if (!HasInputAuthority || cameraTransform == null) return;

        // หันตัวละครตามทิศทางกล้อง
        Vector3 lookDir = cameraTransform.forward;
        lookDir.y = 0;

        if (lookDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDir);

            // Sync การหันไปยังผู้เล่นอื่น
            NetworkedYRotation = transform.eulerAngles.y;
        }
    }

    private void FlipCharacter(float horizontalInput)
    {
        // Flip ตัวละครตามทิศทางการเคลื่อนที่แนวนอน
        if (horizontalInput > 0.1f)
        {
            transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            NetworkedFlipX = false;
        }
        else if (horizontalInput < -0.1f)
        {
            transform.localScale = new Vector3(-0.5f, 0.5f, 0.5f);
            NetworkedFlipX = true;
        }
    }

    // Update ใช้สำหรับ visual effects และ camera follow เท่านั้น
    protected override void Update()
    {
        // Camera follow สำหรับ local player
        if (HasInputAuthority && cameraTransform != null)
        {
            // Smooth camera follow
            Vector3 desiredPosition = transform.position + Quaternion.AngleAxis(currentCameraAngle, Vector3.up) * cameraOffset;
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPosition, Time.deltaTime * 5f);
            cameraTransform.LookAt(transform.position);
        }
    }

    // Debug display
    void OnGUI()
    {
        if (HasInputAuthority)
        {
            GUI.Label(new Rect(10, 130, 300, 20), $"[LOCAL] Pos: {transform.position:F2}");
            GUI.Label(new Rect(10, 150, 300, 20), $"[LOCAL] Vel: {rb?.velocity:F2}");
            GUI.Label(new Rect(10, 170, 300, 20), $"[LOCAL] NetPos: {NetworkedPosition:F2}");
        }
    }
}