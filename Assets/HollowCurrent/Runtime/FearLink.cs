using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace HollowCurrent
{
    // Wave F contract bridge (docs/haunted-castle/FEAR_PLAN.md), integrator-owned. Every castle fear hook in the
    // shared files goes through here. F1 (hc/dread) is merged and called directly. Until hc/count11 (F2) and
    // hc/scares (F3) merge, their accessors resolve the contract member by name once and fall back to today's
    // behaviour when it is absent; after those merges these bodies become direct calls too.
    public static class FearLink
    {
        const BindingFlags Instance=BindingFlags.Instance|BindingFlags.Public;
        const BindingFlags Static=BindingFlags.Static|BindingFlags.Public;
        static bool resolved;
        static Func<Presence,bool> diving;
        static Type castleScaresType,throwableType;
        static readonly Dictionary<string,MethodInfo> scareMethods=new Dictionary<string,MethodInfo>();

        static void Resolve()
        {
            if(resolved)return;resolved=true;
            diving=Getter<Presence,bool>("Diving");
            var assembly=typeof(Game).Assembly;
            castleScaresType=assembly.GetType("HollowCurrent.CastleScares");throwableType=assembly.GetType("HollowCurrent.Throwable");
            Debug.Log("HC_FEAR_LINK diving="+(diving!=null)+" castleScares="+(castleScaresType!=null)+" throwable="+(throwableType!=null));
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

        // ---- F1: Investigation / HuntRules (hc/dread merged: direct) --------------------------------------
        public static float Dread(Investigation c)=>c.Dread;
        public static void AddDread(Investigation c,float amount)=>c.AddDread(amount);
        public static bool HuntActive(Investigation c)=>c.HuntActive;
        public static bool FalseWarning(Investigation c)=>c.FalseWarning;
        public static bool NoTellHunt(Investigation c)=>c.NoTellHunt;
        public static bool DoorHeld(Investigation c)=>c.DoorHeld;
        public static bool Lies(Investigation c)=>c.Lies;
        public static bool TryEscalate(Investigation c)=>c.TryEscalate();
        public static float WarningSeconds(Investigation c)=>c.WarningLength>0?c.WarningLength:c.Rules.WarningSeconds;

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
