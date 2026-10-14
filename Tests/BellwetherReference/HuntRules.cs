using System;

namespace HollowCurrent
{
    // Per-map hunt and dread rules read by Investigation (pure data, no Unity).
    // HuntRules.Bellwether holds the shipped house's constants exactly, so the
    // shared Investigation plays Bellwether byte-for-byte as before. The castle
    // adds dice-roll hunts gated by Dread, broken tells, a hostile escape and a
    // death penalty (docs/haunted-castle/FEAR_PLAN.md, wave A).
    public sealed class HuntRules
    {
        public string Name;

        // Timed hunt (Bellwether's 95 s timer, the castle's floor). Time accrues only in
        // Disturbance while Dread >= HuntDreadMin; once it reaches TimedHuntSeconds with at
        // least TimedHuntEvidence records a hunt is forced. It resets when a hunt starts
        // (false warnings do not reset it). 0 = dice only.
        public float TimedHuntSeconds;
        public int TimedHuntEvidence;
        // Dread needed for dice rolls and for the floor clock to run (Bellwether 0 = always).
        public float HuntDreadMin;

        // First-hunt guarantee: before any hunt, FirstHuntSeconds of Disturbance with at
        // least FirstHuntEvidence records forces one, so the mystery (which needs a hunt
        // survived) can never stall at low Dread. The deadline is FirstHuntSeconds *
        // (1 - Jitter + 2 * Jitter * roll); a deterministic roll (0.5) gives exactly
        // FirstHuntSeconds. 0 = off.
        public float FirstHuntSeconds, FirstHuntJitter;
        public int FirstHuntEvidence;

        // Dice-roll hunts: every DiceIntervalMin..Max seconds in Disturbance while
        // Dread >= HuntDreadMin, roll; chance by Dread band [HuntDreadMin,60), [60,80), [80,100],
        // multiplied by (1 + DiceEvidenceBonus * evidence count). DiceIntervalMax 0 = off.
        public float DiceIntervalMin, DiceIntervalMax;
        public float DiceChanceAt40, DiceChanceAt60, DiceChanceAt80, DiceEvidenceBonus;

        // Phase lengths. WarningSeconds 0 = no Warning phase at all (every hunt is tell-less).
        public float WarningSeconds, HuntSeconds, RecoverySeconds;
        // Tell before a hunt converted from a close apparition (TryEscalate).
        public float EscalationWarningSeconds;
        // Dice and forced hunts: chance the hunt starts with no Warning. Dice hunts only:
        // chance the Warning is a false alarm (ends back in Disturbance). Close apparition:
        // chance TryEscalate converts it into a hunt.
        public float NoTellChance, FalseWarningChance, EventEscalationChance;
        // Return phase is a hunt until the bridge is crossed (FinishAtVan).
        public bool HostileReturn;

        // Dread 0-100 (never shown as a number). UsesDread false = Dread stays 0 and AddDread is ignored.
        public bool UsesDread;
        // Per second: inside with no lit candle nearby / inside otherwise / in the safe zone (falls).
        public float DreadDarkRate, DreadInsideRate, DreadSafeRecovery, DreadStart;
        // Suggested AddDread amounts for Game's events.
        public float DreadApparition, DreadThrow, DreadSeen, DreadGrabSurvived;
        // Lies (false readings) and held hide doors from these Dread levels. 0 = never.
        public float LieThreshold, DoorHeldThreshold;
        // On a checkpoint restore after a death, Dread restarts at DeathDread.
        public bool DeathPenalty;
        public float DeathDread;
        // Hide spots nailed shut for this run (Game picks which).
        public int NailedHides;
        // Perch dwell between hunts for the hunter's runtime. 0 = the profile's default.
        public float PerchMinSeconds, PerchMaxSeconds;

        // Dice chance for one roll at this Dread and evidence count (0 below HuntDreadMin).
        public float DiceChance(float dread,int evidence)
        {
            if(DiceIntervalMax<=0||dread<HuntDreadMin)return 0;
            float band=dread>=80?DiceChanceAt80:dread>=60?DiceChanceAt60:DiceChanceAt40;
            return band*(1+DiceEvidenceBonus*Math.Max(0,evidence));
        }
        public HuntRules Clone()=>(HuntRules)MemberwiseClone();

        // The rules a map plays with. The Long Night dial applies to the castle only.
        public static HuntRules For(MapId map,bool longNight=false)=>map==MapId.Castle?(longNight?LongNight:Castle):Bellwether;

        // The shipped house: 95 s timed hunt with two records, 9 s warning, 38 s hunt,
        // 32 s recovery, no dread, no dice, no lies, no hostile return, no death penalty.
        public static readonly HuntRules Bellwether=new HuntRules
        {
            Name="Bellwether",
            TimedHuntSeconds=95,TimedHuntEvidence=2,HuntDreadMin=0,
            FirstHuntSeconds=0,FirstHuntJitter=0,FirstHuntEvidence=0,
            DiceIntervalMin=0,DiceIntervalMax=0,DiceChanceAt40=0,DiceChanceAt60=0,DiceChanceAt80=0,DiceEvidenceBonus=0,
            WarningSeconds=9,HuntSeconds=38,RecoverySeconds=32,EscalationWarningSeconds=0,
            NoTellChance=0,FalseWarningChance=0,EventEscalationChance=0,HostileReturn=false,
            UsesDread=false,DreadDarkRate=0,DreadInsideRate=0,DreadSafeRecovery=0,DreadStart=0,
            DreadApparition=0,DreadThrow=0,DreadSeen=0,DreadGrabSurvived=0,
            LieThreshold=0,DoorHeldThreshold=0,DeathPenalty=false,DeathDread=0,NailedHides=0,
            PerchMinSeconds=0,PerchMaxSeconds=0,
        };

        // The castle (FEAR_PLAN items 1-5, 7).
        public static readonly HuntRules Castle=new HuntRules
        {
            Name="Castle",
            TimedHuntSeconds=240,TimedHuntEvidence=0,HuntDreadMin=40,
            FirstHuntSeconds=95,FirstHuntJitter=.25f,FirstHuntEvidence=2,
            DiceIntervalMin=2,DiceIntervalMax=6,DiceChanceAt40=1f/12,DiceChanceAt60=1f/6,DiceChanceAt80=1f/4,DiceEvidenceBonus=.5f,
            WarningSeconds=9,HuntSeconds=38,RecoverySeconds=32,EscalationWarningSeconds=1.5f,
            NoTellChance=.25f,FalseWarningChance=.25f,EventEscalationChance=.2f,HostileReturn=true,
            UsesDread=true,DreadDarkRate=.12f,DreadInsideRate=.05f,DreadSafeRecovery=.5f,DreadStart=0,
            DreadApparition=12,DreadThrow=8,DreadSeen=15,DreadGrabSurvived=20,
            LieThreshold=70,DoorHeldThreshold=90,DeathPenalty=true,DeathDread=60,NailedHides=0,
            PerchMinSeconds=0,PerchMaxSeconds=0,
        };

        // Field Settings "Long Night" dial on the castle (FEAR_PLAN item 6): no Warning
        // phase, Dread starts at 50, 50 s hunts, 2-5 s perches, two hide spots nailed shut.
        public static readonly HuntRules LongNight=MakeLongNight();
        static HuntRules MakeLongNight()
        {
            var r=Castle.Clone();
            r.Name="Long Night";r.WarningSeconds=0;r.DreadStart=50;r.HuntSeconds=50;
            r.PerchMinSeconds=2;r.PerchMaxSeconds=5;r.NailedHides=2;r.FalseWarningChance=0;
            return r;
        }
    }
}
