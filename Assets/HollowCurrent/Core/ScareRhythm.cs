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
        public void Tick(float dt){if(dt<0||float.IsNaN(dt)||float.IsInfinity(dt))throw new ArgumentOutOfRangeException(nameof(dt));Age+=dt;}
        public void RecordAppearance(float randomUnit)
        {
            quietUntil=Age+MinimumSpacing;
            nextAppearance=Age+105+60*Math.Max(0,Math.Min(1,randomUnit));
        }
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
