using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceBossClone : MonoBehaviour
{
    private Animator animator;
    private HashSet<Hero> hitHeroes = new HashSet<Hero>(); // ✅ เพิ่มตัวนี้

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    public IEnumerator PerformDash(Vector3 direction, float dashDistance, float dashDuration, float dashWidth, IceBoss boss)
    {
        Debug.Log($"🧊 Clone dashing in direction {direction}");

        // ✅ Reset hit list
        hitHeroes.Clear();

        Vector3 startPos = transform.position;
        direction.y = 0;

        // Check wall
        float actualDashDistance = dashDistance;
        RaycastHit wallHit;

        if (Physics.Raycast(
            startPos + Vector3.up * 0.5f,
            direction,
            out wallHit,
            dashDistance,
            LayerMask.GetMask("Wall", "Obstacle", "Default")))
        {
            actualDashDistance = Mathf.Max(0.5f, wallHit.distance - 0.5f);
        }

        Vector3 endPos = startPos + direction * actualDashDistance;

        // Play animation
        if (animator != null)
        {
            animator.SetTrigger("skill1");
        }

        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / dashDuration;
            float smoothProgress = Mathf.Sin(progress * Mathf.PI * 0.5f);

            Vector3 targetPos = Vector3.Lerp(startPos, endPos, smoothProgress);
            transform.position = targetPos;

            // Check collision
            CheckDashCollision(targetPos, direction, dashWidth, boss);

            yield return null;
        }

        transform.position = endPos;

        Debug.Log($"🧊 Clone dash complete - DESTROYING IMMEDIATELY!");

        // ✅ ตายทันทีหลัง Dash เสร็จ (ไม่รอ)
        Destroy(gameObject);
    }

    // ✅ เพิ่ม Method สำหรับ Clone Slow AoE
    private IEnumerator CreateCloneDashEndSlowAoE(Vector3 position, IceBoss boss)
    {
        // ใช้การตั้งค่าจาก Boss
        float aoeRadius = 4f; // เล็กกว่า Boss นิดหน่อย
        float aoeDuration = 3f;
        float slowAmount = 0.3f; // 30% slow
        float slowDuration = 2f;

        Debug.Log($"🧊 Clone creating Slow AoE at {position}");

        // สร้าง Visual (simple)
      

       

        // ทำ Slow ต่อเนื่อง
        float elapsed = 0f;
        float nextTickTime = 0f;
        float tickInterval = 0.5f;

        while (elapsed < aoeDuration)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= nextTickTime)
            {
                Collider[] targets = Physics.OverlapSphere(position, aoeRadius, LayerMask.GetMask("Player"));

                foreach (Collider target in targets)
                {
                    Hero hero = target.GetComponent<Hero>();
                    if (hero != null)
                    {
                        hero.ApplyStatusEffect(StatusEffectType.Slow, 0, slowDuration, slowAmount);
                        Debug.Log($"🧊 Clone Slow AoE affected {hero.CharacterName}!");
                    }
                }

                nextTickTime += tickInterval;
            }

            yield return null;
        }

        // ทำลาย AoE
       

        Debug.Log($"🧊 Clone Slow AoE ended");
    }

    private void CheckDashCollision(Vector3 currentPos, Vector3 direction, float dashWidth, IceBoss boss)
    {
        Vector3 boxSize = new Vector3(dashWidth, 2f, 1f);
        Collider[] hits = Physics.OverlapBox(
            currentPos,
            boxSize * 0.5f,
            Quaternion.LookRotation(direction),
            LayerMask.GetMask("Player")
        );

        foreach (Collider hit in hits)
        {
            Hero hero = hit.GetComponent<Hero>();
            // ✅ เช็คว่าโดนแล้วหรือยัง
            if (hero != null && !hitHeroes.Contains(hero))
            {
                hitHeroes.Add(hero); // ✅ เพิ่มเข้า list

                hero.TakeDamageFromAttacker(boss.AttackDamage, 0, boss, DamageType.Normal);

                Vector3 knockbackDirection = (hero.transform.position - transform.position).normalized;
                hero.ApplyKnockback(knockbackDirection, 30f, 0.6f);

                Debug.Log($"🧊 Clone dash hit {hero.CharacterName}!");
            }
        }
    }

    public IEnumerator PerformIceProjectile(
        Vector3 targetPosition,
        float travelTime,
        float radius,
        GameObject projectilePrefab,
        ParticleSystem effect,
        IceBoss boss,
        float freezeDuration)
    {
        Debug.Log($"🧊 Clone shooting ice projectile at {targetPosition}");

        // Play animation
        if (animator != null)
        {
            animator.SetTrigger("attack");
        }

        yield return new WaitForSeconds(0.3f);

        Vector3 startPos = transform.position + Vector3.up * 1f;
        Vector3 endPos = new Vector3(targetPosition.x, targetPosition.y, targetPosition.z);

        GameObject projectile = null;
        if (projectilePrefab != null)
        {
            projectile = Instantiate(projectilePrefab, startPos, Quaternion.identity);
        }

        float elapsed = 0f;

        while (elapsed < travelTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / travelTime;

            if (projectile != null)
            {
                projectile.transform.position = Vector3.Lerp(startPos, endPos, progress);
            }

            yield return null;
        }

        if (projectile != null)
        {
            Destroy(projectile);
        }

        // ✅ แก้ไข: ให้ Effect หายไป
        if (effect != null)
        {
            ParticleSystem fx = Instantiate(effect, targetPosition, Quaternion.identity);
            Destroy(fx.gameObject, 2f); // ✅ เพิ่มบรรทัดนี้
        }

        Collider[] targets = Physics.OverlapSphere(targetPosition, radius, LayerMask.GetMask("Player"));

        foreach (Collider target in targets)
        {
            Hero hero = target.GetComponent<Hero>();
            if (hero != null)
            {
                hero.TakeDamageFromAttacker(0, boss.MagicDamage, boss, DamageType.Magic);
                hero.ApplyStatusEffect(StatusEffectType.Freeze, 0, freezeDuration);
                Debug.Log($"🧊 Clone projectile hit {hero.CharacterName}!");
            }
        }

        Debug.Log($"🧊 Clone projectile complete");

        // Clone disappears after shooting
        Destroy(gameObject, 0.5f);
    }
}