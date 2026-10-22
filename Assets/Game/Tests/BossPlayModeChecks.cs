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
    public sealed class BossPlayModeChecks : MonoBehaviour
    {
        [Serializable] public class Check { public string name, detail; public bool passed; }
        [Serializable] public class Report { public bool complete; public int passed, failed; public List<Check> checks = new List<Check>(); }
        public string ReportDirectory;
        private Report report = new Report();
        private LabStageController stage;
        private LabBossGolem boss;
        private LabHealth player;
        private LabPlayerInput input;
        private LabStageHUD hud;
        private Keyboard keyboard;
        private Mouse mouse;
        private bool oldRun;
        private InputSettings.BackgroundBehavior oldBackground;
        private InputSettings.EditorInputBehaviorInPlayMode oldEditor;
        private GameObject wall;
        private static readonly Key[] NoKeys = new Key[0];
        private string Output => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ReportDirectory));
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            oldRun = Application.runInBackground;
            oldBackground = InputSystem.settings.backgroundBehavior;
            oldEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>("BossTestKeyboard");
            mouse = InputSystem.AddDevice<Mouse>("BossTestMouse");
        }
        private void Bind()
        {
            stage = FindFirstObjectByType<LabStageController>(); player = stage.Player; boss = stage.Boss;
            hud = FindFirstObjectByType<LabStageHUD>();
            input = player.GetComponent<LabPlayerInput>();
            var actions = (InputActionAsset)typeof(LabPlayerInput).GetField("runtimeActions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(input);
            actions.devices = new InputDevice[] { keyboard, mouse };
        }
        private void Save() { Directory.CreateDirectory(Output); File.WriteAllText(Path.Combine(Output, "BossChecks.json"), JsonUtility.ToJson(report, true)); }
        private void CheckResult(string name, bool ok, string detail = "")
        {
            report.checks.Add(new Check { name = name, passed = ok, detail = detail });
            if(ok) report.passed++; else report.failed++; Save();
        }
        private IEnumerator Drive(float seconds, bool click = false, params Key[] held)
        {
            float until = Time.realtimeSinceStartup + seconds;
            do {
                if(input != null && input.isActiveAndEnabled && !LabGameSession.GameplayBlocked && !input.HasControl) input.CaptureCursor();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(held));
                var state = new MouseState(); if(click) state = state.WithButton(MouseButton.Left);
                InputSystem.QueueStateEvent(mouse, state);
                yield return null;
            } while(Time.realtimeSinceStartup < until);
        }
        private IEnumerator Until(Func<bool> predicate, float timeout = 7f)
        {
            float until = Time.realtimeSinceStartup + timeout;
            while(!predicate() && Time.realtimeSinceStartup < until) yield return Drive(.015f);
        }
        private void Place(Transform target, Vector3 point, Quaternion rotation)
        {
            var cc = target.GetComponent<CharacterController>(); bool active = cc.enabled; cc.enabled = false;
            target.SetPositionAndRotation(point, rotation); cc.enabled = active; Physics.SyncTransforms();
        }
        private void InFront(float distance = 2.2f)
        {
            player.GetComponent<LabPlayerCombat>().CancelCombo();
            player.GetComponent<LabPlayerDodge>().Cancel();
            Place(player.transform, boss.transform.position + boss.transform.forward * distance,
                Quaternion.LookRotation(-boss.transform.forward));
        }
        private void Capture(string name) { Directory.CreateDirectory(Path.Combine(Output,"Captures")); ScreenCapture.CaptureScreenshot(Path.Combine(Output,"Captures",name+".png")); }
        private void KillGuards()
        {
            foreach(var guard in FindObjectsByType<LabEnemyGuard>(FindObjectsSortMode.None))
            { guard.enabled = false; guard.GetComponent<LabHealth>().TryTakeDamage(1000, player); }
        }
        private IEnumerator Start()
        {
            yield return null; Bind();
            CheckResult("Boss is dormant and exit locked before starting", stage.RemainingEnemies == 3 && !boss.HasAwakened && !boss.gameObject.activeSelf && !stage.ExitReady);
            yield return Drive(.15f, true, Key.W);
            CheckResult("Main menu cannot activate boss", !boss.HasAwakened && !hud.BossPanel.activeSelf);
            LabGameSession.Instance.StartGame(); yield return Drive(.1f);
            var guards = FindObjectsByType<LabEnemyGuard>(FindObjectsSortMode.None);
            foreach(var guard in guards) guard.enabled = false;
            guards[0].GetComponent<LabHealth>().TryTakeDamage(1000,player);
            guards[1].GetComponent<LabHealth>().TryTakeDamage(1000,player);
            CheckResult("Two guard deaths do not summon boss", stage.RemainingEnemies == 1 && !boss.HasAwakened);
            guards[2].GetComponent<LabHealth>().TryTakeDamage(1000,player);
            yield return Drive(.15f);
            CheckResult("Last guard summons boss once and keeps exit locked", boss.HasAwakened && boss.State == LabBossGolem.BossState.Introduction && !stage.ExitReady && !stage.TryExit(player));
            float introEnd = boss.StateEndsAt; boss.Awaken(player);
            CheckResult("Repeated awaken cannot restart the encounter", boss.StateEndsAt == introEnd);
            CheckResult("Boss appears away from player with full health", Vector3.Distance(player.transform.position,boss.transform.position) >= 4 && boss.Health.CurrentHealth == 300);
            CheckResult("Introduction cannot deal or receive damage", boss.AttackCount == 0 && !boss.Health.TryTakeDamage(15,player));
            CheckResult("Boss health bar is visible at full health", hud.BossPanel.activeSelf && hud.BossFill.fillAmount == 1);
            CheckResult("Boss rig and all seven motions are valid", boss.Animator.avatar.isValid && !boss.Animator.isHuman && !boss.Animator.applyRootMotion &&
                new[]{"Idle","Walk","Run","Roar","Punch","Swipe","Death"}.All(s=>boss.Animator.HasState(0,Animator.StringToHash("Base Layer."+s))));
            Capture("01_BossIntroduction"); yield return Drive(.15f);
            LabGameSession.Instance.PauseGame(); yield return Drive(.05f);
            var origin=boss.transform.position;float frozen=Time.time,pose=boss.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            yield return Drive(.35f);
            CheckResult("Pause freezes boss clock movement and pose", Time.time==frozen && boss.transform.position==origin && Mathf.Abs(boss.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime-pose)<.001f);
            CheckResult("Pause hides gameplay boss bar canvas", !hud.GetComponent<Canvas>().enabled);
            LabGameSession.Instance.ResumeGame();
            yield return Until(()=>boss.State==LabBossGolem.BossState.Chase);
            CheckResult("Roar finishes and enables combat", boss.Health.enabled && boss.State==LabBossGolem.BossState.Chase);
            Place(boss.transform,new Vector3(0,.05f,2),Quaternion.Euler(0,180,0));
            Place(player.transform,new Vector3(0,.05f,-7),Quaternion.identity);
            origin=boss.transform.position;yield return Drive(.4f);
            CheckResult("Distant boss runs toward player", boss.transform.position.z<origin.z-1 &&
                boss.Animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Run"),boss.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime.ToString());
            InFront();yield return Until(()=>boss.State==LabBossGolem.BossState.Windup);
            CheckResult("First attack is telegraphed punch", !boss.IsSwipe && boss.WarningVisible && boss.AttackCount==1);
            var facing=boss.transform.rotation;float hp=player.CurrentHealth;
            Place(player.transform,boss.transform.position+boss.transform.right*3,Quaternion.identity);
            yield return Until(()=>boss.State==LabBossGolem.BossState.Recovery);
            CheckResult("Attack commits its direction and can be sidestepped", Quaternion.Angle(facing,boss.transform.rotation)<.01f && player.CurrentHealth==hp && boss.HitAttempts==1);
            CheckResult("Warning ends after contact and recovery gives an opening", !boss.WarningVisible && boss.StateEndsAt-Time.time>.4f);
            InFront();yield return Until(()=>boss.State==LabBossGolem.BossState.Windup);
            CheckResult("Second attack alternates to wide swipe", boss.IsSwipe && boss.WarningVisible);
            Capture("02_SwipeWarning");
            yield return Until(()=>boss.State==LabBossGolem.BossState.Attack);
            hp=player.CurrentHealth;
            yield return Until(()=>boss.HitAttempts==2);
            CheckResult("Swipe applies exactly 22 damage at contact", hp-player.CurrentHealth==22, "Damage="+(hp-player.CurrentHealth));
            hp=player.CurrentHealth;yield return Until(()=>boss.State==LabBossGolem.BossState.Recovery);
            CheckResult("One animation cannot apply repeated damage", player.CurrentHealth==hp && boss.HitAttempts==2);
            CheckResult("Attack sounds play once per contact", boss.GetComponent<LabCombatAudio>().RobotPlayCount==2);
            InFront();yield return Until(()=>boss.State==LabBossGolem.BossState.Attack);
            yield return Until(()=>boss.ContactAt-Time.time<.09f,3);
            hp=player.CurrentHealth;
            yield return Drive(.05f,false,Key.Space);
            bool invulnerable=player.GetComponent<LabPlayerDodge>().IsInvulnerable;
            yield return Until(()=>boss.HitAttempts==3);
            CheckResult("Timed Space dodge avoids boss contact", invulnerable && player.CurrentHealth==hp);
            yield return Until(()=>boss.State==LabBossGolem.BossState.Recovery);
            InFront();yield return Until(()=>boss.State==LabBossGolem.BossState.Windup);
            wall=new GameObject("BossSightWall_RUNTIME_ONLY");wall.transform.position=boss.transform.position+boss.transform.forward*1.1f+Vector3.up;
            wall.transform.rotation=boss.transform.rotation;wall.AddComponent<BoxCollider>().size=new Vector3(2,2,.2f);Physics.SyncTransforms();
            hp=player.CurrentHealth;yield return Until(()=>boss.State==LabBossGolem.BossState.Recovery);
            CheckResult("Boss cannot damage through solid wall", player.CurrentHealth==hp && boss.HitAttempts==4);
            Destroy(wall);wall=null;yield return Drive(.05f);
            boss.Suspend();InFront(1.8f);yield return Drive(.15f);
            float before=boss.Health.CurrentHealth;int shown=LabGameSession.Instance.DamageNumbers.TotalShown;
            for(int i=0;i<3;i++){yield return Drive(.05f,true);yield return Drive(.06f);}
            yield return Drive(1.8f);
            CheckResult("Three actual mouse clicks deal three 15-damage combo hits", before-boss.Health.CurrentHealth==45,"Damage="+(before-boss.Health.CurrentHealth));
            CheckResult("Boss health bar tracks remaining fraction", Mathf.Abs(hud.BossFill.fillAmount-boss.Health.CurrentHealth/300)<.001f);
            CheckResult("Boss hits generate floating damage numbers", LabGameSession.Instance.DamageNumbers.TotalShown>=shown+3);
            Capture("03_BossHealthAfterCombo");
            var enemy=guards[0].GetComponent<LabHealth>();
            CheckResult("Friendly and invalid damage cannot hurt boss", !boss.Health.TryTakeDamage(float.NaN,player) && !boss.Health.TryTakeDamage(10,enemy));
            LabGameSession.Instance.PauseGame();
            CheckResult("Paused combat rejects boss damage", !boss.Health.TryTakeDamage(10,player));
            LabGameSession.Instance.ResumeGame();
            int deaths=0;boss.Health.Died+=_=>deaths++;
            boss.Health.TryTakeDamage(1000,player);bool duplicate=boss.Health.TryTakeDamage(1,player);
            yield return Drive(.2f);
            CheckResult("Boss death happens once and disables collision", deaths==1 && !duplicate && boss.IsDefeated && !boss.GetComponent<CharacterController>().enabled);
            CheckResult("Only defeated boss unlocks exit", stage.ExitReady && FindFirstObjectByType<LabExitDoor>().IsUnlocked);
            CheckResult("Boss death hides its health bar", !hud.BossPanel.activeSelf);
            yield return Drive(3.6f);
            CheckResult("Death animation reaches its final pose", boss.Animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Death") && boss.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime>=.99f);
            var head=boss.Animator.GetComponentsInChildren<Transform>().First(t=>t.name=="mixamorig:Head");
            origin=head.position;yield return Drive(.3f);
            CheckResult("Dead boss stays still at final pose", Vector3.Distance(origin,head.position)<.005f);
            Capture("04_BossDefeated"); yield return Drive(.15f);
            CheckResult("Exit clears completed encounter", stage.TryExit(player) && stage.State==LabStageController.StageState.Cleared);
            stage.Restart();yield return Drive(1.2f);Bind();yield return Drive(.1f);
            CheckResult("Restart restores three guards and dormant boss", stage.RemainingEnemies==3 && !boss.HasAwakened && !boss.gameObject.activeSelf && !stage.ExitReady);
            KillGuards();yield return Drive(.2f);
            player.TryTakeDamage(1000,null);yield return Drive(.1f);
            CheckResult("Player death during boss intro produces Game Over and stops boss", stage.State==LabStageController.StageState.GameOver && boss.State==LabBossGolem.BossState.Suspended && !stage.ExitReady);
            LabGameSession.Instance.PauseGame();yield return Drive(.1f);
            LabGameSession.Instance.ReturnToMenu();yield return Drive(1.2f);Bind();yield return Drive(.1f);
            CheckResult("Return to menu reloads fresh dormant encounter", LabGameSession.GameplayBlocked && !boss.HasAwakened && stage.RemainingEnemies==3 && !hud.GetComponent<Canvas>().enabled);
            report.complete=true;Save();
        }
        private void OnDestroy()
        {
            if(wall!=null)Destroy(wall);
            if(keyboard!=null && keyboard.added)InputSystem.RemoveDevice(keyboard);
            if(mouse!=null && mouse.added)InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior=oldBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;
            Application.runInBackground=oldRun;
        }
    }
}
#endif
