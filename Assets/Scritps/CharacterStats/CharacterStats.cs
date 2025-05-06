using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterStats", menuName = "Character/Stats")]
public class CharacterStats : ScriptableObject
{
    public string characterName;
    public float moveSpeed;
    public int maxHp;
    public int attackDamage;
    public int arrmor;
    public int attackCoolDown;
    public int attackRange;
}
