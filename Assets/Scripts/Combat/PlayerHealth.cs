using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxHealth = 100f;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0f;

    public event Action <float, float> HealthChanged;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void ApplyDamage(float amount)
    {
        if (amount <= 0f || IsDead)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    [ContextMenu("Reset Health")]
    public void ResetHealth()
    {
        CurrentHealth = maxHealth;
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}