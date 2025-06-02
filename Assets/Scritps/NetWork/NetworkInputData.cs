using UnityEngine;
using Fusion;


public struct NetworkInputData : INetworkInput
{
    // ข้อมูลการเคลื่อนไหว
    public Vector2 movementInput;      // จาก Joystick การเดิน
    public float cameraRotationInput;  // จาก Joystick กล้อง

    // ข้อมูลการกระทำ (เตรียมไว้สำหรับอนาคต)
    public NetworkBool attack;
    public NetworkBool skill1;
    public NetworkBool skill2;
    public NetworkBool dodge;

    // ข้อมูลเพิ่มเติม
    public Vector3 lookDirection;  // ทิศทางที่ตัวละครหัน

}