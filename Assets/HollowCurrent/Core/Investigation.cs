using System;
using System.Collections.Generic;

namespace HollowCurrent
{
    public enum Phase { Briefing, Survey, Disturbance, Warning, Hunt, Recovery, Containment, Return, Won, Dead }
    public enum Evidence { Induction, Handprint, Voice }
    public enum AnchorKind { Doll, Furnace, MusicBox }
    public enum HauntingPattern { DomesticLoop, Imitation, FollowingPerson }
    public enum ClosureMethod { LetSongEnd, BurnObject, MoveObject }

    // Pure deterministic rules: no scene, provider, clock, or random dependency.
    // Chance (castle dice hunts, broken tells) comes only from the injected
    // Dice function; null = a deterministic 0.5 so tests stay deterministic.
    public sealed class Investigation
    {
        // Per-map names and text (CaseBook.Bellwether unless a map says otherwise).
        public CaseBook Book { get; }
        // Per-map hunt and dread rules (Book.Rules unless overridden, e.g. HuntRules.LongNight).
        public HuntRules Rules { get; }
        // Returns a value in [0,1); Game passes () => UnityEngine.Random.value. null = 0.5.
        public Func<float> Dice { get; set; }
        public Investigation() : this(CaseBook.Bellwether) { }
        public Investigation(CaseBook book, Func<float> random = null, HuntRules rules = null)
        {
            Book = book ?? CaseBook.Bellwether;
            Rules = rules ?? Book.Rules ?? HuntRules.Bellwether;
            Dice = random;
            Dread = Rules.UsesDread ? Clamp(Rules.DreadStart) : 0;
            RunDreadMin = Rules.HuntDreadMin;
            CurrentHuntSeconds = Rules.HuntSeconds;
            CurrentRecoverySeconds = Rules.RecoverySeconds;
            // Per-run draws, castle only, in this order (Bellwether draws nothing):
            // 1 boundary, 2 felt lag, 3 felt noise seed.
            if (Rules.UsesDread)
            {
                if (Rules.HuntDreadMinSpread > 0) RunDreadMin = Rules.HuntDreadMin + Rules.HuntDreadMinSpread * (2 * Roll() - 1);
                FeltLagSeconds = Rules.FeltLagMax > Rules.FeltLagMin ? Rules.FeltLagMin + (Rules.FeltLagMax - Rules.FeltLagMin) * Roll() : Rules.FeltLagMin;
                if (Rules.FeltNoise > 0) feltSeed = (uint)(Roll() * 16777216f);
                feltBase = Dread;
                ScheduleFeltEpisode(0);
            }
        }
        public Phase Phase { get; private set; } = Phase.Briefing;
        public readonly HashSet<Evidence> Evidence = new HashSet<Evidence>();
        public readonly HashSet<string> Notes = new HashSet<string>();
        public bool Equipped { get; private set; }
        public bool BreakerOn { get; private set; } = true;
        public bool Diagnosed { get; private set; }
        public bool Survived { get; private set; }
        public float Elapsed { get; private set; }
        public float PhaseTime { get; private set; }
        public float SealProgress { get; private set; }
        public int Hunts { get; private set; }
        public int WrongDiagnoses { get; private set; }
        // Dread 0-100 (sanity). Never shown as a number and never part of Goal.
        public float Dread { get; private set; }
        // Dread is high enough for instruments, tells and sounds to lie (Game decides how).
        public bool Lies => Rules.LieThreshold > 0 && Dread >= Rules.LieThreshold;
        // Dread is high enough for a hide spot to refuse to open.
        // Roll mode (castle, wave K: DoorHeldChance > 0) never holds every door; call TryDoorHeld per hide attempt.
        public bool DoorHeld => Rules.DoorHeldChance <= 0 && Rules.DoorHeldThreshold > 0 && Dread >= Rules.DoorHeldThreshold;
        // One hide attempt: at Dread >= DoorHeldThreshold, held with DoorHeldChance (injected dice). No roll below it.
        public bool TryDoorHeld()
        {
            if (!Rules.UsesDread || Rules.DoorHeldChance <= 0 && Rules.DoorHeldBag <= 0 || Dread < Rules.DoorHeldThreshold || Terminal) return false;
            DoorAttempts++;
            bool held;
            if (Rules.DoorHeldBag > 0)
            {
                if (doorBagLeft <= 0) { doorBagLeft = Rules.DoorHeldBag; doorHeldAt = Math.Min(Rules.DoorHeldBag - 1, (int)(Roll() * Rules.DoorHeldBag)); }
                held = Rules.DoorHeldBag - doorBagLeft == doorHeldAt; doorBagLeft--;
            }
            else held = Roll() < Rules.DoorHeldChance;
            if (held) DoorsHeld++;
            return held;
        }
        // Eligible hide attempts (Dread >= DoorHeldThreshold) seen by TryDoorHeld.
        public int DoorAttempts { get; private set; }
        int doorBagLeft, doorHeldAt;
        public int DoorsHeld { get; private set; }
        // This Warning is a false alarm: it ends back in Disturbance, nothing comes.
        public bool FalseWarning { get; private set; }
        // The current hunt started with no Warning (dice no-tell, Long Night, hostile Return).
        public bool NoTellHunt { get; private set; }
        // The hunter may kill: a hunt, or the hostile Return until the bridge is crossed.
        public bool HuntActive => Phase == Phase.Hunt || Phase == Phase.Return && Rules.HostileReturn;
        // Deaths this run, carried through checkpoints (+1 for each restore after a death).
        public int Deaths { get; private set; }
        // The last checkpoint restore applied the death penalty (Game moves the next relic or page).
        public bool DeathPenaltyApplied { get; private set; }
        // Seconds the current Warning lasts: drawn WarningSecondsMin..Max for told and false
        // warnings alike (Bellwether 9), or EscalationWarningSeconds after TryEscalate.
        public float WarningLength { get; private set; }
        public float CurrentWarningSeconds => WarningLength;
        // This Warning is the short tell of TryEscalate (set before Changed fires). Use this, never the
        // length, to tell an escalation from a told or false Warning: those are drawn 5-14 s.
        public bool EscalatedWarning { get; private set; }
        // The phase the current threat (Warning or Hunt) started from: Disturbance, Recovery,
        // Survey, Containment (dice or bell), or Containment for the hostile Return. For logs.
        public Phase ThreatFrom { get; private set; }
        // This hunt's length and this Recovery's length (drawn when each starts; Bellwether 38 / 32).
        // UI and dispatch text must read these, never print a fixed number.
        public float CurrentHuntSeconds { get; private set; }
        public float CurrentRecoverySeconds { get; private set; }
        // This run's Dread boundary for the banded dice and the floor (castle 30-50). Diagnostics
        // only: never drive a player-facing channel from it.
        public float RunDreadMin { get; private set; }
        // What the player may feel: Dread lagged by FeltLagSeconds (20-30 s per run), with slow
        // +-12 noise, and seeded lie episodes (1-3 per 10 min, 20-40 s: a drift to calm at Dread >= 40,
        // a false spike at Dread < 60). 0-100. Drive heartbeat, journal pulse and silence frequency
        // from this, never from Dread. Always 0 when the map has no dread.
        public float FeltDread => Rules.UsesDread ? Clamp(feltBase + Rules.FeltNoise * FeltNoiseAt(Elapsed)) : 0;
        public float FeltLagSeconds { get; private set; }
        // Wave I (blind critic P3, runaway Dread). The part of Dread added by events (AddDread); it decays
        // in Recovery and the calm phases. The rest (darkness, time, DreadStart, ForceDread) only falls in
        // the safe zone. Diagnostics.
        public float DreadEvent { get; private set; }
        // Multiplier on the event-Dread decay; the integrator sets it by location (gatehouse, chapel fire). 1 = normal.
        public float DreadRelief { get; set; } = 1;
        // Dread AddDread has added during the current hunt (capped at Rules.HuntDreadCap).
        public float HuntDreadAdded { get; private set; }
        // Post-hunt grace: dice (banded, low, quiet-phase, recovery) and escalation are disarmed until it runs
        // out. GraceSeconds = the last draw (30-90 s, at hunt end), GraceRemaining counts down.
        public float GraceSeconds { get; private set; }
        public float GraceRemaining { get; private set; }
        // Share of the last 10 minutes (or of the run so far, if shorter) spent in Warning, Hunt or the
        // hostile Return. Observation only; it never changes the rules.
        public float HuntShare
        {
            get
            {
                float window = Math.Min(ShareWindow, Elapsed);
                if (window <= 0) return 0;
                float from = Elapsed - window, total = 0;
                foreach (var span in threatSpans) total += Math.Max(0, Math.Min(Elapsed, span[1] < 0 ? Elapsed : span[1]) - Math.Max(from, span[0]));
                return Math.Min(1, total / window);
            }
        }
        // Pacing governor (wave J): the live multiplier (1 = neutral) and the one applied to the last grace.
        public float Governor => GovernorFor(quietShareTime >= Rules.GovernorQuietSeconds);
        float GovernorFor(bool quiet)
        {
            if (Rules.GovernorGain <= 0) return 1;
            float g = GovernorShare;
            if (g > Rules.GovernorStart) return Math.Min(Math.Max(1, Rules.GovernorMax), 1 + Rules.GovernorGain * (g - Rules.GovernorStart));
            return quiet ? Rules.GovernorQuietScale : 1;
        }
        bool quietBeforeThreat;        // the quiet streak had reached GovernorQuietSeconds when the current threat began
        public float GovernorAtLastGrace { get; private set; } = 1;
        // The share the governor reads: threat time over the time since the first threat began (at most 10 minutes,
        // at least GovernorMinWindow seconds), so quiet minutes before the first hunt do not hide a dense run after it.
        public float GovernorShare
        {
            get
            {
                if (threatSpans.Count == 0) return 0;
                float since = Math.Min(ShareWindow, Elapsed - firstThreatAt);
                return Math.Min(1, ThreatSeconds() / Math.Max(since, Math.Max(1, Rules.GovernorMinWindow)));
            }
        }
        float firstThreatAt = -1;
        float quietShareTime;          // seconds the governor share has stayed below GovernorQuietShare
        float ThreatSeconds()
        {
            float from = Elapsed - Math.Min(ShareWindow, Elapsed), total = 0;
            foreach (var span in threatSpans) total += Math.Max(0, Math.Min(Elapsed, span[1] < 0 ? Elapsed : span[1]) - Math.Max(from, span[0]));
            return total;
        }
        const float ShareWindow = 600;
        readonly List<float[]> threatSpans = new List<float[]>(); // [start, end or -1 while open] in Elapsed time
        // Diagnostics: the running felt lie episode ("none", "calm", "spike") and how many have started.
        public string FeltEpisode => feltEpisodeKind == 1 ? "calm" : feltEpisodeKind == 2 ? "spike" : "none";
        public int FeltEpisodes { get; private set; }
        float feltBase;               // lagged Dread (or the episode target), before noise
        int feltEpisodeKind;          // 0 none, 1 calm, 2 spike
        float feltEpisodeEnd, feltEpisodeTarget, feltNextEpisodeAt = float.MaxValue;
        uint feltSeed;                // per-run noise seed
        float huntClock;              // floor clock: Disturbance time with Dread >= RunDreadMin since the last hunt
        float diceTimer = float.NaN;  // seconds to the next dice roll; NaN = not yet drawn
        bool diceHigh;                // the running dice timer belongs to the banded (true) or low-Dread (false) roll
        float firstHuntAt = -1;       // first-hunt deadline; -1 = not yet drawn
        float firstHuntClock;         // Disturbance time since the FirstHuntEvidence-th record (before any hunt)
        public int ContextCount => (Notes.Contains(Book.NoteTitles[0])?1:0)+(Notes.Contains(Book.NoteTitles[1])?1:0)+(Notes.Contains(Book.NoteTitles[2])?1:0);
        // Castle rules: phase-blind (critic r10b N5); a reconstruction filed during a hunt moves to Containment when the hunt ends.
        public bool MysteryReady => Evidence.Count==3&&ContextCount==3&&Survived&&!Diagnosed&&!Terminal&&(Rules.UsesDread||(Phase!=Phase.Warning||FalseWarning)&&Phase!=Phase.Hunt);
        public bool Terminal => Phase == Phase.Dead || Phase == Phase.Won;
        public event Action<string> Message;
        public event Action<Phase> Changed;

        [Serializable] public sealed class Checkpoint
        {
            public Evidence[] Evidence;
            public string[] Notes;
            public bool Equipped,BreakerOn,Diagnosed,Survived;
            public int RelicMask; public bool SwordForged,EscapeRoute;
            public float Elapsed;
            public int Hunts,WrongDiagnoses;
            public float Dread;
            public int Deaths;
            public float RunDreadMin = -1;
            public float DreadEvent;
        }
        public Checkpoint SaveCheckpoint()=>new Checkpoint {Evidence=new List<Evidence>(Evidence).ToArray(),Notes=new List<string>(Notes).ToArray(),Equipped=Equipped,BreakerOn=BreakerOn,Diagnosed=Diagnosed,Survived=Survived,Elapsed=Elapsed,Hunts=Hunts,WrongDiagnoses=WrongDiagnoses,Dread=Dread,Deaths=Deaths,RunDreadMin=RunDreadMin,DreadEvent=DreadEvent};
        // A restore follows a death unless afterDeath is false (the game only offers it on the
        // lost screen). After a death Deaths counts it, and with Rules.DeathPenalty Dread
        // restarts at Rules.DeathDread and DeathPenaltyApplied is set.
        public void RestoreCheckpoint(Checkpoint saved) => RestoreCheckpoint(saved, true);
        public void RestoreCheckpoint(Checkpoint saved, bool afterDeath)
        {
            if(saved==null)return;
            Evidence.Clear();Evidence.UnionWith(saved.Evidence);Notes.Clear();Notes.UnionWith(saved.Notes);
            Equipped=saved.Equipped;BreakerOn=saved.BreakerOn;Diagnosed=saved.Diagnosed;Survived=saved.Survived;Elapsed=saved.Elapsed;Hunts=saved.Hunts;WrongDiagnoses=saved.WrongDiagnoses;SealProgress=0;
            Deaths=Math.Max(Deaths,saved.Deaths+(afterDeath?1:0));
            DeathPenaltyApplied=afterDeath&&Rules.DeathPenalty&&Rules.UsesDread;
            Dread=!Rules.UsesDread?0:DeathPenaltyApplied?Clamp(Rules.DeathDread):Clamp(saved.Dread);
            // The saved event part keeps decaying; the death penalty's Dread above the saved ambient part is event Dread too.
            float savedAmbient=Math.Max(0,saved.Dread-Math.Max(0,saved.DreadEvent));
            DreadEvent=!Rules.UsesDread?0:Math.Max(0,Math.Min(Dread,DeathPenaltyApplied?Dread-savedAmbient:saved.DreadEvent));
            GraceRemaining=0;HuntDreadAdded=0;threatSpans.Clear();apparitionShadow=0;apparitionAt=float.NegativeInfinity;escalationPending=false;escalationDueAt=-1;sharePullAt=-1;followDueAt=-1;restUntil=-1;owedLead=-1;owedPlaceableAt=-1;quietShareTime=0;GovernorAtLastGrace=1;firstThreatAt=-1;quietBeforeThreat=false;
            if(Rules.UsesDread&&saved.RunDreadMin>=0)RunDreadMin=saved.RunDreadMin;
            feltBase=Dread;feltEpisodeKind=0;if(Rules.UsesDread)ScheduleFeltEpisode(Elapsed);firstHuntClock=0;firstHuntAt=-1;CurrentHuntSeconds=Rules.HuntSeconds;CurrentRecoverySeconds=Rules.RecoverySeconds;
            FalseWarning=false;NoTellHunt=false;huntClock=0;
            if(ContextCount>0){FirstGateDocAt=Elapsed;FirstGateDocKind="checkpoint";}
            Set(!Equipped?Phase.Briefing:Diagnosed?Phase.Containment:Evidence.Count>0?Phase.Disturbance:Phase.Survey);
        }
        public bool Equip()
        {
            if (Equipped) return false;
            Equipped = true;
            Say(Book.EquipMessage);
            return true;
        }
        public void EnterHouse()
        {
            if (Equipped && Phase == Phase.Briefing) Set(Phase.Survey);
        }
        public bool Record(Evidence value)
        {
            // Castle rules (UsesDread, critic r10b N5): a record succeeds in and out of a hunt, so refusal is never a hunt tell.
            bool huntGate = !Rules.UsesDread && (Phase == Phase.Hunt || Phase == Phase.Warning && !FalseWarning);
            if (!Equipped || Terminal || Phase == Phase.Briefing) return false;
            if (!Evidence.Add(value)) return false;
            if (FirstGateDocAt < 0) { FirstGateDocAt = Elapsed; FirstGateDocKind = "record"; }   // wave FF: an evidence record opens a cold run's first-hunt gate
            Say("Evidence secured / " + Book.EvidenceName(value) + ". Journal updated.");
            if (Evidence.Count == 1 && Phase == Phase.Survey) Set(Phase.Disturbance);
            return true;
        }
        public bool Read(string note)
        {
            if (Terminal || !Notes.Add(note)) return false;
            if (FirstNoteAt < 0) FirstNoteAt = Elapsed;
            if (FirstGateDocAt < 0 && note != Book.ArrivalNoteTitle) { FirstGateDocAt = Elapsed; FirstGateDocKind = "page"; }   // wave FF: a ledger page, never the arrival letter
            Say("Filed / " + note);
            return true;
        }
        public bool Conclude(AnchorKind anchor,HauntingPattern pattern,ClosureMethod closure)
        {
            if(!MysteryReady)return false;
            if(anchor!=AnchorKind.MusicBox||pattern!=HauntingPattern.DomesticLoop||closure!=ClosureMethod.LetSongEnd)
            {
                WrongDiagnoses++;
                Say(anchor!=AnchorKind.MusicBox?Book.WrongAnchor:pattern!=HauntingPattern.DomesticLoop?Book.WrongPattern:Book.WrongClosure);
                return false;
            }
            Diagnosed=true;
            // Castle: during a Warning or hunt the case is diagnosed now and enters Containment when Recovery ends.
            if (!(Phase == Phase.Hunt || Phase == Phase.Warning && !FalseWarning)) Set(Phase.Containment);
            Say(Book.Diagnosed);
            return true;
        }
        // The records establish the answer; the player closes the physical anchor, not a multiple-choice quiz.
        public bool ResolveNaturally()
        {
            if(!Equipped||Terminal||Diagnosed||Evidence.Count<3||(Rules.UsesDread&&ContextCount<3))return false;
            Diagnosed=true;
            if(Phase!=Phase.Hunt&&Phase!=Phase.Warning)Set(Phase.Containment);
            Say(Book.Diagnosed);return true;
        }
        public void RestorePower()
        {
            if (!Rules.UsesDread && (Phase == Phase.Hunt || Phase == Phase.Warning) || Terminal) return; // castle: any live phase (N5)
            BreakerOn = true;
            Say(Book.PowerRestored);
        }
        // Keeps the original signature: inside with a lit candle nearby, never in the safe zone.
        public void Tick(float dt, bool inside) => Tick(dt, inside, true, false);
        public void Tick(float dt, bool inside, bool litNearby, bool inSafe)
        {
            if (dt < 0 || float.IsNaN(dt) || float.IsInfinity(dt)) throw new ArgumentOutOfRangeException(nameof(dt));
            if (FrameSource == null) wonThisTick = false;
            if (Terminal || Phase == Phase.Briefing) return;
            Elapsed += dt;
            // Leaving the house does not freeze threat progression.
            PhaseTime += dt;
            if (Rules.UsesDread)
            {
                if (inSafe) { Dread = Clamp(Dread - Rules.DreadSafeRecovery * dt); DreadEvent = Math.Min(DreadEvent, Dread); }
                else if (inside) Dread = Clamp(Dread + (litNearby ? Rules.DreadInsideRate : Rules.DreadDarkRate) * dt);
                DecayEventDread(dt);
                if (GraceRemaining > 0) GraceRemaining = Math.Max(0, GraceRemaining - dt);
                if (DiceHoldRemaining > 0 && Phase != Phase.Warning && Phase != Phase.Hunt) DiceHoldRemaining = Math.Max(0, DiceHoldRemaining - dt);
                if (Hunts > 0 && !LongQuietDone && Rules.FirstHuntAfterHold > 0) { quietStretch = HuntActive || Phase == Phase.Warning ? 0 : quietStretch + dt; if (quietStretch >= Rules.FirstHuntAfterHold) LongQuietDone = true; }
                if (Rules.GovernorGain > 0)
                {
                    quietShareTime = GovernorShare < Rules.GovernorQuietShare ? quietShareTime + dt : 0;
                }
                UpdateFeltEpisode();
                float target = feltEpisodeKind == 1 ? Rules.FeltCalm : feltEpisodeKind == 2 ? feltEpisodeTarget : Dread;
                feltBase += (target - feltBase) * (FeltLagSeconds > 0 ? 1 - (float)Math.Exp(-dt / FeltLagSeconds) : 1);
            }
            if (Phase == Phase.Disturbance) TickDisturbance(dt);
            else if (Phase == Phase.Warning && PhaseTime >= WarningLength)
            {
                if (FalseWarning) { FalseWarning = false; Set(Diagnosed ? Phase.Containment : Phase.Disturbance); }
                else StartHunt(false);
            }
            else if (Phase == Phase.Hunt && PhaseTime >= CurrentHuntSeconds)
            {
                Survived = true;
                CurrentRecoverySeconds = Rules.RecoverySecondsMax > Rules.RecoverySecondsMin ? HuntRules.Draw(Rules.RecoverySecondsMin, Rules.RecoverySeconds, Rules.RecoverySecondsMax, Roll()) : Rules.RecoverySeconds;
                if (Hunts == 1 && Rules.FirstHuntAfterHold > 0) HoldDice(Rules.FirstHuntAfterHold);
                if (Rules.GraceMax > 0) { GovernorAtLastGrace = GovernorFor(quietBeforeThreat); GraceSeconds = (Rules.GraceMin + (Rules.GraceMax - Rules.GraceMin) * Roll()) * GovernorAtLastGrace; GraceRemaining = GraceSeconds; }
                Set(Phase.Recovery);
                if (Rules.AnnounceRecovery) Say(Book.RecoveryMessage);
            }
            else if (Phase == Phase.Recovery)
            {
                if (PhaseTime >= CurrentRecoverySeconds) Set(Diagnosed ? Phase.Containment : Phase.Disturbance);
                else TickRecoveryDice(dt);
            }
            else if (Phase == Phase.Survey || Phase == Phase.Containment) { if (!TickFirstHunt(dt)) TickQuietDice(dt); }
            // Wave FE (castle): an armed escalation's delayed tell, then the share floor (both no-ops without their rules).
            if (Rules.UsesDread && !Terminal) { TickArmedEscalation(); TickApparitionFollow(); TickShareFloor(); }
        }
        // First-hunt guarantee (the mystery needs a hunt survived, so it can never stall). Record-keyed (Disturbance, from the
        // FirstHuntEvidence-th record) or, with Rules.FirstHuntFromSpawn, from the run's first Survey second in Survey and
        // Disturbance alike. Returns true when the threat started this tick.
        bool TickFirstHunt(float dt)
        {
            if (Rules.FirstHuntSeconds <= 0 || Hunts > 0) return false;
            if (Rules.FirstHuntFromSpawn ? Phase != Phase.Survey && Phase != Phase.Disturbance : Phase != Phase.Disturbance || Evidence.Count < Rules.FirstHuntEvidence) return false;
            firstHuntClock += dt;
            if (firstHuntAt < 0) firstHuntAt = Plan != null ? Plan.FirstHuntMin + (Plan.FirstHuntMax - Plan.FirstHuntMin) * Roll() : Rules.FirstHuntMax > Rules.FirstHuntMin ? HuntRules.Draw(Rules.FirstHuntMin, Rules.FirstHuntSeconds, Rules.FirstHuntMax, Roll()) : Rules.FirstHuntSeconds * (1 - Rules.FirstHuntJitter + 2 * Rules.FirstHuntJitter * Roll());
            if (firstHuntClock < firstHuntAt) return false;
            if (ColdHold) return false;   // wave FD: a cold run's first hunt waits for a read page or ColdFirstHuntCap
            if (HuntGateHolds("first")) return false;   // wave FE: never within ApparitionHuntGap after a visible apparition
            StartThreat(false);
            return true;
        }
        // Diagnostics: the drawn first-hunt deadline (seconds on the first-hunt clock; -1 = not drawn yet) and the clock itself.
        public float FirstHuntDeadline => firstHuntAt;
        public float FirstHuntClock => firstHuntClock;
        // Event Dread decays toward 0 (after the first hunt, never below DreadDecayFloor in total).
        void DecayEventDread(float dt)
        {
            float above = Rules.DreadDecayAllAbove > 0 ? Math.Max(0, Dread - Rules.DreadDecayAllAbove) : 0;
            float decaying = Math.Max(DreadEvent, above);
            if (decaying <= 0) return;
            float rate = Phase == Phase.Recovery ? Rules.DreadDecayRecovery : Phase == Phase.Survey || Phase == Phase.Disturbance || Phase == Phase.Containment ? Rules.DreadDecayCalm : 0;
            rate *= Math.Max(0, DreadRelief);
            if (rate <= 0) return;
            float drop = Math.Min(decaying, rate * dt);
            if (Hunts > 0 && Rules.DreadDecayFloor > 0) drop = Math.Min(drop, Math.Max(0, Dread - Rules.DreadDecayFloor));
            Dread = Clamp(Dread - drop); DreadEvent = Math.Max(0, Math.Min(Dread, DreadEvent - drop));
        }
        // Survey and Containment after the first hunt: a rare flat roll, so no phase is a refuge.
        void TickQuietDice(float dt)
        {
            if (Rules.QuietPhaseChance <= 0 || Rules.DiceIntervalMax <= 0 || Hunts == 0 || GraceRemaining > 0 || DiceHeld || ApparitionRestHolds) return;
            if (float.IsNaN(diceTimer)) { diceTimer = NextDiceInterval(); return; }
            diceTimer -= dt;
            if (diceTimer > 0) return;
            if (HuntGateHolds("dice")) { diceTimer = NextDiceInterval(); return; }
            if (Roll() < Rules.QuietPhaseChance) { StartThreat(true); return; }
            diceTimer = NextDiceInterval();
        }
        // Felt lie episodes, scheduled from the noise seed (never from Dice).
        void ScheduleFeltEpisode(float from)
        {
            feltNextEpisodeAt = Rules.FeltEpisodeGapMax > 0 ? from + Lerp(Rules.FeltEpisodeGapMin, Rules.FeltEpisodeGapMax, Unit(101, FeltEpisodes)) : float.MaxValue;
        }
        void UpdateFeltEpisode()
        {
            if (feltEpisodeKind != 0 && Elapsed >= feltEpisodeEnd) feltEpisodeKind = 0;
            if (feltEpisodeKind != 0 || Elapsed < feltNextEpisodeAt) return;
            int i = FeltEpisodes;
            bool calm = Dread >= Rules.FeltCalmFrom, spike = Dread < Rules.FeltSpikeBelow;
            feltEpisodeKind = calm && spike ? (Unit(202, i) < .5f ? 1 : 2) : calm ? 1 : 2;
            feltEpisodeEnd = Elapsed + Lerp(Rules.FeltEpisodeMin, Rules.FeltEpisodeMax, Unit(303, i));
            feltEpisodeTarget = Lerp(Rules.FeltSpikeMin, Rules.FeltSpikeMax, Unit(404, i));
            FeltEpisodes++;
            ScheduleFeltEpisode(feltNextEpisodeAt);
        }
        float Unit(uint salt, int i) => (Knot(feltSeed ^ unchecked(salt * 0x85ebca6bu), i) + 1) * .5f;
        static float Lerp(float a, float b, float t) => a + (b - a) * t;
        // Recovery is not a guaranteed quiet window at high Dread.
        void TickRecoveryDice(float dt)
        {
            float chance = DiceDread >= Rules.RecoveryDiceDread ? Rules.RecoveryDiceChance : Rules.RecoveryLowChance;
            if (chance <= 0 || Rules.DiceIntervalMax <= 0 || GraceRemaining > 0 || DiceHeld || ApparitionRestHolds) return;
            if (float.IsNaN(diceTimer)) { diceTimer = NextDiceInterval(); return; }
            diceTimer -= dt;
            if (diceTimer > 0) return;
            if (HuntGateHolds("dice")) { diceTimer = NextDiceInterval(); return; }
            if (Roll() < chance) { StartThreat(true); return; }
            diceTimer = NextDiceInterval();
        }
        // Smooth value noise in [-1,1], one knot every FeltNoisePeriod seconds (pure hash, no Random).
        float FeltNoiseAt(float t)
        {
            if (Rules.FeltNoise <= 0 || Rules.FeltNoisePeriod <= 0) return 0;
            float x = t / Rules.FeltNoisePeriod;
            int i = (int)Math.Floor(x);
            float f = x - i, s = f * f * (3 - 2 * f);
            float a = Knot(feltSeed, i), b = Knot(feltSeed, i + 1);
            return a + (b - a) * s;
        }
        static float Knot(uint seed, int i)
        {
            uint h = seed ^ unchecked((uint)i * 0x9E3779B9u);
            h ^= h >> 16; h = unchecked(h * 0x7feb352du); h ^= h >> 15; h = unchecked(h * 0x846ca68bu); h ^= h >> 16;
            return h / 4294967295f * 2 - 1;
        }
        void TickDisturbance(float dt)
        {
            float diceDread = DiceDread;
            bool dreadful = diceDread >= RunDreadMin;
            if (dreadful) huntClock += dt;
            // Crossing the boundary restarts the roll schedule for the new kind of roll.
            if (dreadful != diceHigh) { diceHigh = dreadful; diceTimer = float.NaN; }
            // Wave FD: before a cold run's gate opens, only TickFirstHunt may start the first hunt (and it waits for the gate too).
            if (ColdHold) { TickFirstHunt(dt); return; }
            // Timed hunt: Bellwether's 95 s timer, the castle's 240 s floor.
            if (Rules.TimedHuntSeconds > 0 && dreadful && huntClock >= Rules.TimedHuntSeconds && Evidence.Count >= Rules.TimedHuntEvidence && !DiceHeld) { if (HuntGateHolds("timed")) return; StartThreat(false); return; }
            // First-hunt guarantee so the mystery cannot stall at low Dread (TickFirstHunt: record-keyed, or from spawn).
            if (TickFirstHunt(dt)) return;
            // Dice-roll hunts: banded at or above the run's boundary, a flat low roll below it after the first hunt.
            bool low = !dreadful && Hunts > 0 && Rules.LowDreadChance > 0;
            if (Rules.DiceIntervalMax > 0 && (dreadful || low) && GraceRemaining <= 0 && !DiceHeld && !ApparitionRestHolds)
            {
                if (float.IsNaN(diceTimer)) { diceTimer = NextDiceInterval(); return; }
                diceTimer -= dt;
                if (diceTimer > 0) return;
                if (HuntGateHolds("dice")) { diceTimer = NextDiceInterval(); return; }
                float chance = dreadful ? Rules.DiceChance(diceDread, Evidence.Count, RunDreadMin) / Math.Max(1, Governor) : LowChanceNow;
                if (Roll() < chance) { StartThreat(true); return; }
                diceTimer = NextDiceInterval();
            }
        }
        // Wave FE: the low-Dread roll, scaled by Rules.LateLowChanceScale in a Late-band run once its long quiet stretch has happened
        // (the Late band's first hunt comes 160-200 s in, so its later cadence is quicker; still a memoryless roll, so gaps stay irregular).
        public float LowChanceNow => Rules.LowDreadChance * (Plan != null && Plan.Band == FirstHuntBand.Late && LongQuietDone && Rules.LateLowChanceScale > 0 ? Rules.LateLowChanceScale : 1);
        float NextDiceInterval() => Rules.DiceIntervalMin + (Rules.DiceIntervalMax - Rules.DiceIntervalMin) * Roll();
        float Roll()
        {
            float value = Dice == null ? .5f : Dice();
            return float.IsNaN(value) ? .5f : Math.Max(0, Math.Min(.99999f, value));
        }
        static float Clamp(float value) => Math.Max(0, Math.Min(100, value));
        // A rolled or forced hunt: no-tell, false alarm (dice only) or a normal Warning.
        void StartThreat(bool fromDice)
        {
            ThreatFrom = Phase;
            // Wave FD (r13 critic: a cold player's first hunt came with no tell at 87 s): a cold run's first hunt is always a told
            // Warning (no no-tell roll, never a false alarm), drawn WarningSecondsMin..Max like every Warning.
            if (ColdFirstHunt) { Warn(DrawWarningSeconds(), false, true); return; }
            // No-tell starts leave BreakerOn alone: the brazier and room tone must not announce them.
            if (Rules.WarningSeconds <= 0 || Rules.NoTellChance > 0 && Roll() < Rules.NoTellChance) { StartHunt(true); return; }
            bool falseAlarm = fromDice && Rules.FalseWarningChance > 0 && Roll() < Rules.FalseWarningChance;
            Warn(DrawWarningSeconds(), falseAlarm, true);
        }
        void Warn(float seconds, bool falseAlarm, bool say, bool escalated = false)
        {
            // Wave FE: a Warning that would hand over to its hunt inside ApparitionHuntGap after a visible apparition is lengthened at its
            // start (so the strobe and its timer agree). Dice, timed and first-hunt Warnings never start inside the gap; this covers the bell.
            if (Rules.ApparitionHuntGap > 0 && !falseAlarm)
            {
                float minLength = apparitionAt + Rules.ApparitionHuntGap - Elapsed;
                if (seconds < minLength) { Note("HC_APPARITION_GATE deferred=hunt source=" + (escalated ? "escalation" : "warning") + " reason=apparition_recent since=" + F1(Elapsed - apparitionAt) + " warning=" + F1(seconds) + "->" + F1(minLength) + " t=" + F1(Elapsed)); seconds = minLength; }
            }
            EscalatedWarning = escalated; escalationPending = escalated;
            BreakerOn = false;
            WarningLength = seconds;
            FalseWarning = falseAlarm;
            NoTellHunt = false;
            Set(Phase.Warning);
            if (say) Say(Book.WarningMessage);
        }
        void StartHunt(bool noTell)
        {
            // Wave FD: what preceded this hunt, for HC_HUNT_TELL (read before Set clears EscalatedWarning).
            LastTellKind = noTell || Phase != Phase.Warning ? "none" : EscalatedWarning ? "escalation" : "warning";
            LastTellLead = LastTellKind == "none" ? 0 : PhaseTime;
            if (Hunts == 0) FirstHuntColdGate = ColdFirstHunt ? (ColdGateDocAt >= 0 ? (Rules.ColdGateNeedsLedger ? FirstGateDocKind : "page") : "cap") : "off";
            Hunts++;
            previousHuntFromEscalation = LastHuntFromEscalation; LastHuntFromEscalation = escalationPending; escalationPending = false;
            FalseWarning = false;
            NoTellHunt = noTell;
            HuntDreadAdded = 0;
            CurrentHuntSeconds = Rules.HuntSecondsMax > Rules.HuntSecondsMin ? HuntRules.Draw(Rules.HuntSecondsMin, Rules.HuntSeconds, Rules.HuntSecondsMax, Roll()) : Rules.HuntSeconds;
            Set(Phase.Hunt);
        }
        // Castle rules only (UsesDread): a hunt that starts now with no Warning (the sarcophagus gamble, item 18).
        // Bellwether rules ignore it. Only from the calm phases; returns whether the hunt started.
        public bool BeginNoTellHunt()
        {
            if (!Rules.UsesDread || Terminal) return false;
            if (Phase != Phase.Survey && Phase != Phase.Disturbance && Phase != Phase.Recovery) return false;
            ThreatFrom = Phase;
            StartHunt(true);
            return true;
        }
        public void BeginWarning()
        {
            if (Terminal || Phase == Phase.Return || Phase == Phase.Containment) return;
            ThreatFrom = Phase;
            // Summons (the bell) always come true. Long Night has no Warning phase.
            if (Rules.WarningSeconds <= 0) { StartHunt(true); return; }
            Warn(DrawWarningSeconds(), false, true);
        }
        float DrawWarningSeconds() => Rules.WarningSecondsMax > Rules.WarningSecondsMin ? HuntRules.Draw(Rules.WarningSecondsMin, Rules.WarningSeconds, Rules.WarningSecondsMax, Roll()) : Rules.WarningSeconds;
        // Automated playtest driver only: set Dread exactly and restart the dice schedule, so the next
        // value the driver queues is always consumed as an interval (then hunt roll, tell roll, false-alarm
        // roll, warning length, hunt length). Ignored for Dread when the map has no dread.
        public void ForceDread(float target)
        {
            if (Rules.UsesDread && !float.IsNaN(target) && !float.IsInfinity(target)) { Dread = Clamp(target); DreadEvent = 0; }
            GraceRemaining = 0; DiceHoldRemaining = 0;
            diceTimer = float.NaN;
            diceHigh = Dread >= RunDreadMin;
        }
        // Wave L. ScareDirector calls this for every visible castle apparition, in place of AddDread(Rules.DreadApparition)
        // and before TryEscalate: it counts the apparition and adds its Dread, which the dice ignore for
        // ApparitionDiceShadowSeconds (so an apparition's bump never arms or raises the dice by itself).
        public void NoteApparition()
        {
            Apparitions++;
            float before = Dread;
            AddDread(Rules.DreadApparition);
            apparitionShadow = Math.Max(0, Dread - before); apparitionAt = Elapsed;
            DrawApparitionLead();
        }
        // ---- Wave FF: the apparition's lead (HuntRules.ApparitionFollow* / ApparitionRest*) -------------------------------------------------
        // One draw per visible apparition. "follow": a dice-style threat (the usual no-tell / false-alarm / told rolls) is armed
        // ApparitionFollowMin..Max s after it; "rest": the dice (banded, low, quiet-phase, recovery) are held until ApparitionRestMin..Max s
        // after it; "open": nothing (the memoryless dice). Never before the first hunt, in the post-hunt grace, in a dice hold, with a threat
        // already armed or outside Disturbance / Recovery: those log kind=none. Logs HC_APPARITION_LEAD.
        public string LastApparitionLead { get; private set; } = "";
        public int LeadFollows { get; private set; }
        public int LeadRests { get; private set; }
        public int LeadFollowFired { get; private set; }
        float followDueAt = -1, restUntil = -1;
        public float FollowDueAt => followDueAt;
        // The dice are resting after an apparition (wave FF).
        public bool ApparitionRestHolds => restUntil >= 0 && Elapsed < restUntil;
        // The first hunt's lead kind for this run ("" = not drawn yet, then follow | rest | open), drawn at the run's first apparition.
        public string FirstLead { get; private set; } = "";
        bool firstLeadPulled;
        public bool FirstLeadPulled => firstLeadPulled;
        // Seconds before a planned threat during which no apparition may land (ScareRhythm.CastlePlannedHuntWindow, or FirstLeadRestWindow
        // before a first hunt whose lead drew "rest").
        public float ApparitionWindow => Hunts == 0 && FirstLead == "rest" && Rules.FirstLeadRestWindow > ScareRhythm.CastlePlannedHuntWindow ? Rules.FirstLeadRestWindow : ScareRhythm.CastlePlannedHuntWindow;
        // PlannedThreatIn shifted so ScareRhythm.CastleBlocked's 11.5 s window acts as ApparitionWindow (ScareDirector and the session model).
        public float PlannedThreatInForApparition => PlannedThreatIn - Math.Max(0, ApparitionWindow - ScareRhythm.CastlePlannedHuntWindow);
        float firstFollowLead = -1;
        void DrawFirstLead()
        {
            if (FirstLead.Length > 0 || Rules.FirstLeadFollowChance <= 0 && Rules.FirstLeadRestChance <= 0) return;
            float u = Roll();
            FirstLead = u < Rules.FirstLeadFollowChance ? "follow" : u < Rules.FirstLeadFollowChance + Rules.FirstLeadRestChance ? "rest" : "open";
            if (FirstLead == "follow") firstFollowLead = Rules.ApparitionFollowMin + (Rules.ApparitionFollowMax - Rules.ApparitionFollowMin) * Roll();
            Note("HC_APPARITION_LEAD first=" + FirstLead + (FirstLead == "follow" ? " lead=" + F1(firstFollowLead) : "") + " u=" + u.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " t=" + F1(Elapsed));
        }
        // Wave FF: a first hunt whose lead drew "follow" and that no apparition has been pulled to yet asks for one now, its drawn lead before
        // the deadline (ScareDirector fires it when the rhythm's spacing allows; NoteApparition then ties the deadline to it).
        public bool WantsApparitionNow
        {
            get
            {
                if (Hunts > 0) return OwedFollowWanted;   // wave FG: an owed follow asks for its apparition
                if (FirstLead != "follow" || firstLeadPulled || firstHuntAt < 0 || ColdHold || Terminal) return false;
                if (Phase != Phase.Survey && Phase != Phase.Disturbance) return false;
                float left = firstHuntAt - firstHuntClock;
                // fg-polish (FF natural4: the window was lead..12 s, 0.9 s wide for a 12.9 s lead, and its one try found no visible point):
                // it opens FirstFollowWantEarly s before the lead and stays open to FirstFollowWantLast s, while tying the deadline to the
                // apparition (pulled earlier or pushed later) keeps the plan's band, the rule NoteApparition applies.
                if (left > firstFollowLead + FirstFollowWantEarly || left <= FirstFollowWantLast) return false;
                float bandMin = Plan != null ? Plan.FirstHuntMin : Rules.FirstHuntMin, bandMax = Plan != null ? Plan.FirstHuntMax : Rules.FirstHuntMax;
                float tied = firstHuntClock + firstFollowLead;
                return tied >= bandMin && tied <= Math.Max(bandMax, firstHuntAt);
            }
        }
        public const float FirstFollowWantEarly = 8, FirstFollowWantLast = 3;
        void DrawApparitionLead()
        {
            if (Hunts == 0 && Rules.UsesDread)
            {
                // (FirstLead is drawn with the run plan, UseRunPlan; a run without a plan keeps the open first lead.)
                // The first hunt is pulled to follow this apparition (once), if its deadline is further out and the pull keeps the band minimum.
                if (FirstLead == "follow" && !firstLeadPulled && firstHuntAt >= 0 && Rules.FirstHuntSeconds > 0)
                {
                    float lead = firstFollowLead;
                    float bandMin = Plan != null ? Plan.FirstHuntMin : Rules.FirstHuntMin, bandMax = Plan != null ? Plan.FirstHuntMax : Rules.FirstHuntMax;
                    float left = firstHuntAt - firstHuntClock;
                    // Pulled earlier, or pushed a little later (a deadline due sooner than the lead), always inside the plan's band.
                    if (firstHuntClock + lead >= bandMin && firstHuntClock + lead <= Math.Max(bandMax, firstHuntAt) && !ColdHold)
                    {
                        firstLeadPulled = true; firstHuntAt = firstHuntClock + lead;
                        Note("HC_APPARITION_LEAD kind=first_follow threatIn=" + F1(lead) + " was=" + F1(left) + " t=" + F1(Elapsed));
                    }
                }
            }
            if (Rules.ApparitionFollowChance <= 0 && Rules.ApparitionRestChance <= 0) return;
            // (An apparition in the post-hunt grace or a dice hold draws too: a follow that would still land in it is cancelled when due, a rest
            // reaches past it.)
            string block = Hunts == 0 ? "before_first_hunt" : escalationDueAt >= 0 || followDueAt >= 0 || sharePullAt >= 0 ? "armed"
                : Phase != Phase.Disturbance && Phase != Phase.Recovery ? "phase_" + Phase : null;
            // Wave FG: an owed follow is adopted by the first apparition whose follow would land outside the quiet (or in the grace's last
            // ApparitionFollowGraceCut s); an apparition that cannot carry it draws nothing.
            if (block == null && owedLead >= 0)
            {
                if (FollowQuietAt(owedLead, out float graceAt, out float holdAt) == null)
                {
                    followDueAt = apparitionAt + owedLead; FollowAdopted++;
                    Note("HC_APPARITION_FOLLOWUP adopted lead=" + F1(owedLead) + " owedFor=" + F1(Elapsed - owedSince) + " graceAtDue=" + F1(graceAt) + " t=" + F1(Elapsed));
                    Note("HC_APPARITION_LEAD kind=follow owed=adopted threatIn=" + F1(followDueAt - Elapsed) + " t=" + F1(Elapsed));
                    owedLead = -1; owedPlaceableAt = -1; LastApparitionLead = "follow";
                    return;
                }
                block = "owed";
            }
            if (block != null) { LastApparitionLead = "none"; Note("HC_APPARITION_LEAD kind=none reason=" + block + " t=" + F1(Elapsed)); return; }
            float u = Roll();
            if (u < Rules.ApparitionFollowChance)
            {
                float lead = Rules.ApparitionFollowMin + (Rules.ApparitionFollowMax - Rules.ApparitionFollowMin) * Roll();
                if (Rules.ApparitionHuntGap > 0) lead = Math.Max(lead, Rules.ApparitionHuntGap + .1f);
                LeadFollows++; LastApparitionLead = "follow";
                string quiet = FollowQuietAt(lead, out float graceAt, out float holdAt);
                if (quiet != null && Rules.ApparitionFollowOweSeconds > 0)
                {
                    // Wave FG: it would land in the quiet: owed to a later apparition (WantsApparitionNow asks for one when the quiet allows).
                    owedLead = lead; owedSince = Elapsed; owedPlaceableAt = -1; FollowOwed++;
                    Note("HC_APPARITION_FOLLOWUP drawn=owed lead=" + F1(lead) + " quiet=" + quiet + " graceAtDue=" + F1(graceAt) + " holdAtDue=" + F1(holdAt) + " t=" + F1(Elapsed));
                    Note("HC_APPARITION_LEAD kind=follow owed=" + quiet + " lead=" + F1(lead) + " u=" + u.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " t=" + F1(Elapsed));
                }
                else
                {
                    followDueAt = apparitionAt + lead;
                    Note("HC_APPARITION_FOLLOWUP drawn=direct lead=" + F1(lead) + " graceAtDue=" + F1(graceAt) + " t=" + F1(Elapsed));
                    Note("HC_APPARITION_LEAD kind=follow threatIn=" + F1(followDueAt - Elapsed) + " u=" + u.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " t=" + F1(Elapsed));
                }
            }
            else if (u < Rules.ApparitionFollowChance + Rules.ApparitionRestChance)
            {
                restUntil = apparitionAt + Rules.ApparitionRestMin + (Rules.ApparitionRestMax - Rules.ApparitionRestMin) * Roll();
                LeadRests++; LastApparitionLead = "rest";
                Note("HC_APPARITION_LEAD kind=rest diceHeldFor=" + F1(restUntil - Elapsed) + " u=" + u.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " t=" + F1(Elapsed));
            }
            else { LastApparitionLead = "open"; Note("HC_APPARITION_LEAD kind=open u=" + u.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " t=" + F1(Elapsed)); }
        }
        // Fires an armed follow when its time comes (Disturbance or Recovery, no dice hold, at most ApparitionFollowGraceCut s of grace left,
        // which it ends); anything else first cancels it. Wave FG: an owed follow lapses or is cancelled here too.
        void TickApparitionFollow()
        {
            if (owedLead >= 0)
            {
                if (Terminal || Phase != Phase.Disturbance && Phase != Phase.Recovery) { owedLead = -1; owedPlaceableAt = -1; CancelFollow(Phase == Phase.Warning || Phase == Phase.Hunt ? "threat" : "phase_" + Phase, "owed"); }
                else if (FollowQuietAt(owedLead, out _, out _) == null)
                {
                    if (owedPlaceableAt < 0) owedPlaceableAt = Elapsed;
                    else if (Elapsed - owedPlaceableAt > Rules.ApparitionFollowOweSeconds) { owedLead = -1; owedPlaceableAt = -1; CancelFollow("lapsed", "owed"); }
                }
            }
            if (followDueAt < 0) return;
            if (Terminal || Phase != Phase.Disturbance && Phase != Phase.Recovery) { followDueAt = -1; CancelFollow(Phase == Phase.Warning || Phase == Phase.Hunt ? "threat" : "phase_" + Phase, "armed"); return; }
            if (Elapsed < followDueAt) return;
            followDueAt = -1;
            if (HuntGateHolds("follow")) { CancelFollow("gate", "armed"); return; }
            float cut = 0;
            if (GraceRemaining > 0 && GraceRemaining <= Rules.ApparitionFollowGraceCut && !DiceHeld) { cut = GraceRemaining; GraceRemaining = 0; }
            if (GraceRemaining > 0 || DiceHeld) { CancelFollow(GraceRemaining > 0 ? "grace" : "held", "armed"); return; }
            LeadFollowFired++;
            Note("HC_APPARITION_FOLLOWUP fired since=" + F1(Elapsed - apparitionAt) + " graceCut=" + F1(cut) + " t=" + F1(Elapsed));
            Note("HC_APPARITION_LEAD follow=fired since=" + F1(Elapsed - apparitionAt) + " t=" + F1(Elapsed));
            StartThreat(true);
        }
        // ---- Wave FG: follow-ups that survive the quiet ---------------------------------------------------------------------------------------
        // Drawn follows: direct (armed at once) or owed; adopted owed follows; fired (LeadFollowFired); cancelled (any reason, armed or owed).
        public int FollowOwed { get; private set; }
        public int FollowAdopted { get; private set; }
        public int FollowCancelled { get; private set; }
        public float OwedFollowLead => owedLead;
        float owedLead = -1, owedSince = -1, owedPlaceableAt = -1;
        void CancelFollow(string reason, string state)
        {
            FollowCancelled++;
            Note("HC_APPARITION_FOLLOWUP cancelled=" + reason + " state=" + state + " t=" + F1(Elapsed));
            Note("HC_APPARITION_LEAD follow=cancelled reason=" + reason + " t=" + F1(Elapsed));
        }
        // What quiet a follow due `lead` s from now would meet: "held" (a dice hold still running then; the hold is threat-free time, so this
        // keeps the long quiet stretch and the lull whole), "grace" (more than ApparitionFollowGraceCut s of grace left then), or null (it fires).
        string FollowQuietAt(float lead, out float graceAtDue, out float holdAtDue)
        {
            graceAtDue = Math.Max(0, GraceRemaining - lead);
            holdAtDue = DiceHeld ? Math.Max(0, DiceHoldRemaining - lead) : 0;
            if (holdAtDue > 0) return "held";
            if (graceAtDue > Rules.ApparitionFollowGraceCut) return "grace";
            return null;
        }
        // An owed follow could be carried by an apparition now: the calm phases, its threat would fire, and no other known threat comes first.
        bool OwedFollowWanted => owedLead >= 0 && !Terminal && (Phase == Phase.Disturbance || Phase == Phase.Recovery) && escalationDueAt < 0 && sharePullAt < 0
            && FollowQuietAt(owedLead, out _, out _) == null && PlannedThreatIn > owedLead + 2;
        public int Apparitions { get; private set; }
        // Wave L3. The run's plan (RunHistory.Next): the first hunt is due FirstHuntMin..Max s after the second record (uniform;
        // fb-onboard: after spawn when Rules.FirstHuntFromSpawn),
        // instead of Draw(60, 95, 180). Call once at the run's start, before the second record.
        public void UseRunPlan(RunPlan plan) { if (plan == null) return; Plan = plan; firstHuntAt = -1; if (Rules.UsesDread && Hunts == 0) DrawFirstLead(); }   // wave FF: the first hunt's lead is drawn with the plan
        public RunPlan Plan { get; private set; }
        // Wave FD (r13 critic onboarding: a cold player's first hunt was no-tell at 87 s, in Survey, with 0 evidence). A cold run (RunPlan.Cold:
        // no history, so the Early band) keeps its band, but the first hunt (a) never starts before a document is read (Read: the gate
        // keeper's letter or a ledger page) or Rules.ColdFirstHuntCap seconds have passed, whichever comes first, so a passive player is
        // still hunted, and (b) is always preceded by a told Warning (5-14 s). Long Night (no Warning phase) and Bellwether: off.
        public bool ColdFirstHunt => Plan != null && Plan.Cold && Rules.ColdFirstHuntCap > 0 && Rules.WarningSeconds > 0 && Hunts == 0;
        public bool ColdGateOpen => (Rules.ColdGateNeedsLedger ? FirstGateDocAt : FirstNoteAt) >= 0 || Elapsed >= Rules.ColdFirstHuntCap;
        // Wave FF: Elapsed at the first ledger page read or evidence record (-1 = none yet); with Rules.ColdGateNeedsLedger this, not the
        // arrival letter, opens a cold run's first-hunt gate. HC_HUNT_TELL firstDocAt logs it.
        public float FirstGateDocAt { get; private set; } = -1;
        public string FirstGateDocKind { get; private set; } = "";
        // The time that opened (or will open) the cold gate under the current rules (-1 = none yet).
        public float ColdGateDocAt => Rules.ColdGateNeedsLedger ? FirstGateDocAt : FirstNoteAt;
        bool ColdHold => ColdFirstHunt && !ColdGateOpen;
        // Elapsed at the first document read (-1 = none yet). Diagnostics and the cold gate.
        public float FirstNoteAt { get; private set; } = -1;
        // How the run's first hunt was gated: "off" (not a cold run), "page" (a document was read first), "cap" (ColdFirstHuntCap); "" before it.
        public string FirstHuntColdGate { get; private set; } = "";
        // The tell before the current hunt: "warning", "escalation" or "none" (no-tell, hostile Return), and its length in seconds.
        public string LastTellKind { get; private set; } = "none";
        public float LastTellLead { get; private set; }
        // Wave L3. The rhythm's lull: for `seconds` of non-threat time (the countdown stops in Warning and Hunt) every dice roll and the
        // floor are held unless DiceDread >= Rules.LullDiceDread. The first-hunt guarantee is not held.
        public void HoldDice(float seconds) { if (Rules.UsesDread && !float.IsNaN(seconds) && seconds > 0) DiceHoldRemaining = Math.Max(DiceHoldRemaining, seconds); }
        public float DiceHoldRemaining { get; private set; }
        public bool DiceHeld => DiceHoldRemaining > 0 && DiceDread < Rules.LullDiceDread;
        // The rhythm's lull (ScareDirector: if(rhythm.TakeLullStart())Case.HoldDiceForLull()). Holds only until the run's long quiet
        // stretch has happened.
        // Wave FE (r13 FD natural2: a lull that began 1 s before the long quiet stretch completed held the dice 119 s past it, so the Late run
        // had one hunt in 520 s): after the first hunt a lull hold ends when the long quiet stretch does.
        public void HoldDiceForLull()
        {
            if (LongQuietDone) return;
            float seconds = Rules.LullHoldSeconds;
            if (Hunts > 0 && Rules.FirstHuntAfterHold > 0) seconds = Math.Min(seconds, Math.Max(0, Rules.FirstHuntAfterHold - quietStretch));
            HoldDice(seconds);
        }
        // True once the run has had a threat-free stretch of FirstHuntAfterHold seconds after its first hunt.
        public bool LongQuietDone { get; private set; }
        float quietStretch;
        float apparitionShadow, apparitionAt = float.NegativeInfinity;
        // The Dread the dice read: Dread minus a fresh apparition's bump.
        public float DiceDread => Math.Max(0, Dread - (Rules.ApparitionDiceShadowSeconds > 0 && Elapsed - apparitionAt < Rules.ApparitionDiceShadowSeconds ? apparitionShadow : 0));
        // Hunt sources: whether the last two hunts (most recent first) began from an escalation.
        public bool LastHuntFromEscalation { get; private set; }
        bool previousHuntFromEscalation, escalationPending;
        // The chance TryEscalate would use now (0 = blocked). Wave L policy when EscalationLit/DarkChance are set.
        public float EscalationChanceNow(bool dark)
        {
            if (Terminal || GraceRemaining > 0) return 0;
            if (Rules.EscalationLitChance > 0 || Rules.EscalationDarkChance > 0)
            {
                if (Phase != Phase.Disturbance && Phase != Phase.Recovery) return 0;
                if (escalationDueAt >= 0 || followDueAt >= 0) return 0;                  // wave FE: one is already armed (wave FF: or a follow)
                if (Apparitions <= 1) return 0;                                            // never the run's first apparition
                if (Rules.EscalationNeedsFirstHunt && Hunts == 0) return 0;              // wave L3: the first hunt comes from its band
                if (DiceHeld) return 0;                                                  // wave L3: a lull or the post-first-hunt hold
                if (LastHuntFromEscalation || previousHuntFromEscalation) return 0;       // at most 1 in 3 consecutive hunts
                return dark ? Rules.EscalationDarkChance : Rules.EscalationLitChance;
            }
            return Phase == Phase.Disturbance ? Rules.EventEscalationChance : Phase == Phase.Recovery ? Rules.EscalationRecoveryChance : 0;
        }
        // Why EscalationChanceNow is 0 (for HC_ESCALATE logs), or "ok".
        public string EscalationBlock(bool dark)
        {
            if (Terminal) return "terminal"; if (GraceRemaining > 0) return "grace";
            if (Rules.EscalationLitChance > 0 || Rules.EscalationDarkChance > 0)
            {
                if (Phase != Phase.Disturbance && Phase != Phase.Recovery) return "phase";
                if (escalationDueAt >= 0 || followDueAt >= 0) return "armed";
                if (Apparitions <= 1) return "first_apparition";
                if (Rules.EscalationNeedsFirstHunt && Hunts == 0) return "before_first_hunt";
                if (DiceHeld) return "held";
                if (LastHuntFromEscalation || previousHuntFromEscalation) return "escalation_cap";
            }
            return EscalationChanceNow(dark) > 0 ? "ok" : "rules_off";
        }
        public bool TryEscalate() => TryEscalate(false);
        // Game calls this when an apparition lands within 4 m. With EventEscalationChance (Disturbance) or
        // EscalationRecoveryChance (Recovery) it becomes a hunt after a short tell (EscalationWarningSeconds,
        // no dispatch line). Never during the post-hunt grace.
        public bool TryEscalate(bool dark)
        {
            float chance = EscalationChanceNow(dark);
            if (chance <= 0) return false;
            if (Roll() >= chance) return false;
            // Wave FE: with an apparition gap the escalation is armed, not started: its short tell begins so that the hunt starts
            // ApparitionHuntGap..+EscalationArmJitter s after the apparition (Tick fires it; a threat or Containment first cancels it).
            if (Rules.ApparitionHuntGap > 0)
            {
                float tell = Rules.WarningSeconds <= 0 || Rules.EscalationWarningSeconds <= 0 ? 0 : Rules.EscalationWarningSeconds;
                escalationDueAt = Math.Max(Elapsed, apparitionAt + Rules.ApparitionHuntGap - tell) + EscalationArmJitter * Roll();
                Note("HC_APPARITION_GATE deferred=escalation reason=apparition_recent since=" + F1(Elapsed - apparitionAt) + " tellIn=" + F1(escalationDueAt - Elapsed) + " huntIn=" + F1(escalationDueAt + tell - Elapsed) + " t=" + F1(Elapsed));
                return true;
            }
            FireEscalation();
            return true;
        }
        // Wave FE: an armed escalation's tell starts at this case time (-1 = none armed).
        public float EscalationDueAt => escalationDueAt;
        public const float EscalationArmJitter = 2;
        float escalationDueAt = -1;
        void FireEscalation()
        {
            escalationPending = true;
            ThreatFrom = Phase;
            if (Rules.WarningSeconds <= 0 || Rules.EscalationWarningSeconds <= 0) StartHunt(true);
            else Warn(Rules.EscalationWarningSeconds, false, false, true);
        }
        // Wave FE: fires an armed escalation when its time comes, in the phases an escalation may start from (Disturbance, Recovery);
        // anything else by then (a threat, Containment, the end) cancels it.
        void TickArmedEscalation()
        {
            if (escalationDueAt < 0) return;
            if (Terminal || Phase != Phase.Disturbance && Phase != Phase.Recovery) { Note("HC_APPARITION_GATE escalation=cancelled phase=" + Phase + " t=" + F1(Elapsed)); escalationDueAt = -1; return; }
            if (Elapsed < escalationDueAt) return;
            escalationDueAt = -1;
            Note("HC_APPARITION_GATE escalation=fired since=" + F1(Elapsed - apparitionAt) + " t=" + F1(Elapsed));
            FireEscalation();
        }
        // ---- Wave FE: the apparition gap and the share floor ------------------------------------------------------------------------------
        // A hunt may not start now: a visible apparition was noted less than Rules.ApparitionHuntGap s ago. Logs one
        // HC_APPARITION_GATE deferred=hunt line per apparition and source.
        public bool ApparitionGateClosed => Rules.ApparitionHuntGap > 0 && Elapsed - apparitionAt < Rules.ApparitionHuntGap;
        bool HuntGateHolds(string source)
        {
            if (!ApparitionGateClosed) return false;
            if (gateLoggedAt != apparitionAt || gateLoggedSource != source)
            {
                gateLoggedAt = apparitionAt; gateLoggedSource = source; GateDeferrals++;
                Note("HC_APPARITION_GATE deferred=hunt source=" + source + " reason=apparition_recent since=" + F1(Elapsed - apparitionAt) + " opensIn=" + F1(apparitionAt + Rules.ApparitionHuntGap - Elapsed) + " phase=" + Phase + " t=" + F1(Elapsed));
            }
            return true;
        }
        float gateLoggedAt = float.NaN; string gateLoggedSource = "";
        public int GateDeferrals { get; private set; }
        // Seconds until a threat this rules engine already knows about starts: the first-hunt deadline (after the cold gate), the timed
        // hunt, an armed share-floor pull or an armed escalation. +Infinity when none is known (dice are never known in advance).
        // ScareDirector (castle) holds an apparition that would land within ScareRhythm.CastlePlannedHuntWindow s before it.
        public float PlannedThreatIn
        {
            get
            {
                float best = float.PositiveInfinity;
                if (Terminal || Phase == Phase.Briefing || Phase == Phase.Warning || Phase == Phase.Hunt) return best;
                if (Hunts == 0 && Rules.FirstHuntSeconds > 0 && firstHuntAt >= 0 && (Rules.FirstHuntFromSpawn ? Phase == Phase.Survey || Phase == Phase.Disturbance : Phase == Phase.Disturbance && Evidence.Count >= Rules.FirstHuntEvidence))
                {
                    float d = firstHuntAt - firstHuntClock;
                    if (ColdHold) d = Math.Max(d, Rules.ColdFirstHuntCap - Elapsed);
                    best = Math.Min(best, d);
                }
                if (Phase == Phase.Disturbance && Rules.TimedHuntSeconds > 0 && DiceDread >= RunDreadMin && Evidence.Count >= Rules.TimedHuntEvidence && !DiceHeld)
                    best = Math.Min(best, Rules.TimedHuntSeconds - huntClock);
                if (sharePullAt >= 0) best = Math.Min(best, sharePullAt - Elapsed);
                if (escalationDueAt >= 0) best = Math.Min(best, escalationDueAt - Elapsed);
                if (followDueAt >= 0) best = Math.Min(best, followDueAt - Elapsed);   // wave FF
                return Math.Max(0, best);
            }
        }
        // The share floor (Rules.ShareFloor): see HuntRules. Logs HC_SHARE_FLOOR armed / pulled / cancelled.
        float sharePullAt = -1;
        public int SharePulls { get; private set; }
        public float SharePullAt => sharePullAt;
        bool ShareFloorEligible(out string why)
        {
            why = "ok";
            if (Terminal) { why = "terminal"; return false; }
            if (Hunts == 0) { why = "no_hunt_yet"; return false; }
            if (Rules.ShareFloorMaxPulls > 0 && SharePulls >= Rules.ShareFloorMaxPulls) { why = "pulls_used"; return false; }
            if (Elapsed < Rules.ShareFloorAfter) { why = "early"; return false; }
            if (Phase != Phase.Survey && Phase != Phase.Disturbance && Phase != Phase.Recovery) { why = "phase_" + Phase; return false; }
            if (GraceRemaining > 0) { why = "grace"; return false; }
            if (Rules.FirstHuntAfterHold > 0 && !LongQuietDone) { why = "long_quiet_pending"; return false; }
            if (HuntShare >= Rules.ShareFloor) { why = "share_ok"; return false; }
            return true;
        }
        void TickShareFloor()
        {
            if (Rules.ShareFloor <= 0) return;
            if (!ShareFloorEligible(out string why))
            {
                if (sharePullAt >= 0 && (why == "share_ok" || why == "terminal" || why.StartsWith("phase_", StringComparison.Ordinal)))
                { Note("HC_SHARE_FLOOR cancelled reason=" + why + " share=" + HuntShare.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + " t=" + F1(Elapsed)); sharePullAt = -1; }
                return;
            }
            if (sharePullAt < 0)
            {
                sharePullAt = Elapsed + Lerp(Rules.ShareFloorPullMin, Rules.ShareFloorPullMax, Roll());
                Note("HC_SHARE_FLOOR armed share=" + HuntShare.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + " floor=" + Rules.ShareFloor.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " pullIn=" + F1(sharePullAt - Elapsed) + " phase=" + Phase + " t=" + F1(Elapsed));
                return;
            }
            if (Elapsed < sharePullAt) return;
            if (HuntGateHolds("share_floor")) return;
            SharePulls++; sharePullAt = -1;
            Note("HC_SHARE_FLOOR pulled=" + SharePulls + " share=" + HuntShare.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + " phase=" + Phase + " t=" + F1(Elapsed));
            StartThreat(false);
        }
        // Rules diagnostics for the runtime log (Game: Case.Diagnostic += Debug.Log). Castle only; Bellwether never raises it.
        public event Action<string> Diagnostic;
        void Note(string line) => Diagnostic?.Invoke(line);
        static string F1(float v) => v.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        // Clamped 0-100; ignored when the map has no dread. What it adds is event Dread (it decays);
        // during a hunt the total added is capped at Rules.HuntDreadCap whoever calls it.
        public void AddDread(float amount)
        {
            if (!Rules.UsesDread || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            if (amount > 0 && Rules.HuntDreadCap > 0 && HuntActive)
            {
                amount = Math.Min(amount, Rules.HuntDreadCap - HuntDreadAdded);
                if (amount <= 0) return;
                HuntDreadAdded += amount;
            }
            float before = Dread;
            Dread = Clamp(Dread + amount);
            DreadEvent = Math.Max(0, Math.Min(Dread, DreadEvent + (Dread - before)));
        }
        public bool ChooseSwordPath()
        {
            if(!Rules.UsesDread||Terminal||Phase==Phase.Return)return false;
            Diagnosed=false;if(Phase==Phase.Containment)Set(Evidence.Count>0?Phase.Disturbance:Phase.Survey);
            return true;
        }
        public void SustainSwordFight()
        {
            if(Rules.UsesDread&&Phase==Phase.Hunt)CurrentHuntSeconds=Math.Max(CurrentHuntSeconds,PhaseTime+8);
        }
        public void Seal(float dt, bool holding, bool atAnchor)
        {
            if (dt < 0 || float.IsNaN(dt) || float.IsInfinity(dt)) throw new ArgumentOutOfRangeException(nameof(dt));
            if (Phase != Phase.Containment || !BreakerOn || !holding || !atAnchor)
            { SealProgress = Math.Max(0, SealProgress - dt * .2f); return; }
            SealProgress = Math.Min(1, SealProgress + dt / 3f);
            if (SealProgress >= 1)
            {
                // Hostile Return: the escape is a tell-less hunt until the bridge is crossed.
                if (Rules.HostileReturn) { LastTellKind = "none"; LastTellLead = 0; Hunts++; NoTellHunt = true; ThreatFrom = Phase.Containment; HuntDreadAdded = 0; previousHuntFromEscalation = LastHuntFromEscalation; LastHuntFromEscalation = false; }
                Set(Phase.Return);
                Say(Book.SealedMessage);
            }
        }
        public bool FinishAtVan()
        {
            if (Phase != Phase.Return) return false;
            wonThisTick = Rules.HostileReturn; wonFrame = FrameSource != null ? FrameSource() : -1;
            Set(Phase.Won); return true;
        }
        // Automated playtest driver only (never set by the game): a grab during a hunt is
        // counted instead of ending the run, so a scripted route can finish both endings.
        public bool Invulnerable { get; set; }
        public int BlockedDeaths { get; private set; }
        public void Die()
        {
            // Castle (wave K): a death resolved in the same tick as a win (bridge crossing or the killing blow) wins
            // over it: death first, whatever order the callers ran in. Bellwether never sets wonThisTick.
            if (Phase == Phase.Won && wonThisTick && (FrameSource == null || FrameSource() == wonFrame))
            {
                if (Invulnerable) { BlockedDeaths++; return; }
                wonThisTick = false; HunterSlain = false; DeathOverrodeWin = true; Deaths++; Set(Phase.Dead); return;
            }
            if (!Terminal && HuntActive) { if (Invulnerable) { BlockedDeaths++; return; } Deaths++; Set(Phase.Dead); }
        }
        // A death in the same tick replaced a win (Changed fired Won, then Dead).
        public bool DeathOverrodeWin { get; private set; }
        // Optional injected frame counter (Game: () => Time.frameCount). Set, "the same tick" means the same frame, whatever
        // order the frame's scripts run in (Case.Tick may run between the win and the grab). Null: since the last Tick.
        public Func<int> FrameSource { get; set; }
        bool wonThisTick;
        int wonFrame = -1;
        // Castle second win: the hunter is slain (Forge/Fight). Escape stays FinishAtVan.
        public bool HunterSlain { get; private set; }
        public bool WinByKill() { if (Terminal) return false; HunterSlain = true; wonThisTick = Rules.UsesDread; wonFrame = FrameSource != null ? FrameSource() : -1; Set(Phase.Won); return true; }
        void Set(Phase next)
        {
            if (next == Phase.Hunt || next == Phase.Return) huntClock = 0;
            if (next != Phase.Warning) { FalseWarning = false; EscalatedWarning = false; }
            if (next != Phase.Hunt && next != Phase.Return) NoTellHunt = false;
            if (next == Phase.Disturbance || next == Phase.Recovery || next == Phase.Survey || next == Phase.Containment) diceTimer = float.NaN;
            bool wasThreat = IsThreat(Phase), isThreat = IsThreat(next);
            if (wasThreat && !isThreat && threatSpans.Count > 0) threatSpans[threatSpans.Count - 1][1] = Elapsed;
            if (!wasThreat && isThreat) { quietBeforeThreat = quietShareTime >= Rules.GovernorQuietSeconds; if (firstThreatAt < 0) firstThreatAt = Elapsed; threatSpans.Add(new[] { Elapsed, -1f }); while (threatSpans.Count > 1 && threatSpans[0][1] >= 0 && threatSpans[0][1] < Elapsed - ShareWindow) threatSpans.RemoveAt(0); }
            Phase = next; PhaseTime = 0; Changed?.Invoke(next);
        }
        bool IsThreat(Phase p) => p == Phase.Warning || p == Phase.Hunt || p == Phase.Return && Rules.HostileReturn;
        void Say(string value) => Message?.Invoke(value);
        public string Goal
        {
            get
            {
                if (!Equipped) return Book.GoalNoKit;
                switch (Phase)
                {
                    case Phase.Briefing: return Book.GoalBriefing;
                    case Phase.Warning: return Book.GoalWarning;
                    case Phase.Hunt: return Book.GoalHunt;
                    case Phase.Containment: return BreakerOn ? Book.GoalContainmentOn : Book.GoalContainmentOff;
                    case Phase.Return: return Book.GoalReturn;
                    case Phase.Won: return Book.GoalWon;
                    case Phase.Dead: return Book.GoalDead;
                    default:
                        if(!Rules.UsesDread)
                        {
                            if(!Evidence.Contains(HollowCurrent.Evidence.Induction))return "Explore the dining room with the reader. Aim at the empty place setting to record its signal.";
                            if(!Evidence.Contains(HollowCurrent.Evidence.Handprint))return "Use ultraviolet beside the upstairs bedroom window. Hold the light on the handprint.";
                            if(!Evidence.Contains(HollowCurrent.Evidence.Voice))return "Ask with the recorder beside the basement music box, or interact with the box to listen.";
                            return Book.GoalContainmentOn;
                        }
                        if (Evidence.Count == 3) return ContextCount<3 ? Book.GoalNeedContext : Book.GoalReconstruct;
                        return Book.GoalDefault;
                }
            }
        }
    }

    // Records only observations made by sensors. A future provider may suggest search
    // choices, but must never supply hidden player position or override these rules.
    public sealed class ObservationMemory
    {
        public float X, Y, Z;
        public float Age { get; private set; } = 999;
        public bool Knows => Age < 7;
        public void Observe(float x, float y, float z) { X = x; Y = y; Z = z; Age = 0; }
        public void Tick(float dt) => Age += dt;
        public void Clear() => Age = 999;
    }
}
