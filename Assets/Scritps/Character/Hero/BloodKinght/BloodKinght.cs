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
    [Networked] public Vector3 NetworkedScale { get; set; }

    // เก็บ input ที่ได้รับจาก network
    private NetworkInputData networkInputData;
    private float currentCameraAngle = 0f;

    // เพิ่มตัวแปรสำหรับ smooth interpolation
    private Vector3 previousPosition;
    private Vector3 previousScale;

    protected override void Start()
    {
        base.Start();
        BloodKnight[] allKnights = FindObjectsOfType<BloodKnight>();
        int myInstanceCount = 0;

        foreach (var knight in allKnights)
        {
            if (knight.HasInputAuthority == this.HasInputAuthority &&
                knight.Object.InputAuthority == this.Object.InputAuthority)
            {
                myInstanceCount++;
                if (myInstanceCount > 1)
                {
                    Debug.LogWarning($"Duplicate player found, destroying: {gameObject.name}");
                    Runner.Despawn(Object);
                    return;
                }
            }
        }
        Debug.Log($"[SPAWNED] {gameObject.name} - Input: {HasInputAuthority}, State: {HasStateAuthority}");

        // กำหนดค่าเริ่มต้นของ scale
        NetworkedScale = new Vector3(0.5f, 0.5f, 0.5f);
        previousScale = NetworkedScale;
        previousPosition = transform.position;

        // Setup camera เฉพาะ local player
        if (HasInputAuthority)
        {
          //  Debug.Log($"[LOCAL PLAYER] {gameObject.name} spawned at position: {transform.position}");

            cameraTransform = Camera.main?.transform;
            if (cameraTransform != null)
            {   
              //  Debug.Log("Camera assigned to local player");
            }
        }
        else
        {
            //Debug.Log($"[REMOTE PLAYER] {gameObject.name} spawned at position: {transform.position}");
        }

       // Debug.Log($"BloodKnight spawned - HasInputAuthority: {HasInputAuthority}, HasStateAuthority: {HasStateAuthority}");
    }

    public override void FixedUpdateNetwork()
    {
        // *** ตรวจสอบว่าเป็น Local player บน Client หรือไม่ ***
        if (HasInputAuthority && !HasStateAuthority)
        {
            // Client ต้องส่งข้อมูลไปให้ Host อัพเดท
            if (GetInput(out networkInputData))
            {
                ProcessMovement();
                ProcessCameraRotation();
                ProcessCharacterFacing();

                // *** สำคัญ: ส่ง RPC ไปให้ Host อัพเดท position ***
                RPC_UpdatePosition(transform.position, rb.velocity, transform.localScale, transform.eulerAngles.y);
            }
        }
        else if (HasStateAuthority)
        {
            // Host หรือ Server อัพเดท Network Properties
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
        else
        {
            // Remote player - apply network state
            Debug.Log($"[REMOTE] NetPos: {NetworkedPosition}, NetVel: {NetworkedVelocity}");
            ApplyNetworkState();
        }
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_UpdatePosition(Vector3 position, Vector3 velocity, Vector3 scale, float yRotation)
    {
        // Host อัพเดทตำแหน่งของ Client
        transform.position = position;
        if (rb != null) rb.velocity = velocity;
        transform.localScale = scale;
        transform.eulerAngles = new Vector3(0, yRotation, 0);
    }

    private void ApplyNetworkState()
    {
        // Position synchronization
        float positionDistance = Vector3.Distance(transform.position, NetworkedPosition);
        Debug.Log($"[CLIENT] Applying state - NetPos: {NetworkedPosition}, NetVel: {NetworkedVelocity}");

        if (positionDistance > 0.1f) // เคลื่อนที่เฉพาะเมื่อมีความแตกต่างมากพอ
        {
            // ใช้ velocity สำหรับการเคลื่อนที่ที่ smooth
            if (rb != null)
            {
                rb.velocity = NetworkedVelocity;
            }

            // Position interpolation
            float lerpRate = positionDistance > 2f ? 50f : 20f; // เร็วขึ้นถ้าห่างมาก
            transform.position = Vector3.Lerp(
                transform.position,
                NetworkedPosition,
                Runner.DeltaTime * lerpRate
            );

            Debug.Log($"[REMOTE] {gameObject.name} - Applying position: {NetworkedPosition}, Distance: {positionDistance:F3}");
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

    private void ProcessMovement()
    {
        if (!HasInputAuthority) return;
        Debug.Log($"[ProcessMovement] {gameObject.name} - Input: {networkInputData.movementInput}, HasStateAuth: {HasStateAuthority}");

        if (networkInputData.movementInput.magnitude > 0.01f)
        {
            //Debug.Log($"[HOST Movement] Input: {networkInputData.movementInput}, Frame: {Runner.Tick}");
        }
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
                Vector3 newVelocity = new Vector3(
                    moveDirection.x * MoveSpeed,
                    rb.velocity.y,
                    moveDirection.z * MoveSpeed
                );

                rb.velocity = newVelocity;

                // Flip ตัวละครตามทิศทางที่เดิน
                FlipCharacter(networkInputData.movementInput.x);

                // Debug movement
               // Debug.Log($"[LOCAL] Moving with velocity: {newVelocity}, Direction: {moveDirection}");
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
        Vector3 newScale = transform.localScale;

        if (horizontalInput > 0.1f)
        {
            // หันไปทางขวา
            newScale.x = Mathf.Abs(newScale.x);
            NetworkedFlipX = false;
        }
        else if (horizontalInput < -0.1f)
        {
            // หันไปทางซ้าย
            newScale.x = -Mathf.Abs(newScale.x);
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
    }

    // Fusion's Render method สำหรับ visual interpolation
    public override void Render()
    {
        // เฉพาะ remote players ใช้ render interpolation
        if (!HasInputAuthority)
        {
            // Smooth visual interpolation between network updates
            float alpha = Runner.DeltaTime * 20f;

            // Position interpolation
            if (Vector3.Distance(transform.position, NetworkedPosition) > 0.01f)
            {
                transform.position = Vector3.Lerp(transform.position, NetworkedPosition, alpha);
            }

            // Scale interpolation
            if (Vector3.Distance(transform.localScale, NetworkedScale) > 0.001f)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, NetworkedScale, alpha);
            }
        }
    }
    public override void Spawned()
    {
        Debug.Log($"[NETWORK SPAWNED] {gameObject.name} on {Runner.LocalPlayer}, Object ID: {Object.Id}");
    }
    // Debug display
    void OnGUI()
    {
        string playerType = HasInputAuthority ? "LOCAL" : "REMOTE";
        Color originalColor = GUI.color;

        int yOffset = HasInputAuthority ? 130 : 270;

        if (HasInputAuthority)
        {
            GUI.color = Color.green;
            GUI.Label(new Rect(10, yOffset, 600, 20), $"[{playerType}] {gameObject.name}");
            GUI.Label(new Rect(10, yOffset + 20, 600, 20), $"Position: {transform.position:F2} | NetPos: {NetworkedPosition:F2}");
            GUI.Label(new Rect(10, yOffset + 40, 600, 20), $"Velocity: {rb?.velocity:F2} | NetVel: {NetworkedVelocity:F2}");
            GUI.Label(new Rect(10, yOffset + 60, 600, 20), $"Scale: {transform.localScale:F2} | NetScale: {NetworkedScale:F2}");
            GUI.Label(new Rect(10, yOffset + 80, 600, 20), $"Input: {networkInputData.movementInput:F2}");
        }
        else
        {
            GUI.color = Color.yellow;
            GUI.Label(new Rect(10, yOffset, 600, 20), $"[{playerType}] {gameObject.name}");
            GUI.Label(new Rect(10, yOffset + 20, 600, 20), $"Position: {transform.position:F2} | NetPos: {NetworkedPosition:F2}");
            GUI.Label(new Rect(10, yOffset + 40, 600, 20), $"Scale: {transform.localScale:F2} | NetScale: {NetworkedScale:F2}");
            GUI.Label(new Rect(10, yOffset + 60, 600, 20), $"NetVelocity: {NetworkedVelocity:F2}");

            // แสดงระยะห่างระหว่าง actual กับ network position
            float distance = Vector3.Distance(transform.position, NetworkedPosition);
            GUI.Label(new Rect(10, yOffset + 80, 600, 20), $"Distance to NetPos: {distance:F3}");

            // แสดงสถานะการรับข้อมูล
            bool receivingData = NetworkedPosition != Vector3.zero || NetworkedVelocity != Vector3.zero;
            GUI.color = receivingData ? Color.green : Color.red;
            GUI.Label(new Rect(10, yOffset + 100, 600, 20), $"Receiving Network Data: {receivingData}");
        }

        GUI.color = originalColor;
    }
}