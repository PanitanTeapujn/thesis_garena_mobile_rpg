using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class BloodKnight : Hero
{
    // Network Properties สำหรับซิงค์
    [Networked] public Vector3 NetworkedPosition { get; set; }
    [Networked] public Vector3 NetworkedVelocity { get; set; }
    [Networked] public bool NetworkedFlipX { get; set; }
    [Networked] public float NetworkedYRotation { get; set; }
    [Networked] public Vector3 NetworkedScale { get; set; } // เพิ่มการซิงค์ scale

    // เก็บ input ที่ได้รับจาก network
    private NetworkInputData networkInputData;
    private float currentCameraAngle = 0f;

    // เพิ่มตัวแปรสำหรับ smooth interpolation
    private Vector3 previousPosition;
    private Vector3 previousScale;

    protected override void Start()
    {
        base.Start();

        // กำหนดค่าเริ่มต้นของ scale
        NetworkedScale = new Vector3(0.5f, 0.5f, 0.5f);
        previousScale = NetworkedScale;
        previousPosition = transform.position;

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
            NetworkedScale = transform.localScale; // ซิงค์ scale ด้วย

            if (rb != null)
            {
                NetworkedVelocity = rb.velocity;
            }
        }
        else
        {
            // Remote player - apply network properties with interpolation
            ApplyNetworkState();
        }
    }

    private void ApplyNetworkState()
    {
        // ใช้ Fusion's built-in interpolation
        if (rb != null)
        {
            // ใช้ velocity สำหรับการเคลื่อนที่ที่ smooth
            rb.velocity = NetworkedVelocity;
        }

        // Position interpolation - ใช้ Runner.DeltaTime สำหรับ Fusion
        float positionLerpRate = 20f;
        transform.position = Vector3.Lerp(
            transform.position,
            NetworkedPosition,
            Runner.DeltaTime * positionLerpRate
        );

        // Scale interpolation - สำคัญมาก!
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            NetworkedScale,
            Runner.DeltaTime * 15f
        );

        // Rotation interpolation
        float targetYRotation = NetworkedYRotation;
        Quaternion targetRotation = Quaternion.Euler(0, targetYRotation, 0);
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            Runner.DeltaTime * 10f
        );

        // อัพเดทค่า previous values
        previousPosition = transform.position;
        previousScale = transform.localScale;
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
            NetworkedYRotation = transform.eulerAngles.y;
        }
    }

    private void FlipCharacter(float horizontalInput)
    {
        // ปรับปรุงการ flip character ให้แม่นยำมากขึ้น
        Vector3 newScale = transform.localScale;

        if (horizontalInput > 0.1f)
        {
            // หันไปทางขวา
            newScale.x = Mathf.Abs(newScale.x); // ทำให้แน่ใจว่าเป็นค่าบวก
            NetworkedFlipX = false;
        }
        else if (horizontalInput < -0.1f)
        {
            // หันไปทางซ้าย
            newScale.x = -Mathf.Abs(newScale.x); // ทำให้แน่ใจว่าเป็นค่าลบ
            NetworkedFlipX = true;
        }

        transform.localScale = newScale;
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

        // Debug: แสดงสถานะของ remote players
        if (!HasInputAuthority)
        {
            DebugRemotePlayer();
        }
    }

    private void DebugRemotePlayer()
    {
        // ตรวจสอบว่า scale ถูกซิงค์หรือไม่
        if (Vector3.Distance(transform.localScale, NetworkedScale) > 0.1f)
        {
            Debug.LogWarning($"Scale mismatch - Current: {transform.localScale}, Networked: {NetworkedScale}");
        }
    }

    // เพิ่ม method สำหรับ debug network state
    public override void Render()
    {
        // Fusion's Render method สำหรับ visual interpolation
        if (!HasInputAuthority)
        {
            // ใช้ Time.fixedDeltaTime สำหรับ interpolation แทน
            float alpha = Time.fixedDeltaTime * 10f; // ปรับค่าตามต้องการ
            transform.position = Vector3.Lerp(previousPosition, NetworkedPosition, alpha);
            transform.localScale = Vector3.Lerp(previousScale, NetworkedScale, alpha);

            // อัพเดทค่า previous สำหรับ frame ถัดไป
            previousPosition = transform.position;
            previousScale = transform.localScale;
        }
    }

    // Debug display - ปรับปรุงให้แสดงข้อมูลมากขึ้น
    void OnGUI()
    {
        if (HasInputAuthority)
        {
            GUI.Label(new Rect(10, 130, 400, 20), $"[LOCAL] Pos: {transform.position:F2}");
            GUI.Label(new Rect(10, 150, 400, 20), $"[LOCAL] Vel: {rb?.velocity:F2}");
            GUI.Label(new Rect(10, 170, 400, 20), $"[LOCAL] Scale: {transform.localScale:F2}");
            GUI.Label(new Rect(10, 190, 400, 20), $"[LOCAL] NetScale: {NetworkedScale:F2}");
            GUI.Label(new Rect(10, 210, 400, 20), $"[LOCAL] FlipX: {NetworkedFlipX}");
        }
        else
        {
            GUI.Label(new Rect(10, 250, 400, 20), $"[REMOTE] Pos: {transform.position:F2}");
            GUI.Label(new Rect(10, 270, 400, 20), $"[REMOTE] NetPos: {NetworkedPosition:F2}");
            GUI.Label(new Rect(10, 290, 400, 20), $"[REMOTE] Scale: {transform.localScale:F2}");
            GUI.Label(new Rect(10, 310, 400, 20), $"[REMOTE] NetScale: {NetworkedScale:F2}");
            GUI.Label(new Rect(10, 330, 400, 20), $"[REMOTE] FlipX: {NetworkedFlipX}");
        }
    }
}