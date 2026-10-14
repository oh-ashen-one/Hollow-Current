using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace HollowCurrent
{
    // Wave F contract bridge (docs/haunted-castle/FEAR_PLAN.md), integrator-owned. Every castle fear hook in
    // the shared files goes through here. Until hc/dread (F1), hc/count11 (F2) and hc/scares (F3) merge, each
    // accessor resolves its contract member by name once and falls back to today's behaviour when it is absent,
    // so the shared files compile and play exactly as before. After the merges these bodies become direct calls.
    public static class FearLink
    {
        const BindingFlags Instance=BindingFlags.Instance|BindingFlags.Public;
        const BindingFlags Static=BindingFlags.Static|BindingFlags.Public;
        static bool resolved;
        static Func<Investigation,float> dread;
        static Func<Investigation,bool> huntActive,falseWarning,noTellHunt,doorHeld,lies;
        static Action<Investigation,float> addDread;
        static Action<Investigation,float,bool,bool,bool> tick4;
        static MethodInfo tryEscalate;
        static Func<Presence,bool> diving;
        static Type castleScaresType,throwableType,huntRulesType;
        static readonly Dictionary<string,MethodInfo> scareMethods=new Dictionary<string,MethodInfo>();

        static void Resolve()
        {
            if(resolved)return;resolved=true;
            dread=Getter<Investigation,float>("Dread");
            huntActive=Getter<Investigation,bool>("HuntActive");falseWarning=Getter<Investigation,bool>("FalseWarning");
            noTellHunt=Getter<Investigation,bool>("NoTellHunt");doorHeld=Getter<Investigation,bool>("DoorHeld");lies=Getter<Investigation,bool>("Lies");
            var add=typeof(Investigation).GetMethod("AddDread",Instance,null,new[]{typeof(float)},null);
            if(add!=null)addDread=(Action<Investigation,float>)Delegate.CreateDelegate(typeof(Action<Investigation,float>),add);
            var t=typeof(Investigation).GetMethod("Tick",Instance,null,new[]{typeof(float),typeof(bool),typeof(bool),typeof(bool)},null);
            if(t!=null&&t.ReturnType==typeof(void))tick4=(Action<Investigation,float,bool,bool,bool>)Delegate.CreateDelegate(typeof(Action<Investigation,float,bool,bool,bool>),t);
            tryEscalate=typeof(Investigation).GetMethod("TryEscalate",Instance,null,Type.EmptyTypes,null);
            diving=Getter<Presence,bool>("Diving");
            var assembly=typeof(Game).Assembly;
            castleScaresType=assembly.GetType("HollowCurrent.CastleScares");throwableType=assembly.GetType("HollowCurrent.Throwable");
            huntRulesType=typeof(Investigation).Assembly.GetType("HollowCurrent.HuntRules");
            Debug.Log("HC_FEAR_LINK dread="+(dread!=null)+" tick4="+(tick4!=null)+" huntActive="+(huntActive!=null)+" escalate="+(tryEscalate!=null)+" diving="+(diving!=null)+" castleScares="+(castleScaresType!=null)+" throwable="+(throwableType!=null)+" huntRules="+(huntRulesType!=null));
        }
        static Func<T,R> Getter<T,R>(string name)
        {
            var p=typeof(T).GetProperty(name,Instance);
            if(p!=null&&p.PropertyType==typeof(R)&&p.GetGetMethod()!=null)return (Func<T,R>)Delegate.CreateDelegate(typeof(Func<T,R>),p.GetGetMethod());
            var f=typeof(T).GetField(name,Instance);
            if(f!=null&&f.FieldType==typeof(R))return o=>(R)f.GetValue(o);
            return null;
        }
        static object Member(object target,Type type,string name)
        {
            if(type==null)return null;
            var flags=target==null?Static:Instance;
            var p=type.GetProperty(name,flags);if(p!=null&&p.GetGetMethod()!=null)return p.GetValue(target);
            var f=type.GetField(name,flags);return f?.GetValue(target);
        }
        static bool SetMember(object target,string name,object value)
        {
            var type=target.GetType();
            var p=type.GetProperty(name,Instance);if(p!=null&&p.GetSetMethod()!=null&&(value==null||p.PropertyType.IsInstanceOfType(value))){p.SetValue(target,value);return true;}
            var f=type.GetField(name,Instance);if(f!=null&&!f.IsInitOnly&&(value==null||f.FieldType.IsInstanceOfType(value))){f.SetValue(target,value);return true;}
            return false;
        }

        // ---- F1: Investigation / HuntRules -------------------------------------------------------------
        public static bool HasDread{get{Resolve();return dread!=null;}}
        public static float Dread(Investigation c){Resolve();return dread!=null?dread(c):0;}
        public static void AddDread(Investigation c,float amount){Resolve();addDread?.Invoke(c,amount);}
        // Phase Hunt, or (castle) the hostile return before the bridge is crossed.
        public static bool HuntActive(Investigation c){Resolve();return huntActive!=null?huntActive(c):c.Phase==Phase.Hunt;}
        public static bool FalseWarning(Investigation c){Resolve();return falseWarning!=null&&falseWarning(c);}
        public static bool NoTellHunt(Investigation c){Resolve();return noTellHunt!=null&&noTellHunt(c);}
        public static bool DoorHeld(Investigation c){Resolve();return doorHeld!=null&&doorHeld(c);}
        public static bool Lies(Investigation c){Resolve();return lies!=null&&lies(c);}
        // The castle tick (dread, dice hunts). Falls back to the two-argument tick until F1 lands.
        public static bool Tick(Investigation c,float dt,bool inside,bool litNearby,bool inSafe)
        {Resolve();if(tick4!=null){tick4(c,dt,inside,litNearby,inSafe);return true;}c.Tick(dt,inside);return false;}
        public static bool TryEscalate(Investigation c)
        {Resolve();if(tryEscalate==null)return false;var r=tryEscalate.Invoke(c,null);return !(r is bool b)||b;}
        // Dice rolls: F1 injects a Func<float> (null = deterministic 0.5 for the rules tests). The game passes Random.value.
        public static bool InjectRandom(Investigation c)
        {
            Func<float> roll=()=>UnityEngine.Random.value;
            foreach(var p in typeof(Investigation).GetProperties(Instance))if(p.PropertyType==typeof(Func<float>)&&p.GetSetMethod()!=null){p.SetValue(c,roll);return true;}
            foreach(var f in typeof(Investigation).GetFields(Instance))if(f.FieldType==typeof(Func<float>)&&!f.IsInitOnly){f.SetValue(c,roll);return true;}
            return false;
        }
        static object Rules(Investigation c)=>Member(c,typeof(Investigation),"Rules")??(c!=null?Member(c.Book,typeof(CaseBook),"Rules"):null);
        public static float WarningSeconds(Investigation c)
        {Resolve();var r=Rules(c);var v=r==null?null:Member(r,r.GetType(),"WarningSeconds");return v is float f&&f>0?f:9;}
        public static int NailedHides(Investigation c,int fallback)
        {Resolve();var r=Rules(c);var v=r==null?null:Member(r,r.GetType(),"NailedHides");return v is int n?n:fallback;}
        // Points the map's CaseBook at HuntRules.LongNight (or back at HuntRules.Castle). Call before
        // `new Investigation(book)`; also pushes the rules into a live Investigation if it exposes a setter.
        public static bool ApplyCastleRules(CaseBook book,Investigation live,bool longNight)
        {
            Resolve();if(huntRulesType==null||book==null)return false;
            var rules=Member(null,huntRulesType,longNight?"LongNight":"Castle");if(rules==null)return false;
            bool set=SetMember(book,"Rules",rules);if(live!=null)SetMember(live,"Rules",rules);
            return set;
        }

        // ---- F2: Presence ------------------------------------------------------------------------------
        public static bool Diving(Presence p){Resolve();return p&&diving!=null&&diving(p);}

        // ---- F3: CastleScares / Throwable --------------------------------------------------------------
        public static Component AddCastleScares(Game game)
        {
            Resolve();if(castleScaresType==null)return null;
            var c=game.gameObject.AddComponent(castleScaresType);
            castleScaresType.GetMethod("Initialize",Instance,null,new[]{typeof(Game)},null)?.Invoke(c,new object[]{game});
            return c;
        }
        // Invokes a parameterless CastleScares beat by name. False if absent or if the beat said it could not run.
        public static bool Scare(Component scares,string beat)
        {
            if(!scares)return false;
            if(!scareMethods.TryGetValue(beat,out var m)){m=scares.GetType().GetMethod(beat,Instance,null,Type.EmptyTypes,null);scareMethods[beat]=m;}
            if(m==null)return false;
            var r=m.Invoke(scares,null);return !(r is bool b)||b;
        }
        public static bool FalseCompass(Component scares)=>scares&&Member(scares,scares.GetType(),"FalseCompass") is bool b&&b;
        public static void CollectThrowables(Transform root,List<Transform> into)
        {
            Resolve();if(throwableType==null||!root)return;
            foreach(var c in root.GetComponentsInChildren(throwableType,true))if(!into.Contains(c.transform))into.Add(c.transform);
        }
        public static bool IsThrowable(Transform t){Resolve();return throwableType!=null&&t&&t.GetComponent(throwableType);}
    }
}
