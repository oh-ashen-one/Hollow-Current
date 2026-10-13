using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

namespace HollowCurrent
{
    // Unity's native HID gamepad layouts handle USB and Bluetooth DualSense.
    // No keyboard emulation or OS-wide synthetic input is used.
    [DefaultExecutionOrder(-100)]
    public sealed class PadInput : MonoBehaviour
    {
        public static PadInput Current {get;private set;}
        public bool Connected {get;private set;}
        public bool UsingPad {get;private set;}
        public bool PlayStation {get;private set;}
        public string DeviceName {get;private set;}="No controller";
        public Vector2 Move {get;private set;}
        public Vector2 Look {get;private set;}
        public Vector2 Navigate {get;private set;}
        public bool Confirm,Cancel,Pause,Journal,Torch,Radio,Fire,FireHeld,UseHeld;
        public bool Sprint {get;private set;}
        public bool Crouch {get;private set;}
        public bool Disconnected {get;private set;}
        public int ToolStep {get;private set;}
        public int InputFrames {get;private set;}
        public void Apply(FieldInput input,int tool)
        {
            if(!Connected||!UsingPad)return;
            input.Move=Vector2.ClampMagnitude(input.Move+Move,1);
            input.Use|=Confirm;input.UseHeld|=UseHeld;input.Fire|=Fire;input.FireHeld|=FireHeld;
            input.Torch|=Torch;input.Radio|=Radio;input.Sprint|=Sprint;input.Crouch|=Crouch;
            if(ToolStep!=0)input.SelectTool=(tool-1+ToolStep+3)%3+1;
        }
        public Vector2 LookDelta(float sensitivity,bool invert,float seconds)=>new Vector2(Look.x*150*sensitivity*seconds,Look.y*120*sensitivity*seconds*(invert?1:-1));
        int deviceId;
        uint previousButtons;
        bool suppressNextEdges;
        Game owner;
        public static bool Active=>Current&&Current.Connected&&Current.UsingPad;
        void Awake(){Current=this;owner=GetComponent<Game>();}
        void OnDestroy(){if(Current==this)Current=null;}
        void OnApplicationFocus(bool focused)
        {
            if(focused)return;
            suppressNextEdges=true;Move=Look=Navigate=Vector2.zero;
            Confirm=Cancel=Pause=Journal=Torch=Radio=Fire=FireHeld=UseHeld=false;Sprint=false;
        }
        public static Vector2 Stick(Vector2 raw,bool aiming=false)
        {
            float length=raw.magnitude;
            if(length<=.18f)return Vector2.zero;
            float strength=Mathf.Clamp01((length-.18f)/(.95f-.18f));
            if(aiming)strength=Mathf.Pow(strength,1.35f);
            return raw/length*strength;
        }
        void Update()
        {
            bool wasActive=Active;
            var pad=Gamepad.current;
            // Existing automated FieldInput routes must not consume an owner's
            // physical controller. The separate editor audit uses virtual HID.
            if(NativeVerification.Requested)pad=null;
            if(pad!=null&&!pad.added)pad=null;
            bool connected=pad!=null;
            Disconnected=Connected&&!connected;
            if(!connected)
            {
                Connected=false;Move=Look=Navigate=Vector2.zero;
                Confirm=Cancel=Pause=Journal=Torch=Radio=Fire=FireHeld=UseHeld=false;
                ToolStep=0;Sprint=Crouch=false;
                previousButtons=0;
                if(Disconnected)Debug.Log("HC_GAMEPAD_DISCONNECTED");
                if(wasActive&&Application.isPlaying&&owner&&owner.Case!=null)owner.ApplyFocus();
                return;
            }
            if(!Connected||deviceId!=pad.deviceId)
            {
                deviceId=pad.deviceId;UsingPad=true;Sprint=Crouch=false;
                previousButtons=0;
                DeviceName=pad.displayName;PlayStation=pad is DualShockGamepad;
                Debug.Log("HC_GAMEPAD_CONNECTED "+DeviceName+" layout="+pad.layout+" native="+pad.native);
            }
            Connected=true;
            Move=Stick(pad.leftStick.ReadUnprocessedValue());Look=Stick(pad.rightStick.ReadUnprocessedValue(),true);
            Navigate=pad.dpad.ReadValue();if(Navigate.sqrMagnitude<.1f)Navigate=Move;
            // Sample semantic controls once per game frame and derive edges
            // here, so a held trigger/cross never repeats one-shot actions.
            uint now=0;
            if(pad.buttonSouth.ReadValue()>.5f)now|=1;if(pad.buttonEast.ReadValue()>.5f)now|=2;
            if(pad.buttonWest.ReadValue()>.5f)now|=4;if(pad.buttonNorth.ReadValue()>.5f)now|=8;
            if(pad.startButton.ReadValue()>.5f)now|=16;if(pad.selectButton.ReadValue()>.5f)now|=32;
            if(pad.leftShoulder.ReadValue()>.5f)now|=64;if(pad.rightShoulder.ReadValue()>.5f)now|=128;
            if(pad.leftTrigger.ReadValue()>.5f)now|=256;if(pad.rightTrigger.ReadValue()>.5f)now|=512;
            if(pad.leftStickButton.ReadValue()>.5f)now|=1024;
            if(pad is DualShockGamepad sony&&sony.touchpadButton!=null&&sony.touchpadButton.ReadValue()>.5f)now|=2048;
            uint pressed=suppressNextEdges?0:now&~previousButtons;
            // The player's event history also preserves a quick press/release
            // that occurs entirely between two rendered frames.
            if(Application.isPlaying&&!suppressNextEdges)
            {
                if(pad.buttonSouth.wasPressedThisFrame)pressed|=1;if(pad.buttonEast.wasPressedThisFrame)pressed|=2;
                if(pad.buttonWest.wasPressedThisFrame)pressed|=4;if(pad.buttonNorth.wasPressedThisFrame)pressed|=8;
                if(pad.startButton.wasPressedThisFrame)pressed|=16;if(pad.selectButton.wasPressedThisFrame)pressed|=32;
                if(pad.leftShoulder.wasPressedThisFrame)pressed|=64;if(pad.rightShoulder.wasPressedThisFrame)pressed|=128;
                if(pad.leftTrigger.wasPressedThisFrame)pressed|=256;if(pad.rightTrigger.wasPressedThisFrame)pressed|=512;
                if(pad.leftStickButton.wasPressedThisFrame)pressed|=1024;
                if(pad is DualShockGamepad ds&&ds.touchpadButton!=null&&ds.touchpadButton.wasPressedThisFrame)pressed|=2048;
            }
            previousButtons=now;suppressNextEdges=false;
            if(Application.isPlaying&&!Application.isFocused&&!NativeVerification.Requested)
            {OnApplicationFocus(false);return;}
            Confirm=(pressed&1)!=0;Cancel=(pressed&2)!=0;Torch=(pressed&4)!=0;Journal=(pressed&(8|32|2048))!=0;Pause=(pressed&16)!=0;
            Radio=(pressed&256)!=0;Fire=(pressed&512)!=0;FireHeld=(now&512)!=0;UseHeld=(now&1)!=0;
            ToolStep=((pressed&128)!=0?1:0)-((pressed&64)!=0?1:0);
            if((pressed&1024)!=0)Sprint=!Sprint;
            if(Move.sqrMagnitude<.01f)Sprint=false;
            // Circle is also menu Back; only toggle crouching during gameplay.
            if(Cancel&&owner&&!owner.Modal)Crouch=!Crouch;
            bool activity=Move.sqrMagnitude+Look.sqrMagnitude>0||Confirm||Cancel||Pause||Torch||Journal||Radio||FireHeld||UseHeld||ToolStep!=0||pad.dpad.ReadValue().sqrMagnitude>.1f;
            if(activity){UsingPad=true;InputFrames++;}
            else if((Keyboard.current!=null&&Keyboard.current.anyKey.wasPressedThisFrame)||(Mouse.current!=null&&(Mouse.current.delta.ReadValue().sqrMagnitude>1||Mouse.current.leftButton.wasPressedThisFrame))){UsingPad=false;Sprint=Crouch=false;}
            if(wasActive!=Active&&Application.isPlaying&&owner&&owner.Case!=null)owner.ApplyFocus();
        }
        public static string Prompt(string keyboard)
        {
            if(!Active)return keyboard;
            bool ps=Current.PlayStation;
            switch(keyboard)
            {
                case "E":case "ENTER":case "N":case "OPEN":return ps?"×":"A";
                case "ESC":return ps?"○":"B";
                case "F":return ps?"□":"X";
                case "J":return ps?"△":"Y";
                case "R":return "L2";
                default:return "";
            }
        }
    }
}
