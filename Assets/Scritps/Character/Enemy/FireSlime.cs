using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class FireSlime : NetworkEnemy
{
    [Header("🔥 Fire Slime Settings")]
    [SerializeField] private float burnRadius = 3f;           // รัศมีการเผาไหม้
    [SerializeField] private float burnTickInterval = 1f;      // ช่วงเวลาการเผาไหม้
    [SerializeField] private int burnDamage = 30;              // ดาเมจการเผาไหม้
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

    [Header("⚠️ Telegraph System")]
    [SerializeField] private GameObject telegraphIndicatorPrefab; // Prefab สำหรับแสดงพื้นแดง (ถ้าไม่มีจะสร้างอัตโนมัติ)
    [SerializeField] private float fireAuraTelegraphDuration = 1.5f; // เวลาแสดงเตือนก่อน Fire Aura
    [SerializeField] private float deathExplosionTelegraphDuration = 2f; // เวลาแสดงเตือนก่อนระเบิด
    [SerializeField] private float fireAuraCooldown = 8f;     // Cooldown ของ Fire Aura (วินาที)
    [SerializeField] private float fireAuraActiveDuration = 3f; // ระยะเวลาที่ Fire Aura ทำงาน
    [SerializeField] private Color telegraphColor = new Color(1f, 0f, 0f, 0.5f); // สีของ Telegraph (แดง โปร่งใส)
    [SerializeField] private Color explosionTelegraphColor = new Color(1f, 0.5f, 0f, 0.6f); // สีของ Telegraph ระเบิด (ส้ม)

    private float nextBurnTime = 0f;
    private float nextFireAuraTime = 0f;
    private bool isFireAuraActive = false;
    private List<GameObject> activeHeatZones = new List<GameObject>();
    private Coroutine heatZoneCoroutine;
    private GameObject currentTelegraphIndicator;

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
            ProcessFireAura();
        }
    }

    // ระบบ Fire Aura ใหม่ที่มี Telegraph และ Cooldown
    private void ProcessFireAura()
    {
        // ตรวจสอบว่าพร้อมใช้ Fire Aura หรือยัง
        if (!isFireAuraActive && Runner.SimulationTime >= nextFireAuraTime)
        {
            // เริ่มทำ Fire Aura พร้อม Telegraph
            StartCoroutine(FireAuraSequence());
        }
    }

    private IEnumerator FireAuraSequence()
    {
        isFireAuraActive = true;

        // ========== ขั้นที่ 1: แสดง Telegraph ==========
        Debug.Log($"⚠️ {CharacterName} is charging Fire Aura!");

        // แสดง Telegraph Indicator บนพื้น
        RPC_ShowTelegraph(transform.position, burnRadius, telegraphColor, fireAuraTelegraphDuration);

        // หยุดเคลื่อนที่ขณะเตรียมโจมตี
        bool wasMoving = CurrentState == EnemyState.Chasing;
        if (wasMoving)
        {
            // สามารถเพิ่ม animation หรือหยุดการเคลื่อนที่ชั่วคราว
        }

        // รอจนครบเวลา Telegraph
        yield return new WaitForSeconds(fireAuraTelegraphDuration);

        // ========== ขั้นที่ 2: เปิดใช้งาน Fire Aura ==========
        Debug.Log($"🔥 {CharacterName} activated Fire Aura!");

        // เพิ่มเอฟเฟกต์พิเศษเมื่อเริ่ม Aura
        if (fireAuraEffect != null)
        {
            fireAuraEffect.Play();
        }

        // ทำ Burn Damage ซ้ำๆ ตาม duration
        float auraEndTime = Time.time + fireAuraActiveDuration;
        nextBurnTime = Time.time;

        while (Time.time < auraEndTime && !IsDead)
        {
            if (Time.time >= nextBurnTime)
            {
                nextBurnTime = Time.time + burnTickInterval;
                ApplyBurnDamageToNearbyPlayers();
            }
            yield return null;
        }

        // ========== ขั้นที่ 3: ปิด Aura และเข้า Cooldown ==========
        Debug.Log($"💤 {CharacterName} Fire Aura ended. Entering cooldown...");

        isFireAuraActive = false;
        nextFireAuraTime = Runner.SimulationTime + fireAuraCooldown;
    }

    private void ApplyBurnDamageToNearbyPlayers()
    {
        // หาผู้เล่นในรัศมี burn
        Collider[] nearbyTargets = Physics.OverlapSphere(transform.position, burnRadius, LayerMask.GetMask("Player"));

        foreach (Collider target in nearbyTargets)
        {
            Hero hero = target.GetComponent<Hero>();
            if (hero != null)
            {
                // ใส่ Burn effect
                hero.ApplyStatusEffect(StatusEffectType.Burn, burnDamage, 6f);

                Debug.Log($"🔥 {CharacterName} burned {hero.CharacterName}!");

                // เอฟเฟกต์การเผาไหม้
                RPC_ShowBurnEffect(hero.transform.position);
            }
        }
    }

    // แสดง Telegraph Indicator (พื้นแดง)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowTelegraph(Vector3 position, float radius, Color color, float duration)
    {
        GameObject indicator;

        // ถ้ามี Prefab ให้ใช้ Prefab
        if (telegraphIndicatorPrefab != null)
        {
            indicator = Instantiate(telegraphIndicatorPrefab, position, Quaternion.identity);
            indicator.transform.localScale = Vector3.one * radius * 2f;
        }
        else
        {
            // ถ้าไม่มี สร้างแบบ procedural (วงกลมแบน)
            indicator = CreateProceduralTelegraph(position, radius, color);
        }

        // เก็บ reference ของ indicator ปัจจุบัน
        if (currentTelegraphIndicator != null)
        {
            Destroy(currentTelegraphIndicator);
        }
        currentTelegraphIndicator = indicator;

        // ทำให้ค่อยๆ จางหายหรือกระพริบ
        StartCoroutine(AnimateTelegraph(indicator, duration, color));
    }

    private GameObject CreateProceduralTelegraph(Vector3 position, float radius, Color color)
    {
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        indicator.name = "TelegraphIndicator";

        // ปรับตำแหน่งและขนาด
        indicator.transform.position = position + Vector3.up * 0.1f; // ยกขึ้นเล็กน้อยเพื่อไม่ให้ชนพื้น
        indicator.transform.localScale = new Vector3(radius * 2f, 0.05f, radius * 2f);

        // ลบ Collider เพราะไม่ต้องการให้ชนได้
        Destroy(indicator.GetComponent<Collider>());

        // ตั้งค่า Material
        Renderer renderer = indicator.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Mode", 3); // Transparent mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;

            renderer.material = mat;
        }

        return indicator;
    }

    private IEnumerator AnimateTelegraph(GameObject indicator, float duration, Color baseColor)
    {
        Renderer renderer = indicator.GetComponent<Renderer>();
        float elapsed = 0f;

        while (elapsed < duration && indicator != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            if (renderer != null)
            {
                // กระพริบเร็วขึ้นเมื่อใกล้หมดเวลา
                float pulseSpeed = Mathf.Lerp(2f, 8f, progress);
                float pulse = Mathf.Sin(elapsed * pulseSpeed) * 0.3f + 0.7f;

                Color newColor = baseColor;
                newColor.a = baseColor.a * pulse;
                renderer.material.color = newColor;
            }

            yield return null;
        }

        // ทำลาย indicator หลังจบเวลา
        if (indicator != null)
        {
            Destroy(indicator);
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

    // Override การตาย - ระเบิดไฟ พร้อม Telegraph!
    protected override void RPC_OnDeath()
    {
        Debug.Log($"🔥 {CharacterName} is about to explode!");

        if (HasStateAuthority)
        {
            // เริ่ม Telegraph แล้วค่อยระเบิด
            StartCoroutine(DeathExplosionSequence());
        }
        else
        {
            // สำหรับ client ให้ทำการตายปกติ
            base.RPC_OnDeath();
        }
    }

    private IEnumerator DeathExplosionSequence()
    {
        // ========== ขั้นที่ 1: แสดง Telegraph ก่อนระเบิด ==========
        Debug.Log($"⚠️💀 {CharacterName} is charging death explosion!");

        // แสดง Telegraph สีส้มระเบิด
        RPC_ShowTelegraph(transform.position, explodeRadius, explosionTelegraphColor, deathExplosionTelegraphDuration);

        // เปลี่ยนสีของ Fire Slime เป็นสีส้มแดง (กำลังจะระเบิด)
        RPC_ChangeColorBeforeExplosion();

        // รอจนครบเวลา Telegraph
        yield return new WaitForSeconds(deathExplosionTelegraphDuration);

        // ========== ขั้นที่ 2: ระเบิด! ==========
        Debug.Log($"💥🔥 {CharacterName} EXPLODED!");
        ExplodeOnDeath();

        // ========== ขั้นที่ 3: ทำการตายจริง ==========
        base.RPC_OnDeath();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ChangeColorBeforeExplosion()
    {
        // เปลี่ยนสีเป็นส้มแดงสว่าง เพื่อบอกว่ากำลังจะระเบิด
        Renderer slimeRenderer = GetComponent<Renderer>();
        if (slimeRenderer != null)
        {
            slimeRenderer.material.color = Color.Lerp(Color.red, Color.yellow, 0.5f);
            slimeRenderer.material.SetColor("_EmissionColor", Color.yellow * 2f);
        }

        // เพิ่มความสว่างของไฟ
        if (fireLight != null)
        {
            fireLight.color = Color.yellow;
            fireLight.intensity = 3f;
        }

        // เพิ่มเสียงหรือเอฟเฟกต์พิเศษ
        if (fireAuraEffect != null)
        {
            var main = fireAuraEffect.main;
            main.startColor = Color.yellow;
        }
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

    protected virtual void OnDestroy()
    {
        // ทำลายพื้นที่ร้อนทั้งหมด
        foreach (GameObject heatZone in activeHeatZones)
        {
            if (heatZone != null)
                Destroy(heatZone);
        }
        activeHeatZones.Clear();

        // ทำลาย Telegraph indicator ถ้ายังมีอยู่
        if (currentTelegraphIndicator != null)
        {
            Destroy(currentTelegraphIndicator);
        }

        if (heatZoneCoroutine != null)
            StopCoroutine(heatZoneCoroutine);

        base.OnDestroy();
    }

    // Debug Gizmos สำหรับแสดงรัศมี Fire Aura และ Explosion
    private void OnDrawGizmosSelected()
    {
        // วงกลม Fire Aura
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, burnRadius);

        // วงกลม Death Explosion
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, explodeRadius);
    }
}