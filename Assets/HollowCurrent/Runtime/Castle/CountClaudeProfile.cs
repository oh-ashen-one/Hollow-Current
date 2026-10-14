namespace HollowCurrent
{
    /// <summary>
    /// Tuning for Count Claude, the Haunted Castle hunter (docs/haunted-castle/DESIGN.md, "The Count").
    /// Inherited HunterProfile fields keep their meaning; the Count-only fields below are read by Presence's
    /// castle branch (perch / glide / dive / grab / stagger / death). Presence never takes that branch for
    /// SplitGrinProfile, so Bellwether is unaffected by anything in this file.
    /// </summary>
    public sealed class CountClaudeProfile : HunterProfile
    {
        public static readonly CountClaudeProfile Default=new CountClaudeProfile();

        // Animator "Mode" ints (HearthboundActor.Pose). 0-5 are the shared contract; 6 Grab is the wing-wrap.
        // 8 GrabDrop (falls from the vault onto the camera) and 9 GrabCrawl (four-limbed lunge from a door) are F5's clips
        // (Count_GrabDrop / Count_GrabCrawl); Presence plays them only when the controller has them, else the Mode 6 wing-wrap.
        public const int ModePerch=0,ModeGlide=1,ModeSearchGlide=2,ModeDive=3,ModeStaggerId=4,ModeDeathId=5,ModeGrabId=6,ModeCrawlId=7,ModeGrabDropId=8,ModeGrabCrawlId=9,ModeCount=10;
        public int ModeGrab=ModeGrabId;
        // Set either to -1 to never use that variant.
        public int ModeGrabDrop=ModeGrabDropId, ModeGrabCrawl=ModeGrabCrawlId; // GrabCrawl re-enabled after hc/grabs fef50da (skinning fix)
        // GrabDrop when he was at least this far above the player (height decays at GrabDropHeightDecay m/s, so it means
        // "he came down on you", not "he once flew past high up"). Reaches from F5's receipt (grab_drop_reach_m /
        // grab_crawl_reach_m); until it lands they equal the wing-wrap reach so the camera stand-off rule holds.
        public float GrabDropMinHeight=2f, GrabDropHeightDecay=1f, GrabDropReach=1.758f, GrabCrawlReach=1.729f; // receipt grab_drop_reach_m / grab_crawl_reach_m (hc/grabs)
        // Mode 7 Crawl, a stooped four-limbed knuckle-walk (top 1.75 m) under 2.0 m lintels. Used on foot at service
        // doors and wherever the clear height over the floor is lower than CrawlCeiling (Glide clip top 2.33 m + margin).
        public int ModeCrawl=ModeCrawlId;
        public float CrawlCeiling=2.45f;
        // CastleArt.Gore on the demon: submesh 1 wing membrane = Membrane, 2 flayed flesh = Flesh; body/coat plain unless GoreSkinOnBody.
        public bool GoreMaterials=true, GoreSkinOnBody=false; public float GoreSkinWet=.55f, GoreMembraneWet=.2f, GoreFleshWet=.6f;   // r8: Skin gore off (red cast)

        public float ModelHeight=2.5f;                         // head top, metres (tools/count_prepare.py HEAD_TOP): the nine-foot demon

        // Movement. Glide is faster than a sprint (3.9) on straights and slower in tight rooms (wings clip columns).
        public float GlideStraightSpeed=4.6f;
        public float GlideTightSpeed=3.0f;
        public float LandedWalkSpeed=2.4f;                     // he lands and crawls (Mode 7) through 0.9 m service doors
        public float TightRoomWidth=4.0f;                      // open width (left + right) below which GlideTightSpeed applies
        public float StraightLookahead=3.0f;                   // a leg shorter than this before a turn is not a straight
        public float TurnDegreesPerSecond=420f;
        public float ServiceDoorLandRadius=1.4f;               // within this of a service door he is on foot, hover 0

        // Flight height: clearance under his lowest point (the clips are exported with the lowest point 0.10 m above
        // the origin, so hover 0 skims the floor). Capped by the ceiling above the route.
        public float HoverMax=1.1f;
        public float BodyClearance=2.45f;                      // ceiling - this = highest hover (Glide clip top is 2.33 m)
        public float HoverNearPlayer=.15f;                     // last leg of a chase: drop to grab height
        public float HoverTierScale=.25f;                      // each completed retreat: comes back lower (1 - tier * this)
        public float FlightClearRadius=.28f;                   // sphere for "can he fly straight there"

        // Perches (CastleWorld.Points.PerchPoints): idle and search stations.
        public float PerchMinSeconds=6f, PerchMaxSeconds=14f;  // between hunts (scripted appearances)
        public float HuntPerchMinSeconds=2.5f, HuntPerchMaxSeconds=5f;
        public float PerchTellSeconds=.9f;                     // watches you this long from a perch before the dive
        // Round 10 (playtest: no attack in two runs): the hunt starts with 1-2 s on the perch, then he glides to where the bell
        // told him you are (a point HeardOffset m from you, never the exact cupboard). Until he has dived, he hears you again
        // every AttackGuaranteeSeconds, so every hunt has at least one attack attempt.
        public float HuntStartPerchMinSeconds=1f, HuntStartPerchMaxSeconds=2f;
        public float HeardOffsetMin=1.5f, HeardOffsetMax=3f, HeardOffsetHiddenMin=3f, HeardOffsetHiddenMax=5f;
        public float AttackGuaranteeSeconds=10f;
        public float PerchEyeHeight=1.3f, FlightEyeHeight=1.85f, PerchLookDown=25f;
        public float LedgeProbe=2.2f;                          // balustrade search distance in front of a perch point
        public float SearchLandSeconds=2.6f;                   // lands at a heard/last-seen spot and prowls (Mode 2)

        // Fight.
        public int HitsToKill=3;
        public float StaggerRetreatSeconds=20f;                // retreat to a perch for 20 s, return lower and angrier (Fight.RetreatSeconds)

        // Grab presentation: the Grab clip reaches 1.39 m ahead of his origin (receipt grab_reach_m);
        // keep the camera at least CameraClearance outside it.
        public float GrabReach=1.39f, CameraClearance=.6f;   // r8 forward wing-wrap (receipt grab_reach_m)
        public float GrabStandOff=>GrabReach+CameraClearance+.05f;
        /// <summary>Stand-off for a grab presentation in Animator mode <paramref name="mode"/>: its reach + CameraClearance.</summary>
        // Modes 8/9 are grounded clips (hover 0): exactly their reach + CameraClearance (2.36 / 2.33 m).
        public float StandOffFor(int mode)=>mode==ModeGrabDropId?GrabDropReach+CameraClearance:mode==ModeGrabCrawlId?GrabCrawlReach+CameraClearance:GrabStandOff;

        // Critic capture only (CaptureMode.Requested): he dives to this distance and holds instead of grabbing,
        // then feints back to a perch, so the hunt sequence shows perch, glide and dive without a kill.
        public float CaptureHoldDistance=2.8f, CaptureHoldSeconds=1.6f;
        // Choreography climax (round 4): origin height at the hold (Grab clip lowest point +0.10 => 1.30 m, above the
        // 1.1 m ground-fog planes) and how long the feint keeps facing the camera before it turns.
        public float CaptureHoldHeight=1.25f, CaptureFeintFacingSeconds=1.3f;
        // Round 5: glide end height (approach stays above the fog), climax lunge distance, and the whole-figure lean back
        // (degrees, negative = back) while gliding/diving toward the camera so it sees chest and face, not soles.
        public float CaptureGlideEndHeight=2.2f, CaptureLungeDistance=2.5f, CaptureGlideLean=-34f, CaptureDiveLean=-16f;

        // Cold rim light parented behind his shoulders (Presence.CountRimLight): keeps the wings readable in the dark.
        // Wave F item 16: off during hunts (candles, moon and torch only), half between hunts for the perch sightings.
        // Staged captures and the capture choreography keep the full value.
        public float RimLightRange=3.6f, RimLightIntensity=1.1f, RimLightHuntScale=0f, RimLightBetweenScale=.5f;

        // ---- Wave F (docs/haunted-castle/FEAR_PLAN.md, piece F2) ---------------------------------------------------
        // Item 9, perch sightings between hunts: he is active and visible on a perch at least PerchMinPlayerDistance from the
        // investigator, never on her floor within PerchSameFloorExclusion, and outside her view cone (more than SightAngle
        // off the camera forward, or occluded) when he lands there. He changes perch only after the camera has not seen
        // him for UnobservedSeconds (PerchMin/MaxSeconds apart). Off in critic captures (CaptureMode).
        public bool PerchBetweenHunts=true;
        public float PerchMinPlayerDistance=8f, PerchSameFloorExclusion=6f, UnobservedSeconds=.5f;
        // Item 14: a perch watching the floor where she spent most time gets up to this much extra weight (base weight 1).
        public float PerchFloorWeight=2.5f;
        // Item 10, cupboard near-miss: once per hunt, when she is hidden (torch off) and he did not see her hide, he lands,
        // crawls (Mode 7) to NearMissDoorDistance from the cupboard door, holds, and leaves. Rolled once she has been
        // hidden NearMissAfterSeconds into the hunt.
        public float NearMissChance=.33f, NearMissAfterSeconds=4f, NearMissDoorDistance=.5f, NearMissCrawlDistance=2.2f, NearMissCrawlSpeed=1.1f, NearMissHoldSeconds=3f;
        // Item 11, gatehouse violation (once per run, after the first survived hunt).
        public float GatehouseSafeSeconds=40f, GatehouseHoldSeconds=4f, GatehouseFallbackHeight=3.5f, GatehouseArriveSeconds=1.4f, GatehouseChainsVolume=.4f, GatehouseHeadroom=1.75f;
        // The battlement (F3: gate wall top) is seen from the courtyard, not from inside: he holds past GatehouseHoldSeconds
        // while she stays in the gatehouse, until she has looked at him GatehouseSeenSeconds or GatehouseMaxHoldSeconds pass.
        public float GatehouseSeenSeconds=1.2f, GatehouseMaxHoldSeconds=14f;
        // A dive that came within this of her and ended without a kill counts as a grab survived (Dread).
        public float GrabSurvivedDistance=1.6f;
        // Item 13, hunt voices: each line at most once per VoiceCooldown, never two lines closer than VoiceGap.
        public float VoiceCooldown=8f, VoiceGap=2f, FoundVoiceDistance=6f;
        // Item 4, hostile return (F1 HuntActive in Phase Return): short perch, no perch tell, straight-glide speed everywhere
        // he can fly, and he hears her again every ReturnHearSeconds.
        public float ReturnPerchSeconds=.4f, ReturnHearSeconds=3f;

        public CountClaudeProfile()
        {
            Name="Count Claude";
            ModelResource="Characters/CountClaude";            // Resources path of the prefab (CountClaudeImport.Prepare writes it)
            AllowPlaceholderModel=true;                        // magenta stand-in if the prefab is missing
            MinimumBones=24;
            // Perception: half-angle cone, line of sight required (Presence.CountSees). Split Grin: 12 / 72.
            SightRange=14f;SightAngle=80f;EyeHeight=FlightEyeHeight;
            // The nav agent is never enabled for the Count (he flies kinematically along NavMesh corners); these keep
            // the agent component sane if anything inspects it.
            Speed=GlideTightSpeed;AgentRadius=.30f;AgentHeight=2.6f;Acceleration=10f;AngularSpeed=250f;
            GrabDistance=1.05f;
            Search=SearchRoute.PerchPoints;
            HuntSpawn=null;                                    // CastleWorld.Points.HunterSpawn; the hunt starts on the nearest perch
            ModeIdle=ModePerch;ModeMove=ModeGlide;ModeSearch=ModeSearchGlide;ModeReach=ModeDive;ModeStagger=ModeStaggerId;ModeDeath=ModeDeathId;
        }

        /// <summary>Glide speed for an open width in metres: tight rooms clip his wings on columns.</summary>
        public float GlideSpeedFor(float openWidth,bool straight=true)=>openWidth<TightRoomWidth||!straight?GlideTightSpeed:GlideStraightSpeed;

        public override void OnStagger(Presence presence)=>presence.CountStagger();
        public override void OnDeath(Presence presence)=>presence.CountDefeat();
    }
}
