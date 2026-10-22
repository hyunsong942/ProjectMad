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
        [SerializeField, Min(0.1f)] private float walkMetersPerSecond = 1.8f;
        private LabPlayerCombat combat;
        private LabPlayerDodge dodge;
        private LabPlayerInput input;
        private LabHealth health;
        private CharacterController controller;
        private int lastAttackCount = -1;
        private string state;
        private static readonly int PlaybackSpeed = Animator.StringToHash("PlaybackSpeed");
        public Animator Animator => animator;
        public string CurrentState => state;

        private void Awake()
        {
            combat = GetComponent<LabPlayerCombat>();
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
            if (health.IsDead || !combat.enabled)
            {
                animator.speed = 0f;
                return;
            }
            animator.speed = 1f;
            if (dodge.IsDodging)
            {
                Play("Dodge", dodgeClip.length / dodge.Duration, 0.035f);
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
                Play(moving ? "Walk" : "Idle",
                    moving ? Mathf.Clamp(velocity.magnitude / walkMetersPerSecond, 0.6f, 2.8f) : 1f, 0.12f);
            }
        }
        private void Play(string next, float speed, float blend, bool restart = false)
        {
            animator.SetFloat(PlaybackSpeed, Mathf.Max(0.1f, speed));
            if (state == next && !restart) return;
            state = next;
            animator.CrossFadeInFixedTime(Animator.StringToHash("Base Layer." + next), blend, 0, 0f);
        }
        private void OnDisable()
        {
            if (animator != null) animator.speed = 0f;
            state = null;
        }
    }
}
