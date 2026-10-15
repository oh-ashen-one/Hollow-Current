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
        // Dread needed for the banded dice and for the floor clock to run (Bellwether 0 = always).
        // Each run draws its own boundary from HuntDreadMin +- HuntDreadMinSpread (castle 30-50),
        // so no fixed Dread level is provably safe (Investigation.RunDreadMin).
        public float HuntDreadMin, HuntDreadMinSpread;
        // After the first hunt the dice also roll below the run's boundary at this flat chance
        // per roll (the "demon" roll; castle 1/40). 0 = below the boundary is safe.
        public float LowDreadChance;

        // First-hunt guarantee: before any hunt, FirstHuntSeconds of Disturbance with at
        // least FirstHuntEvidence records forces one, so the mystery (which needs a hunt
        // survived) can never stall at low Dread. The deadline is FirstHuntSeconds *
        // (1 - Jitter + 2 * Jitter * roll); a deterministic roll (0.5) gives exactly
        // FirstHuntSeconds. 0 = off.
        public float FirstHuntSeconds, FirstHuntJitter;
        public int FirstHuntEvidence;
        // Wave K: when FirstHuntMax > FirstHuntMin the deadline is Draw(Min, FirstHuntSeconds, Max, roll) instead
        // (castle 60 / 95 / 180 s after the second record; wave L2, was 240); the jitter is then unused.
        public float FirstHuntMin, FirstHuntMax;

        // Dice-roll hunts: every DiceIntervalMin..Max seconds in Disturbance while
        // Dread >= HuntDreadMin, roll; chance by Dread band [HuntDreadMin,60), [60,80), [80,100],
        // multiplied by (1 + DiceEvidenceBonus * evidence count). DiceIntervalMax 0 = off.
        public float DiceIntervalMin, DiceIntervalMax;
        public float DiceChanceAt40, DiceChanceAt60, DiceChanceAt80, DiceEvidenceBonus;

        // Phase lengths. WarningSeconds 0 = no Warning phase at all (every hunt is tell-less).
        // HuntSeconds / RecoverySeconds are the medians: each hunt draws its own length from
        // Min..Max (piecewise linear, a 0.5 roll gives exactly the median). Min == Max = fixed,
        // no roll is drawn. Investigation.CurrentHuntSeconds / CurrentRecoverySeconds hold the draw.
        // WarningSeconds is also a median: every told or false Warning draws WarningSecondsMin..Max
        // (Investigation.CurrentWarningSeconds), so a Warning's length never identifies a false alarm.
        public float WarningSeconds, HuntSeconds, RecoverySeconds;
        public float WarningSecondsMin, WarningSecondsMax;
        public float HuntSecondsMin, HuntSecondsMax, RecoverySecondsMin, RecoverySecondsMax;
        // Dice during Recovery: at Dread >= RecoveryDiceDread each roll hunts with RecoveryDiceChance,
        // below it with RecoveryLowChance. 0 = off.
        public float RecoveryDiceChance, RecoveryDiceDread, RecoveryLowChance;
        // After the first hunt, flat dice in Survey and Containment (no phase is a guaranteed refuge). 0 = off.
        public float QuietPhaseChance;
        // Say Book.RecoveryMessage ("a quiet window") when a hunt ends. The castle stays silent.
        public bool AnnounceRecovery;
        // Tell before a hunt converted from a close apparition (TryEscalate).
        public float EscalationWarningSeconds;
        // Dice and forced hunts: chance the hunt starts with no Warning. Dice hunts only:
        // chance the Warning is a false alarm (ends back in Disturbance). Close apparition:
        // chance TryEscalate converts it into a hunt.
        public float NoTellChance, FalseWarningChance, EventEscalationChance;
        // Wave K: escalation chance in Recovery once the post-hunt grace has run out (never during the grace). 0 = off.
        public float EscalationRecoveryChance;
        // Wave L escalation policy (replaces the two chances above when either is > 0): lit / dark chance in Disturbance and
        // in Recovery after the grace; never on the run's first apparition; never when either of the last two hunts was
        // escalation-born (at most 1 in 3 consecutive hunts). See Investigation.EscalationChanceNow.
        public float EscalationLitChance, EscalationDarkChance;
        // Wave L: Dread an apparition adds (Investigation.NoteApparition) is left out of the dice input for this many seconds.
        public float ApparitionDiceShadowSeconds;
        // Wave L: door-held bag: of every DoorHeldBag eligible hide attempts (Dread >= DoorHeldThreshold) exactly one is
        // held, at a random position (rate 1/bag, never more than bag - 1 misses in a row ... 2 x bag - 2 at worst). 0 = the
        // independent DoorHeldChance roll.
        public int DoorHeldBag;
        // Wave L, for Presence (F2): chance an unwatched near-miss opens the hide, piecewise linear over Dread:
        // 0 -> NearMissOpen0, 40 -> NearMissOpen40, 80 -> NearMissOpen80, 100 -> NearMissOpen100.
        public float NearMissOpen0, NearMissOpen40, NearMissOpen80, NearMissOpen100;
        public float NearMissOpenChance(float dread)
        {
            float d=Math.Max(0,Math.Min(100,float.IsNaN(dread)?0:dread));
            if(d<40)return NearMissOpen0+(NearMissOpen40-NearMissOpen0)*d/40;
            if(d<80)return NearMissOpen40+(NearMissOpen80-NearMissOpen40)*(d-40)/40;
            return NearMissOpen80+(NearMissOpen100-NearMissOpen80)*(d-80)/20;
        }
        // Return phase is a hunt until the bridge is crossed (FinishAtVan).
        public bool HostileReturn;

        // Dread 0-100 (never shown as a number). UsesDread false = Dread stays 0 and AddDread is ignored.
        public bool UsesDread;
        // Per second: inside with no lit candle nearby / inside otherwise / in the safe zone (falls).
        public float DreadDarkRate, DreadInsideRate, DreadSafeRecovery, DreadStart;
        // Suggested AddDread amounts for Game's events.
        public float DreadApparition, DreadThrow, DreadSeen, DreadGrabSurvived;
        // Dread from events (AddDread) decays; Dread from darkness and time does not (only the safe zone
        // lowers it). Per second, times Investigation.DreadRelief: in Recovery, and in the calm phases
        // (Survey, Disturbance, Containment). After the first hunt the decay stops at DreadDecayFloor.
        public float DreadDecayRecovery, DreadDecayCalm, DreadDecayFloor;
        // Wave K: above this total the same decay also takes ambient Dread (a careful player can bring a high
        // Dread down without the gatehouse). 0 = only event Dread decays.
        public float DreadDecayAllAbove;
        // Total Dread that AddDread may add during one hunt (hunt or hostile Return). 0 = no cap.
        public float HuntDreadCap;
        // After a hunt ends the dice are disarmed for GraceMin..Max seconds (drawn per hunt). 0 = none.
        public float GraceMin, GraceMax;
        // Pacing governor (wave J). G = threat share (Warning, Hunt, hostile Return) over the last 10 minutes, measured
        // over at least GovernorMinWindow seconds. Above GovernorStart the multiplier is
        // 1 + GovernorGain x (G - GovernorStart), at most GovernorMax: it scales each post-hunt grace and divides the
        // banded (high-Dread) dice odds. After G has stayed below GovernorQuietShare for GovernorQuietSeconds the
        // multiplier is GovernorQuietScale (< 1: shorter graces). It never disarms anything. GovernorGain 0 = off.
        public float GovernorStart, GovernorGain, GovernorMax, GovernorMinWindow, GovernorQuietShare, GovernorQuietSeconds, GovernorQuietScale;
        // Lies (false readings) and held hide doors from these Dread levels. 0 = never.
        public float LieThreshold, DoorHeldThreshold;
        // Wave K: with DoorHeldChance > 0, DoorHeldThreshold arms a per-attempt roll (Investigation.TryDoorHeld)
        // instead of holding every door; Investigation.DoorHeld is then always false.
        public float DoorHeldChance;
        // On a checkpoint restore after a death, Dread restarts at DeathDread.
        public bool DeathPenalty;
        public float DeathDread;
        // FeltDread: what the heartbeat / journal pulse may show. Dread lagged by a per-run time
        // constant FeltLagMin..Max seconds, plus slow noise of +-FeltNoise (one knot every
        // FeltNoisePeriod s). Never uses the boundary and is not tied to Lies.
        public float FeltLagMin, FeltLagMax, FeltNoise, FeltNoisePeriod, FeltCalm;
        // Felt lie episodes, scheduled from the run's noise seed (no dice): one starts every
        // FeltEpisodeGapMin..Max s (start to start) and lasts FeltEpisodeMin..Max s. At Dread >= FeltCalmFrom
        // it can drift toward FeltCalm; at Dread < FeltSpikeBelow it can drift up to FeltSpikeMin..Max;
        // where both are possible the kind is a coin flip. FeltEpisodeGapMax 0 = no episodes.
        public float FeltEpisodeGapMin, FeltEpisodeGapMax, FeltEpisodeMin, FeltEpisodeMax;
        public float FeltCalmFrom, FeltSpikeBelow, FeltSpikeMin, FeltSpikeMax;
        // Hide spots nailed shut for this run (Game picks which).
        public int NailedHides;
        // Perch dwell between hunts for the hunter's runtime. 0 = the profile's default.
        public float PerchMinSeconds, PerchMaxSeconds;

        // Dice chance for one roll at this Dread and evidence count (0 below HuntDreadMin).
        public float DiceChance(float dread,int evidence)=>DiceChance(dread,evidence,HuntDreadMin);
        // Banded chance with this run's boundary (bands: [boundary,60), [60,80), [80,100]).
        public float DiceChance(float dread,int evidence,float boundary)
        {
            if(DiceIntervalMax<=0||dread<boundary)return 0;
            float band=dread>=80?DiceChanceAt80:dread>=60?DiceChanceAt60:DiceChanceAt40;
            return band*(1+DiceEvidenceBonus*Math.Max(0,evidence));
        }
        // Piecewise-linear draw: roll 0 -> min, 0.5 -> median, 1 -> max.
        public static float Draw(float min,float median,float max,float roll)=>roll<.5f?min+(median-min)*(roll/.5f):median+(max-median)*((roll-.5f)/.5f);
        public HuntRules Clone()=>(HuntRules)MemberwiseClone();

        // The rules a map plays with. The Long Night dial applies to the castle only.
        public static HuntRules For(MapId map,bool longNight=false)=>map==MapId.Castle?(longNight?LongNight:Castle):Bellwether;

        // The shipped house: 95 s timed hunt with two records, 9 s warning, 38 s hunt,
        // 32 s recovery, no dread, no dice, no lies, no hostile return, no death penalty.
        public static readonly HuntRules Bellwether=new HuntRules
        {
            Name="Bellwether",
            TimedHuntSeconds=95,TimedHuntEvidence=2,HuntDreadMin=0,HuntDreadMinSpread=0,LowDreadChance=0,
            FirstHuntSeconds=0,FirstHuntJitter=0,FirstHuntEvidence=0,FirstHuntMin=0,FirstHuntMax=0,DreadDecayAllAbove=0,DoorHeldChance=0,
            DiceIntervalMin=0,DiceIntervalMax=0,DiceChanceAt40=0,DiceChanceAt60=0,DiceChanceAt80=0,DiceEvidenceBonus=0,
            WarningSeconds=9,HuntSeconds=38,RecoverySeconds=32,EscalationWarningSeconds=0,
            HuntSecondsMin=38,HuntSecondsMax=38,RecoverySecondsMin=32,RecoverySecondsMax=32,RecoveryDiceChance=0,RecoveryDiceDread=0,AnnounceRecovery=true,
            WarningSecondsMin=9,WarningSecondsMax=9,RecoveryLowChance=0,QuietPhaseChance=0,
            FeltLagMin=0,FeltLagMax=0,FeltNoise=0,FeltNoisePeriod=0,FeltCalm=0,
            FeltEpisodeGapMin=0,FeltEpisodeGapMax=0,FeltEpisodeMin=0,FeltEpisodeMax=0,FeltCalmFrom=0,FeltSpikeBelow=0,FeltSpikeMin=0,FeltSpikeMax=0,
            NoTellChance=0,FalseWarningChance=0,EventEscalationChance=0,EscalationRecoveryChance=0,HostileReturn=false,
            EscalationLitChance=0,EscalationDarkChance=0,ApparitionDiceShadowSeconds=0,DoorHeldBag=0,NearMissOpen0=0,NearMissOpen40=0,NearMissOpen80=0,NearMissOpen100=0,
            UsesDread=false,DreadDarkRate=0,DreadInsideRate=0,DreadSafeRecovery=0,DreadStart=0,
            DreadApparition=0,DreadThrow=0,DreadSeen=0,DreadGrabSurvived=0,
            DreadDecayRecovery=0,DreadDecayCalm=0,DreadDecayFloor=0,HuntDreadCap=0,GraceMin=0,GraceMax=0,
            GovernorStart=0,GovernorGain=0,GovernorMax=0,GovernorMinWindow=0,GovernorQuietShare=0,GovernorQuietSeconds=0,GovernorQuietScale=0,
            LieThreshold=0,DoorHeldThreshold=0,DeathPenalty=false,DeathDread=0,NailedHides=0,
            PerchMinSeconds=0,PerchMaxSeconds=0,
        };

        // The castle (FEAR_PLAN items 1-5, 7).
        public static readonly HuntRules Castle=new HuntRules
        {
            Name="Castle",
            TimedHuntSeconds=240,TimedHuntEvidence=0,HuntDreadMin=40,HuntDreadMinSpread=10,LowDreadChance=1f/24,
            FirstHuntSeconds=95,FirstHuntJitter=.25f,FirstHuntEvidence=2,FirstHuntMin=60,FirstHuntMax=180,DreadDecayAllAbove=60,DoorHeldChance=1f/6,
            DiceIntervalMin=2,DiceIntervalMax=6,DiceChanceAt40=1f/12,DiceChanceAt60=1f/6,DiceChanceAt80=1f/4,DiceEvidenceBonus=.5f,
            WarningSeconds=9,HuntSeconds=38,RecoverySeconds=32,EscalationWarningSeconds=1.5f,
            HuntSecondsMin=25,HuntSecondsMax=50,RecoverySecondsMin=10,RecoverySecondsMax=40,RecoveryDiceChance=1f/8,RecoveryDiceDread=80,AnnounceRecovery=false,
            WarningSecondsMin=5,WarningSecondsMax=14,RecoveryLowChance=1f/16,QuietPhaseChance=1f/80,
            FeltLagMin=20,FeltLagMax=30,FeltNoise=12,FeltNoisePeriod=6,FeltCalm=12,
            FeltEpisodeGapMin=200,FeltEpisodeGapMax=600,FeltEpisodeMin=20,FeltEpisodeMax=40,FeltCalmFrom=40,FeltSpikeBelow=60,FeltSpikeMin=65,FeltSpikeMax=85,
            NoTellChance=.35f,FalseWarningChance=.25f,EventEscalationChance=.35f,EscalationRecoveryChance=.15f,HostileReturn=true,
            EscalationLitChance=.15f,EscalationDarkChance=.25f,ApparitionDiceShadowSeconds=20,DoorHeldBag=5,NearMissOpen0=.02f,NearMissOpen40=.05f,NearMissOpen80=.25f,NearMissOpen100=.4f,
            UsesDread=true,DreadDarkRate=.08f,DreadInsideRate=.03f,DreadSafeRecovery=.5f,DreadStart=0,
            DreadApparition=12,DreadThrow=8,DreadSeen=15,DreadGrabSurvived=20,
            DreadDecayRecovery=.4f,DreadDecayCalm=.3f,DreadDecayFloor=20,HuntDreadCap=25,GraceMin=30,GraceMax=90,
            GovernorStart=.25f,GovernorGain=12,GovernorMax=3,GovernorMinWindow=300,GovernorQuietShare=.12f,GovernorQuietSeconds=180,GovernorQuietScale=.8f,
            LieThreshold=70,DoorHeldThreshold=60,DeathPenalty=true,DeathDread=60,NailedHides=0,
            PerchMinSeconds=0,PerchMaxSeconds=0,
        };

        // Field Settings "Long Night" dial on the castle (FEAR_PLAN item 6): no Warning
        // phase, Dread starts at 50, hunts 40-60 s (median 50), 2-5 s perches, two hide spots nailed shut.
        public static readonly HuntRules LongNight=MakeLongNight();
        static HuntRules MakeLongNight()
        {
            var r=Castle.Clone();
            r.Name="Long Night";r.WarningSeconds=0;r.DreadStart=50;r.HuntSeconds=50;r.HuntSecondsMin=40;r.HuntSecondsMax=60;
            r.PerchMinSeconds=2;r.PerchMaxSeconds=5;r.NailedHides=2;r.FalseWarningChance=0;
            return r;
        }
    }
}
