using System.Collections;
using System.Collections.Generic;
using UnityEngine;
enum StatusEffectType
{
    None,
    Poison,
    Burn,
    Freeze,
    Stun,
    Bleed
}

enum DamageType
{
    Normal,
    Critical,
    Magic,
    Poison,
    Burn,
    Freeze,
    Stun,
    Bleed
}
public class Character : MonoBehaviour
{

    [Header("Base Stats")]
    private int currentHp; 
    public int CurrentHp { get { return currentHp; } set { currentHp = value; } }
    private int maxHp;
    public int MaxHp { get { return maxHp; } set { currentHp = value; } }

    private int attackDamage;
    public int AttackDamage { get { return attackDamage; } set { attackDamage = value; } }

    private int armor;
    public int Armor { get { return armor; } set { armor = value; } }

    private int moveSpeed;
    public int MoveSpeed { get { return moveSpeed; } set { moveSpeed = value; } }
    private float attackRange;
    public float AttackRange { get { return attackRange; } set { attackRange = value; } }

    private float attackCooldown;
    public float AttackCooldown { get { return attackCooldown; } set { attackCooldown = value; } }

    [Header("Physics")]
    public Rigidbody rb;

    [Header("Visual")]
    public Renderer characterRenderer;
    public Color originalColor;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        characterRenderer = GetComponent<Renderer>();
        if (characterRenderer != null)
        {
            originalColor = characterRenderer.material.color;
        }

    }
    void Start()
    {
       
    }

    void Update()
    {
        
    }
}
