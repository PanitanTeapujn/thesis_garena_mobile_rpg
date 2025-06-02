using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class BloodKnight : Hero
{
    // ไม่ต้องใช้ Joystick โดยตรงแล้ว
    // public Joystick joystick;
    // public Joystick joystickCamera;

    // Network Properties สำหรับซิงค์
    [Networked] public Vector3 NetworkedPosition { get; set; }
    [Networked] public bool NetworkedFlipX { get; set; } // ซิงค์การ flip แทน rotation
    [Networked] public float NetworkedYRotation { get; set; } // ซิงค์การหันตามกล้อง

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
        // รับ input จาก network
        if (GetInput(out networkInputData))
        {
            // Process movement
            ProcessMovement();

            // Process camera rotation
            ProcessCameraRotation();

            // Process character facing direction (หันตามกล้อง)
            ProcessCharacterFacing();
        }

        // Sync position, flip & rotation สำหรับ non-local players
        if (!HasInputAuthority)
        {
            transform.position = Vector3.Lerp(transform.position, NetworkedPosition, Time.deltaTime * 10f);

            // Sync flip
            if (NetworkedFlipX)
            {
                transform.localScale = new Vector3(-1f, 1f, 1f);
            }
            else
            {
                transform.localScale = new Vector3(1f, 1f, 1f);
            }

            // Sync Y rotation (การหันตามกล้อง)
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
        if (rb != null && moveDirection.magnitude > 0.1f)
        {
            rb.velocity = new Vector3(
                moveDirection.x * MoveSpeed,
                rb.velocity.y,
                moveDirection.z * MoveSpeed
            );

            // อัพเดท network position
            NetworkedPosition = transform.position;

            // Flip ตัวละครตามทิศทางที่เดิน (แบบ 2D)
            FlipCharacter(networkInputData.movementInput.x);
        }
        else if (rb != null)
        {
            // หยุดเคลื่อนที่
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
            NetworkedPosition = transform.position;
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
            Vector3 rotatedOffset = rotation * new Vector3(0, 10, -10); // ใช้ offset เดิม

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
            transform.localScale = new Vector3(1f, 1f, 1f);
            NetworkedFlipX = false;
        }
        else if (horizontalInput < -0.1f)
        {
            transform.localScale = new Vector3(-1f, 1f, 1f);
            NetworkedFlipX = true;
        }
        // ถ้าไม่มีการเคลื่อนที่ จะคงทิศทางเดิมไว้
    }

    // Update ใช้สำหรับ visual effects และ camera follow เท่านั้น
    protected override void Update()
    {
        // อย่าเรียก base.Update() ที่มีการควบคุมแบบเก่า
        // base.Update();

        // Camera follow สำหรับ local player
        if (HasInputAuthority && cameraTransform != null)
        {
            // Smooth camera follow
            Vector3 desiredPosition = transform.position + Quaternion.AngleAxis(currentCameraAngle, Vector3.up) * cameraOffset;
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPosition, Time.deltaTime * 5f);
            cameraTransform.LookAt(transform.position);
        }
    }
}