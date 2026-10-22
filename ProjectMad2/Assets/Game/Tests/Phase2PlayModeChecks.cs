#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace MadLab
{
    public sealed class Phase2PlayModeChecks : MonoBehaviour
    {
        [Serializable] public class Check { public string name; public bool passed; public string detail; }
        [Serializable] public class Report
        {
            public string phase="Phase 2";
            public string timestamp;
            public bool complete;
            public int passed,failed;
            public List<Check> checks=new List<Check>();
        }
        private Report report=new Report();
        private LabPlayerInput input;
        private LabPlayerMotor motor;
        private LabPlayerDodge dodge;
        private LabPlayerCombat combat;
        private LabThirdPersonCamera orbit;
        private LabEnemyGuard guard;
        private LabHealth playerHealth,enemyHealth;
        private CharacterController playerCC,enemyCC;
        private InputActionAsset actions;
        private Keyboard keyboard;
        private Mouse mouse;
        private bool oldRunInBackground,initialized;
        private InputSettings.BackgroundBehavior oldBackground;
        private static readonly Key[] NoKeys=new Key[0];
        private static void Set(object o,string name,object value)
        {
            o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
        }
        private static T Get<T>(object o,string name)
        {
            return (T)o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
        }
        private void CheckResult(string name,bool passed,string detail)
        {
            report.checks.Add(new Check{name=name,passed=passed,detail=detail});
            if(passed)report.passed++;else report.failed++;
        }
        private void Persist()
        {
            report.timestamp=DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(9)).ToString("o");
            File.WriteAllText(Path.Combine(Application.dataPath,"..","Phase2TestResults.json"),JsonUtility.ToJson(report,true));
        }
        private IEnumerator Drive(float seconds,Key[] keys,bool click=false,Vector2 mouseDelta=default(Vector2))
        {
            float end=Time.time+seconds;
            do {
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
                var state=new MouseState{delta=mouseDelta};
                if(click)state=state.WithButton(MouseButton.Left);
                InputSystem.QueueStateEvent(mouse,state);
                yield return null;
            }while(Time.time<end);
        }
        private void Place(CharacterController cc,Vector3 position,float yaw)
        {
            cc.enabled=false;cc.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));cc.enabled=true;
        }
        private IEnumerator ResetCombat(Vector3 playerPosition,Vector3 enemyPosition,bool ai=false,float playerYaw=0)
        {
            guard.enabled=false;motor.enabled=false;combat.enabled=false;dodge.Cancel();
            Set(playerHealth,"<CurrentHealth>k__BackingField",100f);
            Set(enemyHealth,"<CurrentHealth>k__BackingField",30f);
            Place(playerCC,playerPosition,playerYaw);Place(enemyCC,enemyPosition,180);
            input.enabled=true;dodge.enabled=true;motor.enabled=true;combat.enabled=true;
            Set(dodge,"nextReadyTime",0f);Set(dodge,"invulnerableUntil",0f);
            Set(combat,"nextAttackTime",0f);Set(combat,"<AttackCount>k__BackingField",0);
            Set(combat,"attackRange",2.2f);
            Set(guard,"<State>k__BackingField",LabEnemyGuard.GuardState.Idle);
            Set(guard,"<IsWindingUp>k__BackingField",false);
            Set(guard,"<AttackCount>k__BackingField",0);Set(guard,"<SuccessfulHitCount>k__BackingField",0);
            Set(guard,"nextAttackTime",0f);Set(guard,"attackDamage",10f);Set(guard,"target",playerHealth);
            Set(orbit,"yaw",0f);Set(orbit,"pitch",20f);
            input.CaptureCursor();guard.enabled=ai;Physics.SyncTransforms();
            yield return Drive(.12f,NoKeys);
        }
        private IEnumerator Start()
        {
            input=FindFirstObjectByType<LabPlayerInput>();motor=input.GetComponent<LabPlayerMotor>();
            dodge=input.GetComponent<LabPlayerDodge>();combat=input.GetComponent<LabPlayerCombat>();
            orbit=FindFirstObjectByType<LabThirdPersonCamera>();guard=FindFirstObjectByType<LabEnemyGuard>();
            playerHealth=input.GetComponent<LabHealth>();enemyHealth=guard.GetComponent<LabHealth>();
            playerCC=input.GetComponent<CharacterController>();enemyCC=guard.GetComponent<CharacterController>();
            oldBackground=InputSystem.settings.backgroundBehavior;oldRunInBackground=Application.runInBackground;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;Application.runInBackground=true;
            keyboard=InputSystem.AddDevice<Keyboard>("Phase2TestKeyboard");mouse=InputSystem.AddDevice<Mouse>("Phase2TestMouse");
            actions=Get<InputActionAsset>(input,"runtimeActions");actions.devices=new InputDevice[]{keyboard,mouse};
            initialized=true;input.CaptureCursor();
            yield return Drive(.15f,NoKeys);

            CheckResult("Initial health and factions",playerHealth.CurrentHealth==100 && enemyHealth.CurrentHealth==30 &&
                playerHealth.Faction==LabFaction.Player && enemyHealth.Faction==LabFaction.Enemy,"Player 100; guard 30.");
            CheckResult("Input actions include Attack and exclude Jump",actions.FindAction("Gameplay/Attack")!=null &&
                actions.FindAction("Jump")==null,"Left mouse Attack and Space Dodge.");

            yield return ResetCombat(new Vector3(0,.05f,-3),new Vector3(0,.05f,-1.5f));
            yield return Drive(.05f,NoKeys,true);
            CheckResult("Mouse attack damages guard once",enemyHealth.CurrentHealth==15 && combat.LastHitCount==1 &&
                playerHealth.CurrentHealth==100,"Guard HP="+enemyHealth.CurrentHealth+", hits="+combat.LastHitCount);
            var block=new MaterialPropertyBlock();
            enemyHealth.GetComponentInChildren<MeshRenderer>().GetPropertyBlock(block);
            CheckResult("Damage flashes the guard",block.GetColor("_BaseColor").r>.9f,"PropertyBlock red channel="+block.GetColor("_BaseColor").r);
            yield return Drive(.05f,NoKeys);yield return Drive(.05f,NoKeys,true);
            CheckResult("Attack cooldown rejects spam",combat.AttackCount==1 && enemyHealth.CurrentHealth==15,"Attacks="+combat.AttackCount);
            yield return Drive(.6f,NoKeys);yield return Drive(.05f,NoKeys,true);
            CheckResult("Two valid hits kill 30 HP guard",enemyHealth.IsDead && enemyHealth.CurrentHealth==0,"HP="+enemyHealth.CurrentHealth);

            yield return ResetCombat(new Vector3(0,.05f,-3),new Vector3(0,.05f,-1.5f));
            var extra=new GameObject("Test_ExtraHurtbox");extra.transform.SetParent(guard.transform,false);extra.transform.localPosition=Vector3.up*.9f;
            var extraCollider=extra.AddComponent<BoxCollider>();extraCollider.size=Vector3.one;extraCollider.isTrigger=true;
            Physics.SyncTransforms();yield return Drive(.05f,NoKeys,true);
            CheckResult("Multiple colliders do not duplicate damage",enemyHealth.CurrentHealth==15 && combat.LastHitCount==1,
                "Root CharacterController plus child trigger: HP="+enemyHealth.CurrentHealth);
            Destroy(extra);yield return Drive(.04f,NoKeys);

            yield return ResetCombat(new Vector3(0,.05f,-3),new Vector3(0,.05f,-4.5f));
            yield return Drive(.05f,NoKeys,true);
            CheckResult("Targets behind player are excluded",enemyHealth.CurrentHealth==30,"HP="+enemyHealth.CurrentHealth);
            yield return ResetCombat(new Vector3(0,.05f,-3),new Vector3(0,.05f,.5f));
            yield return Drive(.05f,NoKeys,true);
            CheckResult("Targets outside range are excluded",enemyHealth.CurrentHealth==30,"Distance 3.5m; HP="+enemyHealth.CurrentHealth);
            yield return ResetCombat(new Vector3(-4,.05f,.8f),new Vector3(-4,.05f,3f));
            Set(combat,"attackRange",3f);yield return Drive(.05f,NoKeys,true);
            CheckResult("Wall blocks player damage",enemyHealth.CurrentHealth==30 &&
                !LabMeleeQuery.HasLineOfSight(input.transform,guard.transform),"Dodge_Blocker between attacker and target.");

            yield return ResetCombat(new Vector3(0,.05f,-3),new Vector3(0,.05f,-1.5f));
            yield return Drive(.05f,new[]{Key.Space},true);
            CheckResult("Attack cannot start during dodge",dodge.IsDodging && combat.AttackCount==0 && enemyHealth.CurrentHealth==30,
                "Dodge and click on the same input frame.");
            yield return ResetCombat(new Vector3(0,.05f,-3),new Vector3(0,.05f,-1.5f));
            input.ReleaseCursor();yield return Drive(.05f,NoKeys,true);
            CheckResult("Cursor capture click does not attack",input.HasControl && combat.AttackCount==0 && enemyHealth.CurrentHealth==30,
                "Capture was consumed before Attack.");
            yield return Drive(.05f,NoKeys);yield return Drive(.05f,NoKeys,true);
            CheckResult("Next click attacks after cursor capture",enemyHealth.CurrentHealth==15,"HP="+enemyHealth.CurrentHealth);

            yield return ResetCombat(new Vector3(0,.05f,10),new Vector3(0,.05f,8.5f));
            CheckResult("Invalid damage is rejected",!playerHealth.TryTakeDamage(-5,enemyHealth) &&
                !playerHealth.TryTakeDamage(float.NaN,enemyHealth) && !playerHealth.TryTakeDamage(float.PositiveInfinity,enemyHealth) &&
                playerHealth.CurrentHealth==100,"Negative, NaN and infinity.");
            var ally=new GameObject("Test_Ally").AddComponent<LabHealth>();
            CheckResult("Friendly and self damage are rejected",!playerHealth.TryTakeDamage(10,ally) &&
                !playerHealth.TryTakeDamage(10,playerHealth),"Same faction and same actor.");
            Destroy(ally.gameObject);
            yield return Drive(.05f,new[]{Key.Space});
            CheckResult("Dodge begins with invulnerability",dodge.IsInvulnerable &&
                !playerHealth.TryTakeDamage(10,enemyHealth) && playerHealth.CurrentHealth==100,
                "Damage rejected by LabHealth.");
            yield return Drive(.16f,NoKeys);
            bool stillDodging=dodge.IsDodging;
            CheckResult("Invulnerability expires before dodge ends",stillDodging && !dodge.IsInvulnerable &&
                playerHealth.TryTakeDamage(10,enemyHealth) && playerHealth.CurrentHealth==90,
                "StillDodging="+stillDodging+", HP="+playerHealth.CurrentHealth);
            yield return Drive(.15f,NoKeys);
            CheckResult("Damage works after dodge",!dodge.IsDodging && playerHealth.TryTakeDamage(10,enemyHealth) &&
                playerHealth.CurrentHealth==80,"HP="+playerHealth.CurrentHealth);

            yield return ResetCombat(new Vector3(0,.05f,-6),new Vector3(0,.05f,4),true);
            Vector3 before=guard.transform.position;yield return Drive(.2f,NoKeys);
            CheckResult("Guard remains Idle outside detection",guard.State==LabEnemyGuard.GuardState.Idle &&
                Vector3.Distance(before,guard.transform.position)<.05f,"State="+guard.State);
            Place(playerCC,new Vector3(0,.05f,-3),0);Physics.SyncTransforms();
            before=guard.transform.position;yield return Drive(.4f,NoKeys);
            CheckResult("Guard detects and chases at configured speed",guard.State==LabEnemyGuard.GuardState.Chase &&
                before.z-guard.transform.position.z>.8f && before.z-guard.transform.position.z<1.2f,
                "Travel="+(before.z-guard.transform.position.z));
            Place(playerCC,new Vector3(-10,.05f,-10),0);Physics.SyncTransforms();yield return Drive(.12f,NoKeys);
            CheckResult("Guard loses distant player",guard.State==LabEnemyGuard.GuardState.Idle,"State="+guard.State);

            yield return ResetCombat(new Vector3(0,.05f,-3),new Vector3(0,.05f,-1.5f),true);
            CheckResult("Attack has a visible windup",guard.State==LabEnemyGuard.GuardState.Attack && guard.IsWindingUp &&
                guard.transform.Find("AttackTelegraph").gameObject.activeSelf && playerHealth.CurrentHealth==100,
                "No damage before 0.45 second strike.");
            yield return Drive(.45f,NoKeys);
            CheckResult("Guard strike deals 10 damage",playerHealth.CurrentHealth==90 && guard.SuccessfulHitCount==1,
                "HP="+playerHealth.CurrentHealth);
            yield return Drive(.25f,NoKeys);
            CheckResult("Guard attack cooldown prevents repeated hits",playerHealth.CurrentHealth==90 && guard.AttackCount==1,
                "HP="+playerHealth.CurrentHealth+", attacks="+guard.AttackCount);
            yield return Drive(1.2f,NoKeys);
            CheckResult("Guard can attack again after cooldown",playerHealth.CurrentHealth==80 && guard.SuccessfulHitCount==2,
                "HP="+playerHealth.CurrentHealth+", hits="+guard.SuccessfulHitCount);

            yield return ResetCombat(new Vector3(0,.05f,-3),new Vector3(0,.05f,-1.5f),true);
            yield return Drive(.05f,new[]{Key.S,Key.Space});yield return Drive(.5f,NoKeys);
            CheckResult("Dodge escapes an enemy strike range",playerHealth.CurrentHealth==100 && guard.SuccessfulHitCount==0 &&
                Vector3.Distance(input.transform.position,guard.transform.position)>1.8f,
                "HP="+playerHealth.CurrentHealth+", distance="+Vector3.Distance(input.transform.position,guard.transform.position));

            yield return ResetCombat(new Vector3(0,.05f,11.05f),new Vector3(0,.05f,9.55f),true);
            float untilDodge=Mathf.Max(0f,Get<float>(guard,"strikeTime")-Time.time-.06f);
            yield return Drive(untilDodge,NoKeys);
            yield return Drive(.04f,new[]{Key.Space});yield return Drive(.07f,NoKeys);
            CheckResult("Timed dodge blocks an in-range AI hit",playerHealth.CurrentHealth==100 && guard.SuccessfulHitCount==0 &&
                dodge.IsInvulnerable && LabMeleeQuery.CanReach(guard.transform,input.transform,1.8f,120),
                "Wall blocks displacement; target remains in range during invulnerability.");

            yield return ResetCombat(new Vector3(0,.05f,-3),new Vector3(0,.05f,-1.5f),true);
            int deathEvents=0;Action<LabHealth> deathListener=h=>deathEvents++;enemyHealth.Died+=deathListener;
            enemyHealth.TryTakeDamage(100,playerHealth);
            bool duplicateAccepted=enemyHealth.TryTakeDamage(100,playerHealth);
            bool deadSourceAccepted=playerHealth.TryTakeDamage(10,enemyHealth);
            before=guard.transform.position;yield return Drive(.6f,NoKeys);
            CheckResult("Enemy death event occurs once",deathEvents==1 && !duplicateAccepted && enemyHealth.CurrentHealth==0,
                "Death events="+deathEvents);
            CheckResult("Dead guard stops and cancels windup",guard.State==LabEnemyGuard.GuardState.Dead && !guard.IsWindingUp &&
                !enemyCC.enabled && Vector3.Distance(before,guard.transform.position)<.01f && playerHealth.CurrentHealth==100,
                "State="+guard.State+", player HP="+playerHealth.CurrentHealth);
            CheckResult("Dead source cannot inflict damage",!deadSourceAccepted,"Central damage guard.");
            enemyHealth.Died-=deathListener;

            yield return ResetCombat(new Vector3(0,.05f,-3),new Vector3(0,.05f,-1.5f),true);
            Set(guard,"attackDamage",100f);
            yield return Drive(.5f,NoKeys);
            CheckResult("AI strike can kill player",playerHealth.IsDead && playerHealth.CurrentHealth==0,"HP="+playerHealth.CurrentHealth);
            Vector3 playerBefore=input.transform.position;
            yield return Drive(.2f,new[]{Key.W,Key.Space},true);
            CheckResult("Dead player cannot move attack or dodge",!motor.enabled && !combat.enabled && !dodge.enabled &&
                !input.enabled && !playerCC.enabled && !input.HasControl && Vector3.Distance(playerBefore,input.transform.position)<.01f,
                "Movement, combat, dodge, input and CharacterController disabled.");
            CheckResult("Guard stops attacking dead player",guard.State==LabEnemyGuard.GuardState.Idle && !guard.IsWindingUp,
                "State="+guard.State);

            yield return ResetCombat(new Vector3(0,.05f,10),new Vector3(0,.05f,8.5f));
            yield return Drive(.05f,new[]{Key.Space});yield return Drive(.16f,NoKeys);
            playerHealth.TryTakeDamage(100,enemyHealth);
            CheckResult("Death cancels an active vulnerable dodge",playerHealth.IsDead && !dodge.IsDodging && !motor.enabled,
                "Killed after the 0.18-second invulnerability window.");

            yield return ResetCombat(new Vector3(0,.05f,-6),new Vector3(0,.05f,4));
            playerBefore=input.transform.position;yield return Drive(.3f,new[]{Key.W});
            CheckResult("Phase 1 movement still works",input.transform.position.z>playerBefore.z+1.1f,
                "Movement regression after combat changes.");
            yield return Drive(.06f,NoKeys);playerBefore=input.transform.position;
            yield return Drive(.05f,new[]{Key.Space});yield return Drive(.35f,NoKeys);
            CheckResult("Phase 1 dodge still travels 3m",Mathf.Abs(input.transform.position.z-playerBefore.z-3)<.15f,
                "Travel="+(input.transform.position.z-playerBefore.z));
            yield return Drive(.05f,NoKeys,false,new Vector2(5,-2));
            CheckResult("Phase 1 camera still rotates",orbit.Yaw>1 && orbit.Pitch>20,"yaw="+orbit.Yaw+", pitch="+orbit.Pitch);

            yield return ResetCombat(new Vector3(0,.05f,-3),new Vector3(0,.05f,-1.5f),true);
            yield return Drive(.05f,NoKeys,true);
            yield return Drive(.6f,NoKeys);
            yield return Drive(.05f,NoKeys,true);
            CheckResult("Default-settings combat exchange",enemyHealth.IsDead && guard.State==LabEnemyGuard.GuardState.Dead &&
                !enemyCC.enabled && playerHealth.CurrentHealth==90 && combat.AttackCount==2,
                "Two 15-damage clicks defeat the guard; its 10-damage strike leaves the player at 90 HP.");

            yield return ResetCombat(new Vector3(0,.05f,-6),new Vector3(0,.05f,4),true);
            report.complete=true;Persist();
            Debug.Log("Phase 2 PlayMode checks: "+report.passed+" passed, "+report.failed+" failed. See Phase2TestResults.json.");
            Cleanup();Destroy(gameObject);
        }
        private void Cleanup()
        {
            if(!initialized)return;
            if(actions!=null)actions.devices=null;
            if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);
            if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior=oldBackground;Application.runInBackground=oldRunInBackground;
            if(input!=null)input.ReleaseCursor();
        }
        private void OnDestroy()
        {
            if(!report.complete) {
                CheckResult("Test run completed",false,"Interrupted or exception; inspect Console.");
                Persist();Cleanup();
            }
        }
    }
}
#endif
