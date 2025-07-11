﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterStats", menuName = "Character/Stats")]
public class CharacterStats : ScriptableObject
{
    public string characterName;
    public float moveSpeed;
    public int maxHp;
    public int maxMana;
    public int attackDamage;
    public int magicDamage;
    public int arrmor;
    public int attackCoolDown;
    public int attackRange;
    public float criticalChance;
    public float criticalDamageBonus;
    public float hitRate;      // เปอร์เซ็นต์การโจมตีโดน (85%)
    public float evasionRate;  // เปอร์เซ็นต์การหลบหลีก (5%)
    public float attackSpeed;
    public float reductionCoolDown;
    public float lifeSteal;
    public AttackType attackType = AttackType.Physical;
}
