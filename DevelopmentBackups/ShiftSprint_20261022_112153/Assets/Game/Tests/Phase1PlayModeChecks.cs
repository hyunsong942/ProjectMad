#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace MadLab
{
    // Created only during MCP verification. Never saved into the gameplay scene or included in a build.
    public sealed class Phase1PlayModeChecks : MonoBehaviour
    {
        [Serializable] public class Check { public string name; public bool passed; public string detail; }
        [Serializable] public class Report
        {
            public string phase = "Phase 1";
            public string timestamp;
            public bool complete;
            public int passed, failed;
            public List<Check> checks = new List<Check>();
        }
        private Report report = new Report();
        private Keyboard keyboard;
        private Mouse mouse;
        private LabPlayerInput input;
        private LabPlayerMotor motor;
        private LabPlayerDodge dodge;
        private LabThirdPersonCamera orbit;
        private CharacterController controller;
        private Transform player;
        private InputActionAsset actions;
        private InputSettings.BackgroundBehavior oldBackground;
        private bool oldRunInBackground;
        private float maxY;
        private static readonly Key[] NoKeys = new Key[0];

        private static void Set(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
        private void CheckResult(string name, bool passed, string detail)
        {
            report.checks.Add(new Check { name=name, passed=passed, detail=detail });
            if (passed) report.passed++; else report.failed++;
        }
        private void Persist()
        {
            report.timestamp=DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(9)).ToString("o");
            File.WriteAllText(Path.Combine(Application.dataPath,"..","Phase1TestResults.json"),JsonUtility.ToJson(report,true));
        }
        private IEnumerator Drive(float seconds, Key[] keys, Vector2 delta = default(Vector2), bool click = false)
        {
            float end=Time.time+seconds;
            do
            {
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
                var state=new MouseState { delta=delta };
                if(click) state=state.WithButton(MouseButton.Left);
                InputSystem.QueueStateEvent(mouse,state);
                yield return null;
                maxY=Mathf.Max(maxY,player.position.y);
            } while(Time.time<end);
        }
        private IEnumerator ResetPlayer(Vector3 position, float facing=0, float cameraYaw=0)
        {
            motor.enabled=false;
            controller.enabled=false;
            player.SetPositionAndRotation(position,Quaternion.Euler(0,facing,0));
            controller.enabled=true;
            motor.enabled=true;
            Set(dodge,"nextReadyTime",0f);
            Set(orbit,"yaw",cameraYaw);
            Set(orbit,"pitch",20f);
            input.CaptureCursor();
            yield return Drive(.15f,NoKeys);
        }
        private Vector3 Flat(Vector3 v) { v.y=0;return v; }

        private IEnumerator Start()
        {
            input=FindFirstObjectByType<LabPlayerInput>();
            motor=FindFirstObjectByType<LabPlayerMotor>();
            dodge=FindFirstObjectByType<LabPlayerDodge>();
            orbit=FindFirstObjectByType<LabThirdPersonCamera>();
            player=motor.transform;controller=player.GetComponent<CharacterController>();
            oldBackground=InputSystem.settings.backgroundBehavior;
            oldRunInBackground=Application.runInBackground;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            Application.runInBackground=true;
            keyboard=InputSystem.AddDevice<Keyboard>("Phase1TestKeyboard");
            mouse=InputSystem.AddDevice<Mouse>("Phase1TestMouse");
            actions=(InputActionAsset)typeof(LabPlayerInput).GetField("runtimeActions",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(input);
            actions.devices=new InputDevice[]{keyboard,mouse};
            input.CaptureCursor();
            yield return Drive(.3f,NoKeys);

            CheckResult("Single isolated development scene",SceneManager.sceneCount==1 &&
                FindObjectsByType<Camera>(FindObjectsSortMode.None).Length==1 &&
                FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1,"Scene, Camera, AudioListener counts must each be 1.");
            CheckResult("Dedicated input has no Jump action",actions.FindAction("Jump")==null &&
                actions.FindAction("Gameplay/Dodge")!=null,"Only Move, Look, Dodge, ReleaseCursor, CaptureCursor.");

            yield return ResetPlayer(new Vector3(0,.05f,-6));
            Vector3 start=player.position;
            yield return Drive(.6f,NoKeys);
            CheckResult("Idle remains grounded",motor.IsGrounded && Mathf.Abs(player.position.y)<.08f &&
                Flat(player.position-start).magnitude<.02f,player.position.ToString("F3"));

            start=player.position;
            float began=Time.time;
            yield return Drive(.6f,new[]{Key.W});
            float elapsed=Time.time-began;
            float forwardDistance=Flat(player.position-start).magnitude;
            CheckResult("W movement speed",player.position.z>start.z+2 &&
                Mathf.Abs(forwardDistance-4.5f*elapsed)<.25f,
                "distance="+forwardDistance.ToString("F3")+", seconds="+elapsed.ToString("F3"));

            yield return Drive(.08f,NoKeys);start=player.position;
            yield return Drive(.35f,NoKeys);
            CheckResult("Release movement stops",Flat(player.position-start).magnitude<.02f,Flat(player.position-start).ToString("F3"));

            yield return ResetPlayer(new Vector3(0,.05f,-6));
            start=player.position;yield return Drive(.4f,new[]{Key.S});
            CheckResult("S moves backwards",player.position.z<start.z-1.4f,"z delta="+(player.position.z-start.z).ToString("F3"));
            yield return ResetPlayer(new Vector3(0,.05f,-6));
            start=player.position;yield return Drive(.4f,new[]{Key.A});
            CheckResult("A moves left and turns character",player.position.x<start.x-1.4f &&
                Vector3.Dot(player.forward,Vector3.left)>.98f,player.forward.ToString("F3"));
            yield return ResetPlayer(new Vector3(0,.05f,-6));
            start=player.position;yield return Drive(.4f,new[]{Key.D});
            CheckResult("D moves right",player.position.x>start.x+1.4f,"x delta="+(player.position.x-start.x).ToString("F3"));

            yield return ResetPlayer(new Vector3(0,.05f,-6));
            start=player.position;began=Time.time;
            yield return Drive(.6f,new[]{Key.W,Key.D});elapsed=Time.time-began;
            float diagonalDistance=Flat(player.position-start).magnitude;
            CheckResult("Diagonal speed normalized",Mathf.Abs(diagonalDistance-4.5f*elapsed)<.25f,
                "distance="+diagonalDistance.ToString("F3")+", seconds="+elapsed.ToString("F3"));

            yield return ResetPlayer(new Vector3(0,.05f,-6),0,90);
            start=player.position;yield return Drive(.5f,new[]{Key.W});
            CheckResult("Movement follows camera yaw",player.position.x>start.x+1.9f &&
                Mathf.Abs(player.position.z-start.z)<.1f,Flat(player.position-start).ToString("F3"));

            yield return ResetPlayer(new Vector3(0,.05f,-6));
            yield return Drive(.12f,NoKeys,new Vector2(10,-5));
            CheckResult("Mouse rotates camera",orbit.Yaw>1f && orbit.Pitch>20.5f,"yaw="+orbit.Yaw+", pitch="+orbit.Pitch);
            yield return Drive(.08f,NoKeys,new Vector2(0,-10000));
            CheckResult("Upper pitch limit",Mathf.Abs(orbit.Pitch-65)<.01f,"pitch="+orbit.Pitch);
            yield return Drive(.08f,NoKeys,new Vector2(0,10000));
            CheckResult("Lower pitch limit",Mathf.Abs(orbit.Pitch+20)<.01f,"pitch="+orbit.Pitch);

            yield return ResetPlayer(new Vector3(0,.05f,-6),90);
            start=player.position;
            yield return Drive(.06f,new[]{Key.Space});
            CheckResult("Space starts a dodge",dodge.IsDodging,"Space was delivered through the new InputAction.");
            yield return Drive(.4f,NoKeys);
            CheckResult("No-input dodge follows facing and travels 3m",Mathf.Abs(player.position.x-start.x-3)<.15f &&
                Mathf.Abs(player.position.z-start.z)<.1f && !dodge.IsDodging,Flat(player.position-start).ToString("F3"));
            start=player.position;
            yield return Drive(.06f,new[]{Key.Space});
            yield return Drive(.16f,NoKeys);
            CheckResult("Cooldown blocks a second dodge",!dodge.IsDodging &&
                Flat(player.position-start).magnitude<.03f,"cooldown remaining="+dodge.CooldownRemaining.ToString("F3"));
            yield return Drive(dodge.CooldownRemaining+.08f,NoKeys);
            yield return Drive(.05f,new[]{Key.Space});
            CheckResult("Dodge available after recovery",dodge.IsDodging,"Recovery cooldown is 0.8 seconds after the 0.3-second dodge.");
            yield return Drive(.4f,NoKeys);

            yield return ResetPlayer(new Vector3(0,.05f,-6));
            start=player.position;
            yield return Drive(.04f,new[]{Key.Space,Key.A});
            yield return Drive(.29f,new[]{Key.D});
            CheckResult("Directional dodge ignores movement changes",player.position.x<start.x-2.7f &&
                Mathf.Abs(player.position.z-start.z)<.1f,Flat(player.position-start).ToString("F3"));
            yield return Drive(.12f,NoKeys);start=player.position;
            yield return Drive(.4f,new[]{Key.D});
            CheckResult("Normal movement resumes after dodge",player.position.x>start.x+1.4f,
                "x delta="+(player.position.x-start.x).ToString("F3"));

            yield return ResetPlayer(new Vector3(0,.05f,10));
            yield return Drive(.8f,new[]{Key.W});
            CheckResult("Walking blocked by perimeter wall",player.position.z>10.8f && player.position.z<11.2f,
                "z="+player.position.z.ToString("F3"));
            yield return ResetPlayer(new Vector3(0,.05f,10));
            yield return Drive(.06f,new[]{Key.Space});
            yield return Drive(.4f,NoKeys);
            CheckResult("Dodge blocked by perimeter wall",player.position.z>10.8f && player.position.z<11.2f,
                "z="+player.position.z.ToString("F3"));
            yield return ResetPlayer(new Vector3(-4,.05f,0));
            yield return Drive(.06f,new[]{Key.Space});
            yield return Drive(.4f,NoKeys);
            CheckResult("Dodge blocked by obstacle",player.position.z>.8f && player.position.z<1.2f,
                "z="+player.position.z.ToString("F3"));

            yield return ResetPlayer(new Vector3(0,.05f,-10.8f));
            yield return Drive(.2f,NoKeys);
            CheckResult("Camera avoids wall",orbit.transform.position.z>-11.45f &&
                Vector3.Distance(orbit.transform.position,player.position+Vector3.up*1.4f)<2,
                orbit.transform.position.ToString("F3"));

            yield return ResetPlayer(new Vector3(0,.05f,-6));
            yield return Drive(.06f,new[]{Key.Escape});
            start=player.position;yield return Drive(.2f,new[]{Key.W,Key.Space});
            CheckResult("Escape releases cursor and stops controls",!input.HasControl &&
                Flat(player.position-start).magnitude<.02f && !dodge.IsDodging,"control="+input.HasControl);
            yield return Drive(.05f,NoKeys,Vector2.zero,true);
            CheckResult("Click recaptures controls",input.HasControl,"control="+input.HasControl);
            CheckResult("No upward launch during movement or dodge",maxY<.12f,"max root y="+maxY.ToString("F3"));

            yield return ResetPlayer(new Vector3(0,.05f,-6));
            report.complete=true;
            Persist();
            Debug.Log("Phase 1 PlayMode checks: "+report.passed+" passed, "+report.failed+" failed. See Phase1TestResults.json.");
            Cleanup();
            Destroy(gameObject);
        }
        private void Cleanup()
        {
            if(actions!=null) actions.devices=null;
            if(keyboard!=null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if(mouse!=null && mouse.added) InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior=oldBackground;
            Application.runInBackground=oldRunInBackground;
            if(input!=null) input.ReleaseCursor();
        }
        private void OnDestroy()
        {
            if(!report.complete)
            {
                CheckResult("Test run completed",false,"Runner interrupted or an exception occurred; inspect Console.");
                Persist();
                Cleanup();
            }
        }
    }
}
#endif
