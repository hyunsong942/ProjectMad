using UnityEngine;

namespace MadLab
{
    [DefaultExecutionOrder(60)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LabEnemyGuard), typeof(LabHealth))]
    public sealed class LabEnemyAnimation : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform visual;
        [SerializeField] private AnimationClip attackClip;
        [SerializeField, Min(0.1f)] private float walkMetersPerSecond = 1.4f;
        [SerializeField] private AnimationCurve idleGrounding;
        [SerializeField] private AnimationCurve walkGrounding;
        [SerializeField] private AnimationCurve attackGrounding;
        [SerializeField] private AnimationCurve deathGrounding;

        private LabEnemyGuard guard;
        private LabHealth health;
        private CharacterController controller;
        private int lastAttackCount = -1;
        private string state;
        public Animator Animator => animator;
        public string CurrentState => state;

        private void Awake()
        {
            guard = GetComponent<LabEnemyGuard>();
            health = GetComponent<LabHealth>();
            controller = GetComponent<CharacterController>();
            if (animator == null || animator.avatar == null || !animator.avatar.isValid ||
                animator.runtimeAnimatorController == null || visual == null || attackClip == null)
            {
                Debug.LogError("LabEnemyAnimation needs the rigged golem, controller and attack clip.", this);
                enabled = false;
                return;
            }
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        private void Update()
        {
            if (LabGameSession.GameplayBlocked) return;
            if (health.IsDead)
            {
                animator.speed = 1f;
                Play("Death", 0.12f);
                return;
            }
            if (!guard.enabled)
            {
                animator.speed = 0f;
                return;
            }
            animator.speed = 1f;
            if (guard.IsWindingUp || guard.IsRecovering)
            {
                bool restart = lastAttackCount != guard.AttackCount;
                lastAttackCount = guard.AttackCount;
                animator.SetFloat("AttackSpeed", attackClip.length / guard.AttackDuration);
                Play("Attack", 0.06f, restart, guard.AttackElapsed);
            }
            else
            {
                Vector3 velocity = controller.velocity;
                velocity.y = 0f;
                bool moving = guard.State == LabEnemyGuard.GuardState.Chase && velocity.sqrMagnitude > 0.0025f;
                animator.SetFloat("WalkSpeed", Mathf.Clamp(velocity.magnitude / walkMetersPerSecond, 0.2f, 3f),
                    0.07f, Time.deltaTime);
                Play(moving ? "Walk" : "Idle", 0.16f);
            }
        }

        private void Play(string next, float blend, bool restart = false, float elapsed = 0f)
        {
            if (state == next && !restart) return;
            state = next;
            animator.CrossFadeInFixedTime("Base Layer." + next, blend, 0, elapsed);
        }

        private void LateUpdate()
        {
            if (animator == null || visual == null) return;
            // Curves are baked from the skinned silhouette; no per-frame mesh baking is needed.
            float height = Grounding(animator.GetCurrentAnimatorStateInfo(0));
            if (animator.IsInTransition(0))
                height = Mathf.Lerp(height, Grounding(animator.GetNextAnimatorStateInfo(0)),
                    Mathf.Clamp01(animator.GetAnimatorTransitionInfo(0).normalizedTime));
            Vector3 position = visual.localPosition;
            position.y = height;
            visual.localPosition = position;
        }

        private float Grounding(AnimatorStateInfo info)
        {
            bool loop = info.IsName("Base Layer.Idle") || info.IsName("Base Layer.Walk");
            float t = loop ? Mathf.Repeat(info.normalizedTime, 1f) : Mathf.Clamp01(info.normalizedTime);
            AnimationCurve curve = info.IsName("Base Layer.Walk") ? walkGrounding :
                info.IsName("Base Layer.Attack") ? attackGrounding :
                info.IsName("Base Layer.Death") ? deathGrounding : idleGrounding;
            return curve != null ? curve.Evaluate(t) : 0f;
        }

        private void OnDisable()
        {
            if (animator != null) animator.speed = 0f;
            state = null;
        }
    }
}
