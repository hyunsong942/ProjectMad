using UnityEngine;

namespace MadLab
{
    [DefaultExecutionOrder(40)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LabPlayerCombat), typeof(LabPlayerDodge), typeof(LabPlayerInput))]
    public sealed class LabPlayerAnimation : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private AnimationClip dodgeClip;
        [SerializeField] private AnimationClip deathClip;
        [SerializeField, Min(0.1f)] private float walkMetersPerSecond = 1.8f;
        [SerializeField, Min(0.1f)] private float runMetersPerSecond = 4.5f;
        private LabPlayerMotor motor;
        private LabPlayerCombat combat;
        private LabPlayerDodge dodge;
        private LabPlayerInput input;
        private LabHealth health;
        private CharacterController controller;
        private int lastAttackCount = -1;
        private string state;
        private static readonly int WalkSpeed = Animator.StringToHash("WalkSpeed");
        private static readonly int RunSpeed = Animator.StringToHash("RunSpeed");
        private static readonly int DodgeSpeed = Animator.StringToHash("DodgeSpeed");
        private static readonly int AttackSpeed = Animator.StringToHash("AttackSpeed");
        public Animator Animator => animator;
        public string CurrentState => state;
        public bool DeathFinished { get; private set; }
        public float DeathDuration => deathClip != null ? deathClip.length : 0f;

        private void Awake()
        {
            combat = GetComponent<LabPlayerCombat>();
            motor = GetComponent<LabPlayerMotor>();
            dodge = GetComponent<LabPlayerDodge>();
            input = GetComponent<LabPlayerInput>();
            health = GetComponent<LabHealth>();
            controller = GetComponent<CharacterController>();
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman ||
                !animator.avatar.isValid || animator.runtimeAnimatorController == null || dodgeClip == null)
            {
                Debug.LogError("LabPlayerAnimation needs the scientist Humanoid Animator and dodge clip.", this);
                enabled = false;
                return;
            }
            animator.applyRootMotion = false; // CharacterController remains the only movement authority.
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
        private void Update()
        {
            if (LabGameSession.GameplayBlocked) return;
            if (health.IsDead)
            {
                if (deathClip == null)
                {
                    DeathFinished = true;
                    animator.speed = 0f;
                    return;
                }
                if (state != "Death") Play("Death", 1f, 0.1f);
                var pose = animator.GetCurrentAnimatorStateInfo(0);
                DeathFinished = !animator.IsInTransition(0) && pose.IsName("Base Layer.Death") && pose.normalizedTime >= 1f;
                animator.speed = DeathFinished ? 0f : 1f;
                return;
            }
            if (!combat.enabled)
            {
                animator.speed = 0f;
                return;
            }
            animator.speed = 1f;
            if (dodge.IsDodging || dodge.MovedThisFrame)
            {
                // Fixed-time offset uses elapsed state seconds; the state applies DodgeSpeed.
                Play("Dodge", dodgeClip.length / dodge.Duration, 0.09f, false, dodge.Progress * dodge.Duration);
            }
            else if (combat.IsAttacking)
            {
                bool restart = lastAttackCount != combat.AttackCount;
                lastAttackCount = combat.AttackCount;
                Play("Attack" + combat.CurrentComboStep, combat.CurrentPlaybackSpeed, 0.06f, restart);
            }
            else
            {
                Vector3 velocity = controller.velocity;
                velocity.y = 0f;
                bool moving = input.HasControl && velocity.magnitude > 0.08f;
                bool running = moving && motor != null && motor.IsSprinting;
                float authoredSpeed = running ? runMetersPerSecond : walkMetersPerSecond;
                Play(moving ? (running ? "Run" : "Walk") : "Idle",
                    moving ? Mathf.Clamp(velocity.magnitude / authoredSpeed, 0.6f, 2.8f) : 1f, 0.14f);
            }
        }
        private void Play(string next, float speed, float blend, bool restart = false, float timeOffset = 0f)
        {
            // Separate state speeds keep the outgoing walk/roll unchanged during a blend.
            if (next == "Walk") animator.SetFloat(WalkSpeed, Mathf.Max(0.1f, speed), 0.06f, Time.deltaTime);
            else if (next == "Run") animator.SetFloat(RunSpeed, Mathf.Max(0.1f, speed), 0.06f, Time.deltaTime);
            else if (next == "Dodge") animator.SetFloat(DodgeSpeed, Mathf.Max(0.1f, speed));
            else if (next.StartsWith("Attack")) animator.SetFloat(AttackSpeed, Mathf.Max(0.1f, speed));
            if (state == next && !restart) return;
            state = next;
            animator.CrossFadeInFixedTime(Animator.StringToHash("Base Layer." + next), blend, 0, timeOffset);
        }
        private void OnDisable()
        {
            if (animator != null) animator.speed = 0f;
            state = null;
        }
    }
}
