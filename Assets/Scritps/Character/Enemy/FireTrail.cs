using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireTrail : MonoBehaviour
{
    private int burnDamagePerTick;
    private float burnDuration;
    private float lifetime;
    private HashSet<Hero> affectedHeroes = new HashSet<Hero>();
    private bool isInitialized = false;

    public void Initialize(int burnDmg, float burnDur, float life)
    {
        burnDamagePerTick = burnDmg;
        burnDuration = burnDur;
        lifetime = life;
        isInitialized = true;
        Debug.Log($"🔥 Fire Trail initialized: Burn {burnDamagePerTick} dmg/tick, Duration {burnDuration}s, Lifetime {lifetime}s");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isInitialized) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Hero hero = other.GetComponent<Hero>();
            if (hero != null && !affectedHeroes.Contains(hero))
            {
                hero.ApplyStatusEffect(StatusEffectType.Burn, burnDamagePerTick, burnDuration);
                affectedHeroes.Add(hero);
                Debug.Log($"🔥 Fire Trail burned {hero.CharacterName}!");

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