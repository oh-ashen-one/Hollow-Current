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
        public const int ModePerch=0,ModeGlide=1,ModeSearchGlide=2,ModeDive=3,ModeStaggerId=4,ModeDeathId=5,ModeGrabId=6,ModeCrawlId=7,ModeGrabDropId=8,ModeGrabCrawlId=9,
            ModeGrabSideLId=10,ModeGrabSideRId=11,ModeGrabDropSpinId=12,ModeCount=13;   // F5 hc/grabs-l: side snatches and the ceiling drop behind her
        public int ModeGrab=ModeGrabId;
        // Set either to -1 to never use that variant.
        public int ModeGrabDrop=ModeGrabDropId, ModeGrabCrawl=ModeGrabCrawlId; // GrabCrawl re-enabled after hc/grabs fef50da (skinning fix)
        // Set to -1 to never use. Reaches from CountClaudeModes.json grab_variants (fallbacks below); stand-off = reach + CameraClearance.
        public int ModeGrabSideL=ModeGrabSideLId, ModeGrabSideR=ModeGrabSideRId, ModeGrabDropSpin=ModeGrabDropSpinId;
        public float GrabSideLReach=1.596f, GrabSideRReach=1.607f, GrabDropSpinReach=1.614f, GrabDropSpinClipTop=4.735f;
        // GrabDropSpin camera: her view is held, then spun toward him from SpinStart to SpinEnd (receipt grab_spin_*), at most SpinMaxYaw.
        public float SpinStart=.5f, SpinEnd=1.15f, SpinMaxYaw=120f;
        // Selection (wave L2): a high dive with headroom ends in GrabDropSpin (when she faces away: his bearing from her view between
        // SpinMinBearing and SpinMaxYaw) with probability SpinShare, else GrabDrop, so GrabDrop stays >= 30 % of eligible grabs.
        // A low dive whose bearing from her view is SideMinBearing-SideMaxBearing and whose flank is clear ends in a side snatch with
        // probability SideShare (GrabSideL from her left, GrabSideR from her right), committed within SideCommitDistance of her.
        // fd-grab: SpinShare, SideShare and DropDiveShare are retired (the dive plans its grab from the variety deck, PlanVariant).
        public float SpinMinBearing=50f, SpinShare=.45f, SideMinBearing=35f, SideMaxBearing=110f, SideShare=.6f, SideCommitDistance=3.8f, SideStartLateral=3.4f, SideSkullHeight=1.55f;
        // GrabDrop when he was at least this far above the player (height decays at GrabDropHeightDecay m/s, so it means
        // "he came down on you", not "he once flew past high up"). Reaches from F5's receipt (grab_drop_reach_m /
        // grab_crawl_reach_m); until it lands they equal the wing-wrap reach so the camera stand-off rule holds.
        public float GrabDropMinHeight=2f, GrabDropHeightDecay=1f, GrabDropReach=1.615f, GrabCrawlReach=1.729f; // fallbacks; ApplyReceipt reads CountClaudeModes.json (hc/grabs2: drop 1.615) // receipt grab_drop_reach_m / grab_crawl_reach_m (hc/grabs)
        // Mode 7 Crawl, a stooped four-limbed knuckle-walk (top 1.75 m) under 2.0 m lintels. Used on foot at service
        // doors and wherever the clear height over the floor is lower than CrawlCeiling (Glide clip top 2.33 m + margin).
        public int ModeCrawl=ModeCrawlId;
        // ---- Wave I (r10 blind critic, item 1-5) -------------------------------------------------------------------------
        // Flight posture: in Glide and Dive the whole figure pitches nose-down into the travel direction (>= FlightPitchMin),
        // pivoting at FlightPitchPivot m above his feet; more when descending, never past FlightPitchMax. Perch, search, crawl,
        // grabs, stagger and death stay upright. In Dive the arms reach along the pitched forward (DiveArmReach 0-1).
        public float FlightPitchMin=38f, FlightPitchMax=68f, FlightPitchPivot=1.2f, FlightPitchRate=6f, FlightPitchMinSpeed=1.2f, DiveArmReach=.8f;
        // Dive: closing speed once Chase begins (>= the 4.6 m/s design dive), with its own acceleration. Service-door crawl
        // keeps LandedWalkSpeed.
        public float DiveSpeed=5.2f, DiveAcceleration=16f;
        // Grab choice (heights are his origin above her feet, max of now and the recent decaying approach height):
        // hide entrance -> GrabDrop; she is crouched or he comes in under GrabCrawlBelow -> GrabCrawl; Mode 6 wing-wrap only
        // within GrabWrapDistance at <= GrabWrapMaxHeight; anything higher -> GrabDrop.
        public float GrabCrawlBelow=1.6f, GrabWrapMaxHeight=2.2f, GrabWrapDistance=1.3f;
        // Wing tips: a fan Ext bone may stretch at most WingExtMaxStretch x its rest offset (the Grab clip asks for 2.2 and
        // crumples the tip, r10 H09/H10; GrabDrop 1.7-2.1). 1.6 keeps Glide 1.45, Dive 1.26 and GrabCrawl 1.6 untouched. 0 disables.
        public float WingExtMaxStretch=1.6f;
        // ---- Wave K (r11 blind playtest critic) --------------------------------------------------------------------------
        // Stalls: a door is waited for at most DoorWaitSeconds; an airborne leg that moves less than StallMinProgress in
        // StallSeconds is blocked; either re-routes at once, else he lands and crawls the NavMesh for OnFootSeconds.
        public float DoorWaitSeconds=1f, StallSeconds=.5f, StallMinProgress=.1f, OnFootSeconds=4f;
        // Grab camera: when a grab commits the view turns to his skull over GrabCamTurnSeconds (no cut) and holds through the
        // death. The aim sits GrabCamClawBias of the way from the skull toward the claws (claws fill the lower frame). In the
        // kill presentation he is eased so the nearer claw is GrabCamClawDistance from the lens, never under CameraClearance.
        // ClawWidth is the world width used for
        // the claw_px proof (2560-wide frame).
        public float GrabCamTurnSeconds=.15f, GrabCamClawBias=.3f, GrabCamClawDistance=.8f, ClawWidth=.3f;
        // count17: the GrabDrop clip falls from its own height (receipt clips.GrabDrop.top_m_max 4.722 m), so a drop needs that
        // much clear ceiling over his floor; otherwise the grab is GrabCrawl from the door.
        public float GrabDropClipTop=4.72f;
        // count18: a door grab keeps his origin at least DoorOutside beyond the door plane (never inside or behind the wardrobe)
        // and within DoorLateral of the door's axis, feet on her floor.
        public float DoorOutside=.05f, DoorLateral=.3f;
        // ---- Wave L (r12 blind playtest critic) --------------------------------------------------------------------------
        // Unwatched hides fail sometimes: the near-miss open chance follows Dread through HuntRules.NearMissOpenChance (F1: 2 % at 0,
        // 5 % at 40, 25 % at 80, 40 % at 100), floors NearMissOpenChanceLow after a survived hunt and NearMissOpenChance in Long Night. The hide he last
        // found her in or near is checked first (no attempt roll, from LearnedHideAfterSeconds) and opens at x LearnedHideOpenMultiplier
        // (capped at LearnedHideOpenCap).
        public float LearnedHideAfterSeconds=2.5f, LearnedHideOpenMultiplier=2f, LearnedHideOpenCap=.8f;
        // Dive styles: where the ceiling over her clears the GrabDrop clip, DropDiveShare of dives come in high (hover up to
        // HighDiveHover) and end in the drop grab; the rest come in low (GrabCrawl).
        public float DropDiveShare=.6f, HighDiveHover=2.6f;
        // The grab commits at the chosen clip's stand-off (its reach + CameraClearance), so the kill presentation never moves him
        // (r12: 0.5 m -> 2.2 m pop). FrameClaws eases at most GrabCamMaxEase m/s.
        public float GrabCamMaxEase=1.2f;
        // count-l3: after the head-matched commit the root eases to her floor over this long (the drop clip lands at 0.5 s).
        public float GrabRootBlendDrop=.5f, GrabRootBlendCrawl=.35f;
        // count-l4: commits happen within RingTolerance of the clip's stand-off ring; the high dive flares upright over the last
        // FlareDistance; a release blocks any new grab for RecommitBlock; a committed grab holds for its clip (ClipSecondsFor).
        // count-l4: at Dread >= BagDread the hide he keeps finding her in opens exactly once in every BagSize visits.
        public float BagDread=50f; public int BagSize=3;
        public float RingTolerance=.25f, FlareDistance=1.2f, RecommitBlock=2f, GrabDropPeakSkullDefault=4.2f;
        public float ClipSecondsFor(int mode)=>mode==ModeGrabDropId?1.6f:mode==ModeGrabCrawlId?1.4f:mode==ModeGrabSideLId||mode==ModeGrabSideRId?1.2f:mode==ModeGrabDropSpinId?1.9f:.9f;
        // ---- fd-grab (r13 blind critic, top change 1: the kill presentation) -----------------------------------------------
        // Presentation clock: while the grab camera runs, the clip, the root easing and the camera advance at most GrabMaxFrameStep per
        // rendered frame (a slow frame, e.g. a screenshot, plays slower instead of jumping), so every frame continues the last.
        // Seat: at the commit the root is offset so his skull starts exactly where the last rendered dive frame left it, and the offset
        // decays over SeatSeconds (longer for a big offset, at most SeatMaxSpeed m/s, never past SeatMaxSeconds).
        // MinSkullDistance: the lens never comes closer than this to his skull (the critic's floor is 0.6 m).
        // FirstFrameMargin: in the commit frame his skull sits within this share of the half field of view (the view turns there).
        // ViewLine*: small props (candelabra, floating books) within ViewLineRadius of the eye-to-skull/claw line are hidden for the grab.
        public float GrabMaxFrameStep=1f/30, SeatSeconds=.35f, SeatMaxSpeed=3f, SeatMaxSeconds=.9f, MinSkullDistance=.65f, FirstFrameMargin=.7f;
        public float ViewLineRadius=.22f, ViewPropMaxSize=2.6f, SideTurnSeconds=.25f;
        // Ring: the stand-off point of a grab is searched in RingStepDegrees steps either side of his bearing (RingSteps each way) for open
        // floor at her level with nothing between it and her (the giant skeleton's collision boxes, pews, columns): the clip's body sweep
        // and her eye line. Within reach on a blocked bearing the commit waits; DiveStallSeconds near her without a commit ends the dive
        // as a readable miss (he pulls up and leaves), never a hover.
        public float RingStepDegrees=25f; public int RingSteps=7; public float DiveStallSeconds=1.5f;
        // Variety (r13: side snatch and drop-spin 0 of 12): each open-floor dive plans its grab from a deck kept across runs
        // (PlayerPrefs hc.castle.grabdeck); the eligible variant furthest below its target share is chosen. Side and spin approaches go
        // to their own start points (her flank / beside-behind her) and fall back to the plain grab if not committed in time.
        public float DropTargetShare=.40f, SpinTargetShare=.15f, SideTargetShare=.25f, PlanJitter=.35f;
        public float SpinPlanBearing=80f, PlanLockDistance=6f, VariantCommitTolerance=.7f, VariantGiveUpSeconds=3.5f;
        // A known hide is approached to the stand-off point of its grab; within this of it he commits.
        public float HideStandTolerance=.5f;
        // After the slats, an opening visit rears back to the grab's stand-off at this speed before the grab commits.
        public float RearBackSpeed=3.5f;
        // Readability in the dive (Hunt only, Chase only): the shoulder rim brightens to DiveRimScale between DiveRimNear and
        // DiveRimFar metres from her (silhouette against the window light); otherwise RimLightScale as everywhere.
        public float DiveRimScale=1.1f, DiveRimNear=2.5f, DiveRimFar=9f;
        // The Count lives on this layer (fd-grab: from his first frame, no switch at the commit) and the grave-ash (ultraviolet)
        // lantern never lights it, so it never recolours him purple, in the grab or in the fight.
        public int GrabLightLayer=29;
        // Sword hits: he recoils SwordRecoilDistance away from her over SwordRecoilSeconds, and until SwordClearSeconds after the
        // stagger no joint (wing markers included) comes within SwordClearRadius (+0.15 m skin) of the camera.
        public float SwordRecoilDistance=1.2f, SwordRecoilSeconds=.25f, SwordClearRadius=.6f, SwordClearSeconds=1f;
        // Near-miss: base chance NearMissChance; NearMissChanceLost when he lost sight of her within NearMissLostSeconds and
        // NearMissLostRadius of the hide she is in.
        public float NearMissChanceLost=.5f, NearMissLostSeconds=10f, NearMissLostRadius=6f;
        // Capture choreography dive: from CaptureGlideEndDistance ahead it closes in CaptureDiveArrive seconds (>= 4.6 m/s) and
        // grabs with GrabDrop at its stand-off (GrabCrawl, then Mode 6 only if the clip is missing).
        public float CaptureGlideEndDistance=7f;
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
        // r10 blind P2: the re-hear interval is drawn from this range each time (no 10 s metronome).
        public float AttackGuaranteeMin=8f, AttackGuaranteeMax=14f;
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
        public float StandOffFor(int mode)=>mode==ModeGrabDropId?GrabDropReach+CameraClearance:mode==ModeGrabCrawlId?GrabCrawlReach+CameraClearance:
            mode==ModeGrabSideLId?GrabSideLReach+CameraClearance:mode==ModeGrabSideRId?GrabSideRReach+CameraClearance:mode==ModeGrabDropSpinId?GrabDropSpinReach+CameraClearance:GrabStandOff;

        // Critic capture only (CaptureMode.Requested): he dives to this distance and holds instead of grabbing,
        // then feints back to a perch, so the hunt sequence shows perch, glide and dive without a kill.
        public float CaptureHoldDistance=2.8f, CaptureHoldSeconds=1.6f;
        // Choreography climax (round 4): origin height at the hold (Grab clip lowest point +0.10 => 1.30 m, above the
        // 1.1 m ground-fog planes) and how long the feint keeps facing the camera before it turns.
        public float CaptureHoldHeight=1.25f, CaptureFeintFacingSeconds=1.3f;
        // Round 5: glide end height (approach stays above the fog), climax lunge distance, and the whole-figure lean back
        // (degrees, negative = back) while gliding/diving toward the camera so it sees chest and face, not soles.
        // Wave I: positive = nose-down into the travel direction (round 5's lean-back made him hang upright, r10 blind critic).
        public float CaptureGlideEndHeight=2.2f, CaptureLungeDistance=2.5f, CaptureGlidePitch=40f, CaptureDivePitch=48f;

        // Cold rim light parented behind his shoulders (Presence.CountRimLight): keeps the wings readable in the dark.
        // Wave G (r10 static #11): ONE value in hunts and between hunts, so the light never tells the phase. Staged captures and
        // the capture choreography keep the full value.
        public float RimLightRange=3.6f, RimLightIntensity=1.1f, RimLightScale=.35f;
        // Wave G #11: when a between-hunt perch move is due, this share of them is a visible flight even while she watches
        // (not a hunt; nothing can kill), so seeing him move no longer means "hunt".
        public float ObservedReperchChance=.2f;
        // Wave G #35: the lantern is a beacon. With the torch on and not hidden, his sight range is SightRange x this.
        public float TorchSightScale=1.5f;
        // Wave G #14: DiveEndsIn below this fires DiveLastSecond once per dive (the integrator's torch failure).
        public float DiveLastSecond=1f;
        // Wave G #16/#28: a hide he saw her enter is opened from any hunt state: approach, pause, rattle, then the grab.
        public float WitnessedPauseSeconds=1.6f;
        // Wave G #17: at Dread >= NearMissOpenDread or in Long Night, this share of near-misses opens the door (crawl grab).
        public float NearMissOpenDread=80f, NearMissOpenChance=.25f;
        // r10b: below NearMissOpenDread (not Long Night), once she has survived a hunt, this share still opens.
        public float NearMissOpenChanceLow=.1f;
        // r10b N8: memory ages out. Heard places after MemoryHeardSeconds, seen ones after MemorySeenSeconds (x Long Night scale).
        public float MemoryHeardSeconds=60f, MemorySeenSeconds=120f, MemoryLongNightScale=1.5f;
        // r10b N3: between hunts this share of unseen perch moves is a real flight (wings audible) instead of a silent move.
        public float AudibleReperchChance=.33f;
        // r10b N3: every Travel leg (search, re-perch, fly-off; hunts and between hunts alike) is a silent glide, a soft flap or
        // a full flap, one third each. A dive always flaps at full volume.
        public float WingSoftVolume=.5f;
        // r10 rerun: a no-tell or hostile-return hunt starts quiet: first leg silent, second at most soft, no one-shot (breath) for
        // this many seconds unless he has seen her. Told hunts keep the one-third roll.
        public float QuietStartSeconds=8f;
        // r10b N3: claw steps follow distance and gait only, never the phase: ClawNearVolume within 2 m, ClawFarVolume at 16 m.
        public float ClawNearVolume=.7f, ClawFarVolume=.12f;
        // Wave G #34: the hostile return opens with a visible swoop over the courtyard (castle-local points, sky above the
        // fountain at (0,0,10), out over the gate wall top at y 7.5) before he hunts the bridge. Nothing can kill during it.
        public UnityEngine.Vector3[] ReturnSwoopLocal={new UnityEngine.Vector3(6f,10f,12f),new UnityEngine.Vector3(0f,4.6f,9.5f),new UnityEngine.Vector3(0f,8.7f,.4f)};

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
        public float GatehouseSafeSeconds=40f, GatehouseHoldSeconds=4f, GatehouseArriveSeconds=1.4f, GatehouseChainsVolume=.4f;
        // The battlement (F3: gate wall top) is seen from the courtyard, not from inside: he holds past GatehouseHoldSeconds
        // while she stays in the gatehouse, until she has looked at him GatehouseSeenSeconds or GatehouseMaxHoldSeconds pass.
        public float GatehouseSeenSeconds=1.2f, GatehouseMaxHoldSeconds=14f;
        // A dive that came within this of her and ended without a kill counts as a grab survived (Dread).
        public float GrabSurvivedDistance=1.6f;
        // Item 13 / owner feedback 13, the Count's voice is diegetic only (Presence.CountSpeak): from his head, in the world, within
        // VoiceMaxDistance of her; the four hunt lines (found/smell/run/stay) within VoiceCloseDistance; one line per hunt and one per
        // VoiceOffHuntGap seconds outside hunts. FoundVoiceDistance gates the "found" trigger. (VoiceCooldown/VoiceGap/VoiceJitter* are
        // retired with the search line; kept so serialised profiles still load.)
        public float VoiceCooldown=8f, VoiceGap=5f, FoundVoiceDistance=6f, VoiceJitterMin=.8f, VoiceJitterMax=4.5f;
        public float VoiceMaxDistance=14f, VoiceCloseDistance=6f, VoiceOffHuntGap=180f;
        // Item 4, hostile return (F1 HuntActive in Phase Return): short perch, no perch tell, straight-glide speed everywhere
        // he can fly, and he hears her again every ReturnHearSeconds.
        public float ReturnPerchSeconds=.4f, ReturnHearSeconds=3f;

        // Integrator (hc/grabs2): the grab reaches come from the shipped receipt Resources/Characters/CountClaude/CountClaudeModes.json
        // (F5's tools/count_prepare.py writes it), so a re-posed clip changes the camera stand-off without a code edit. The field
        // defaults above are only the fallback when the file is missing.
        [System.Serializable] sealed class Variant{public string name;public int mode;public float seconds,claw_reach_m,top_m_max;}
        [System.Serializable] sealed class Receipt{public float grab_reach_m,grab_drop_reach_m,grab_crawl_reach_m,grab_spin_start_s,grab_spin_end_s,grab_spin_yaw_deg,grab_side_start_x_m;public Variant[] grab_variants;}
        static bool reachLogged;
        public void ApplyReceipt()
        {
            try
            {
                var text=UnityEngine.Resources.Load<UnityEngine.TextAsset>("Characters/CountClaude/CountClaudeModes");
                if(!text)return;
                var r=UnityEngine.JsonUtility.FromJson<Receipt>(text.text);
                if(r.grab_reach_m>0)GrabReach=r.grab_reach_m;
                if(r.grab_drop_reach_m>0)GrabDropReach=r.grab_drop_reach_m;
                if(r.grab_crawl_reach_m>0)GrabCrawlReach=r.grab_crawl_reach_m;
                if(r.grab_variants!=null)foreach(var v in r.grab_variants)
                {
                    if(v==null||v.claw_reach_m<=0)continue;
                    if(v.mode==ModeGrabSideLId)GrabSideLReach=v.claw_reach_m;else if(v.mode==ModeGrabSideRId)GrabSideRReach=v.claw_reach_m;
                    else if(v.mode==ModeGrabDropSpinId){GrabDropSpinReach=v.claw_reach_m;if(v.top_m_max>0)GrabDropSpinClipTop=v.top_m_max;}
                }
                if(r.grab_spin_end_s>r.grab_spin_start_s&&r.grab_spin_start_s>=0){SpinStart=r.grab_spin_start_s;SpinEnd=r.grab_spin_end_s;}
                if(r.grab_spin_yaw_deg>0)SpinMaxYaw=r.grab_spin_yaw_deg;
                if(r.grab_side_start_x_m>0)SideStartLateral=r.grab_side_start_x_m;
                if(!reachLogged){reachLogged=true;UnityEngine.Debug.Log("HC_COUNT_REACH receipt grab="+GrabReach.ToString("F3")+" drop="+GrabDropReach.ToString("F3")+" crawl="+GrabCrawlReach.ToString("F3")+" standOff drop="+StandOffFor(ModeGrabDropId).ToString("F2")+" crawl="+StandOffFor(ModeGrabCrawlId).ToString("F2")+" sideL="+StandOffFor(ModeGrabSideLId).ToString("F2")+" sideR="+StandOffFor(ModeGrabSideRId).ToString("F2")+" spin="+StandOffFor(ModeGrabDropSpinId).ToString("F2")+" spinWindow="+SpinStart.ToString("F2")+"-"+SpinEnd.ToString("F2")+"s/"+SpinMaxYaw.ToString("F0")+"deg");}
            }
            catch(System.Exception e){UnityEngine.Debug.LogWarning("HC_COUNT_REACH receipt unreadable: "+e.Message);}
        }

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
