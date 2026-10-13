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
    public sealed class Investigation
    {
        // Per-map names and text (CaseBook.Bellwether unless a map says otherwise).
        public CaseBook Book { get; }
        public Investigation() : this(CaseBook.Bellwether) { }
        public Investigation(CaseBook book) { Book = book ?? CaseBook.Bellwether; }
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
        public int ContextCount => (Notes.Contains(Book.NoteTitles[0])?1:0)+(Notes.Contains(Book.NoteTitles[1])?1:0)+(Notes.Contains(Book.NoteTitles[2])?1:0);
        public bool MysteryReady => Evidence.Count==3&&ContextCount==3&&Survived&&!Diagnosed&&!Terminal&&Phase!=Phase.Warning&&Phase!=Phase.Hunt;
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
        }
        public Checkpoint SaveCheckpoint()=>new Checkpoint {Evidence=new List<Evidence>(Evidence).ToArray(),Notes=new List<string>(Notes).ToArray(),Equipped=Equipped,BreakerOn=BreakerOn,Diagnosed=Diagnosed,Survived=Survived,Elapsed=Elapsed,Hunts=Hunts,WrongDiagnoses=WrongDiagnoses};
        public void RestoreCheckpoint(Checkpoint saved)
        {
            if(saved==null)return;
            Evidence.Clear();Evidence.UnionWith(saved.Evidence);Notes.Clear();Notes.UnionWith(saved.Notes);
            Equipped=saved.Equipped;BreakerOn=saved.BreakerOn;Diagnosed=saved.Diagnosed;Survived=saved.Survived;Elapsed=saved.Elapsed;Hunts=saved.Hunts;WrongDiagnoses=saved.WrongDiagnoses;SealProgress=0;
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
            if (!Equipped || Terminal || Phase == Phase.Hunt || Phase == Phase.Warning || Phase == Phase.Briefing) return false;
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
        public void Tick(float dt, bool inside)
        {
            if (dt < 0 || float.IsNaN(dt) || float.IsInfinity(dt)) throw new ArgumentOutOfRangeException(nameof(dt));
            if (Terminal || Phase == Phase.Briefing) return;
            Elapsed += dt;
            // Leaving the house does not freeze threat progression.
            PhaseTime += dt;
            if (Phase == Phase.Disturbance && PhaseTime >= 95 && Evidence.Count >= 2) BeginWarning();
            else if (Phase == Phase.Warning && PhaseTime >= 9) { Hunts++; Set(Phase.Hunt); }
            else if (Phase == Phase.Hunt && PhaseTime >= 38)
            {
                Survived = true;
                Set(Phase.Recovery);
                Say(Book.RecoveryMessage);
            }
            else if (Phase == Phase.Recovery && PhaseTime >= 32)
            {
                Set(Diagnosed ? Phase.Containment : Phase.Disturbance);
            }
        }
        public void BeginWarning()
        {
            if (Terminal || Phase == Phase.Return || Phase == Phase.Containment) return;
            BreakerOn = false;
            Set(Phase.Warning);
            Say(Book.WarningMessage);
        }
        public void Seal(float dt, bool holding, bool atAnchor)
        {
            if (dt < 0 || float.IsNaN(dt) || float.IsInfinity(dt)) throw new ArgumentOutOfRangeException(nameof(dt));
            if (Phase != Phase.Containment || !BreakerOn || !holding || !atAnchor)
            { SealProgress = Math.Max(0, SealProgress - dt * .2f); return; }
            SealProgress = Math.Min(1, SealProgress + dt / 12f);
            if (SealProgress >= 1)
            {
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
        public void Die() { if (!Terminal && Phase == Phase.Hunt) { if (Invulnerable) { BlockedDeaths++; return; } Set(Phase.Dead); } }
        // Castle second win: the hunter is slain (Forge/Fight). Escape stays FinishAtVan.
        public bool HunterSlain { get; private set; }
        public bool WinByKill() { if (Terminal) return false; HunterSlain = true; Set(Phase.Won); return true; }
        void Set(Phase next) { Phase = next; PhaseTime = 0; Changed?.Invoke(next); }
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
