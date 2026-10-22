using System;
using UnityEngine;

namespace MadLab
{
    public enum LabFaction { Player, Enemy }

    [DisallowMultipleComponent]
    public sealed class LabHealth : MonoBehaviour
    {
        [SerializeField] private LabFaction faction;
        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        private LabPlayerDodge dodge;
        public LabFaction Faction => faction;
        public float MaximumHealth => maximumHealth;
        public float CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0f;
        public event Action<float> Damaged;
        public event Action<LabHealth> Died;

        private void Awake()
        {
            CurrentHealth = Mathf.Max(1f, maximumHealth);
            dodge = GetComponent<LabPlayerDodge>();
        }

        // The only damage entry point. All attackers share faction, death and invulnerability checks.
        public bool TryTakeDamage(float amount, LabHealth source)
        {
            if (!isActiveAndEnabled || IsDead || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount))
                return false;
            if (source != null && (source == this || source.IsDead || source.Faction == faction))
                return false;
            if (dodge != null && dodge.IsInvulnerable) return false;
            float applied = Mathf.Min(CurrentHealth, amount);
            CurrentHealth = Mathf.Max(0f, CurrentHealth - applied);
            Damaged?.Invoke(applied);
            if (IsDead) Died?.Invoke(this); // Subsequent calls are rejected, so death is emitted exactly once.
            return true;
        }
    }
}
