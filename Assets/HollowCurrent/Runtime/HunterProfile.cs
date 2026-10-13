using UnityEngine;

namespace HollowCurrent
{
    public enum SearchRoute { WorldSearchPoints, PerchPoints }

    // Every tuning constant Presence used to hard-code, as plain data (no
    // ScriptableObject). Field defaults are Split Grin's shipped values, so a
    // map's hunter only overrides what differs. Presence reads nothing else.
    public class HunterProfile
    {
        public string Name="Split Grin";
        // Resources path of the animated hunter prefab (needs a HearthboundActor).
        public string ModelResource="Characters/SplitGrin";
        public int MinimumBones=20;
        // Castle: a missing model becomes a magenta stand-in instead of a throw.
        public bool AllowPlaceholderModel;

        // Navigation agent.
        public float AgentRadius=.26f,AgentHeight=2,Speed=3.15f,AngularSpeed=250,Acceleration=10,StoppingDistance=.35f;
        // Sight: eye height above the agent, range in metres, half-angle in degrees.
        public float EyeHeight=1.75f,SightRange=12,SightAngle=72;
        // Hearing: sounds more than VerticalGap metres above/below are scaled down.
        public float VerticalGap=2,VerticalHearingScale=.35f,HiddenTorchHearRadius=4;
        // Pursuit and search.
        public float ThinkInterval=.35f,SearchArriveDistance=.7f,TargetSampleRadius=1.8f,DoorOpenDistance=2,MovingVelocitySqr=.015f;
        public SearchRoute Search=SearchRoute.WorldSearchPoints;
        // Grab: reach distance, contact time to kill, minimum hunt time before a kill.
        public float GrabDistance=1.05f,GrabSeconds=.3f,GrabGraceSeconds=2,HideEntranceDistance=1.05f;
        public float BreathInterval=3.6f,BreathVolume=.6f;
        // Hunt start. Null = the map's World.HunterSpawn. A desired origin closer
        // than MinPlayerDistance to the player falls back to the spawn.
        public Vector3? HuntSpawn=new Vector3(5,-3.05f,9.5f);
        public float HuntSpawnMinPlayerDistance=3,HuntSpawnSampleRadius=3;
        // Scripted presentation.
        public float FakeRushSpeed=4.6f,FakeRushStop=1.5f,FakeRushArrive=1.7f,CapturePresentationSeconds=2.8f,ContainmentReachProgress=.62f;
        // HearthboundActor "Mode" ids. Split Grin: Idle, Walk, Search, Reach.
        // Count Claude: 0 Perch, 1 Glide, 2 SearchGlide, 3 Dive, 4 Stagger, 5 Death.
        public int ModeIdle=0,ModeMove=1,ModeSearch=2,ModeReach=3,ModeStagger=4,ModeDeath=5;

        // Fight hooks (modes 4 Stagger / 5 Death). Split Grin has no fight: no-ops.
        public virtual void OnStagger(Presence presence){}
        public virtual void OnDeath(Presence presence){}

        public static HunterProfile For(MapId map)=>map==MapId.Castle?(HunterProfile)new CountClaudeProfile():new SplitGrinProfile();
    }

    // Today's Bellwether hunter: every value is the HunterProfile default.
    public sealed class SplitGrinProfile : HunterProfile
    {
        public SplitGrinProfile(){Name="Split Grin";ModelResource="Characters/SplitGrin";}
    }
}
