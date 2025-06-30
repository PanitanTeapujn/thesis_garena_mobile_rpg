using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

// Updated NetworkInputData with attack button
public struct NetworkInputData : INetworkInput
{
    // ข้อมูลการเคลื่อนไหว
    public Vector2 movementInput;      // จาก Joystick การเดิน
    public float cameraRotationInput;  // จาก Joystick กล้อง

    // ข้อมูลการกระทำ
    public NetworkBool attack;         // ปุ่มโจมตี
    public NetworkBool skill1;
    public NetworkBool skill2; 
    public NetworkBool skill3;
    public NetworkBool skill4;
    public NetworkBool dodge;
    public NetworkBool potion1;       // ปุ่ม Potion slot 1
    public NetworkBool potion2;       // ปุ่ม Potion slot 2
    public NetworkBool potion3;       // ปุ่ม Potion slot 3
    public NetworkBool potion4;       // ปุ่ม Potion slot 4
    public NetworkBool potion5;       // ปุ่ม Potion slot 5

    // ข้อมูลเพิ่มเติม
    public Vector3 lookDirection;      // ทิศทางที่ตัวละครหัน
}
