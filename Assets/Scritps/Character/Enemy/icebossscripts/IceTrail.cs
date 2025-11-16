using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceTrail : MonoBehaviour
{
    private float slowAmount;
    private float slowDuration;
    private float lifetime;
    private HashSet<Hero> affectedHeroes = new HashSet<Hero>();
    private bool isInitialized = false; // ✅ เพิ่มตัวนี้

    public void Initialize(float slowAmt, float slowDur, float life)
    {
        slowAmount = slowAmt;
        slowDuration = slowDur;
        lifetime = life;
        isInitialized = true; // ✅ เพิ่มบรรทัดนี้

        Debug.Log($"🧊 Ice Trail initialized: Slow {slowAmount * 100}%, Duration {slowDuration}s, Lifetime {lifetime}s");
    }

    private void OnTriggerEnter(Collider other)
    {
        // ✅ เปลี่ยนเป็น OnTriggerEnter แทน OnTriggerStay
        if (!isInitialized) return; // ✅ เช็คว่า Initialize แล้วหรือยัง

        // ✅ ป้องกันโดนซ้ำ
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Hero hero = other.GetComponent<Hero>();
            if (hero != null && !affectedHeroes.Contains(hero))
            {
                hero.ApplyStatusEffect(StatusEffectType.Slow, 0, slowDuration, slowAmount);
                affectedHeroes.Add(hero);

                Debug.Log($"🧊 Ice Trail slowed {hero.CharacterName}!");

                // ✅ Reset หลัง 1 วินาที (ให้โดนใหม่ได้ถ้ายังอยู่ในพื้นที่)
                StartCoroutine(ResetHeroAfterDelay(hero, 1f));
            }
        }
    }

    private IEnumerator ResetHeroAfterDelay(Hero hero, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (affectedHeroes.Contains(hero))
        {
            affectedHeroes.Remove(hero);
        }
    }
}