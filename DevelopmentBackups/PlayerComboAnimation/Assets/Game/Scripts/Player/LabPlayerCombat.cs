using System.Collections.Generic;
using UnityEngine;

namespace MadLab
{
    [DefaultExecutionOrder(20)]
    [RequireComponent(typeof(LabHealth), typeof(LabPlayerInput), typeof(LabPlayerDodge))]
    [DisallowMultipleComponent]
    public sealed class LabPlayerCombat : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float attackRange = 2.2f;
        [SerializeField, Range(1f, 180f)] private float attackAngle = 100f;
        [SerializeField, Min(0.01f)] private float attackCooldown = 0.55f;
        [SerializeField, Min(0f)] private float damage = 15f;
        [SerializeField] private LayerMask targetMask = ~0;

        private LabPlayerInput input;
        private LabPlayerDodge dodge;
        private LabHealth health;
        private LabCombatFeedback feedback;
        private float nextAttackTime;
        private readonly HashSet<LabHealth> struck = new HashSet<LabHealth>();
        public int AttackCount { get; private set; }
        public int LastHitCount { get; private set; }

        private void Awake()
        {
            input = GetComponent<LabPlayerInput>();
            dodge = GetComponent<LabPlayerDodge>();
            health = GetComponent<LabHealth>();
            feedback = GetComponent<LabCombatFeedback>();
        }
        private void OnEnable() { health.Died += OnDeath; }
        private void OnDisable() { if (health != null) health.Died -= OnDeath; }
        private void Update()
        {
            if (input.AttackPressed) TryAttack();
        }

        public bool TryAttack()
        {
            if (!isActiveAndEnabled || health.IsDead || !input.HasControl || dodge.IsDodging || Time.time < nextAttackTime)
                return false;
            nextAttackTime = Time.time + attackCooldown;
            AttackCount++;
            LastHitCount = 0;
            struck.Clear();
            if (feedback != null) feedback.PlaySwing();
            Vector3 center = transform.position + Vector3.up * 0.9f;
            // Allocates only once per attack; unlike a fixed buffer this cannot silently omit nearby enemies.
            Collider[] candidates = Physics.OverlapSphere(center, attackRange, targetMask, QueryTriggerInteraction.Collide);
            foreach (Collider candidate in candidates)
            {
                LabHealth target = candidate.GetComponentInParent<LabHealth>();
                if (target == null || target == health || target.IsDead || target.Faction != LabFaction.Enemy ||
                    !struck.Add(target)) continue;
                if (LabMeleeQuery.CanReach(transform, target.transform, attackRange, attackAngle) &&
                    target.TryTakeDamage(damage, health)) LastHitCount++;
            }
            return true;
        }

        private void OnDeath(LabHealth dead)
        {
            dodge.Cancel();
            var motor = GetComponent<LabPlayerMotor>();
            if (motor != null) motor.enabled = false;
            dodge.enabled = false;
            input.enabled = false; // Clears values and releases the cursor.
            var controller = GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            enabled = false;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = transform.position + Vector3.up * 0.9f;
            Gizmos.color = new Color(0.1f, 0.9f, 0.9f, 0.8f);
            Gizmos.DrawWireSphere(center, attackRange);
            Gizmos.DrawLine(center, center + Quaternion.AngleAxis(-attackAngle / 2f, Vector3.up) * transform.forward * attackRange);
            Gizmos.DrawLine(center, center + Quaternion.AngleAxis(attackAngle / 2f, Vector3.up) * transform.forward * attackRange);
        }
    }
}
