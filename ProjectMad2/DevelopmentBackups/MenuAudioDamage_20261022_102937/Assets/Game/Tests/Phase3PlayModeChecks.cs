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
    public sealed class Phase3PlayModeChecks : MonoBehaviour
    {
        [Serializable] public class Check { public string name; public bool passed; public string detail; }
        [Serializable] public class Report
        {
            public string phase="Phase 3"; public string timestamp; public bool complete;
            public int passed,failed; public List<Check> checks=new List<Check>();
        }
        private Report report=new Report();
        private LabStageController stage; private LabStageHUD hud; private LabExitDoor door;
        private LabPlayerInput input; private LabPlayerMotor motor; private LabPlayerCombat combat;
        private LabHealth player; private LabEnemyGuard[] guards; private CharacterController playerCC;
        private InputActionAsset actions,uiActions; private Keyboard keyboard; private Mouse mouse;
        private InputSettings.EditorInputBehaviorInPlayMode oldEditorInput;
        private InputSettings.BackgroundBehavior oldBackground; private bool oldRun,initialized;
        private static readonly Key[] NoKeys=new Key[0];
        private static T Get<T>(object obj,string field) =>
            (T)obj.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(obj);
        private string Root => Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        private void CheckResult(string name,bool passed,string detail)
        {
            report.checks.Add(new Check{name=name,passed=passed,detail=detail});
            if(passed)report.passed++;else report.failed++;
            Persist();
        }
        private void Persist()
        {
            report.timestamp=DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(9)).ToString("o");
            File.WriteAllText(Path.Combine(Root,"Phase3TestResults.json"),JsonUtility.ToJson(report,true));
        }
        private void Bind()
        {
            stage=FindFirstObjectByType<LabStageController>();hud=FindFirstObjectByType<LabStageHUD>();
            door=FindFirstObjectByType<LabExitDoor>();player=stage.Player;
            input=player.GetComponent<LabPlayerInput>();motor=player.GetComponent<LabPlayerMotor>();
            combat=player.GetComponent<LabPlayerCombat>();playerCC=player.GetComponent<CharacterController>();
            guards=FindObjectsByType<LabEnemyGuard>(FindObjectsSortMode.None).OrderBy(g=>g.name).ToArray();
            actions=Get<InputActionAsset>(input,"runtimeActions");actions.devices=new InputDevice[]{keyboard,mouse};
            uiActions=FindFirstObjectByType<InputSystemUIInputModule>().actionsAsset;
            uiActions.devices=new InputDevice[]{keyboard,mouse};input.CaptureCursor();
        }
        private IEnumerator Drive(float seconds,Key[] keys,bool click=false,Vector2 position=default(Vector2))
        {
            float end=Time.time+seconds;
            do {
                // Editor capture/focus changes must not interrupt synthetic gameplay input.
                if(input!=null && input.enabled && stage!=null && stage.State==LabStageController.StageState.Running && !input.HasControl) input.CaptureCursor();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
                var state=new MouseState{position=position};
                if(click)state=state.WithButton(MouseButton.Left);
                InputSystem.QueueStateEvent(mouse,state);
                yield return null;
            }while(Time.time<end);
        }
        private void Place(Vector3 position)
        {
            playerCC.enabled=false;player.transform.SetPositionAndRotation(position,Quaternion.identity);
            playerCC.enabled=true;Physics.SyncTransforms();
        }
        private Key[] MoveKeys(Vector3 delta)
        {
            var keys=new List<Key>();
            if(Mathf.Abs(delta.x)>.2f)keys.Add(delta.x>0?Key.D:Key.A);
            if(Mathf.Abs(delta.z)>.2f)keys.Add(delta.z>0?Key.W:Key.S);
            return keys.ToArray();
        }
        private Text Label(string name) => hud.GetComponentsInChildren<Text>(true).First(t=>t.name==name);
        private IEnumerator Capture(string name)
        {
            Directory.CreateDirectory(Path.Combine(Root,"Captures/Phase3"));
            ScreenCapture.CaptureScreenshot(Path.Combine(Root,"Captures/Phase3/"+name+".png"));
            yield return Drive(.25f,NoKeys);
        }
        private IEnumerator ClickRestart(string checkName)
        {
            float panelDeadline = Time.time + 5f;
            while (!hud.EndPanel.activeSelf && Time.time < panelDeadline) yield return Drive(.05f, NoKeys);
            var oldStage=stage;var oldFont=Label("HealthText").font;
            var point=RectTransformUtility.WorldToScreenPoint(null,hud.RestartButton.transform.position);
            yield return Drive(.1f,NoKeys,false,point);
            yield return Drive(.08f,NoKeys,true,point);
            yield return Drive(.1f,NoKeys,false,point);
            float deadline=Time.time+8;
            while(oldStage!=null && Time.time<deadline) yield return Drive(.05f,NoKeys,false,point);
            bool reloaded=oldStage==null;
            yield return Drive(.2f,NoKeys);
            if(reloaded) Bind();
            CheckResult(checkName+" keeps shared font atlas",reloaded && Label("HealthText").font==oldFont,"Font reused across scene reload.");
            CheckResult(checkName,reloaded && stage.State==LabStageController.StageState.Running &&
                stage.RemainingEnemies==3 && player.CurrentHealth==100 && !door.IsUnlocked,
                "Actual virtual mouse click through InputSystemUIInputModule; scene reloaded="+reloaded);
        }
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            oldBackground=InputSystem.settings.backgroundBehavior;oldRun=Application.runInBackground;
            oldEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            Application.runInBackground=true;
            keyboard=InputSystem.AddDevice<Keyboard>("Phase3TestKeyboard");
            mouse=InputSystem.AddDevice<Mouse>("Phase3TestMouse");initialized=true;
        }
        private IEnumerator Start()
        {
            yield return null;Bind();yield return Drive(.15f,NoKeys);
            CheckResult("Stage begins with 100 HP and three living guards",stage.State==LabStageController.StageState.Running &&
                player.CurrentHealth==100 && guards.Length==3 && stage.RemainingEnemies==3,"Initial state.");
            CheckResult("HUD displays initial health, enemy count and objective",
                Label("HealthText").text.Contains("100 / 100") && Label("EnemyText").text.Contains("3 / 3") &&
                Label("ObjectiveText").text.Contains("모두 처치") && !hud.EndPanel.activeSelf,"Korean HUD visible.");
            CheckResult("Korean font available",Label("HealthText").font.name.Contains("Malgun"),Label("HealthText").font.name);
            CheckResult("Health bar supports partial fill",Get<Image>(hud,"healthFill").sprite!=null &&
                Get<Image>(hud,"healthFill").type==Image.Type.Filled,"Built-in UI sprite assigned.");
            CheckResult("Exit initially locked",!door.IsUnlocked && Get<Collider>(door,"barrier").enabled &&
                Label("DoorText").text.Contains("잠김"),"Physical barrier and objective agree.");
            CheckResult("Restart ignored while running",!stage.IsRestarting,"No result screen at startup.");
            stage.Restart();CheckResult("Running stage cannot reload from Restart",!stage.IsRestarting,"State guard.");

            foreach(var g in guards)g.enabled=false;
            Place(new Vector3(0,.05f,8));yield return Drive(.6f,new[]{Key.W});
            CheckResult("Locked door physically blocks player",player.transform.position.z<9.1f,
                "Player z="+player.transform.position.z);
            CheckResult("Locked exit rejects player",!stage.TryExit(player) &&
                stage.State==LabStageController.StageState.Running,"Enemy count=3.");
            Place(new Vector3(0,.05f,10.55f));yield return Drive(.2f,NoKeys);
            CheckResult("Locked trigger cannot clear even if entered",stage.State==LabStageController.StageState.Running,
                "Player placed inside trigger for boundary test.");
            Place(new Vector3(0,.05f,-8));foreach(var g in guards)g.enabled=true;
            yield return Drive(.1f,NoKeys);

            float started=Time.time,nextClick=0,deadline=Time.time+65;
            int lastCount=3;var transitions=new List<int>{3}; bool hudCounts=true,defaultStats=true;
            foreach(var g in guards) defaultStats &= g.GetComponent<LabHealth>().MaximumHealth==30 &&
                Get<float>(g,"attackDamage")==10 && Get<float>(g,"moveSpeed")==2.5f;
            while(stage.RemainingEnemies>0 && !player.IsDead && Time.time<deadline)
            {
                var target=guards.Where(g=>!g.GetComponent<LabHealth>().IsDead)
                    .OrderBy(g=>(g.transform.position-player.transform.position).sqrMagnitude).FirstOrDefault();
                if(target==null)break;
                Vector3 delta=target.transform.position-player.transform.position;delta.y=0;
                float distance=delta.magnitude;
                float facing=distance>.001f?Vector3.Dot(player.transform.forward,delta.normalized):1;
                var keys=distance>1.65f || facing<.85f?MoveKeys(delta):NoKeys;
                bool attack=distance<2.15f && facing>.72f && Time.time>=nextClick;
                if(attack)nextClick=Time.time+.65f;
                yield return Drive(.055f,keys,attack);
                if(lastCount!=stage.RemainingEnemies){lastCount=stage.RemainingEnemies;transitions.Add(lastCount);}
                hudCounts &= Label("EnemyText").text.Contains(stage.RemainingEnemies+" / 3");
            }
            yield return Drive(.15f,NoKeys);
            CheckResult("Input-only combat clears all three default guards",stage.RemainingEnemies==0 && !player.IsDead && defaultStats,
                "WASD and mouse only, after restoring spawn; elapsed="+(Time.time-started)+
                "s, HP="+player.CurrentHealth+", attacks="+combat.AttackCount);
            CheckResult("Enemy counter tracks deaths",hudCounts && transitions.First()==3 && transitions.Last()==0,
                "Observed counts="+string.Join(",",transitions));
            bool noDoubleCount=true;
            foreach(var g in guards)noDoubleCount &= !g.GetComponent<LabHealth>().TryTakeDamage(15,player);
            CheckResult("Dead guards cannot decrement count twice",noDoubleCount && stage.RemainingEnemies==0,"Repeated damage rejected.");
            CheckResult("Final guard opens exit without instant victory",door.IsUnlocked &&
                stage.State==LabStageController.StageState.Running && !Get<Collider>(door,"barrier").enabled,
                "Door unlocked, barrier off, still must enter exit.");
            CheckResult("HUD switches objective to escape",Label("ObjectiveText").text.Contains("북쪽 출구") &&
                Label("DoorText").text.Contains("개방"),Label("ObjectiveText").text);
            CheckResult("Health HUD reflects combat damage",Label("HealthText").text.Contains(Mathf.CeilToInt(player.CurrentHealth)+" / 100") &&
                Mathf.Abs(Get<Image>(hud,"healthFill").fillAmount-player.CurrentHealth/100)<.001f,
                "HP="+player.CurrentHealth+", fill="+Get<Image>(hud,"healthFill").fillAmount);
            yield return Drive(1.3f,NoKeys);
            CheckResult("Open door lifts clear of player",Get<Transform>(door,"doorPanel").localPosition.y>4.9f,"Lifted door.");
            var indicator=Get<Renderer>(door,"indicator");var indicatorBlock=new MaterialPropertyBlock();indicator.GetPropertyBlock(indicatorBlock);
            CheckResult("Door status light remains visible and green after opening",indicator.transform.parent==door.transform && indicator.transform.position.y<4 && indicatorBlock.GetColor("_BaseColor").g>.9f,"Fixed header status light.");
            CheckResult("Only player can use exit",!stage.TryExit(guards[0].GetComponent<LabHealth>()),"Non-player rejected.");
            yield return Capture("Phase3_UnlockedHUD");

            deadline=Time.time+15;
            while(stage.State==LabStageController.StageState.Running && Time.time<deadline)
            {
                var delta=new Vector3(0,0,10.55f)-player.transform.position;delta.y=0;
                yield return Drive(.055f,MoveKeys(delta));
            }
            yield return Drive(.2f,NoKeys);
            CheckResult("Walking into unlocked exit wins",stage.State==LabStageController.StageState.Cleared,
                "Exit trigger activated through actual CharacterController movement; pos="+player.transform.position);
            CheckResult("Clear UI appears",hud.EndPanel.activeSelf && Label("ResultTitle").text.Contains("탈출 성공"),
                Label("ResultTitle").text);
            CheckResult("Victory disables player actions",!input.enabled && !motor.enabled && !combat.enabled &&
                !player.GetComponent<LabPlayerDodge>().enabled && !playerCC.enabled,"Movement, attack, dodge and CC stopped.");
            CheckResult("Victory releases cursor",!input.HasControl && Cursor.lockState==CursorLockMode.None && Cursor.visible,"Cursor available for UI.");
            float hp=player.CurrentHealth;bool lateDamage=player.TryTakeDamage(100,null);
            CheckResult("Victory cannot become game over from late damage",!lateDamage && player.CurrentHealth==hp &&
                stage.State==LabStageController.StageState.Cleared,"Terminal state protected.");
            yield return Capture("Phase3_ClearScreen");
            yield return ClickRestart("Clear-screen mouse button restarts scene");
            CheckResult("Restart restores controls and colliders",input.enabled && motor.enabled && combat.enabled &&
                playerCC.enabled && guards.All(g=>g.enabled && g.GetComponent<CharacterController>().enabled),
                "Fresh scene actors.");
            CheckResult("Restart hides result screen and resets HUD",!hud.EndPanel.activeSelf &&
                Label("EnemyText").text.Contains("3 / 3") && Label("HealthText").text.Contains("100 / 100"),"HUD restored.");

            // Approach an actual enemy using movement, then accept its default damage until death.
            deadline=Time.time+35;
            while(!player.IsDead && Time.time<deadline)
            {
                var nearest=guards.OrderBy(g=>(g.transform.position-player.transform.position).sqrMagnitude).First();
                Vector3 delta=nearest.transform.position-player.transform.position;delta.y=0;
                yield return Drive(.08f,delta.magnitude>1.75f?MoveKeys(delta):NoKeys);
            }
            yield return Drive(.2f,NoKeys);
            CheckResult("Default enemy attacks cause game over",player.IsDead &&
                stage.State==LabStageController.StageState.GameOver,"HP="+player.CurrentHealth+
                ", enemy successful hits="+guards.Sum(g=>g.SuccessfulHitCount));
            CheckResult("Death animation stays visible before result panel", !hud.EndPanel.activeSelf &&
                player.GetComponent<LabPlayerAnimation>().CurrentState == "Death", "Game over locks controls immediately while the death clip plays.");
            yield return Drive(player.GetComponent<LabPlayerAnimation>().DeathDuration + .2f, NoKeys);
            CheckResult("Game-over UI and empty health bar",hud.EndPanel.activeSelf &&
                Label("ResultTitle").text=="게임 오버" && Get<Image>(hud,"healthFill").fillAmount==0 &&
                Label("HealthText").text.Contains("0 / 100"),"Death HUD state.");
            var before=player.transform.position;int attacks=combat.AttackCount;
            yield return Drive(.4f,new[]{Key.W,Key.Space},true);
            CheckResult("Death prevents movement attacks and dodge",Vector3.Distance(before,player.transform.position)<.001f &&
                attacks==combat.AttackCount && !player.GetComponent<LabPlayerDodge>().IsDodging,"Input attempted after death.");
            CheckResult("Game over stops all guard AI",guards.All(g=>!g.enabled && !g.IsWindingUp),"No attacks under result screen.");
            CheckResult("Game over cannot trigger victory",!stage.TryExit(player) &&
                stage.State==LabStageController.StageState.GameOver,"Terminal state stays GameOver.");
            yield return Capture("Phase3_GameOverScreen");
            yield return ClickRestart("Game-over mouse button restarts scene");

            foreach(var g in guards)g.enabled=false;
            foreach(var g in guards)g.GetComponent<LabHealth>().TryTakeDamage(30,player);
            player.TryTakeDamage(100,null);
            CheckResult("Player death after final kill still loses before escape",door.IsUnlocked &&
                stage.RemainingEnemies==0 && stage.State==LabStageController.StageState.GameOver && !stage.TryExit(player),
                "Same-frame damage boundary; direct damage used only for this fixture.");
            yield return ClickRestart("Repeated restart resets subscriptions and enemy count");
            CheckResult("Final fresh scene is playable",stage.RemainingEnemies==3 && player.CurrentHealth==100 &&
                !door.IsUnlocked && !hud.EndPanel.activeSelf && input.HasControl,"Three successive reloads completed.");
            report.complete=true;Persist();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if(!initialized)return;
            if(actions!=null)actions.devices=null;if(uiActions!=null)uiActions.devices=null;
            if(keyboard!=null && keyboard.added)InputSystem.RemoveDevice(keyboard);
            if(mouse!=null && mouse.added)InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior=oldBackground;Application.runInBackground=oldRun;
            InputSystem.settings.editorInputBehaviorInPlayMode=oldEditorInput;
        }
    }
}
#endif
