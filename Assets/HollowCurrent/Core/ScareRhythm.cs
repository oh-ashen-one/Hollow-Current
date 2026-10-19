using System;
namespace HollowCurrent
{
    // One budget for every scripted and ambient apparition. Modal time does not
    // advance this clock; a doorway or evidence event cannot stack extra scares.
    public sealed class ScareRhythm
    {
        public const float MinimumSpacing=90;
        public float Age {get;private set;}
        float quietUntil=35,nextAppearance=65;
        public bool CanSurprise=>Age>=quietUntil;
        // Castle (wave L3): never due while the last Tick reported a block, so a caller that also runs in a blocked phase cannot fire.
        public bool AppearanceDue=>CanSurprise&&Age>=nextAppearance&&!blockedNow;
        // Seconds until AppearanceDue (0 or less = due now). Wave L integrator: the castle director checks at this moment.
        public float DueIn=>Math.Max(quietUntil,nextAppearance)-Age;
        public void Tick(float dt)=>Tick(dt,false);
        // Wave L3: the castle director passes whether an apparition is blocked now (CastleBlocked). A due time that expires inside a
        // block is not fired at the block's end: when the block lifts, a fresh short burst gap is drawn from that moment.
        public void Tick(float dt,bool blocked)
        {
            if(dt<0||float.IsNaN(dt)||float.IsInfinity(dt))throw new ArgumentOutOfRangeException(nameof(dt));
            Age+=dt;
            if(castleRandom==null)return;
            blockedNow=blocked;
            if(blocked){if(Age>=nextAppearance)expiredInBlock=true;return;}
            if(expiredInBlock){expiredInBlock=false;Rescheduled++;LastInterval=QuotaClamp(CastleBurstGap(Unit(castleRandom())));nextAppearance=Age+LastInterval;quietUntil=Math.Min(quietUntil,nextAppearance);}
        }
        // The castle's block (wave L3): no apparition in Warning, Hunt, Recovery or the post-hunt grace, nor while hidden, in a modal,
        // in Briefing, Containment or Return. Pure: the director passes these.
        // Wave FD (r13 critic: 2-3 visible apparitions per 8.5 min): the post-hunt grace is open to apparitions except its last
        // CastleGraceMargin seconds (the grace disarms every dice roll and escalation, so an apparition inside it cannot start a hunt,
        // and the margin equals the dice's 20 s apparition shadow). Recovery stays blocked once its grace has run out (its dice are armed).
        public static bool CastleBlocked(Phase phase,float graceRemaining,bool hidden,bool modal)=>
            modal||hidden||graceRemaining>0&&graceRemaining<CastleGraceMargin||phase==Phase.Recovery&&graceRemaining<=0||
            phase==Phase.Warning||phase==Phase.Hunt||phase==Phase.Briefing||phase==Phase.Containment||phase==Phase.Return;
        public const float CastleGraceMargin=20;
        // Wave FE (r13 FD proofs: natural1's floor apparition at 70 s, 7 s before its planned 77 s first hunt): an apparition is also blocked
        // while a threat the rules already know about (Investigation.PlannedThreatIn: first-hunt deadline, timed hunt, share-floor pull, armed
        // escalation) is due within this many seconds, so it lands after that hunt instead of making the hunt wait out the apparition gap.
        public const float CastlePlannedHuntWindow=11.5f;
        public static bool CastleBlocked(Phase phase,float graceRemaining,bool hidden,bool modal,float plannedThreatIn)=>
            CastleBlocked(phase,graceRemaining,hidden,modal)||plannedThreatIn<=CastlePlannedHuntWindow;
        bool expiredInBlock,blockedNow;
        public int Rescheduled{get;private set;}
        // Castle (r10 rerun) sets Spacing 60 and AppearanceBase/Spread 60/50; the defaults are Bellwether's 90 and 105-165 s.
        public float Spacing=MinimumSpacing,AppearanceBase=105,AppearanceSpread=60;
        public void RecordAppearance(float randomUnit)
        {
            quietUntil=Age+Spacing;
            if(castleRandom!=null)
            {
                expiredInBlock=false;burstLeft--;CastleAppearances++;
                bool lull=burstLeft<=0;
                if(!lull)LastInterval=CastleBurstGap(Unit(castleRandom()));
                else{LastInterval=CastleLull(Unit(castleRandom()));burstLeft=CastleBurstSize(Unit(castleRandom()));lullStartPending=true;}
                LastInterval=QuotaClamp(LastInterval);if(lull)LastLull=LastInterval;
                nextAppearance=Age+LastInterval;return;
            }
            nextAppearance=Age+AppearanceBase+AppearanceSpread*Math.Max(0,Math.Min(1,randomUnit));
        }
        // Castle apparition clock (wave L, r12 critic: arrivals every 101-141 s, CV 0.12, and the first one always at ~65 s).
        // Inter-arrival (wave L2: the build measured CV 0.43 over 12 samples against >= 0.5 with the 0.54 design): two modes,
        // 60 % a short cluster (45-55 s uniform), else a long mode: 130 s + a truncated exponential (mean 400, cut at 300 s).
        // Mean about 114 s, CV about 0.74, never under 45 s or over 300 s. Pure: two uniforms in [0,1).
        public const float CastleMinInterval=45,CastleMaxInterval=300;
        public static float CastleInterval(float u1,float u2)
        {
            u1=Unit(u1);u2=Unit(u2);
            if(u1<.6f)return 45+10*u2;
            double m=400,t=CastleMaxInterval-130;
            return (float)Math.Min(CastleMaxInterval,130-m*Math.Log(1-u2*(1-Math.Exp(-t/m))));
        }
        // Wave L3 castle clock: bursts and lulls. A burst is 2-3 apparitions 40-55 s apart; after its last one comes a lull of 150-280 s
        // (TakeLullStart reports it once, for Investigation.HoldDice). A run starts with a burst (first apparition 40-55 s in) or a
        // lull. ScareDirector (castle): rhythm.UseCastleClock(()=>Random.value, plan.BurstStart); Tick(dt, CastleBlocked(...)).
        public void UseCastleClock(Func<float> random)=>UseCastleClock(random,Unit((random??throw new ArgumentNullException(nameof(random)))())<.5f);
        public void UseCastleClock(Func<float> random,bool startWithBurst)
        {
            castleRandom=random??throw new ArgumentNullException(nameof(random));Spacing=CastleBurstMin;StartedWithBurst=startWithBurst;
            burstLeft=CastleBurstSize(Unit(random()));
            if(startWithBurst)LastInterval=QuotaClamp(CastleBurstGap(Unit(random())));
            else{LastInterval=QuotaClamp(CastleLull(Unit(random())));LastLull=LastInterval;lullStartPending=true;}
            nextAppearance=Age+LastInterval;quietUntil=Math.Min(quietUntil,nextAppearance);
        }
        // Wave FD: bursts are 30-50 s apart (was 40-55); lulls stay 150-280 s.
        public const float CastleBurstMin=30,CastleBurstMax=50,CastleLullMin=150,CastleLullMax=280;
        // Wave FD apparition floor (r13 critic: at least 4 visible apparitions per 8 minutes). The n-th castle apparition is due by
        // n x CastleQuotaSeconds of rhythm age: a drawn gap (burst, lull or a post-block redraw) that would end later is cut to that
        // deadline, or, when the deadline has already passed (a hunt blocked it), to a catch-up gap of CastleCatchUpMin..Max s (one extra
        // draw). It never lengthens a gap, so short bursts and the long lulls that fit stay as drawn (pooled CV stays >= 0.5).
        public const float CastleQuotaSeconds=90,CastleCatchUpMin=15,CastleCatchUpMax=30;
        public int CastleAppearances{get;private set;}
        public int QuotaClamps{get;private set;}
        float QuotaClamp(float gap)
        {
            float due=(CastleAppearances+1)*CastleQuotaSeconds;
            if(Age+gap<=due)return gap;
            float catchUp=CastleCatchUpMin+(CastleCatchUpMax-CastleCatchUpMin)*Unit(castleRandom());
            float cut=Math.Max(due-Age,catchUp);
            if(cut>=gap)return gap;
            QuotaClamps++;return cut;
        }
        public static float CastleBurstGap(float u)=>CastleBurstMin+(CastleBurstMax-CastleBurstMin)*Unit(u);
        public static float CastleLull(float u)=>CastleLullMin+(CastleLullMax-CastleLullMin)*Unit(u);
        public static int CastleBurstSize(float u)=>Unit(u)<.5f?2:3;
        // True once when a lull has just begun (the burst's last apparition was recorded, or the run started with a lull).
        public bool TakeLullStart(){bool v=lullStartPending;lullStartPending=false;return v;}
        public bool StartedWithBurst{get;private set;}
        public float LastLull{get;private set;}
        int burstLeft;bool lullStartPending;
        public float LastInterval{get;private set;}
        Func<float> castleRandom;
        static float Unit(float u)=>float.IsNaN(u)?.5f:Math.Max(0,Math.Min(.99999f,u));
        public void Rest(float seconds)
        {
            quietUntil=Math.Max(quietUntil,Age+seconds);
            nextAppearance=Math.Max(nextAppearance,quietUntil);
        }
    }
    // Wave L3 cross-run anti-repeat (pure; Runtime persists RunHistory.Serialize() in PlayerPrefs "hc.castle.runs").
    public enum FirstHuntBand { Early, Mid, Late }
    public sealed class RunPlan
    {
        public FirstHuntBand Band; public bool BurstStart;
        // Wave FD: a first run (no history). Investigation.ColdFirstHunt gates and tells its first hunt.
        public bool Cold;
        // Seconds after the second record: Early 60-100, Mid 110-150, Late 160-200.
        public float FirstHuntMin => Band == FirstHuntBand.Early ? 60 : Band == FirstHuntBand.Mid ? 110 : 160;
        public float FirstHuntMax => FirstHuntMin + 40;
    }
    public sealed class RunHistory
    {
        public FirstHuntBand? LastBand, BandBefore; public bool? LastBurstStart;
        public void Push(RunPlan plan) { BandBefore = LastBand; LastBand = plan.Band; LastBurstStart = plan.BurstStart; }
        public string Serialize() => B(LastBand) + "," + B(BandBefore) + "," + (LastBurstStart == null ? "-" : LastBurstStart.Value ? "B" : "L");
        public static RunHistory Parse(string text)
        {
            var h = new RunHistory(); if (string.IsNullOrEmpty(text)) return h;
            var parts = text.Split(','); if (parts.Length != 3) return h;
            h.LastBand = P(parts[0]); h.BandBefore = P(parts[1]); h.LastBurstStart = parts[2] == "B" ? true : parts[2] == "L" ? (bool?)false : null;
            return h;
        }
        static string B(FirstHuntBand? b) => b == null ? "-" : b.Value == FirstHuntBand.Early ? "E" : b.Value == FirstHuntBand.Mid ? "M" : "L";
        static FirstHuntBand? P(string s) => s == "E" ? FirstHuntBand.Early : s == "M" ? FirstHuntBand.Mid : s == "L" ? FirstHuntBand.Late : (FirstHuntBand?)null;
        // The next run's plan: Early for a first run (no history), else a first-hunt band used in neither of the last two runs (so any three consecutive runs use all three and
        // their first hunts spread >= 60 s), and the other rhythm start than last run. u1, u2 uniforms for the free choices.
        public RunPlan Next(float u1, float u2)
        {
            var free = new System.Collections.Generic.List<FirstHuntBand>();
            // fb-onboard (owner feedback 2026-10-15, item 12): a first run (no history) always gets the Early band (60-100 s).
            if (LastBand == null && BandBefore == null) free.Add(FirstHuntBand.Early);
            else foreach (FirstHuntBand b in Enum.GetValues(typeof(FirstHuntBand))) if (b != LastBand && b != BandBefore) free.Add(b);
            float a = float.IsNaN(u1) ? .5f : Math.Max(0, Math.Min(.99999f, u1));
            var band = free[Math.Min(free.Count - 1, (int)(a * free.Count))];
            bool burst = LastBurstStart.HasValue ? !LastBurstStart.Value : !(float.IsNaN(u2) || u2 >= .5f);
            return new RunPlan { Band = band, BurstStart = burst, Cold = LastBand == null && BandBefore == null };
        }
    }
    public static class FieldMessages
    {
        // Only actual dialogue and meaningful player-action feedback take screen
        // space. Environmental descriptions remain in the journal log if emitted.
        public static bool ShowCard(string message)
        {
            if(string.IsNullOrEmpty(message))return false;
            return message.StartsWith("Dispatch / ",StringComparison.Ordinal)||message.StartsWith("Radio / ",StringComparison.Ordinal)||
                message.StartsWith("Evidence secured / ",StringComparison.Ordinal)||message.StartsWith("Case reconstruction / ",StringComparison.Ordinal)||
                message.StartsWith("Checkpoint restored / ",StringComparison.Ordinal)||message.StartsWith("The investigation could not continue",StringComparison.Ordinal)||
                message.StartsWith("The switch is dead",StringComparison.Ordinal);
        }
        public static float CardSeconds(string message)=>message.StartsWith("Evidence secured / ",StringComparison.Ordinal)?2.2f:5;
    }
}
