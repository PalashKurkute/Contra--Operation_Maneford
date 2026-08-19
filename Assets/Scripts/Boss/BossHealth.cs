using UnityEngine;
using System;

// The boss's health. Player bullets damage it (IDamageable); it tells
// the controller, the health bar and the story when the phase changes
// or the boss dies.
public class BossHealth : MonoBehaviour, IDamageable
{
    public int maxHealth = 40;

    // Phase two starts at or below this share of health
    [Range(0f, 1f)]
    public float phaseTwoAt = 0.5f;

    // Parts that flash white when hit
    public HitFlash[] flashes;

    // While true, bullets hit but do no damage (intro, death)
    public bool invulnerable = true;

    public event Action<float> OnHealthChanged;
    public event Action<int> OnPhaseChange;
    public event Action OnDeath;

    public int Health { get; private set; }
    public int Phase { get; private set; }

    public float Fraction
    {
        get { return (float)Health / maxHealth; }
    }

    public bool IsAlive
    {
        get { return Health > 0; }
    }

    void Awake()
    {
        Health = maxHealth;
        Phase = 1;
    }

    public void TakeDamage(int damage)
    {
        if (Health <= 0 || invulnerable)
            return;

        Health = Mathf.Max(0, Health - damage);

        foreach (HitFlash flash in flashes)
        {
            if (flash != null)
            {
                flash.Flash();
            }
        }

        if (OnHealthChanged != null)
        {
            OnHealthChanged(Fraction);
        }

        if (Phase == 1 && Health > 0 && Fraction <= phaseTwoAt)
        {
            Phase = 2;

            if (OnPhaseChange != null)
            {
                OnPhaseChange(2);
            }
        }

        if (Health == 0 && OnDeath != null)
        {
            OnDeath();
        }
    }
}
