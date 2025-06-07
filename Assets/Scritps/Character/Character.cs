using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

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
public class Character : NetworkBehaviour
{

    [Header("Base Stats")]
  public  CharacterStats characterStats;
    [SerializeField]
    private string characterName;
    public string CharacterName { get { return characterName; } }
    [SerializeField]
    private int currentHp; 
    public int CurrentHp { get { return currentHp; } set { currentHp = value; } }
    [SerializeField]
    private int maxHp;
    public int MaxHp { get { return maxHp; } set { maxHp = value; } }
    [SerializeField]
    private int currentMana;
    public int CurrentMana { get { return currentMana; } set { currentMana = value; } }
    [SerializeField]
    private int maxMana;
    public int MaxMana { get { return maxMana; } set { maxMana = value; } }
    [SerializeField]

    private int attackDamage;
    public int AttackDamage { get { return attackDamage; } set { attackDamage = value; } }
    [SerializeField]

    private int armor;
    public int Armor { get { return armor; } set { armor = value; } }
    [SerializeField]

    private float moveSpeed;
    public float MoveSpeed { get { return moveSpeed; } set { moveSpeed = value; } }
    [SerializeField]
    private float attackRange;
    public float AttackRange { get { return attackRange; } set { attackRange = value; } }
    [SerializeField]

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
   protected virtual void Start()
    {
        characterName = characterStats.characterName;
        maxHp = characterStats.maxHp;
        currentHp = maxHp;
        maxMana = characterStats.maxMana;
        currentMana = maxMana;
        attackDamage = characterStats.attackDamage;
        armor = characterStats.arrmor;
        moveSpeed = characterStats.moveSpeed;
        attackRange = characterStats.attackRange;
        attackCooldown = characterStats.attackCoolDown;

        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ| RigidbodyConstraints.FreezeRotationY;
            rb.useGravity = true;
            rb.drag = 1.0f;
            rb.mass = 10f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

    }

  protected  virtual void Update()
    {
        
    }
}
