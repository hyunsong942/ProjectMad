using System;
using System.Collections.Generic;
using UnityEngine;

namespace MadLab
{
    [DefaultExecutionOrder(20)]
    [RequireComponent(typeof(LabHealth), typeof(LabPlayerInput), typeof(LabPlayerDodge))]
    [DisallowMultipleComponent]
    public sealed class LabPlayerCombat : MonoBehaviour
    {
        [Serializable]
        public sealed class ComboStrike
        {
            public AnimationClip clip;
            [Min(0.1f)] public float playbackSpeed = 1.6f;
            [Range(0.01f, 0.99f)] public float hitNormalizedTime = 0.5f;
        }

        [SerializeField, Min(0.1f)] private float attackRange = 2.2f;
        [SerializeField, Range(1f, 180f)] private float attackAngle = 100f;
        [SerializeField, Min(0f)] private float damage = 15f;
        [SerializeField] private LayerMask targetMask = ~0;
        [SerializeField] private ComboStrike[] strikes = new ComboStrike[3];
        [SerializeField, Min(0f)] private float comboGraceTime = 0.25f;
        [SerializeField, Min(0f)] private float finisherRecovery = 0.12f;

        private LabPlayerInput input;
        private LabPlayerDodge dodge;
        private LabHealth health;
        private LabCombatFeedback feedback;
        private float stepStartedAt, stepDuration, comboExpiresAt, nextAttackTime;
        private bool hitApplied;
        private int queuedClicks;
        private readonly HashSet<LabHealth> struck = new HashSet<LabHealth>();

        public bool IsAttacking { get; private set; }
        public int CurrentComboStep { get; private set; }
        public int QueuedClicks => queuedClicks;
        public int AttackCount { get; private set; }
        public int LastHitCount { get; private set; }
        public int ResolvedStrikeCount { get; private set; }
        public float CurrentPlaybackSpeed => CurrentComboStep > 0 ? strikes[CurrentComboStep - 1].playbackSpeed : 1f;
        public float CurrentStepDuration => stepDuration;
        public float CurrentStepProgress => IsAttacking ? Mathf.Clamp01((Time.time - stepStartedAt) / stepDuration) : 0f;

        private void Awake()
        {
            input = GetComponent<LabPlayerInput>();
            dodge = GetComponent<LabPlayerDodge>();
            health = GetComponent<LabHealth>();
            feedback = GetComponent<LabCombatFeedback>();
            if (strikes == null || strikes.Length != 3 || Array.Exists(strikes, s => s == null || s.clip == null))
            {
                Debug.LogError("LabPlayerCombat needs exactly three combo animation clips.", this);
                enabled = false;
            }
        }
        private void OnEnable() { if (health != null) health.Died += OnDeath; }
        private void OnDisable()
        {
            if (health != null) health.Died -= OnDeath;
            CancelCombo();
        }
        private void Update()
        {
            if (health.IsDead || !input.HasControl || dodge.IsDodging)
            {
                CancelCombo();
                return;
            }

            if (input.AttackPressed) TryAttack();
            if (!IsAttacking)
            {
                if (CurrentComboStep != 0 && Time.time > comboExpiresAt) CurrentComboStep = 0;
                return;
            }

            float elapsed = Time.time - stepStartedAt;
            ComboStrike strike = strikes[CurrentComboStep - 1];
            if (!hitApplied && elapsed >= stepDuration * strike.hitNormalizedTime)
            {
                hitApplied = true; // Damage is resolved once, at this strike's contact frame.
                ResolveHit();
            }
            if (elapsed >= stepDuration)
            {
                if (queuedClicks > 0 && CurrentComboStep < 3)
                {
                    queuedClicks--;
                    BeginStrike(CurrentComboStep + 1);
                }
                else
                {
                    IsAttacking = false;
                    queuedClicks = 0;
                    if (CurrentComboStep < 3) comboExpiresAt = Time.time + comboGraceTime;
                    else
                    {
                        CurrentComboStep = 0;
                        nextAttackTime = Time.time + finisherRecovery;
                    }
                }
            }
        }

        // Each press adds at most one strike. Holding the mouse never calls this repeatedly.
        public bool TryAttack()
        {
            if (!isActiveAndEnabled || health.IsDead || !input.HasControl || dodge.IsDodging || Time.time < nextAttackTime)
                return false;
            if (IsAttacking)
            {
                if (queuedClicks >= 3 - CurrentComboStep) return false;
                queuedClicks++;
                return true;
            }

            int next = CurrentComboStep > 0 && CurrentComboStep < 3 && Time.time <= comboExpiresAt
                ? CurrentComboStep + 1 : 1;
            queuedClicks = 0;
            BeginStrike(next);
            return true;
        }

        private void BeginStrike(int step)
        {
            CurrentComboStep = step;
            ComboStrike strike = strikes[step - 1];
            stepDuration = Mathf.Max(0.01f, strike.clip.length / Mathf.Max(0.1f, strike.playbackSpeed));
            stepStartedAt = Time.time;
            hitApplied = false;
            IsAttacking = true;
            LastHitCount = 0;
            AttackCount++;
        }

        public void CancelCombo()
        {
            IsAttacking = false;
            CurrentComboStep = 0;
            queuedClicks = 0;
            comboExpiresAt = 0f;
            hitApplied = false;
        }

        private void ResolveHit()
        {
            ResolvedStrikeCount++;
            LastHitCount = 0;
            struck.Clear();
            if (feedback != null) feedback.PlaySwing();
            Vector3 center = transform.position + Vector3.up * 0.9f;
            Collider[] candidates = Physics.OverlapSphere(center, attackRange, targetMask, QueryTriggerInteraction.Collide);
            foreach (Collider candidate in candidates)
            {
                LabHealth target = candidate.GetComponentInParent<LabHealth>();
                if (target == null || target == health || target.IsDead || target.Faction != LabFaction.Enemy ||
                    !struck.Add(target)) continue;
                if (LabMeleeQuery.CanReach(transform, target.transform, attackRange, attackAngle) &&
                    target.TryTakeDamage(damage, health)) LastHitCount++;
            }
        }

        private void OnDeath(LabHealth dead)
        {
            CancelCombo();
            dodge.Cancel();
            var motor = GetComponent<LabPlayerMotor>();
            if (motor != null) motor.enabled = false;
            dodge.enabled = false;
            input.enabled = false;
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
