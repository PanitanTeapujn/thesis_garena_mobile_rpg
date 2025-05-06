using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BloodKinght : Hero
{
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
        moveInputX = Input.GetAxisRaw("Horizontal");
        moveInputZ = Input.GetAxisRaw("Vertical");
        moveDirection = new Vector3(moveInputX, 0, moveInputZ).normalized;
        float rotationInput = 0f;
        if (Input.GetKey(KeyCode.Q)) rotationInput = -1f;
        if (Input.GetKey(KeyCode.E)) rotationInput = 1f;

        RotateCamera(rotationInput);
        Move(moveDirection);

    }
}
