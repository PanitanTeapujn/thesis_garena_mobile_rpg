using UnityEngine;

public class SpikeCollisionHandler : MonoBehaviour
{
    private BossSlime boss;
    private int damage;
    private int poisonDamage;
    private float poisonDuration;
    private bool hasHit = false; // ป้องกันโดนซ้ำ

    public void Initialize(BossSlime bossSlime, int spikeDamage, int poisonDmg, float poisonDur)
    {
        boss = bossSlime;
        damage = spikeDamage;
        poisonDamage = poisonDmg;
        poisonDuration = poisonDur;
        hasHit = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // ป้องกันโดนซ้ำ
        if (hasHit) return;

        // เช็คว่าเป็น Player หรือไม่
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Hero hero = other.GetComponent<Hero>();
            if (hero != null && boss != null)
            {
                // ทำดาเมจ
                hero.TakeDamageFromAttacker(0, damage, boss, DamageType.Magic);

                // ใส่พิษ
                hero.ApplyStatusEffect(StatusEffectType.Poison, poisonDamage, poisonDuration);

                Debug.Log($"🐲 Spike hit {hero.CharacterName}! Damage: {damage}, Poison: {poisonDamage}/s");

                // ตั้งค่าให้โดนแล้ว
                hasHit = true;
            }
        }
    }
}