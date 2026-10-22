#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace MadLab
{
    public sealed class SprintPlayModeChecks : MonoBehaviour
    {
        [Serializable] public class Check { public string name, detail; public bool passed; }
        [Serializable] public class Report
        {
            public string timestamp; public bool complete; public int passed, failed;
            public List<Check> checks = new List<Check>();
        }
        public string ReportDirectory;
        private readonly Report report = new Report();
        private LabGameSession session;
        private LabPlayerInput input;
        private LabPlayerMotor motor;
        private LabPlayerCombat combat;
        private LabPlayerDodge dodge;
        private LabPlayerAnimation animationDriver;
        private LabHealth health;
        private CharacterController controller;
        private Keyboard keyboard;
        private Mouse mouse;
        private bool oldRun, initialized, measureRoll;
        private InputSettings.BackgroundBehavior oldBackground;
        private InputSettings.EditorInputBehaviorInPlayMode oldEditor;
        private Vector3 previousPosition, visualOrigin;
        private float rollTravel, rollSeconds;
        private GameObject wall;
        private static readonly Key[] NoKeys = new Key[0];
        private static readonly Key[] RunKeys = { Key.W, Key.LeftShift };
        private string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private float PlanarSpeed { get { Vector3 velocity = controller.velocity; velocity.y = 0; return velocity.magnitude; } }
        private void Awake()
        {
            oldRun = Application.runInBackground;
            oldBackground = InputSystem.settings.backgroundBehavior;
            oldEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>("SprintTestKeyboard");
            mouse = InputSystem.AddDevice<Mouse>("SprintTestMouse");
            initialized = true;
        }
        private void CheckResult(string name, bool passed, string detail = "")
        {
            report.checks.Add(new Check { name = name, passed = passed, detail = detail });
            if (passed) report.passed++; else report.failed++;
            Persist();
        }
        private void Persist()
        {
            report.timestamp = DateTimeOffset.Now.ToString("o");
            Directory.CreateDirectory(Path.Combine(Root, ReportDirectory));
            File.WriteAllText(Path.Combine(Root, ReportDirectory, "SprintChecks.json"), JsonUtility.ToJson(report, true));
        }
        private IEnumerator Drive(float seconds, Key[] held, bool click = false)
        {
            float until = Time.realtimeSinceStartup + seconds;
            do
            {
                if (!LabGameSession.GameplayBlocked && input.isActiveAndEnabled && !input.HasControl) input.CaptureCursor();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(held));
                var state = new MouseState();
                if (click) state = state.WithButton(MouseButton.Left);
                InputSystem.QueueStateEvent(mouse, state);
                yield return null;
            } while (Time.realtimeSinceStartup < until);
        }
        private void Place()
        {
            combat.CancelCombo(); dodge.Cancel();
            controller.enabled = false;
            health.transform.SetPositionAndRotation(new Vector3(0, .05f, -8), Quaternion.identity);
            controller.enabled = true;
            Physics.SyncTransforms();
            input.CaptureCursor();
        }
        private void Capture(string name)
        {
            Directory.CreateDirectory(Path.Combine(Root, ReportDirectory, "Captures"));
            ScreenCapture.CaptureScreenshot(Path.Combine(Root, ReportDirectory, "Captures", name + ".png"));
        }
        private void LateUpdate()
        {
            if (!measureRoll || health == null) return;
            Vector3 delta = health.transform.position - previousPosition; delta.y = 0;
            if (dodge.IsDodging || dodge.MovedThisFrame) { rollTravel += delta.magnitude; rollSeconds += Time.deltaTime; }
            previousPosition = health.transform.position;
        }
        private IEnumerator Start()
        {
            yield return null;
            session = LabGameSession.Instance;
            var player = GameObject.Find("Player");
            input = player.GetComponent<LabPlayerInput>(); motor = player.GetComponent<LabPlayerMotor>();
            combat = player.GetComponent<LabPlayerCombat>(); dodge = player.GetComponent<LabPlayerDodge>();
            animationDriver = player.GetComponent<LabPlayerAnimation>(); health = player.GetComponent<LabHealth>();
            controller = player.GetComponent<CharacterController>();
            visualOrigin = animationDriver.Animator.transform.localPosition;
            var actions = (InputActionAsset)typeof(LabPlayerInput).GetField("runtimeActions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(input);
            actions.devices = new InputDevice[] { keyboard, mouse };
            var sprint = actions.FindAction("Gameplay/Sprint", true);
            CheckResult("Sprint has both Shift bindings and held-state polling", sprint.bindings.Count == 2 &&
                sprint.bindings.Any(b => b.path == "<Keyboard>/leftShift") && sprint.bindings.Any(b => b.path == "<Keyboard>/rightShift") &&
                sprint.wantsInitialStateCheck);
            Vector3 origin = player.transform.position;
            yield return Drive(.25f, RunKeys);
            CheckResult("Shift cannot move or animate gameplay behind main menu", player.transform.position == origin &&
                !input.SprintHeld && !motor.IsSprinting && LabGameSession.GameplayBlocked);
            session.StartGame();
            foreach (var guard in FindObjectsByType<LabEnemyGuard>(FindObjectsSortMode.None)) guard.enabled = false;
            yield return Drive(.2f, NoKeys);
            CheckResult("Run state uses valid Humanoid rig without root motion", animationDriver.Animator.avatar.isValid &&
                animationDriver.Animator.avatar.isHuman && !animationDriver.Animator.applyRootMotion &&
                animationDriver.Animator.HasState(0, Animator.StringToHash("Base Layer.Run")));
            Place();
            yield return Drive(.3f, new[] { Key.W });
            CheckResult("Existing walking remains 4.5 metres per second", Mathf.Abs(PlanarSpeed - 4.5f) < .08f &&
                animationDriver.CurrentState == "Walk" && !motor.IsSprinting, "Speed=" + PlanarSpeed);
            yield return Drive(.25f, RunKeys);
            CheckResult("Left Shift selects Run and reaches seven metres per second", input.SprintHeld && motor.IsSprinting &&
                animationDriver.CurrentState == "Run" && Mathf.Abs(PlanarSpeed - 7f) < .08f, "Speed=" + PlanarSpeed);
            CheckResult("Run animation cadence follows retargeted stride length", animationDriver.Animator.GetFloat("RunSpeed") > 1.4f &&
                animationDriver.Animator.GetFloat("RunSpeed") < 1.8f, "RunSpeed=" + animationDriver.Animator.GetFloat("RunSpeed"));
            Vector3 knee = animationDriver.Animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position - player.transform.position;
            yield return Drive(.16f, RunKeys);
            CheckResult("Running actually animates the leg bones", Vector3.Distance(knee,
                animationDriver.Animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position - player.transform.position) > .02f);
            Capture("01_Running");
            yield return Drive(.25f, new[] { Key.W });
            CheckResult("Releasing Shift returns to walk speed and animation", !input.SprintHeld && !motor.IsSprinting &&
                animationDriver.CurrentState == "Walk" && Mathf.Abs(PlanarSpeed - 4.5f) < .08f, "Speed=" + PlanarSpeed);
            yield return Drive(.2f, new[] { Key.LeftShift });
            CheckResult("Shift alone never runs in place", !motor.IsSprinting && animationDriver.CurrentState == "Idle" && PlanarSpeed < .01f);
            Place();
            yield return Drive(.3f, new[] { Key.W, Key.RightShift });
            CheckResult("Right Shift also runs", input.SprintHeld && motor.IsSprinting && animationDriver.CurrentState == "Run" &&
                Mathf.Abs(PlanarSpeed - 7f) < .08f);
            yield return Drive(.2f, NoKeys); Place();
            yield return Drive(.3f, new[] { Key.W, Key.D, Key.LeftShift });
            CheckResult("Diagonal sprint is normalized", Mathf.Abs(PlanarSpeed - 7f) < .08f &&
                controller.velocity.x > 4.7f && controller.velocity.z > 4.7f, "Speed=" + PlanarSpeed);
            yield return Drive(.2f, NoKeys); Place();
            yield return Drive(.35f, new[] { Key.S, Key.LeftShift });
            CheckResult("Sprint faces the actual movement direction", Vector3.Dot(player.transform.forward, Vector3.back) > .95f &&
                controller.velocity.z < -6.8f);
            yield return Drive(.2f, NoKeys); Place();
            wall = new GameObject("SprintWall_RUNTIME_ONLY");
            wall.transform.position = new Vector3(0, 1, -6);
            wall.AddComponent<BoxCollider>().size = new Vector3(4, 3, .2f); Physics.SyncTransforms();
            yield return Drive(.75f, RunKeys);
            CheckResult("Sprint cannot tunnel through a wall", player.transform.position.z > -6.7f && player.transform.position.z < -6.45f);
            CheckResult("Blocked movement returns animation to idle", PlanarSpeed < .08f && animationDriver.CurrentState == "Idle");
            Destroy(wall); wall = null; yield return Drive(.2f, NoKeys); Place();
            yield return Drive(.2f, RunKeys);
            previousPosition = player.transform.position; rollTravel = rollSeconds = 0; measureRoll = true;
            yield return Drive(.05f, new[] { Key.W, Key.LeftShift, Key.Space });
            CheckResult("Dodge takes animation priority over sprint", dodge.IsDodging && !motor.IsSprinting && animationDriver.CurrentState == "Dodge");
            Capture("02_RunToDodge");
            yield return Drive(dodge.Duration + .15f, RunKeys);
            measureRoll = false;
            CheckResult("Sprinting keeps the original three-metre roll", Mathf.Abs(rollTravel - 3f) < .04f, "Travel=" + rollTravel);
            CheckResult("Sprint does not shorten the dodge animation", rollSeconds >= .65f && rollSeconds <= .86f, "Seconds=" + rollSeconds);
            CheckResult("Held Shift resumes running after dodge recovery", motor.IsSprinting && animationDriver.CurrentState == "Run" &&
                Mathf.Abs(PlanarSpeed - 7f) < .1f, "Speed=" + PlanarSpeed);
            yield return Drive(dodge.CooldownRemaining + .1f, NoKeys); Place();
            yield return Drive(.2f, RunKeys);
            yield return Drive(.05f, new[] { Key.W, Key.LeftShift, Key.Space });
            yield return Drive(dodge.Duration + .18f, new[] { Key.W });
            CheckResult("Releasing Shift during dodge resumes walking", !motor.IsSprinting && animationDriver.CurrentState == "Walk" &&
                Mathf.Abs(PlanarSpeed - 4.5f) < .1f);
            yield return Drive(.2f, NoKeys); Place();
            yield return Drive(.3f, RunKeys);
            yield return Drive(.05f, new[] { Key.W, Key.LeftShift, Key.Escape });
            yield return Drive(.06f, NoKeys);
            origin = player.transform.position;
            float frozenTime = Time.time;
            float pose = animationDriver.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            yield return Drive(.4f, RunKeys);
            CheckResult("Pause freezes sprint position clock and animation", LabGameSession.GameplayBlocked && Time.time == frozenTime &&
                player.transform.position == origin && Mathf.Abs(animationDriver.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime - pose) < .001f);
            yield return Drive(.05f, new[] { Key.W, Key.LeftShift, Key.Escape });
            yield return Drive(.3f, RunKeys);
            CheckResult("Holding Shift through resume restores sprint", !LabGameSession.GameplayBlocked && input.SprintHeld &&
                motor.IsSprinting && animationDriver.CurrentState == "Run" && Mathf.Abs(PlanarSpeed - 7f) < .1f);
            yield return Drive(.2f, NoKeys); Place();
            yield return Drive(.2f, RunKeys);
            yield return Drive(.07f, RunKeys, true);
            origin = player.transform.position;
            yield return Drive(.2f, RunKeys);
            CheckResult("Attack interrupts sprint and holds the player still", combat.IsAttacking && !motor.IsSprinting &&
                animationDriver.CurrentState.StartsWith("Attack") && Vector3.Distance(origin, player.transform.position) < .03f);
            yield return Drive(1.1f, RunKeys);
            CheckResult("Held Shift can resume running after attack", !combat.IsAttacking && motor.IsSprinting &&
                animationDriver.CurrentState == "Run");
            yield return Drive(.2f, NoKeys); Place();
            yield return Drive(.2f, RunKeys);
            health.TryTakeDamage(100, null);
            origin = player.transform.position;
            yield return Drive(.4f, RunKeys, true);
            CheckResult("Death disables sprint input and movement", health.IsDead && !motor.enabled && !motor.IsSprinting &&
                !input.SprintHeld && animationDriver.CurrentState == "Death" && Vector3.Distance(origin, player.transform.position) < .01f);
            CheckResult("Running never accumulates displacement on the visual root",
                Vector3.Distance(visualOrigin, animationDriver.Animator.transform.localPosition) < .01f);
            report.complete = true;
            Persist();
        }
        private void OnDestroy()
        {
            if (wall != null) Destroy(wall);
            if (!initialized) return;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior = oldBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditor;
            Application.runInBackground = oldRun;
        }
    }
}
#endif
