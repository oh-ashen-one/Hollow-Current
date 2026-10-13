using UnityEngine;

namespace HollowCurrent
{
    // STUB: replaced by hc/count (piece 4 owns this file). Keep the class name,
    // base type and parameterless constructor; HunterProfile.For(MapId.Castle)
    // constructs it. Values below are DESIGN.md's deltas from Split Grin.
    public sealed class CountClaudeProfile : HunterProfile
    {
        public CountClaudeProfile()
        {
            Name="Count Claude";
            ModelResource="Characters/CountClaude/CountClaude";
            AllowPlaceholderModel=true;
            SightRange=14;SightAngle=80;
            Speed=3.6f;
            HuntSpawn=null; // CastleWorld.Points.HunterSpawn
        }
    }
}
