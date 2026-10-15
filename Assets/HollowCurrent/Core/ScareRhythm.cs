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
        public bool AppearanceDue=>CanSurprise&&Age>=nextAppearance;
        // Seconds until AppearanceDue (0 or less = due now). Wave L integrator: the castle director checks at this moment.
        public float DueIn=>Math.Max(quietUntil,nextAppearance)-Age;
        public void Tick(float dt){if(dt<0||float.IsNaN(dt)||float.IsInfinity(dt))throw new ArgumentOutOfRangeException(nameof(dt));Age+=dt;}
        // Castle (r10 rerun) sets Spacing 60 and AppearanceBase/Spread 60/50; the defaults are Bellwether's 90 and 105-165 s.
        public float Spacing=MinimumSpacing,AppearanceBase=105,AppearanceSpread=60;
        public void RecordAppearance(float randomUnit)
        {
            quietUntil=Age+Spacing;
            if(castleRandom!=null){LastInterval=CastleInterval(Unit(castleRandom()),Unit(castleRandom()));nextAppearance=Age+LastInterval;return;}
            nextAppearance=Age+AppearanceBase+AppearanceSpread*Math.Max(0,Math.Min(1,randomUnit));
        }
        // Castle apparition clock (wave L, r12 critic: arrivals every 101-141 s, CV 0.12, and the first one always at ~65 s).
        // Inter-arrival = a mixture: 40 % short (45-70 s uniform), else 70 s + a truncated exponential (mean 140, cut at 260 s).
        // Mean about 110 s, CV about 0.54, never under 45 s or over 260 s. Pure: two uniforms in [0,1).
        public const float CastleMinInterval=45,CastleMaxInterval=260;
        public static float CastleInterval(float u1,float u2)
        {
            u1=Unit(u1);u2=Unit(u2);
            if(u1<.4f)return 45+25*u2;
            double m=140,t=CastleMaxInterval-70;
            return (float)Math.Min(CastleMaxInterval,70-m*Math.Log(1-u2*(1-Math.Exp(-t/m))));
        }
        // ScareDirector (castle): rhythm.UseCastleClock(()=>Random.value) instead of setting Spacing/AppearanceBase/Spread.
        // Spacing becomes 45 s, the first appearance is 0.6 x a draw (27-156 s), and every RecordAppearance draws the next.
        public void UseCastleClock(Func<float> random)
        {
            castleRandom=random??throw new ArgumentNullException(nameof(random));Spacing=CastleMinInterval;
            LastInterval=.6f*CastleInterval(Unit(random()),Unit(random()));nextAppearance=Age+LastInterval;quietUntil=Math.Min(quietUntil,nextAppearance);
        }
        public float LastInterval{get;private set;}
        Func<float> castleRandom;
        static float Unit(float u)=>float.IsNaN(u)?.5f:Math.Max(0,Math.Min(.99999f,u));
        public void Rest(float seconds)
        {
            quietUntil=Math.Max(quietUntil,Age+seconds);
            nextAppearance=Math.Max(nextAppearance,quietUntil);
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
