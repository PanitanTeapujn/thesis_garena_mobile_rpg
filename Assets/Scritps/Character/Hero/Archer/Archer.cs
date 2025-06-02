using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Archer : Hero
{
    // ใช้ระบบเดียวกับ BloodKnight
    [Networked] public Vector3 NetworkedPosition { get; set; }
    [Networked] public Vector3 NetworkedVelocity { get; set; }
    [Networked] public bool NetworkedFlipX { get; set; }
    [Networked] public float NetworkedYRotation { get; set; }

    private NetworkInputData networkInputData;
    private float currentCameraAngle = 0f;

    protected override void Start()
    {
        base.Start();

        if (HasInputAuthority)
        {
            cameraTransform = Camera.main?.transform;
            if (cameraTransform != null)
            {
                Debug.Log("Camera assigned to Archer");
            }
        }

        Debug.Log($"Archer spawned - HasInputAuthority: {HasInputAuthority}");
    }

    public override void FixedUpdateNetwork()
    {
        if (HasInputAuthority)
        {
            if (GetInput(out networkInputData))
            {
                ProcessMovement();
                ProcessCameraRotation();
                ProcessCharacterFacing();
            }

            NetworkedPosition = transform.position;
            if (rb != null)
            {
                NetworkedVelocity = rb.velocity;
            }
        }
        else
        {
            if (rb != null)
            {
                rb.velocity = NetworkedVelocity;
            }

            transform.position = Vector3.Lerp(
                transform.position,
                NetworkedPosition,
                Runner.DeltaTime * 15f
            );

            if (NetworkedFlipX)
            {
                transform.localScale = new Vector3(-1f, 1f, 1f);
            }
            else
            {
                transform.localScale = new Vector3(1f, 1f, 1f);
            }

            transform.rotation = Quaternion.Euler(0, NetworkedYRotation, 0);
        }
    }

    private void ProcessMovement()
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
                rb.velocity = new Vector3(
                    moveDirection.x * MoveSpeed,
                    rb.velocity.y,
                    moveDirection.z * MoveSpeed
                );

                FlipCharacter(networkInputData.movementInput.x);
            }
            else
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
            }
        }
    }

    private void ProcessCameraRotation()
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

    private void ProcessCharacterFacing()
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

    private void FlipCharacter(float horizontalInput)
    {
        if (horizontalInput > 0.1f)
        {
            transform.localScale = new Vector3(0.3f, 0.3f, 1f);
            NetworkedFlipX = false;
        }
        else if (horizontalInput < -0.1f)
        {
            transform.localScale = new Vector3(-0.3f, 0.3f, 1f);
            NetworkedFlipX = true;
        }
    }

    protected override void Update()
    {
        if (HasInputAuthority && cameraTransform != null)
        {
            Vector3 desiredPosition = transform.position + Quaternion.AngleAxis(currentCameraAngle, Vector3.up) * cameraOffset;
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPosition, Time.deltaTime * 5f);
            cameraTransform.LookAt(transform.position);
        }
    }
}