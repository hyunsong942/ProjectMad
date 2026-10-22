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
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MadLab
{
    public sealed class MenuAudioDamagePlayModeChecks : MonoBehaviour
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
        private LabStageController stage;
        private LabPlayerInput input;
        private Keyboard keyboard;
        private Mouse mouse;
        private bool oldRun, initialized;
        private InputSettings.BackgroundBehavior oldBackground;
        private InputSettings.EditorInputBehaviorInPlayMode oldEditor;
        private float[] originalValues;
        private bool[] hadPrefs;
        private float[] originalPrefs;
        private readonly string[] keys = { LabGameSession.MasterKey, LabGameSession.EffectsKey, LabGameSession.MusicKey };
        private static readonly Key[] NoKeys = new Key[0];
        private string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            oldRun = Application.runInBackground;
            oldBackground = InputSystem.settings.backgroundBehavior;
            oldEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>("MenuTestKeyboard");
            mouse = InputSystem.AddDevice<Mouse>("MenuTestMouse");
            initialized = true;
        }
        private void Bind()
        {
            session = LabGameSession.Instance;
            stage = FindFirstObjectByType<LabStageController>();
            input = stage.Player.GetComponent<LabPlayerInput>();
            var actions = (InputActionAsset)typeof(LabPlayerInput).GetField("runtimeActions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(input);
            actions.devices = new InputDevice[] { keyboard, mouse };
            FindFirstObjectByType<InputSystemUIInputModule>().actionsAsset.devices = new InputDevice[] { keyboard, mouse };
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
            File.WriteAllText(Path.Combine(Root, ReportDirectory, "MenuAudioDamageChecks.json"), JsonUtility.ToJson(report, true));
        }
        private IEnumerator Drive(float seconds, Key[] held = null, bool click = false, Vector2 position = default(Vector2))
        {
            float until = Time.realtimeSinceStartup + seconds;
            do
            {
                if (!LabGameSession.GameplayBlocked && input != null && input.isActiveAndEnabled && !input.HasControl) input.CaptureCursor();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(held ?? NoKeys));
                var state = new MouseState { position = position };
                if (click) state = state.WithButton(MouseButton.Left);
                InputSystem.QueueStateEvent(mouse, state);
                yield return null;
            } while (Time.realtimeSinceStartup < until);
        }
        private IEnumerator Escape()
        {
            yield return Drive(.07f, new[] { Key.Escape });
            yield return Drive(.07f);
        }
        private IEnumerator Click(Component component)
        {
            var rect = component.GetComponent<RectTransform>();
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            yield return Drive(.05f, null, false, point);
            yield return Drive(.06f, null, true, point);
            yield return Drive(.07f, null, false, point);
        }
        private IEnumerator SliderTo(Slider slider, float value)
        {
            var area = (RectTransform)slider.handleRect.parent;
            Vector3 local = new Vector3(Mathf.Lerp(area.rect.xMin, area.rect.xMax, value), 0, 0);
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, area.TransformPoint(local));
            yield return Drive(.04f, null, false, point);
            yield return Drive(.06f, null, true, point);
            yield return Drive(.07f, null, false, point);
        }
        private Button ButtonNamed(string name) => GameObject.Find(name).GetComponent<Button>();
        private void Capture(string name)
        {
            Directory.CreateDirectory(Path.Combine(Root, ReportDirectory, "Captures"));
            ScreenCapture.CaptureScreenshot(Path.Combine(Root, ReportDirectory, "Captures", name + ".png"));
        }
        private void Place(LabHealth actor, Vector3 position)
        {
            var cc = actor.GetComponent<CharacterController>();
            bool enabled = cc != null && cc.enabled;
            if (cc != null) cc.enabled = false;
            actor.transform.SetPositionAndRotation(position, Quaternion.identity);
            if (cc != null) cc.enabled = enabled;
            Physics.SyncTransforms();
        }
        private IEnumerator AwaitScene(int previous)
        {
            float deadline = Time.realtimeSinceStartup + 12f;
            while ((stage == null || stage.GetInstanceID() == previous) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                stage = FindFirstObjectByType<LabStageController>();
            }
            yield return null;
            Bind();
            yield return Drive(.1f);
        }
        private IEnumerator Start()
        {
            yield return null;
            Bind();
            originalValues = new[] { session.MasterVolume, session.EffectsVolume, session.MusicVolume };
            hadPrefs = keys.Select(PlayerPrefs.HasKey).ToArray();
            originalPrefs = keys.Select(k => PlayerPrefs.GetFloat(k)).ToArray();
            var player = stage.Player;
            var combat = player.GetComponent<LabPlayerCombat>();
            int attacks = combat.AttackCount;
            Vector3 start = player.transform.position;
            var guards = FindObjectsByType<LabEnemyGuard>(FindObjectsSortMode.None);
            Vector3[] guardPositions = guards.Select(g => g.transform.position).ToArray();
            CheckResult("Startup opens main menu and hides gameplay HUD", session.Screen == LabGameSession.ScreenState.MainMenu &&
                !FindFirstObjectByType<LabStageHUD>().GetComponent<Canvas>().enabled && !input.HasControl);
            CheckResult("Startup freezes gameplay but loops streamed BGM", Time.timeScale == 0 && AudioListener.pause &&
                session.MusicSource.loop && session.MusicSource.ignoreListenerPause && session.MusicSource.isPlaying &&
                session.MusicSource.clip.loadType == AudioClipLoadType.Streaming);
            yield return Drive(.5f, new[] { Key.W, Key.Space }, true, new Vector2(5, 5));
            CheckResult("Main menu blocks movement attacks dodge and enemy AI", player.transform.position == start &&
                combat.AttackCount == attacks && !player.GetComponent<LabPlayerDodge>().IsDodging &&
                guards.Select((g, i) => Vector3.Distance(g.transform.position, guardPositions[i]) < .001f).All(v => v));
            yield return Click(ButtonNamed("MenuSettingsButton"));
            CheckResult("Main menu settings button works with paused clock", session.Screen == LabGameSession.ScreenState.Settings && Time.timeScale == 0);
            yield return SliderTo(session.Menu.MasterSlider, .65f);
            yield return SliderTo(session.Menu.EffectsSlider, .25f);
            yield return SliderTo(session.Menu.MusicSlider, .4f);
            CheckResult("Mouse sliders independently set master effects and music", Mathf.Abs(session.MasterVolume - .65f) < .015f &&
                Mathf.Abs(session.EffectsVolume - .25f) < .015f && Mathf.Abs(session.MusicVolume - .4f) < .015f);
            CheckResult("Volume groups apply to listener music and every combat source", Mathf.Approximately(AudioListener.volume, session.MasterVolume) &&
                Mathf.Approximately(session.MusicSource.volume, session.MusicVolume) &&
                FindObjectsByType<LabCombatAudio>(FindObjectsSortMode.None).All(a => Mathf.Approximately(a.GetComponent<AudioSource>().volume, session.EffectsVolume)));
            Capture("02_AudioSettings");
            yield return Click(ButtonNamed("SettingsBackButton"));
            CheckResult("Back returns to the originating main menu", session.Screen == LabGameSession.ScreenState.MainMenu);
            CheckResult("Volume values are saved to PlayerPrefs", Mathf.Abs(PlayerPrefs.GetFloat(keys[0]) - .65f) < .015f &&
                Mathf.Abs(PlayerPrefs.GetFloat(keys[1]) - .25f) < .015f && Mathf.Abs(PlayerPrefs.GetFloat(keys[2]) - .4f) < .015f);
            yield return Click(ButtonNamed("MenuSettingsButton"));
            yield return SliderTo(session.Menu.MasterSlider, 0);
            CheckResult("Master slider reaches mute without changing group values", session.MasterVolume < .001f &&
                session.EffectsVolume > .2f && session.MusicVolume > .3f && AudioListener.volume == 0);
            yield return SliderTo(session.Menu.MasterSlider, 1);
            yield return SliderTo(session.Menu.MusicSlider, 0);
            CheckResult("Music slider mutes only music", session.MusicSource.volume == 0 && AudioListener.volume == 1 &&
                FindObjectsByType<LabCombatAudio>(FindObjectsSortMode.None).All(a => a.GetComponent<AudioSource>().volume > .2f));
            yield return SliderTo(session.Menu.MusicSlider, .35f);
            yield return SliderTo(session.Menu.EffectsSlider, 0);
            CheckResult("Effects slider mutes all combat sources while music plays", session.MusicSource.volume > .3f && session.MusicSource.isPlaying &&
                FindObjectsByType<LabCombatAudio>(FindObjectsSortMode.None).All(a => a.GetComponent<AudioSource>().volume == 0));
            yield return SliderTo(session.Menu.EffectsSlider, .8f);
            yield return Escape();
            CheckResult("Escape closes settings to main menu", session.Screen == LabGameSession.ScreenState.MainMenu);
            float musicTime = session.MusicSource.time;
            int musicId = session.MusicSource.GetInstanceID();
            yield return Click(session.Menu.StartButton);
            CheckResult("Actual Start button enters gameplay without an accidental attack", session.Screen == LabGameSession.ScreenState.Playing &&
                Time.timeScale == 1 && !AudioListener.pause && input.HasControl && combat.AttackCount == attacks &&
                FindFirstObjectByType<LabStageHUD>().GetComponent<Canvas>().enabled);
            CheckResult("BGM continues from menu into game without restarting", session.MusicSource.time >= musicTime && session.MusicSource.GetInstanceID() == musicId);
            foreach (var g in guards) g.enabled = false;
            var enemy = guards[0].GetComponent<LabHealth>();
            Place(player, new Vector3(0, .05f, -8));
            Place(enemy, new Vector3(0, .05f, -6.5f));
            yield return Drive(.15f);
            int shown = session.DamageNumbers.TotalShown;
            yield return Drive(.06f, null, true);
            yield return Drive(.46f);
            CheckResult("Actual melee contact displays one matching enemy damage number", enemy.CurrentHealth == 15 &&
                session.DamageNumbers.TotalShown == shown + 1 && session.DamageNumbers.LastAmount == 15 &&
                session.DamageNumbers.LastFaction == LabFaction.Enemy && session.DamageNumbers.ActiveCount > 0);
            Capture("03_DamageNumber");
            CheckResult("Rejected friendly damage creates no number", !enemy.TryTakeDamage(12, enemy) && session.DamageNumbers.TotalShown == shown + 1);
            player.TryTakeDamage(10, enemy);
            CheckResult("Player damage also displays exact applied damage", player.CurrentHealth == 90 &&
                session.DamageNumbers.LastAmount == 10 && session.DamageNumbers.LastFaction == LabFaction.Player);
            yield return Escape();
            CheckResult("Escape opens pause menu and releases cursor", session.Screen == LabGameSession.ScreenState.Paused &&
                Time.timeScale == 0 && !input.HasControl && Cursor.lockState == CursorLockMode.None && Cursor.visible && AudioListener.pause);
            float frozenTime = Time.time;
            float progress = combat.CurrentStepProgress;
            int popups = session.DamageNumbers.ActiveCount;
            float hp = player.CurrentHealth;
            int resolved = combat.ResolvedStrikeCount;
            musicTime = session.MusicSource.time;
            start = player.transform.position;
            yield return Drive(.8f, new[] { Key.W, Key.Space }, true, new Vector2(4, 4));
            CheckResult("Pause freezes position combo clocks damage numbers and health", Time.time == frozenTime && player.transform.position == start &&
                Mathf.Approximately(progress, combat.CurrentStepProgress) && popups == session.DamageNumbers.ActiveCount &&
                hp == player.CurrentHealth && resolved == combat.ResolvedStrikeCount);
            CheckResult("Paused damage and attack entry points are rejected", !player.TryTakeDamage(20, enemy) && !combat.TryAttack() &&
                !player.GetComponent<LabPlayerDodge>().TryStart(Vector3.forward));
            CheckResult("BGM continues while effects and gameplay are paused", session.MusicSource.isPlaying && session.MusicSource.time > musicTime + .3f && AudioListener.pause);
            Capture("04_PauseMenu");
            yield return Click(ButtonNamed("PauseSettingsButton"));
            CheckResult("Pause settings opens while simulation remains frozen", session.Screen == LabGameSession.ScreenState.Settings && Time.timeScale == 0);
            yield return SliderTo(session.Menu.EffectsSlider, .55f);
            yield return Escape();
            CheckResult("Escape from pause settings returns to pause", session.Screen == LabGameSession.ScreenState.Paused && Time.timeScale == 0);
            int resumeAttacks = combat.AttackCount;
            yield return Click(session.Menu.ResumeButton);
            CheckResult("Resume button restores control and cannot queue a menu click", session.Screen == LabGameSession.ScreenState.Playing &&
                Time.timeScale == 1 && !AudioListener.pause && input.HasControl && combat.AttackCount == resumeAttacks);
            yield return Drive(1f);
            CheckResult("Damage numbers expire after resumed gameplay time", session.DamageNumbers.ActiveCount == 0);
            enemy.TryTakeDamage(999, player);
            CheckResult("Overkill displays remaining health rather than requested damage", enemy.IsDead && session.DamageNumbers.LastAmount == 15);
            yield return Escape();
            int previousStage = stage.GetInstanceID();
            yield return Click(session.Menu.ExitButton);
            yield return AwaitScene(previousStage);
            player = stage.Player;
            CheckResult("Exit reloads a clean stage behind main menu", session.Screen == LabGameSession.ScreenState.MainMenu &&
                Time.timeScale == 0 && stage.GetInstanceID() != previousStage && player.CurrentHealth == 100 && stage.RemainingEnemies == 3 &&
                session.DamageNumbers.ActiveCount == 0);
            CheckResult("Scene reload preserves music volume and avoids duplicate services", session.MusicSource.GetInstanceID() == musicId &&
                Mathf.Abs(session.EffectsVolume - .55f) < .015f &&
                FindObjectsByType<LabGameSession>(FindObjectsSortMode.None).Length == 1 &&
                FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length == 5 &&
                FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length == 1 &&
                FindObjectsByType<LabCombatAudio>(FindObjectsSortMode.None).All(a => Mathf.Abs(a.GetComponent<AudioSource>().volume - .55f) < .015f));
            yield return Click(session.Menu.StartButton);
            CheckResult("Starting again begins a fresh playable run", session.Screen == LabGameSession.ScreenState.Playing && input.HasControl && player.CurrentHealth == 100);
            player.TryTakeDamage(100, null);
            yield return Drive(.4f);
            var animation = player.GetComponent<LabPlayerAnimation>();
            yield return Escape();
            float pose = animation.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            yield return Drive(.8f);
            CheckResult("Pause freezes death animation and postpones game-over UI", Time.timeScale == 0 &&
                Mathf.Abs(pose - animation.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime) < .001f &&
                !FindFirstObjectByType<LabStageHUD>().EndPanel.activeSelf);
            yield return Escape();
            yield return Drive(animation.DeathDuration + .2f);
            CheckResult("Resuming death reaches held pose and game-over panel", animation.DeathFinished && FindFirstObjectByType<LabStageHUD>().EndPanel.activeSelf);
            previousStage = stage.GetInstanceID();
            yield return Click(FindFirstObjectByType<LabStageHUD>().RestartButton);
            yield return AwaitScene(previousStage);
            CheckResult("Existing result Restart returns directly to gameplay", stage.GetInstanceID() != previousStage &&
                session.Screen == LabGameSession.ScreenState.Playing && Time.timeScale == 1 && stage.Player.CurrentHealth == 100 && stage.RemainingEnemies == 3);
            yield return Escape();
            previousStage = stage.GetInstanceID();
            yield return Click(session.Menu.ExitButton);
            yield return AwaitScene(previousStage);
            CheckResult("Repeated exit remains stable with one music source", session.Screen == LabGameSession.ScreenState.MainMenu &&
                session.MusicSource.GetInstanceID() == musicId && FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length == 5);
            RestorePreferences();
            Capture("05_FinalMainMenu");
            report.complete = true;
            Persist();
        }
        private void RestorePreferences()
        {
            if (originalValues == null) return;
            if (session != null)
            {
                session.SetMasterVolume(originalValues[0]);
                session.SetEffectsVolume(originalValues[1]);
                session.SetMusicVolume(originalValues[2]);
            }
            for (int i = 0; i < keys.Length; i++)
                if (hadPrefs[i]) PlayerPrefs.SetFloat(keys[i], originalPrefs[i]); else PlayerPrefs.DeleteKey(keys[i]);
            PlayerPrefs.Save();
            originalValues = null;
        }
        private void OnDestroy()
        {
            RestorePreferences();
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
