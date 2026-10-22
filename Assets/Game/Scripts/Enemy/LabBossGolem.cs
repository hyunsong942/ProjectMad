using System;
using UnityEngine;

namespace MadLab
{
    [DefaultExecutionOrder(50)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController), typeof(LabHealth))]
    public sealed class LabBossGolem : MonoBehaviour
    {
        public enum BossState { Dormant, Introduction, Chase, Windup, Attack, Recovery, Dead, Suspended }
        [Serializable] public sealed class Motion
        {
            public string state;
            public AnimationClip clip;
            public AnimationCurve grounding;
            public float metersPerSecond = 1f;
        }
        [SerializeField] private Animator animator;
        [SerializeField] private Transform visual;
        [SerializeField] private Motion[] motions;
        [SerializeField] private GameObject punchWarning;
        [SerializeField] private GameObject swipeWarning;
        [SerializeField] private float walkSpeed = 2.7f;
        [SerializeField] private float runSpeed = 4.8f;
        [SerializeField] private float introDuration = 3.2f;
        private CharacterController controller;
        private LabHealth health, target;
        private LabCombatAudio audioDriver;
        private float stateUntil, attackStarted, hitAt, gravity;
        private bool swipe, hitAttempted, suspended;
        private string animationState;
        public BossState State { get; private set; }
        public bool HasAwakened { get; private set; }
        public bool IsDefeated => HasAwakened && Health.IsDead;
        public LabHealth Health => health != null ? health : GetComponent<LabHealth>();
        public Animator Animator => animator;
        public int AttackCount { get; private set; }
        public int HitAttempts { get; private set; }
        public int SuccessfulHits { get; private set; }
        public bool IsSwipe => swipe;
        public float ContactAt => hitAt;
        public float StateEndsAt => stateUntil;
        public float CurrentRange => swipe ? 3.8f : 3.1f;
        public float CurrentAngle => swipe ? 160f : 105f;
        public bool WarningVisible => punchWarning.activeSelf || swipeWarning.activeSelf;
        public event Action Changed;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<LabHealth>();
            audioDriver = GetComponent<LabCombatAudio>();
            health.Died += OnDeath;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            SetWarnings(false);
            State = BossState.Dormant;
        }

        public void Awaken(LabHealth player)
        {
            if (HasAwakened || player == null || player.IsDead) return;
            // Choose clear arena space away from the player before enabling any collider.
            Vector3 spawn = transform.position;
            float best = -1f;
            foreach (Vector3 candidate in new[] { new Vector3(0,.05f,5), new Vector3(0,.05f,-3),
                new Vector3(-3,.05f,3), new Vector3(3,.05f,3), new Vector3(-3,.05f,-4), new Vector3(3,.05f,-4) })
            {
                float distance = (candidate - player.transform.position).sqrMagnitude;
                if (distance < 16f || distance <= best) continue;
                bool blocked = false;
                foreach (var collider in Physics.OverlapCapsule(candidate + Vector3.up * .95f,
                    candidate + Vector3.up * 2.35f, .82f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (collider.transform.IsChildOf(transform)) continue;
                    var other = collider.GetComponentInParent<LabHealth>();
                    if (other != null && other.IsDead) continue;
                    blocked = true; break;
                }
                if (!blocked) { best = distance; spawn = candidate; }
            }
            transform.position = spawn;
            gameObject.SetActive(true);
            target = player;
            HasAwakened = true;
            health.enabled = false;
            State = BossState.Introduction;
            stateUntil = Time.time + introDuration;
            FaceTarget(360f);
            animator.SetFloat("RoarSpeed", ClipLength("Roar") / introDuration);
            Play("Roar", .1f);
            Changed?.Invoke();
        }

        private float ClipLength(string state)
        {
            foreach (var motion in motions) if (motion.state == state) return motion.clip.length;
            return 1f;
        }

        private void Update()
        {
            if (!HasAwakened || LabGameSession.GameplayBlocked) return;
            if (health.IsDead) return;
            if (suspended || target == null || target.IsDead) { Suspend(); return; }
            animator.speed = 1f;
            gravity = controller.isGrounded ? -2f : Mathf.Max(-25f, gravity - 20f * Time.deltaTime);
            Vector3 move = Vector3.zero;
            if (State == BossState.Introduction)
            {
                if (Time.time >= stateUntil) { health.enabled = true; State = BossState.Chase; Changed?.Invoke(); }
            }
            else if (State == BossState.Windup)
            {
                if (Time.time >= stateUntil)
                {
                    State = BossState.Attack;
                    attackStarted = Time.time;
                    float speed = swipe ? 1.2f : 1f;
                    float duration = ClipLength(swipe ? "Swipe" : "Punch") / speed;
                    hitAt = attackStarted + duration * (swipe ? .5375f : .2625f);
                    stateUntil = attackStarted + duration;
                    animator.SetFloat(swipe ? "SwipeSpeed" : "PunchSpeed", speed);
                    Play(swipe ? "Swipe" : "Punch", .05f, true);
                }
            }
            else if (State == BossState.Attack)
            {
                if (!hitAttempted && Time.time >= hitAt)
                {
                    hitAttempted = true; HitAttempts++;
                    SetWarnings(false);
                    if (audioDriver != null) audioDriver.PlayRobotAttack();
                    if (LabMeleeQuery.CanReach(transform, target.transform, CurrentRange, CurrentAngle) &&
                        target.TryTakeDamage(swipe ? 22f : 15f, health)) SuccessfulHits++;
                }
                if (Time.time >= stateUntil)
                {
                    State = BossState.Recovery;
                    stateUntil = Time.time + .65f;
                    Play("Idle", .18f);
                }
            }
            else if (State == BossState.Recovery)
            {
                if (Time.time >= stateUntil) State = BossState.Chase;
            }
            else if (State == BossState.Chase)
            {
                Vector3 delta = target.transform.position - transform.position; delta.y = 0;
                FaceTarget(240f * Time.deltaTime);
                if (delta.magnitude <= 2.65f && LabMeleeQuery.CanReach(transform, target.transform, 2.7f, 95f))
                {
                    swipe = AttackCount % 2 == 1;
                    AttackCount++; hitAttempted = false;
                    State = BossState.Windup;
                    stateUntil = Time.time + (swipe ? .3f : .45f);
                    hitAt = float.PositiveInfinity;
                    Play("Idle", .12f);
                    SetWarnings(true);
                }
                else
                {
                    float speed = delta.magnitude > 5f ? runSpeed : walkSpeed;
                    move = delta.normalized * speed;
                }
            }
            if (controller.enabled) controller.Move((move + Vector3.up * gravity) * Time.deltaTime);
            if (State == BossState.Chase)
            {
                Vector3 velocity = controller.velocity; velocity.y = 0f;
                string next = velocity.magnitude < .1f ? "Idle" : move.magnitude > walkSpeed + .1f ? "Run" : "Walk";
                if (next != "Idle")
                {
                    float stride = 1f;
                    foreach (var motion in motions) if (motion.state == next) stride = Mathf.Max(.1f, motion.metersPerSecond);
                    animator.SetFloat(next + "Speed", Mathf.Clamp(velocity.magnitude / stride, .2f, 2.5f), .08f, Time.deltaTime);
                }
                Play(next, .18f);
            }
        }

        private void FaceTarget(float degrees)
        {
            if (target == null) return;
            Vector3 delta = target.transform.position - transform.position; delta.y = 0;
            if (delta.sqrMagnitude > .001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(delta), degrees);
        }

        private void Play(string next, float blend, bool restart = false)
        {
            if (!restart && animationState == next) return;
            animationState = next;
            animator.CrossFadeInFixedTime("Base Layer." + next, blend, 0, 0);
        }

        private void SetWarnings(bool show)
        {
            punchWarning.SetActive(show && !swipe);
            swipeWarning.SetActive(show && swipe);
        }

        public void Suspend()
        {
            if (!HasAwakened || health.IsDead || suspended) return;
            suspended = true; State = BossState.Suspended;
            animator.speed = 0f; SetWarnings(false);
            if (audioDriver != null) audioDriver.StopAction();
        }

        private void OnDeath(LabHealth _)
        {
            State = BossState.Dead; SetWarnings(false);
            controller.enabled = false;
            animator.speed = 1f;
            Play("Death", .1f, true);
            Changed?.Invoke();
        }

        private void LateUpdate()
        {
            if (!HasAwakened) return;
            float y = Grounding(animator.GetCurrentAnimatorStateInfo(0));
            if (animator.IsInTransition(0))
                y = Mathf.Lerp(y, Grounding(animator.GetNextAnimatorStateInfo(0)),
                    Mathf.Clamp01(animator.GetAnimatorTransitionInfo(0).normalizedTime));
            Vector3 p = visual.localPosition; p.y = y; visual.localPosition = p;
        }

        private float Grounding(AnimatorStateInfo info)
        {
            foreach (var motion in motions)
                if (info.IsName("Base Layer." + motion.state))
                {
                    bool loop = motion.state == "Idle" || motion.state == "Walk" || motion.state == "Run";
                    return motion.grounding.Evaluate(loop ? Mathf.Repeat(info.normalizedTime, 1f) : Mathf.Clamp01(info.normalizedTime));
                }
            return motions[0].grounding.Evaluate(0);
        }

        private void OnDestroy() { if (health != null) health.Died -= OnDeath; }
    }
}
