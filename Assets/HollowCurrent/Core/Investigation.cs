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
        public bool DoorHeld => Rules.DoorHeldThreshold > 0 && Dread >= Rules.DoorHeldThreshold;
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
        public bool MysteryReady => Evidence.Count==3&&ContextCount==3&&Survived&&!Diagnosed&&!Terminal&&(Phase!=Phase.Warning||FalseWarning)&&Phase!=Phase.Hunt;
        public bool Terminal => Phase == Phase.Dead || Phase == Phase.Won;
        public event Action<string> Message;
        public event Action<Phase> Changed;

        public sealed class Checkpoint
        {
            public Evidence[] Evidence;
            public string[] Notes;
            public bool Equipped,BreakerOn,Diagnosed,Survived;
            public float Elapsed;
            public int Hunts,WrongDiagnoses;
            public float Dread;
            public int Deaths;
            public float RunDreadMin = -1;
        }
        public Checkpoint SaveCheckpoint()=>new Checkpoint {Evidence=new List<Evidence>(Evidence).ToArray(),Notes=new List<string>(Notes).ToArray(),Equipped=Equipped,BreakerOn=BreakerOn,Diagnosed=Diagnosed,Survived=Survived,Elapsed=Elapsed,Hunts=Hunts,WrongDiagnoses=WrongDiagnoses,Dread=Dread,Deaths=Deaths,RunDreadMin=RunDreadMin};
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
            if(Rules.UsesDread&&saved.RunDreadMin>=0)RunDreadMin=saved.RunDreadMin;
            feltBase=Dread;feltEpisodeKind=0;if(Rules.UsesDread)ScheduleFeltEpisode(Elapsed);firstHuntClock=0;firstHuntAt=-1;CurrentHuntSeconds=Rules.HuntSeconds;CurrentRecoverySeconds=Rules.RecoverySeconds;
            FalseWarning=false;NoTellHunt=false;huntClock=0;
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
            if (!Equipped || Terminal || Phase == Phase.Hunt || Phase == Phase.Warning && !FalseWarning || Phase == Phase.Briefing) return false;
            if (!Evidence.Add(value)) return false;
            Say("Evidence secured / " + Book.EvidenceName(value) + ". Journal updated.");
            if (Evidence.Count == 1 && Phase == Phase.Survey) Set(Phase.Disturbance);
            return true;
        }
        public bool Read(string note)
        {
            if (Terminal || !Notes.Add(note)) return false;
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
            Diagnosed=true;Set(Phase.Containment);
            Say(Book.Diagnosed);
            return true;
        }
        public void RestorePower()
        {
            if (Phase == Phase.Hunt || Phase == Phase.Warning || Terminal) return;
            BreakerOn = true;
            Say(Book.PowerRestored);
        }
        // Keeps the original signature: inside with a lit candle nearby, never in the safe zone.
        public void Tick(float dt, bool inside) => Tick(dt, inside, true, false);
        public void Tick(float dt, bool inside, bool litNearby, bool inSafe)
        {
            if (dt < 0 || float.IsNaN(dt) || float.IsInfinity(dt)) throw new ArgumentOutOfRangeException(nameof(dt));
            if (Terminal || Phase == Phase.Briefing) return;
            Elapsed += dt;
            // Leaving the house does not freeze threat progression.
            PhaseTime += dt;
            if (Rules.UsesDread)
            {
                if (inSafe) Dread = Clamp(Dread - Rules.DreadSafeRecovery * dt);
                else if (inside) Dread = Clamp(Dread + (litNearby ? Rules.DreadInsideRate : Rules.DreadDarkRate) * dt);
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
                Set(Phase.Recovery);
                if (Rules.AnnounceRecovery) Say(Book.RecoveryMessage);
            }
            else if (Phase == Phase.Recovery)
            {
                if (PhaseTime >= CurrentRecoverySeconds) Set(Diagnosed ? Phase.Containment : Phase.Disturbance);
                else TickRecoveryDice(dt);
            }
            else if (Phase == Phase.Survey || Phase == Phase.Containment) TickQuietDice(dt);
        }
        // Survey and Containment after the first hunt: a rare flat roll, so no phase is a refuge.
        void TickQuietDice(float dt)
        {
            if (Rules.QuietPhaseChance <= 0 || Rules.DiceIntervalMax <= 0 || Hunts == 0) return;
            if (float.IsNaN(diceTimer)) { diceTimer = NextDiceInterval(); return; }
            diceTimer -= dt;
            if (diceTimer > 0) return;
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
            float chance = Dread >= Rules.RecoveryDiceDread ? Rules.RecoveryDiceChance : Rules.RecoveryLowChance;
            if (chance <= 0 || Rules.DiceIntervalMax <= 0) return;
            if (float.IsNaN(diceTimer)) { diceTimer = NextDiceInterval(); return; }
            diceTimer -= dt;
            if (diceTimer > 0) return;
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
            bool dreadful = Dread >= RunDreadMin;
            if (dreadful) huntClock += dt;
            // Crossing the boundary restarts the roll schedule for the new kind of roll.
            if (dreadful != diceHigh) { diceHigh = dreadful; diceTimer = float.NaN; }
            // Timed hunt: Bellwether's 95 s timer, the castle's 240 s floor.
            if (Rules.TimedHuntSeconds > 0 && dreadful && huntClock >= Rules.TimedHuntSeconds && Evidence.Count >= Rules.TimedHuntEvidence) { StartThreat(false); return; }
            // First-hunt guarantee so the mystery cannot stall at low Dread. Its clock starts at the
            // record that reaches FirstHuntEvidence, so a slow player gets the full delay too.
            if (Rules.FirstHuntSeconds > 0 && Hunts == 0 && Evidence.Count >= Rules.FirstHuntEvidence)
            {
                firstHuntClock += dt;
                if (firstHuntAt < 0) firstHuntAt = Rules.FirstHuntSeconds * (1 - Rules.FirstHuntJitter + 2 * Rules.FirstHuntJitter * Roll());
                if (firstHuntClock >= firstHuntAt) { StartThreat(false); return; }
            }
            // Dice-roll hunts: banded at or above the run's boundary, a flat low roll below it after the first hunt.
            bool low = !dreadful && Hunts > 0 && Rules.LowDreadChance > 0;
            if (Rules.DiceIntervalMax > 0 && (dreadful || low))
            {
                if (float.IsNaN(diceTimer)) { diceTimer = NextDiceInterval(); return; }
                diceTimer -= dt;
                if (diceTimer > 0) return;
                float chance = dreadful ? Rules.DiceChance(Dread, Evidence.Count, RunDreadMin) : Rules.LowDreadChance;
                if (Roll() < chance) { StartThreat(true); return; }
                diceTimer = NextDiceInterval();
            }
        }
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
            // No-tell starts leave BreakerOn alone: the brazier and room tone must not announce them.
            if (Rules.WarningSeconds <= 0 || Rules.NoTellChance > 0 && Roll() < Rules.NoTellChance) { StartHunt(true); return; }
            bool falseAlarm = fromDice && Rules.FalseWarningChance > 0 && Roll() < Rules.FalseWarningChance;
            Warn(DrawWarningSeconds(), falseAlarm, true);
        }
        void Warn(float seconds, bool falseAlarm, bool say)
        {
            BreakerOn = false;
            WarningLength = seconds;
            FalseWarning = falseAlarm;
            NoTellHunt = false;
            Set(Phase.Warning);
            if (say) Say(Book.WarningMessage);
        }
        void StartHunt(bool noTell)
        {
            Hunts++;
            FalseWarning = false;
            NoTellHunt = noTell;
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
            if (Rules.UsesDread && !float.IsNaN(target) && !float.IsInfinity(target)) Dread = Clamp(target);
            diceTimer = float.NaN;
            diceHigh = Dread >= RunDreadMin;
        }
        // Game calls this when an apparition lands within 4 m. With EventEscalationChance it
        // becomes a hunt after a short tell (EscalationWarningSeconds, no dispatch line).
        public bool TryEscalate()
        {
            if (Rules.EventEscalationChance <= 0 || Terminal || Phase != Phase.Disturbance) return false;
            if (Roll() >= Rules.EventEscalationChance) return false;
            ThreatFrom = Phase;
            if (Rules.WarningSeconds <= 0 || Rules.EscalationWarningSeconds <= 0) StartHunt(true);
            else Warn(Rules.EscalationWarningSeconds, false, false);
            return true;
        }
        // Clamped 0-100; ignored when the map has no dread.
        public void AddDread(float amount)
        {
            if (!Rules.UsesDread || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            Dread = Clamp(Dread + amount);
        }
        public void Seal(float dt, bool holding, bool atAnchor)
        {
            if (dt < 0 || float.IsNaN(dt) || float.IsInfinity(dt)) throw new ArgumentOutOfRangeException(nameof(dt));
            if (Phase != Phase.Containment || !BreakerOn || !holding || !atAnchor)
            { SealProgress = Math.Max(0, SealProgress - dt * .2f); return; }
            SealProgress = Math.Min(1, SealProgress + dt / 12f);
            if (SealProgress >= 1)
            {
                // Hostile Return: the escape is a tell-less hunt until the bridge is crossed.
                if (Rules.HostileReturn) { Hunts++; NoTellHunt = true; ThreatFrom = Phase.Containment; }
                Set(Phase.Return);
                Say(Book.SealedMessage);
            }
        }
        public bool FinishAtVan()
        {
            if (Phase != Phase.Return) return false;
            Set(Phase.Won); return true;
        }
        // Automated playtest driver only (never set by the game): a grab during a hunt is
        // counted instead of ending the run, so a scripted route can finish both endings.
        public bool Invulnerable { get; set; }
        public int BlockedDeaths { get; private set; }
        public void Die() { if (!Terminal && HuntActive) { if (Invulnerable) { BlockedDeaths++; return; } Deaths++; Set(Phase.Dead); } }
        // Castle second win: the hunter is slain (Forge/Fight). Escape stays FinishAtVan.
        public bool HunterSlain { get; private set; }
        public bool WinByKill() { if (Terminal) return false; HunterSlain = true; Set(Phase.Won); return true; }
        void Set(Phase next)
        {
            if (next == Phase.Hunt || next == Phase.Return) huntClock = 0;
            if (next != Phase.Warning) FalseWarning = false;
            if (next != Phase.Hunt && next != Phase.Return) NoTellHunt = false;
            if (next == Phase.Disturbance || next == Phase.Recovery || next == Phase.Survey || next == Phase.Containment) diceTimer = float.NaN;
            Phase = next; PhaseTime = 0; Changed?.Invoke(next);
        }
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
                        if (Evidence.Count == 3) return !Survived ? Book.GoalReacting : ContextCount<3 ? Book.GoalNeedContext : Book.GoalReconstruct;
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
