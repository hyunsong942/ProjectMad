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
    public sealed class PlayerComboPlayModeChecks : MonoBehaviour
    {
        [Serializable] public class Check { public string name; public bool passed; public string detail; }
        [Serializable] public class Report
        {
            public string phase = "Player combo animation";
            public string timestamp; public bool complete; public int passed, failed;
            public List<Check> checks = new List<Check>();
        }
        private readonly Report report = new Report();
        private static readonly Key[] NoKeys = new Key[0];
        private LabPlayerInput input;
        private LabPlayerCombat combat;
        private LabPlayerDodge dodge;
        private LabPlayerAnimation animationDriver;
        private LabHealth health, target;
        private CharacterController controller;
        private Animator animator;
        private GameObject targetObject;
        private InputActionAsset actions;
        private Keyboard keyboard;
        private Mouse mouse;
        private InputSettings.BackgroundBehavior oldBackground;
        private InputSettings.EditorInputBehaviorInPlayMode oldEditorInput;
        private bool oldRun, initialized, captureCombo;
        private int observedAttacks, baselineAttacks, baselineResolved;
        private readonly List<int> observedSteps = new List<int>();
        private readonly HashSet<int> captured = new HashSet<int>();
        private Vector3 visualStart;

        private static void Set(object obj, string name, object value) =>
            obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);
        private static T Get<T>(object obj, string name) =>
            (T)obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
        private string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private void Persist()
        {
            report.timestamp = DateTimeOffset.Now.ToString("o");
            File.WriteAllText(Path.Combine(Root, "PlayerComboTestResults.json"), JsonUtility.ToJson(report, true));
        }
        private void CheckResult(string name, bool passed, string detail)
        {
            report.checks.Add(new Check { name = name, passed = passed, detail = detail });
            if (passed) report.passed++; else report.failed++;
            Persist();
        }
        private void Observe()
        {
            if (combat.AttackCount != observedAttacks)
            {
                observedAttacks = combat.AttackCount;
                observedSteps.Add(combat.CurrentComboStep);
            }
            if (captureCombo && combat.IsAttacking && combat.CurrentStepProgress >= (combat.CurrentComboStep == 3 ? 0.28f : 0.72f) &&
                captured.Add(combat.CurrentComboStep))
                ScreenCapture.CaptureScreenshot(Path.Combine(Root, "Captures/PlayerCombo/Combo_Attack" + combat.CurrentComboStep + ".png"));
        }
        private IEnumerator Drive(float seconds, Key[] keys, bool click = false, bool recapture = true)
        {
            float until = Time.time + seconds;
            do
            {
                if (recapture && input.enabled && !input.HasControl) input.CaptureCursor();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
                var state = new MouseState();
                if (click) state = state.WithButton(MouseButton.Left);
                InputSystem.QueueStateEvent(mouse, state);
                Observe();
                yield return null;
            } while (Time.time < until);
            Observe();
        }
        private IEnumerator Click()
        {
            yield return Drive(0.045f, NoKeys, true);
            yield return Drive(0.045f, NoKeys);
        }
        private void ResetFixture()
        {
            combat.CancelCombo();
            controller.enabled = false;
            health.transform.SetPositionAndRotation(new Vector3(0, 0.05f, -8), Quaternion.identity);
            controller.enabled = true;
            targetObject.transform.position = new Vector3(0, 0.05f, -6.5f);
            Set(target, "<CurrentHealth>k__BackingField", 1000f);
            Physics.SyncTransforms();
            input.CaptureCursor();
            baselineAttacks = observedAttacks = combat.AttackCount;
            baselineResolved = combat.ResolvedStrikeCount;
            observedSteps.Clear();
        }
        private bool Steps(params int[] expected) => observedSteps.SequenceEqual(expected);
        private int Attacks => combat.AttackCount - baselineAttacks;
        private int Resolved => combat.ResolvedStrikeCount - baselineResolved;

        private void Awake()
        {
            oldRun = Application.runInBackground;
            oldBackground = InputSystem.settings.backgroundBehavior;
            oldEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>("ComboTestKeyboard");
            mouse = InputSystem.AddDevice<Mouse>("ComboTestMouse");
            initialized = true;
        }
        private IEnumerator Start()
        {
            yield return null;
            var player = GameObject.Find("Player");
            input = player.GetComponent<LabPlayerInput>();
            combat = player.GetComponent<LabPlayerCombat>();
            dodge = player.GetComponent<LabPlayerDodge>();
            animationDriver = player.GetComponent<LabPlayerAnimation>();
            health = player.GetComponent<LabHealth>();
            controller = player.GetComponent<CharacterController>();
            animator = animationDriver.Animator;
            visualStart = animator.transform.localPosition;
            actions = Get<InputActionAsset>(input, "runtimeActions");
            actions.devices = new InputDevice[] { keyboard, mouse };
            foreach (var guard in FindObjectsByType<LabEnemyGuard>(FindObjectsSortMode.None))
            {
                guard.enabled = false;
                guard.gameObject.SetActive(false);
            }
            targetObject = new GameObject("ComboTarget_RUNTIME_ONLY");
            targetObject.SetActive(false);
            var collider = targetObject.AddComponent<CapsuleCollider>();
            collider.center = Vector3.up * 0.9f; collider.height = 1.8f; collider.radius = 0.3f;
            var second = new GameObject("SecondCollider");
            second.transform.SetParent(targetObject.transform, false);
            second.transform.localPosition = Vector3.up * 0.9f;
            second.AddComponent<BoxCollider>().size = Vector3.one * 0.3f;
            target = targetObject.AddComponent<LabHealth>();
            Set(target, "maximumHealth", 1000f);
            Set(target, "faction", LabFaction.Enemy);
            targetObject.SetActive(true);
            Directory.CreateDirectory(Path.Combine(Root, "Captures/PlayerCombo"));
            ResetFixture();
            yield return Drive(0.4f, NoKeys);
            CheckResult("Valid humanoid and skinned scientist", animator.isHuman && animator.avatar.isValid &&
                player.GetComponentInChildren<SkinnedMeshRenderer>().bones.Length > 40, "Skinned player, not static mesh.");
            CheckResult("Animator root motion disabled", !animator.applyRootMotion, "CharacterController owns displacement.");
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand).position;
            yield return Drive(0.5f, NoKeys);
            CheckResult("Idle animation moves bones", Vector3.Distance(hand, animator.GetBoneTransform(HumanBodyBones.RightHand).position) > 0.003f &&
                animationDriver.CurrentState == "Idle", "Idle pose changes over time.");
            targetObject.transform.position = new Vector3(8, 0, 8); Physics.SyncTransforms();
            var start = player.transform.position;
            yield return Drive(0.35f, new[] { Key.W });
            CheckResult("Walking input moves and selects Walk", Vector3.Distance(start, player.transform.position) > 1f &&
                animationDriver.CurrentState == "Walk", "State=" + animationDriver.CurrentState + ", distance=" + Vector3.Distance(start, player.transform.position));
            yield return Drive(0.4f, NoKeys);

            ResetFixture();
            yield return Drive(0.05f, NoKeys, true);
            yield return Drive(0.15f, NoKeys);
            CheckResult("First strike has windup before damage", target.CurrentHealth == 1000f && combat.IsAttacking, "No immediate click damage.");
            yield return Drive(0.34f, NoKeys);
            CheckResult("Contact frame applies one hit", target.CurrentHealth == 985f && Resolved == 1, "Target HP=" + target.CurrentHealth);
            CheckResult("Two colliders do not duplicate one strike", combat.LastHitCount == 1, "One health target, two colliders.");
            yield return Drive(0.7f, NoKeys);
            CheckResult("One click plays only strike one", Attacks == 1 && Steps(1) && target.CurrentHealth == 985f && !combat.IsAttacking, string.Join(",", observedSteps));

            ResetFixture();
            yield return Click(); yield return Click(); yield return Drive(1.5f, NoKeys);
            CheckResult("Two quick clicks play one then two", Attacks == 2 && Steps(1, 2) && Resolved == 2 && target.CurrentHealth == 970f,
                "Steps=" + string.Join(",", observedSteps) + ", HP=" + target.CurrentHealth);
            CheckResult("Double click does not auto-play third", !combat.IsAttacking && combat.QueuedClicks == 0, "Stopped after two.");

            ResetFixture(); captureCombo = true; captured.Clear();
            yield return Click(); yield return Click(); yield return Click(); yield return Drive(2.5f, NoKeys);
            captureCombo = false;
            CheckResult("Three quick clicks play all three once", Attacks == 3 && Steps(1, 2, 3) && Resolved == 3 && target.CurrentHealth == 955f,
                "Steps=" + string.Join(",", observedSteps) + ", HP=" + target.CurrentHealth);
            CheckResult("Finisher resets combo", !combat.IsAttacking && combat.CurrentComboStep == 0 && combat.QueuedClicks == 0, "No automatic fourth strike.");

            ResetFixture();
            yield return Drive(2.8f, NoKeys, true); yield return Drive(0.3f, NoKeys);
            CheckResult("Holding mouse does not chain attacks", Attacks == 1 && Resolved == 1 && target.CurrentHealth == 985f, "Held for 2.8 seconds.");

            ResetFixture();
            for (int i = 0; i < 5; i++) { yield return Drive(0.025f, NoKeys, true); yield return Drive(0.025f, NoKeys); }
            yield return Drive(2.6f, NoKeys);
            CheckResult("Excess early clicks cap at three strikes", Attacks == 3 && Steps(1, 2, 3) && target.CurrentHealth == 955f, "Five early clicks.");

            ResetFixture();
            yield return Click(); yield return Drive(1.2f, NoKeys);
            yield return Click(); yield return Drive(1.2f, NoKeys);
            CheckResult("Expired combo restarts from one", Steps(1, 1) && Attacks == 2 && target.CurrentHealth == 970f, string.Join(",", observedSteps));

            ResetFixture();
            yield return Click(); yield return Drive(0.68f, NoKeys); yield return Click(); yield return Drive(1.0f, NoKeys);
            CheckResult("Short recovery grace permits next strike", Steps(1, 2) && target.CurrentHealth == 970f, string.Join(",", observedSteps));

            ResetFixture();
            yield return Click(); start = player.transform.position;
            yield return Drive(0.3f, new[] { Key.W });
            var planar = player.transform.position - start; planar.y = 0;
            CheckResult("Attack holds movement and facing", planar.magnitude < 0.03f && Quaternion.Angle(player.transform.rotation, Quaternion.identity) < 1f, "Drift=" + planar.magnitude);
            yield return Drive(0.8f, NoKeys);

            ResetFixture();
            yield return Click(); yield return Click();
            yield return Drive(0.045f, new[] { Key.Space });
            CheckResult("Dodge interrupts buffered combo", !combat.IsAttacking && combat.QueuedClicks == 0 && dodge.IsDodging, "Queued second strike cleared.");
            CheckResult("Dodge animation selected", animationDriver.CurrentState == "Dodge", animationDriver.CurrentState);
            yield return Drive(1.1f, NoKeys);
            CheckResult("Cancelled windup does no delayed damage", target.CurrentHealth == 1000f && Resolved == 0 && Attacks == 1, "HP=" + target.CurrentHealth);

            ResetFixture(); targetObject.transform.position = new Vector3(8, 0, 8); Physics.SyncTransforms();
            start = player.transform.position;
            yield return Drive(0.12f, new[] { Key.Space });
            var hipsDelta = animator.GetBoneTransform(HumanBodyBones.Hips).position - player.transform.position; hipsDelta.y = 0;
            CheckResult("Dodge visual stays with collision root", hipsDelta.magnitude < 0.7f &&
                Vector3.Distance(animator.transform.localPosition, visualStart) < 0.01f, "Hips XZ offset=" + hipsDelta.magnitude);
            yield return Drive(0.05f, NoKeys, true);
            CheckResult("Clicks during dodge do not start or queue attacks", Attacks == 0 && combat.QueuedClicks == 0, "Attack pressed mid-dodge.");
            yield return Drive(0.25f, NoKeys);
            planar = player.transform.position - start; planar.y = 0;
            CheckResult("Dodge keeps original three-metre distance", Mathf.Abs(planar.magnitude - 3f) < 0.08f, "Distance=" + planar.magnitude);
            yield return Drive(0.9f, NoKeys);

            ResetFixture();
            var wall = new GameObject("ComboWall_RUNTIME_ONLY");
            wall.transform.position = new Vector3(0, 1, -7.3f);
            wall.AddComponent<BoxCollider>().size = new Vector3(3, 3, 0.12f); Physics.SyncTransforms();
            yield return Click(); yield return Drive(1.1f, NoKeys);
            CheckResult("Contact damage respects wall occlusion", target.CurrentHealth == 1000f && combat.LastHitCount == 0, "Target behind wall.");
            Destroy(wall); yield return Drive(0.1f, NoKeys);

            ResetFixture(); yield return Click();
            targetObject.transform.position = new Vector3(0, 0, 5); Physics.SyncTransforms();
            yield return Drive(1.1f, NoKeys);
            CheckResult("Range is evaluated at contact, not button press", target.CurrentHealth == 1000f && combat.LastHitCount == 0, "Target moved away during windup.");

            ResetFixture(); yield return Click(); yield return Click();
            yield return Drive(0.05f, new[] { Key.Escape }, false, false);
            yield return Drive(1.1f, NoKeys, false, false);
            CheckResult("Releasing cursor cancels queued strikes", !input.HasControl && !combat.IsAttacking && combat.QueuedClicks == 0 && target.CurrentHealth == 1000f, "Escape during windup.");
            int before = combat.AttackCount;
            yield return Drive(0.05f, NoKeys, true, false); yield return Drive(0.1f, NoKeys, false, false);
            CheckResult("Cursor capture click is not an attack", input.HasControl && combat.AttackCount == before, "First click only captures cursor.");
            yield return Drive(0.3f, NoKeys);

            ResetFixture(); yield return Click(); yield return Click(); yield return Click();
            health.TryTakeDamage(100f, null);
            yield return Drive(1.3f, new[] { Key.W }, true);
            CheckResult("Death clears combo and pending contact", health.IsDead && !combat.enabled && !combat.IsAttacking &&
                combat.QueuedClicks == 0 && target.CurrentHealth == 1000f && Resolved == 0, "Death before first contact.");
            CheckResult("Death freezes animation and movement", animator.speed == 0f && !controller.enabled, "Terminal state.");
            CheckResult("Animator child never accumulates root displacement", Vector3.Distance(animator.transform.localPosition, visualStart) < 0.01f,
                "Local position=" + animator.transform.localPosition);
            report.complete = true; Persist();
            Destroy(gameObject);
        }
        private void OnDestroy()
        {
            if (!initialized) return;
            if (actions != null) actions.devices = null;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior = oldBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorInput;
            Application.runInBackground = oldRun;
            if (targetObject != null) Destroy(targetObject);
        }
    }
}
#endif
