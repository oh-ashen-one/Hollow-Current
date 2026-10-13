using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.DualShock.LowLevel;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace HollowCurrent.Editor
{
    public static class ControllerAudit
    {
        // Synthetic reports enter Unity's real DualSense HID parser. They are
        // restricted to a virtual device; no OS input, rumble or physical-device
        // disable/remove commands are sent to the user's connected controller.
        [StructLayout(LayoutKind.Explicit,Size=64)] struct USB:IInputStateTypeInfo
        {
            public FourCC format=>new FourCC('H','I','D');
            [FieldOffset(0)] public byte id;
            [FieldOffset(1)] public byte lx;[FieldOffset(2)] public byte ly;
            [FieldOffset(3)] public byte rx;[FieldOffset(4)] public byte ry;
            [FieldOffset(5)] public byte lt;[FieldOffset(6)] public byte rt;
            [FieldOffset(8)] public byte b0;[FieldOffset(9)] public byte b1;[FieldOffset(10)] public byte b2;
        }
        [StructLayout(LayoutKind.Explicit,Size=78)] struct Bluetooth:IInputStateTypeInfo
        {
            public FourCC format=>new FourCC('H','I','D');
            [FieldOffset(0)] public byte id;
            [FieldOffset(2)] public byte lx;[FieldOffset(3)] public byte ly;
            [FieldOffset(4)] public byte rx;[FieldOffset(5)] public byte ry;
            [FieldOffset(6)] public byte lt;[FieldOffset(7)] public byte rt;
            [FieldOffset(9)] public byte b0;[FieldOffset(10)] public byte b1;[FieldOffset(11)] public byte b2;
        }
        static readonly List<string> checks=new List<string>();
        static DualSenseGamepadHID pad;static PadInput reader;static Game owner;
        static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks.Add(label);}
        static DualSenseHIDInputReport Neutral()=>new DualSenseHIDInputReport{leftStickX=128,leftStickY=128,rightStickX=128,rightStickY=128,buttons0=8};
        static void Read(){InputSystem.Update();pad.MakeCurrent();typeof(PadInput).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(reader,null);}
        static void Send(DualSenseHIDInputReport state){InputSystem.QueueStateEvent(pad,state);Read();}
        static void Release()=>Send(Neutral());
        static void CheckMenus(GameObject host)
        {
            owner.Paused=true;var ui=host.AddComponent<Interface>();ui.Game=owner;
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(Interface).GetMethod("RefreshMenu",flags).Invoke(ui,null);
            var menu=(IList)typeof(Interface).GetField("menu",flags).GetValue(ui);
            var itemType=typeof(Interface).GetNestedType("MenuEntry",BindingFlags.NonPublic);
            void Add(string id,Rect rect,bool slider=false)
            {var item=Activator.CreateInstance(itemType);itemType.GetField("id").SetValue(item,id);itemType.GetField("rect").SetValue(item,rect);itemType.GetField("slider").SetValue(item,slider);menu.Add(item);}
            Add("first",new Rect(0,0,100,40));Add("second",new Rect(0,80,100,40));Add("slider",new Rect(160,80,200,40),true);
            var navigate=typeof(Interface).GetMethod("ControllerNavigation",flags);
            Release();navigate.Invoke(ui,null);Check(ui.ControllerFocus=="first","Controller menu chooses an initial focus");
            var state=Neutral();state.buttons0=4;Send(state);navigate.Invoke(ui,null);Check(ui.ControllerFocus=="second","D-pad moves menu focus down");
            Release();navigate.Invoke(ui,null);state=Neutral();state.buttons0=2;Send(state);navigate.Invoke(ui,null);Check(ui.ControllerFocus=="slider","D-pad moves menu focus across columns");
            Release();navigate.Invoke(ui,null);Send(state);navigate.Invoke(ui,null);Check((int)typeof(Interface).GetField("pendingSlider",flags).GetValue(ui)==1,"Focused slider receives D-pad adjustment");
            var pending=typeof(Interface).GetField("pendingConfirm",flags);var consume=typeof(Interface).GetMethod("ConsumeControllerAction",flags);
            pending.SetValue(ui,"second");Check(!(bool)consume.Invoke(ui,new object[]{"second",true,false}),"Menu confirm cannot execute during repaint");
            Check((bool)consume.Invoke(ui,new object[]{"second",true,true})&&!(bool)consume.Invoke(ui,new object[]{"second",true,true}),"A menu confirm executes once through the shared button action");
            pending.SetValue(ui,"disabled");Check(!(bool)consume.Invoke(ui,new object[]{"disabled",false,true})&&!(bool)consume.Invoke(ui,new object[]{"disabled",true,true}),"Disabled action cannot retain an armed confirm");
            owner.Paused=false;UnityEngine.Object.DestroyImmediate(ui);
        }
        public static void Run()
        {
            if(!Application.isBatchMode||SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)throw new Exception("Controller audit requires the guarded Null-device editor");
            checks.Clear();GameObject host=null;var previous=Gamepad.current;
            try
            {
                pad=InputSystem.AddDevice<DualSenseGamepadHID>();Check(!pad.native,"Audit device is virtual, not the physical DualSense");
                host=new GameObject("Isolated controller audit");owner=host.AddComponent<Game>();
                typeof(Game).GetProperty("Case").SetValue(owner,new Investigation());owner.Started=true;
                reader=host.AddComponent<PadInput>();typeof(PadInput).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(reader,null);
                Release();Check(reader.Connected&&reader.PlayStation,"DualSense layout selects PlayStation prompts");
                Check(reader.Move==Vector2.zero&&reader.Look==Vector2.zero,"Neutral byte sticks have no camera/movement drift");
                var state=Neutral();state.leftStickX=141;state.rightStickY=141;Send(state);
                Check(reader.Move==Vector2.zero&&reader.Look==Vector2.zero,"Stick dead zone suppresses small offsets");
                state=Neutral();state.leftStickY=0;state.rightStickX=255;Send(state);
                Check(reader.Move.y>.99f&&reader.Look.x>.99f,"Left movement and right look axes have correct signs/range");
                Check((reader.LookDelta(1.65f,false,1f/30)*30-reader.LookDelta(1.65f,false,1f/60)*60).magnitude<.001f,"Look speed is independent of frame rate");
                state.rightStickY=0;Send(state);Check(reader.LookDelta(1,false,1).y<0&&reader.LookDelta(1,true,1).y>0,"Invert Y changes stick look direction");
                Release();state=Neutral();state.buttons0|=32;Send(state);Check(reader.Confirm&&reader.UseHeld,"Cross selects/interacts and holds containment");
                var field=new FieldInput();reader.Apply(field,1);Check(field.Use&&field.UseHeld,"Cross reaches shared gameplay input path");
                Send(state);Check(!reader.Confirm&&reader.UseHeld,"Held Cross does not repeat a press every frame");
                Release();Check(!reader.UseHeld,"Cross release stops hold interaction");
                state=Neutral();state.buttons0|=64;Send(state);Check(reader.Cancel&&reader.Crouch,"Circle backs out and toggles gameplay crouch");
                Release();owner.Paused=true;Send(state);Check(reader.Crouch,"Menu Back does not toggle crouch");owner.Paused=false;
                Release();state=Neutral();state.buttons0|=16;Send(state);Check(reader.Torch,"Square toggles independent flashlight");
                Release();state=Neutral();state.buttons0|=128;Send(state);Check(reader.Journal,"Triangle opens journal");
                Release();state=Neutral();state.buttons2=2;Send(state);Check(reader.Journal,"Touchpad also opens journal");
                Release();state=Neutral();state.buttons1=32;Send(state);Check(reader.Pause,"Options pauses/resumes");
                Release();state=Neutral();state.leftTrigger=255;Send(state);Check(reader.Radio,"L2 calls radio");
                Release();state=Neutral();state.rightTrigger=255;Send(state);Check(reader.Fire&&reader.FireHeld,"R2 asks EVP and holds evidence capture");
                Send(state);Check(!reader.Fire&&reader.FireHeld,"Held R2 records without repeated EVP starts");
                field=new FieldInput();reader.Apply(field,2);Check(field.FireHeld,"Trigger hold reaches tool input path");
                Release();state=Neutral();state.buttons1=2;Send(state);field=new FieldInput();reader.Apply(field,3);Check(field.SelectTool==1,"R1 wraps recorder to reader");
                Release();state=Neutral();state.buttons1=1;Send(state);field=new FieldInput();reader.Apply(field,1);Check(field.SelectTool==3,"L1 wraps reader to recorder");
                Release();state=Neutral();state.leftStickY=0;state.buttons1=64;Send(state);Check(reader.Sprint,"L3 toggles sprint while moving");Release();Check(!reader.Sprint,"Stopping movement cancels sprint toggle");
                state=Neutral();state.buttons0=0;Send(state);Check(reader.Navigate.y>.99f,"D-pad up navigates independently of movement");
                InputSystem.QueueStateEvent(pad,new USB{id=1,lx=128,ly=128,rx=128,ry=128,b0=40,rt=255});Read();
                Check(reader.UseHeld&&reader.FireHeld,"USB HID report parser feeds Cross and R2 correctly");
                InputSystem.QueueStateEvent(pad,new Bluetooth{id=49,lx=128,ly=128,rx=128,ry=128,b0=136,lt=255});Read();
                Check(reader.Journal&&reader.Radio,"Bluetooth HID report parser feeds Triangle and L2 correctly");
                Check(PadInput.Prompt("E")=="×"&&PadInput.Prompt("F")=="□","PlayStation action prompts match mapped controls");
                CheckMenus(host);
                Release();typeof(PadInput).GetMethod("OnApplicationFocus",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(reader,new object[]{false});
                state=Neutral();state.buttons0|=32;Send(state);Check(!reader.Confirm,"Held Cross cannot activate on return from another application");
                Release();Send(state);Check(reader.Confirm,"Fresh Cross press works after focus returns");
                // Remove only our virtual device; no connected hardware changes.
                InputSystem.RemoveDevice(pad);pad=null;
                Directory.CreateDirectory("evidence");File.WriteAllLines("evidence/dualsense-software-checks.txt",checks);
                Debug.Log("HC_CONTROLLER_AUDIT_PASS "+checks.Count+" checks; physical controller and AirPlay acceptance deferred");
                Debug.Log("HC_HEADLESS_DEVICE Null");
            }
            finally{if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);if(previous!=null&&previous.added)previous.MakeCurrent();if(host)UnityEngine.Object.DestroyImmediate(host);}
        }
    }
}
