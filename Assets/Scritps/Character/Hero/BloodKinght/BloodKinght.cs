using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class BloodKnight : Hero
{
    public Joystick joystick;
    public Joystick joystickCamera;
    [Networked] public Vector3 NetworkedPosition { get; set; }

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        if (HasInputAuthority)
        {
            cameraTransform = Camera.main?.transform;

           
        }
        Debug.Log(MoveSpeed);
    }

    // ซิงค์ตำแหน่งในเครือข่าย
    public override void FixedUpdateNetwork()
    {
        if (HasInputAuthority)
        {
            // รับ input → ควบคุมการเคลื่อนไหว
            float h = joystick.Horizontal;
            float v = joystick.Vertical;

            Vector3 direction = new Vector3(h, 0, v);
            transform.position += direction * MoveSpeed * Runner.DeltaTime;

            NetworkedPosition = transform.position; // ซิงค์ตำแหน่ง
        }
        else
        {
            // Sync ตำแหน่ง
            transform.position = NetworkedPosition;
        }
    }

    // ควบคุมการเคลื่อนไหวและหมุนกล้อง
    protected override void Update()
    {
        base.Update();
        if (!HasInputAuthority || cameraTransform == null)
            return;
        // รับอินพุตจาก joystick สำหรับการเคลื่อนไหว
        moveInputX = joystick.Horizontal;
        moveInputZ = joystick.Vertical;
        moveDirection = new Vector3(moveInputX, 0, moveInputZ).normalized;

        // รับอินพุตจาก joystick สำหรับการหมุนกล้อง
        float rotationInput = joystickCamera.Horizontal;

        // หมุนกล้อง
        RotateCamera(rotationInput);

        // เคลื่อนที่
        Move(moveDirection);
    }
}
