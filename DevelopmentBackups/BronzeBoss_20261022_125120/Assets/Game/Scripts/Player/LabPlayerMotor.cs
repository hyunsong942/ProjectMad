using UnityEngine;

namespace MadLab
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(CharacterController), typeof(LabPlayerInput), typeof(LabPlayerDodge))]
    [DisallowMultipleComponent]
    public sealed class LabPlayerMotor : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField, Min(0f)] private float moveSpeed = 4.5f;
        [SerializeField, Min(0f)] private float sprintSpeed = 7f;
        [SerializeField, Min(0f)] private float turnSpeed = 720f;
        [SerializeField, Min(0.1f)] private float gravity = 25f;
        [SerializeField, Min(0.1f)] private float groundStickSpeed = 3f;
        [SerializeField, Min(1f)] private float terminalFallSpeed = 40f;

        private CharacterController controller;
        private LabPlayerInput input;
        private LabPlayerDodge dodge;
        private LabPlayerCombat combat;
        private float verticalSpeed, locomotionSpeed;
        private Vector3 recoveryVelocity;
        private bool recoveringFromDodge;
        private const float WalkRecoverySeconds = 0.12f;
        public bool IsSprinting { get; private set; }
        public bool IsGrounded => controller != null && controller.isGrounded;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<LabPlayerInput>();
            dodge = GetComponent<LabPlayerDodge>();
            combat = GetComponent<LabPlayerCombat>();
            locomotionSpeed = moveSpeed;
            if (cameraTransform == null)
            {
                Debug.LogError("LabPlayerMotor needs a camera reference.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (LabGameSession.GameplayBlocked) return;
            IsSprinting = false;
            if (!controller.enabled) return;
            float dt = Time.deltaTime;
            Vector2 axes = input.Move;
            Quaternion cameraYaw = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);
            Vector3 moveDirection = cameraYaw * new Vector3(axes.x, 0f, axes.y);

            if (!input.HasControl) dodge.Cancel();
            if (input.HasControl && input.DodgePressed && controller.isGrounded)
            {
                Vector3 direction = moveDirection.sqrMagnitude > 0.001f
                    ? moveDirection : transform.forward;
                Vector3 planarVelocity = controller.velocity;
                planarVelocity.y = 0f;
                if (dodge.TryStart(direction, Vector3.Dot(planarVelocity, direction.normalized)))
                    transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }

            Vector3 displacement;
            if (dodge.IsDodging)
            {
                displacement = dodge.ConsumeDisplacement(dt);
                recoveryVelocity = dt > 0f ? displacement / dt : Vector3.zero;
                recoveringFromDodge = true;
            }
            else if (combat != null && combat.IsAttacking)
            {
                displacement = Vector3.zero;
                recoveringFromDodge = false;
            }
            else
            {
                IsSprinting = input.HasControl && input.SprintHeld && moveDirection.sqrMagnitude > 0.001f;
                float targetSpeed = IsSprinting ? Mathf.Max(moveSpeed, sprintSpeed) : moveSpeed;
                // Ease only the walk/run speed change; preserve the existing walk start and roll distance.
                float acceleration = Mathf.Max(0.1f, Mathf.Abs(sprintSpeed - moveSpeed)) / 0.18f;
                locomotionSpeed = Mathf.MoveTowards(locomotionSpeed, targetSpeed, acceleration * dt);
                Vector3 desiredVelocity = moveDirection * locomotionSpeed;
                if (recoveringFromDodge && input.HasControl && moveDirection.sqrMagnitude > 0.001f)
                {
                    recoveryVelocity = Vector3.MoveTowards(recoveryVelocity, desiredVelocity,
                        targetSpeed / WalkRecoverySeconds * dt);
                    displacement = recoveryVelocity * dt;
                    recoveringFromDodge = (recoveryVelocity - desiredVelocity).sqrMagnitude > 0.0001f;
                }
                else
                {
                    displacement = desiredVelocity * dt;
                    recoveringFromDodge = false;
                }
                if (moveDirection.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation,
                        Quaternion.LookRotation(moveDirection, Vector3.up), turnSpeed * dt);
            }

            if (controller.isGrounded && verticalSpeed <= 0f)
                verticalSpeed = -groundStickSpeed;
            else
                verticalSpeed = Mathf.Max(verticalSpeed - gravity * dt, -terminalFallSpeed);

            displacement.y = verticalSpeed * dt;
            CollisionFlags flags = controller.Move(displacement);
            if ((flags & CollisionFlags.Below) != 0) verticalSpeed = -groundStickSpeed;
        }

        private void OnDisable()
        {
            verticalSpeed = 0f;
            IsSprinting = false;
            locomotionSpeed = moveSpeed;
            recoveringFromDodge = false;
            recoveryVelocity = Vector3.zero;
            if (dodge != null) dodge.Cancel();
        }
    }
}
