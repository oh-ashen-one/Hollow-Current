namespace HollowCurrent
{
    /// <summary>
    /// Tuning for Count Claude, the Haunted Castle hunter (docs/haunted-castle/DESIGN.md, "The Count").
    /// Plain data, no Unity types. Piece 0 had not published HunterProfile when this was written
    /// (no Runtime/HunterProfile.cs on haunted-castle), so these are plain public fields named after the
    /// constants hard-coded in Presence.cs today (sight 12 m/72 deg at :94, agent.speed 3.15 at :35, grab 1.05 at :159).
    /// When HunterProfile lands, copy these values into its CountClaude instance, or make this class return one.
    /// </summary>
    public sealed class CountClaudeProfile
    {
        public static readonly CountClaudeProfile Default=new CountClaudeProfile();

        // Animator "Mode" ints. Same contract as HearthboundActor.Pose; 4 and 5 are new.
        public const int ModePerch=0,ModeGlide=1,ModeSearchGlide=2,ModeDive=3,ModeStagger=4,ModeDeath=5,ModeCount=6;

        public string DisplayName="Count Claude";
        public string ModelPath="Characters/CountClaude";     // Resources path of the prefab (CountClaudeImport.Prepare writes it)
        public float ModelHeight=2.15f;                        // head top, metres; wing hooks reach about 2.18 m

        // Perception: Presence.Sees() compares distance and Vector3.Angle(forward, toPlayer), so SightHalfAngle is a half-angle.
        public float SightRange=14f;                           // Split Grin: 12
        public float SightHalfAngle=80f;                       // Split Grin: 72
        public float EyeHeight=2.05f;                          // Split Grin: 1.75; he looks down from above

        // Movement. Glide is faster than sprint (3.9) on long straights and slower than walk-sprint in tight rooms.
        public float GlideStraightSpeed=4.6f;
        public float GlideTightSpeed=3.0f;
        public float LandedWalkSpeed=2.4f;                     // PROPOSED, not in DESIGN.md: used when he lands to follow through 0.9 m service doors
        public float TightRoomWidth=4.0f;                      // PROPOSED: corridor/room width below which GlideTightSpeed applies
        public float NavRadius=.30f;                           // Split Grin .26; wings are folded when he walks
        public float NavHeight=2.2f;
        public float NavAcceleration=10f, NavAngularSpeed=250f;

        // Grab and fight.
        public float GrabRange=1.05f;
        public int HitsToKill=3;
        public float StaggerRetreatSeconds=20f;                // retreat to a perch for 20 s, return lower and angrier

        // Perches (used by the behaviour piece): seconds spent perched between hunts.
        public float PerchMinSeconds=6f, PerchMaxSeconds=14f;

        /// <summary>Glide speed for a given room width in metres: tight rooms clip his wings on columns.</summary>
        public float GlideSpeedFor(float openWidth)
        {
            float t=openWidth<=TightRoomWidth?0f:openWidth>=TightRoomWidth*2f?1f:(openWidth-TightRoomWidth)/TightRoomWidth;
            return GlideTightSpeed+(GlideStraightSpeed-GlideTightSpeed)*t;
        }
    }
}
