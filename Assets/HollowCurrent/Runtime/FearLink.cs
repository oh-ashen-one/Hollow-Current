using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace HollowCurrent
{
    // Wave F contract bridge (docs/haunted-castle/FEAR_PLAN.md), integrator-owned. Every castle fear hook in the
    // shared files goes through here. F1 (hc/dread) is merged and called directly. F3 (hc/scares) is merged and referenced
    // directly where used. Until hc/count11 (F2) merges, Presence.Diving resolves by name once and reads false
    // when absent; after that merge this file reduces to direct calls.
    public static class FearLink
    {
        const BindingFlags Instance=BindingFlags.Instance|BindingFlags.Public;
        static bool resolved;
        static Func<Presence,bool> diving;
        
        static void Resolve()
        {
            if(resolved)return;resolved=true;
            diving=Getter<Presence,bool>("Diving");
            Debug.Log("HC_FEAR_LINK diving="+(diving!=null));
        }
        static Func<T,R> Getter<T,R>(string name)
        {
            var p=typeof(T).GetProperty(name,Instance);
            if(p!=null&&p.PropertyType==typeof(R)&&p.GetGetMethod()!=null)return (Func<T,R>)Delegate.CreateDelegate(typeof(Func<T,R>),p.GetGetMethod());
            var f=typeof(T).GetField(name,Instance);
            if(f!=null&&f.FieldType==typeof(R))return o=>(R)f.GetValue(o);
            return null;
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

    }
}
