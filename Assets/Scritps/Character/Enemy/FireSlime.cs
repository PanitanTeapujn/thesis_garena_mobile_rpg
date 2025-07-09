using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class FireSlime : NetworkEnemy
{
    [Header("🔥 Fire Slime Settings")]
    [SerializeField] private float burnRadius = 3f;           // รัศมีการเผาไหม้
    [SerializeField] private float burnTickInterval = 2f;      // ช่วงเวลาการเผาไหม้
    [SerializeField] private int burnDamage = 8;              // ดาเมจการเผาไหม้
    [SerializeField] private float fireTrailDuration = 5f;    // ระยะเวลาร่องไฟ
    [SerializeField] private float explodeRadius = 4f;        // รัศมีการระเบิดเมื่อตาย
    [SerializeField] private int explodeDamage = 25;          // ดาเมจการระเบิด

    [Header("🎨 Visual Effects")]
    [SerializeField] private ParticleSystem fireAuraEffect;   // เอฟเฟกต์ไฟรอบตัว
    [SerializeField] private ParticleSystem fireTrailEffect;  // เอฟเฟกต์ร่องไฟ
    [SerializeField] private ParticleSystem explodeEffect;    // เอฟเฟกต์การระเบิด
    [SerializeField] private Light fireLight;                 // แสงไฟ

    [Header("🌡️ Heat Zone")]
    [SerializeField] private bool createHeatZones = true;     // สร้างพื้นที่ร้อน
    [SerializeField] private GameObject heatZonePrefab;       // Prefab ของพื้นที่ร้อน

    private float nextBurnTime = 0f;
    private List<GameObject> activeHeatZones = new List<GameObject>();
    private Coroutine heatZoneCoroutine;

    protected override void Start()
    {
        base.Start();

        // ตั้งค่าเฉพาะของ Fire Slime
        SetupFireSlimeStats();
        InitializeFireEffects();

        if (HasStateAuthority)
        {
            // เริ่มสร้างพื้นที่ร้อน
            if (createHeatZones)
            {
                heatZoneCoroutine = StartCoroutine(CreateHeatZonesRoutine());
            }
        }
    }

    private void SetupFireSlimeStats()
    {
        // ปรับ stats ให้เหมาะกับ Fire Slime
        CharacterName = "Fire Slime";
        AttackType = AttackType.Magic; // ใช้ Magic damage

        // เพิ่ม fire resistance และลด ice resistance
        if (equipmentManager != null)
        {
            // Fire Slime มี resistance ต่อ Burn แต่อ่อนแอต่อ Freeze
        }

        Debug.Log($"🔥 {CharacterName} spawned with fire abilities!");
    }

    private void InitializeFireEffects()
    {
        // เปิดเอฟเฟกต์ไฟ
        if (fireAuraEffect != null)
            fireAuraEffect.Play();

        if (fireLight != null)
        {
            fireLight.color = Color.red;
            fireLight.intensity = 1.5f;
            fireLight.range = burnRadius;
        }

        // ตั้งค่าสีของ slime เป็นสีแดง/ส้ม
        Renderer slimeRenderer = GetComponent<Renderer>();
        if (slimeRenderer != null)
        {
            slimeRenderer.material.color = Color.red;
            slimeRenderer.material.SetColor("_EmissionColor", Color.red * 0.5f);
        }
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (HasStateAuthority && !IsDead)
        {
            ProcessBurnAura();
        }
    }

    private void ProcessBurnAura()
    {
        if (Runner.SimulationTime >= nextBurnTime)
        {
            nextBurnTime = Runner.SimulationTime + burnTickInterval;

            // หาผู้เล่นในรัศมี burn
            Collider[] nearbyTargets = Physics.OverlapSphere(transform.position, burnRadius, LayerMask.GetMask("Player"));

            foreach (Collider target in nearbyTargets)
            {
                Hero hero = target.GetComponent<Hero>();
                if (hero != null && !hero.HasStatusEffect(StatusEffectType.Burn))
                {
                    // ใส่ Burn effect
                    hero.ApplyStatusEffect(StatusEffectType.Burn, burnDamage, 6f);

                    Debug.Log($"🔥 {CharacterName} burned {hero.CharacterName}!");

                    // เอฟเฟกต์การเผาไหม้
                    RPC_ShowBurnEffect(hero.transform.position);
                }
            }
        }
    }

    private IEnumerator CreateHeatZonesRoutine()
    {
        while (!IsDead)
        {
            yield return new WaitForSeconds(3f);

            if (heatZonePrefab != null)
            {
                // สร้างพื้นที่ร้อนที่ตำแหน่งปัจจุบัน
                Vector3 heatZonePosition = transform.position;
                GameObject heatZone = Instantiate(heatZonePrefab, heatZonePosition, Quaternion.identity);
                activeHeatZones.Add(heatZone);

                // ทำลายพื้นที่ร้อนหลังจากเวลาผ่านไป
                StartCoroutine(DestroyHeatZoneAfterTime(heatZone, fireTrailDuration));

                Debug.Log($"🔥 {CharacterName} created heat zone at {heatZonePosition}");
            }
        }
    }

    private IEnumerator DestroyHeatZoneAfterTime(GameObject heatZone, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (heatZone != null)
        {
            activeHeatZones.Remove(heatZone);
            Destroy(heatZone);
        }
    }

    // Override การโจมตี เพื่อเพิ่ม Burn effect
    protected override void TryAttackTarget()
    {
        if (targetTransform == null) return;

        float distance = Vector3.Distance(transform.position, targetTransform.position);

        bool canAttack = CurrentState == EnemyState.Attacking &&
                         distance <= AttackRange &&
                         distance >= minDistanceToPlayer * 0.5f &&
                         Runner.SimulationTime >= nextAttackTime;

        if (canAttack)
        {
            float effectiveAttackSpeed = GetEffectiveAttackSpeed();
            float finalAttackCooldown = AttackCooldown / Mathf.Max(0.1f, effectiveAttackSpeed);
            nextAttackTime = Runner.SimulationTime + finalAttackCooldown;

            RPC_FireSlimeAttack(CurrentTarget);

            if (showDebugInfo)
            {
                Debug.Log($"🔥 {CharacterName}: FIRE ATTACK! Distance: {distance:F2}");
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_FireSlimeAttack(PlayerRef targetPlayer)
    {
        Hero targetHero = null;
        Hero[] heroes = FindObjectsOfType<Hero>();

        foreach (Hero hero in heroes)
        {
            if (hero.Object != null && hero.Object.InputAuthority == targetPlayer)
            {
                targetHero = hero;
                break;
            }
        }

        if (targetHero != null)
        {
            Debug.Log($"🔥 {CharacterName} performs fire attack on {targetHero.CharacterName}!");

            // ดาเมจปกติ + โอกาส Burn
            targetHero.TakeDamageFromAttacker(0, MagicDamage, this, DamageType.Magic);

            // 70% โอกาสใส่ Burn effect
            if (Random.Range(0f, 100f) <= 70f)
            {
                targetHero.ApplyStatusEffect(StatusEffectType.Burn, burnDamage, 8f);
                Debug.Log($"🔥 {CharacterName} applied Burn to {targetHero.CharacterName}!");

                RPC_ShowBurnEffect(targetHero.transform.position);
            }
        }
    }

    // Override การเรียก OnSuccessfulAttack
    public new void OnSuccessfulAttack(Character target)
    {
        if (!HasStateAuthority) return;

        // Fire Slime มีโอกาส 80% ใส่ Burn
        if (Random.Range(0f, 100f) <= 80f)
        {
            target.ApplyStatusEffect(StatusEffectType.Burn, burnDamage + 2, 10f);
            Debug.Log($"🔥 {CharacterName} applied enhanced Burn to {target.CharacterName}!");
        }
    }

    // Override การตาย - ระเบิดไฟ!
    protected override void RPC_OnDeath()
    {
        Debug.Log($"🔥 {CharacterName} is exploding!");

        if (HasStateAuthority)
        {
            // ระเบิดไฟใส่ทุกคนในรัศมี
            ExplodeOnDeath();
        }

        // ทำการตายปกติ
        base.RPC_OnDeath();
    }

    private void ExplodeOnDeath()
    {
        // หาทุกตัวละครในรัศมีระเบิด
        Collider[] targets = Physics.OverlapSphere(transform.position, explodeRadius, LayerMask.GetMask("Player"));

        foreach (Collider target in targets)
        {
            Hero hero = target.GetComponent<Hero>();
            if (hero != null)
            {
                // ดาเมจระเบิด + Burn effect
                hero.TakeDamage(explodeDamage, DamageType.Magic);
                hero.ApplyStatusEffect(StatusEffectType.Burn, burnDamage * 2, 12f);

                Debug.Log($"💥🔥 {CharacterName} explosion hit {hero.CharacterName} for {explodeDamage} damage!");
            }
        }

        // แสดงเอฟเฟกต์การระเบิด
        RPC_ShowExplosionEffect(transform.position);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowBurnEffect(Vector3 position)
    {
        // สร้างเอฟเฟกต์การเผาไหม้
        if (fireTrailEffect != null)
        {
            GameObject burnFX = Instantiate(fireTrailEffect.gameObject, position, Quaternion.identity);
            Destroy(burnFX, 2f);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowExplosionEffect(Vector3 position)
    {
        // สร้างเอฟเฟกต์การระเบิด
        if (explodeEffect != null)
        {
            GameObject explosionFX = Instantiate(explodeEffect.gameObject, position, Quaternion.identity);
            Destroy(explosionFX, 3f);
        }

        // สร้างแสงระเบิด
        GameObject lightObj = new GameObject("ExplosionLight");
        lightObj.transform.position = position;
        Light explosionLight = lightObj.AddComponent<Light>();
        explosionLight.color = Color.yellow;
        explosionLight.intensity = 3f;
        explosionLight.range = explodeRadius * 2f;

        // ค่อยๆ หรี่แสง
        StartCoroutine(FadeExplosionLight(explosionLight, lightObj));
    }

    private IEnumerator FadeExplosionLight(Light light, GameObject lightObj)
    {
        float duration = 2f;
        float startIntensity = light.intensity;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            if (light != null)
            {
                light.intensity = Mathf.Lerp(startIntensity, 0f, t / duration);
            }
            yield return null;
        }

        if (lightObj != null)
            Destroy(lightObj);
    }

    protected override void OnDestroy()
    {
        // ทำลายพื้นที่ร้อนทั้งหมด
        foreach (GameObject heatZone in activeHeatZones)
        {
            if (heatZone != null)
                Destroy(heatZone);
        }
        activeHeatZones.Clear();

        if (heatZoneCoroutine != null)
            StopCoroutine(heatZoneCoroutine);

        base.OnDestroy();
    }

    // Debug Gizmos
    private void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // วาดรัศมี burn
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, burnRadius);

        // วาดรัศมีระเบิด
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explodeRadius);
    }
}