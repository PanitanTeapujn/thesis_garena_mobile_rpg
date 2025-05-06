using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BloodKinght : Hero
{
   public Joystick joystick;
    public Joystick joystickCamera;

    // Start is called before the first frame update
   protected override void Start()
    {
        base.Start();
        Debug.Log(MoveSpeed);

    }

    // Update is called once per frame
    protected override  void Update()
    {
        base.Update();
        moveInputX = joystick.Horizontal;
        moveInputZ = joystick.Vertical;
        moveDirection = new Vector3(moveInputX, 0, moveInputZ).normalized;
        float rotationInput = 0f;
        rotationInput = joystickCamera.Horizontal;
       

        RotateCamera(rotationInput);
        Move(moveDirection);

    }
}
