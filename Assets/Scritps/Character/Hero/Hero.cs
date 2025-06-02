using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hero : Character
{
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
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        FlipCharacter();
        FollowCamera();
        RotateCharacterToCamera();
        if (cameraTransform != null)
        {
            Vector3 lookDir = cameraTransform.forward;
            lookDir.y = 0;
            transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }

    #region CharacterMove

    public void Move(Vector3 moveDirection)
    {
        // ถ้าถูกสตั้น ไม่สามารถเคลื่อนที่ได้
        /*  if (isStunned)
              return;
  */
        // โค้ดการเคลื่อนที่เดิม
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

    #endregion

    #region Camera
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
    #endregion
}
