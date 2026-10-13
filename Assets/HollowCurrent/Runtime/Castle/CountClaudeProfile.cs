namespace HollowCurrent
{
    /// <summary>
    /// Tuning for Count Claude, the Haunted Castle hunter (docs/haunted-castle/DESIGN.md, "The Count").
    /// Piece 4's values, reshaped onto piece 0's HunterProfile: inherited fields Presence reads today,
    /// plus the Count-only fields the behaviour pass (perch/glide/dive, fight) will read.
    /// </summary>
    public sealed class CountClaudeProfile : HunterProfile
    {
        public static readonly CountClaudeProfile Default=new CountClaudeProfile();

        // Animator "Mode" ints. Same contract as HearthboundActor.Pose; 4 and 5 are new.
        public const int ModePerch=0,ModeGlide=1,ModeSearchGlide=2,ModeDive=3,ModeStaggerId=4,ModeDeathId=5,ModeCount=6;

        public float ModelHeight=2.15f;                        // head top, metres; wing hooks reach about 2.18 m

        // Movement. Glide is faster than sprint (3.9) on long straights and slower than walk-sprint in tight rooms.
        public float GlideStraightSpeed=4.6f;
        public float GlideTightSpeed=3.0f;
        public float LandedWalkSpeed=2.4f;                     // PROPOSED, not in DESIGN.md: used when he lands to follow through 0.9 m service doors
        public float TightRoomWidth=4.0f;                      // PROPOSED: corridor/room width below which GlideTightSpeed applies

        // Fight.
        public int HitsToKill=3;
        public float StaggerRetreatSeconds=20f;                // retreat to a perch for 20 s, return lower and angrier

        // Perches (used by the behaviour piece): seconds spent perched between hunts.
        public float PerchMinSeconds=6f, PerchMaxSeconds=14f;

        public CountClaudeProfile()
        {
            Name="Count Claude";
            ModelResource="Characters/CountClaude";            // Resources path of the prefab (CountClaudeImport.Prepare writes it)
            AllowPlaceholderModel=true;                        // magenta stand-in if the prefab is missing
            // Perception: Presence.Sees() compares distance and Vector3.Angle(forward, toPlayer): SightAngle is a half-angle.
            SightRange=14f;SightAngle=80f;EyeHeight=2.05f;     // Split Grin: 12 / 72 / 1.75; he looks down from above
            // Navigation agent: until the glide behaviour lands, the nav agent runs at the tight-room glide speed.
            Speed=GlideTightSpeed;AgentRadius=.30f;AgentHeight=2.2f;Acceleration=10f;AngularSpeed=250f;
            GrabDistance=1.05f;
            HuntSpawn=null;                                    // CastleWorld.Points.HunterSpawn
            ModeIdle=ModePerch;ModeMove=ModeGlide;ModeSearch=ModeSearchGlide;ModeReach=ModeDive;ModeStagger=ModeStaggerId;ModeDeath=ModeDeathId;
        }

        /// <summary>Glide speed for a given room width in metres: tight rooms clip his wings on columns.</summary>
        public float GlideSpeedFor(float openWidth)
        {
            float t=openWidth<=TightRoomWidth?0f:openWidth>=TightRoomWidth*2f?1f:(openWidth-TightRoomWidth)/TightRoomWidth;
            return GlideTightSpeed+(GlideStraightSpeed-GlideTightSpeed)*t;
        }
    }
}
