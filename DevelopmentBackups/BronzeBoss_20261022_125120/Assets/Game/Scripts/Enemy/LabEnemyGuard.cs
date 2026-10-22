using UnityEngine;

namespace MadLab
{
    [DefaultExecutionOrder(50)]
    [RequireComponent(typeof(CharacterController), typeof(LabHealth))]
    [DisallowMultipleComponent]
    public sealed class LabEnemyGuard : MonoBehaviour
    {
        public enum GuardState { Idle, Chase, Attack, Dead }
        [SerializeField] private LabHealth target;
        [SerializeField, Min(0f)] private float moveSpeed = 2.5f;
        [SerializeField, Min(0.1f)] private float detectionRange = 8f;
        [SerializeField, Min(0.1f)] private float loseRange = 11f;
        [SerializeField, Min(0.1f)] private float attackRange = 1.8f;
        [SerializeField, Min(0f)] private float attackDamage = 10f;
        [SerializeField, Min(0.01f)] private float attackCooldown = 1.5f;
        [SerializeField, Min(0.01f)] private float windupDuration = 0.45f;
        [SerializeField, Min(0f)] private float recoveryDuration = 1.0125f;
        [SerializeField, Range(1f, 180f)] private float attackAngle = 120f;
        [SerializeField, Min(0f)] private float turnSpeed = 540f;

        private CharacterController controller;
        private LabHealth health;
        private LabCombatFeedback feedback;
        private LabCombatAudio combatAudio;
        private float verticalSpeed, nextAttackTime, strikeTime, attackStartedAt, attackEndsAt;
        public GuardState State { get; private set; }
        public bool IsWindingUp { get; private set; }
        public int AttackCount { get; private set; }
        public int SuccessfulHitCount { get; private set; }
        public bool IsRecovering => State == GuardState.Attack && !IsWindingUp && Time.time < attackEndsAt;
        public float AttackDuration => windupDuration + recoveryDuration;
        public float AttackElapsed => Mathf.Clamp(Time.time - attackStartedAt, 0f, AttackDuration);

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<LabHealth>();
            feedback = GetComponent<LabCombatFeedback>();
            combatAudio = GetComponent<LabCombatAudio>();
        }
        private void Start()
        {
            if (target == null)
            {
                var player = FindFirstObjectByType<LabPlayerMotor>();
                if (player != null) target = player.GetComponent<LabHealth>();
            }
        }
        private void OnEnable() { health.Died += OnDeath; }
        private void OnDisable()
        {
            if (health != null) health.Died -= OnDeath;
            CancelWindup();
            attackEndsAt = 0f;
        }

        private void Update()
        {
            if (LabGameSession.GameplayBlocked) return;
            if (health.IsDead || State == GuardState.Dead || !controller.enabled) return;
            Vector3 movement = Vector3.zero;
            if (target == null || target.IsDead)
            {
                State = GuardState.Idle;
                CancelWindup();
                attackEndsAt = 0f;
            }
            else
            {
                Vector3 difference = target.transform.position - transform.position;
                difference.y = 0f;
                float distance = difference.magnitude;
                if (IsWindingUp)
                {
                    // The direction is committed when the windup begins; movement can evade the strike.
                    if (Time.time >= strikeTime)
                    {
                        CancelWindup();
                        if (combatAudio != null) combatAudio.PlayRobotAttack();
                        if (LabMeleeQuery.CanReach(transform, target.transform, attackRange, attackAngle) &&
                            target.TryTakeDamage(attackDamage, health)) SuccessfulHitCount++;
                    }
                }
                else if (Time.time < attackEndsAt)
                {
                    State = GuardState.Attack; // Finish the swing before resuming pursuit.
                }
                else if (distance > (State == GuardState.Idle ? detectionRange : Mathf.Max(detectionRange, loseRange)) ||
                         !LabMeleeQuery.HasLineOfSight(transform, target.transform))
                {
                    State = GuardState.Idle;
                }
                else if (distance > attackRange)
                {
                    State = GuardState.Chase;
                    if (difference.sqrMagnitude > 0.001f)
                    {
                        transform.rotation = Quaternion.RotateTowards(transform.rotation,
                            Quaternion.LookRotation(difference), turnSpeed * Time.deltaTime);
                        movement = difference.normalized * Mathf.Min(moveSpeed * Time.deltaTime,
                            Mathf.Max(0f, distance - attackRange + 0.02f));
                    }
                }
                else
                {
                    State = GuardState.Attack;
                    if (Time.time >= nextAttackTime)
                    {
                        if (difference.sqrMagnitude > 0.001f)
                            transform.rotation = Quaternion.LookRotation(difference);
                        AttackCount++;
                        attackStartedAt = Time.time;
                        attackEndsAt = Time.time + AttackDuration;
                        nextAttackTime = Time.time + Mathf.Max(attackCooldown, AttackDuration);
                        strikeTime = Time.time + windupDuration;
                        IsWindingUp = true;
                        if (feedback != null) feedback.SetTelegraph(true);
                    }
                }
            }
            verticalSpeed = controller.isGrounded ? -3f : Mathf.Max(-40f, verticalSpeed - 25f * Time.deltaTime);
            movement.y = verticalSpeed * Time.deltaTime;
            controller.Move(movement);
        }

        private void CancelWindup()
        {
            IsWindingUp = false;
            if (feedback != null) feedback.SetTelegraph(false);
        }
        private void OnDeath(LabHealth dead)
        {
            State = GuardState.Dead;
            CancelWindup();
            foreach (Collider collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
            // LabHealth.Died is the once-only notification Phase 3 will subscribe to.
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f, detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f, attackRange);
        }
    }
}
