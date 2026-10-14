using UnityEngine;

namespace HollowCurrent
{
    // Castle-only run state and wiring (piece 0): relics, the 9 s forge hold, the
    // three-hit fight and its kill ending, surreal props and castle audio loops.
    // Created by Game only when the Castle map is open; Bellwether never sees it.
    // Rules live in Core/Forge.cs (Forge, Fight); this class only feeds them input.
    public sealed class CastleRun : MonoBehaviour
    {
        public Game Game;
        public readonly Forge Forge=new Forge();
        public readonly Fight Fight=new Fight();
        public RuneRing Rune {get;private set;}
        public StairLoop Loop {get;private set;}
        public GiantBat Bats {get;private set;}
        public const float AltarReach=2.6f,StrikeReach=2.4f,StrikeAngle=55;
        AudioSource wings,candles,forgeHum,trickle;
        public float WingVolume=>wings?wings.volume:0; // read by the playtest driver's channel log
        int seenScares;
        float bellCooldown;
        Phase lastPhase;
        float revealHold;
        GameObject claws;
        // The grave-ash lantern has shown the claw marks on the reliquary box (opens relic 0).
        public bool BladeRevealed {get;private set;}

        // Flames ease upright after the target is cleared (Floater null-target fix, hc/dressing).
        public static void ClearFlameTarget()=>Floater.SetFlameTarget(null);
        public void Initialize(Game game)
        {
            Game=game;
            Forge.Crafted+=()=>{Game.Say(Game.Book.SwordForged);Fight.Begin();if(Rune)Rune.Ignite();Game.Sound.Cue("evidence_filed",Game.World.Castle.AltarPosition,.7f);};
            Fight.Killed+=()=>{Game.Creature.Defeat();Game.Sound.Cue("death_wail_sunrise",Game.Player.transform.position,.8f);Game.Case.WinByKill();};
            Fight.HitLanded+=()=>{Game.Creature.Stagger();Game.Sound.Cue("stagger_screech",Game.Creature.transform.position+Vector3.up,.9f);if(!Fight.Dead)Game.Say(Game.Book.HitLanded);};
            Game.Creature.Beat+=CountBeat;
        }
        // Wave F beats from Presence's castle branch (FEAR_PLAN items 10, 11, 14).
        void CountBeat(string id)
        {
            var count=Game.Creature;var spots=Game.World.Castle;
            switch(id)
            {
                case "near-miss":   // he breathes at the cupboard slats and rattles the door (item 10)
                    if(count.NearMissHasDoor)Game.Sound.Cue("door_rattle",count.NearMissDoor,.8f);Game.Sound.Speak("cc_hunt_smell",count.HeadPosition);break;
                case "hide-open":   // he saw her go in: the door rattles half a second before he opens it (r10 #16)
                    if(count.NearMissHasDoor)Game.Sound.Cue("door_rattle",count.NearMissDoor,.95f);break;
                case "gatehouse":   // he says cc_stay himself (Presence); the chains answer (item 11)
                    if(spots!=null)Game.Sound.Cue("drawbridge_chains",spots.DrawbridgeWinch,((CountClaudeProfile)count.Profile).GatehouseChainsVolume);break;
            }
            Debug.Log("HC_COUNT_BEAT_AUDIO "+id);
        }
        void Start()
        {
            var spots=Game.World.Castle;var root=Game.World.Root;var view=Game.Player.View.transform;
            SurrealProps.Viewer=view;
            if(spots==null)return;
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                if(t.name=="Reliquary claw-scores")claws=t.gameObject;
                // The sarcophagus art ("Castle / Sarcophagus" or its placeholder), not its runes or collision proxy.
                else if(t.GetComponent<Interactable>() is Interactable i&&i.Kind==InteractKind.Sarcophagus)sarcophagus=t;
            }
            // Props on piece 1's anchors (piece 3's factories).
            if(spots.RuneRingAnchor)Rune=SurrealProps.MakeRuneRing(spots.RuneRingAnchor,Vector3.zero,spots.RuneRingRadius>0?spots.RuneRingRadius:3,24,Game.Player.transform);
            if(spots.KnifeOrbitAnchor)SurrealProps.MakeKnifeOrbit(spots.KnifeOrbitAnchor,Vector3.zero);
            if(spots.StairLoop)
            {
                var drop=new GameObject("Stair loop destination").transform;drop.SetParent(root,false);drop.position=spots.StairLoopReturn;
                spots.StairLoop.gameObject.layer=2;
                Loop=spots.StairLoop.GetComponent<StairLoop>();if(!Loop)Loop=spots.StairLoop.gameObject.AddComponent<StairLoop>();
                Loop.destination=drop;Loop.player=Game.Player.transform;Loop.ResetForNewRun();
            }
            Bats=SurrealProps.MakeGiantBat(root,root.InverseTransformPoint(new Vector3(0,0,10)),6,6,7.5f);
            // Floating candelabras, books and candles are placed by CastleWorld (hc/dressing).
            var hall=spots.RuneRingCenter;
            // Loops (docs/haunted-castle/AUDIO.md).
            wings=Game.Sound.SpatialLoop("wing_beats_loop",Game.Creature.transform,Game.Creature.transform.position+Vector3.up*1.6f,0,2,24);
            candles=Game.Sound.SpatialLoop("candle_whisper_bed",root,hall+Vector3.up*1.5f,.25f,3,18);
            forgeHum=Game.Sound.SpatialLoop("forge_hold_hum",root,spots.AltarPosition,0,1.5f,12);
            trickle=Game.Sound.SpatialLoop("crypt_blood_trickle",root,new Vector3(spots.HunterSpawn.x+2.4f,-3.2f+.3f,spots.HunterSpawn.z),.5f,1.5f,14);
        }
        public void TakeRelic(Interactable item)
        {
            if(item.RelicIndex<0||item.RelicIndex>2||!Game.Case.Equipped)return;
            // The death penalty can move the blade out of the claw-scored box (Game.MovedRelic): nothing to reveal then.
            if(item.RelicIndex==0&&!BladeRevealed&&Game.MovedRelic!=0){Game.Say(Game.Book.BladeSealed);return;}
            if(Forge.Found((Relic)item.RelicIndex))
            {
                Game.Say(Game.Book.RelicFound[item.RelicIndex]);Game.Sound.Cue("evidence_filed",item.transform.position,.5f);
                item.gameObject.SetActive(false);if(item.Art)item.Art.SetActive(false);
            }
        }
        // DESIGN.md: ring the chapel bell, the bargain hears you, survive the hunt, then the
        // ledger can be closed. With the sword, ringing it calls him back for the fight. During
        // containment the bell is the seal (held, Investigation.Seal), not a summons.
        public void RingBell()
        {
            var c=Game.Case;var phase=c.Phase;
            bool summon=c.Equipped&&!c.Terminal&&phase!=Phase.Warning&&phase!=Phase.Hunt&&phase!=Phase.Containment&&phase!=Phase.Return&&phase!=Phase.Briefing
                &&((!c.Survived&&c.Evidence.Count>=2)||Forge.HasSword);
            if(!summon){if(phase!=Phase.Containment)Game.Say(Game.Book.AnchorNotReady);return;}
            if(bellCooldown<=0){bellCooldown=7;Game.Sound.Cue("chapel_bell_toll",Game.World.Castle.Anchor,1);}
            c.BeginWarning();Game.Say(BellRungText());
        }
        // The book's line says "Nine seconds"; the warning length comes from the map's rules (Long Night has none).
        string BellRungText()
        {
            string line=Game.Book.BellRung;float warning=Game.Case.Rules!=null?Game.Case.Rules.WarningSeconds:9;
            // Wave G: the castle card is never numeric. Long Night (no Warning) says he is already coming.
            if(warning<=0&&line.Contains("Get under cover."))return line.Replace("Get under cover.","He is already coming.");
            if(!line.Contains("Nine seconds"))return line;
            if(warning<=0)return line.Replace("Nine seconds: get under cover.","He is already coming.");
            return Mathf.Approximately(warning,9)?line:line.Replace("Nine seconds",Mathf.RoundToInt(warning)+" seconds");
        }
        public void AltarHint(){if(Forge.RelicCount<3&&!Forge.HasSword)Game.ShowNote(Game.Book.AltarLabel,Game.Book.AltarHint);}
        // A ledger page was opened: unlock its relic hint and read it aloud.
        public void NoteRead(string title)
        {
            if(title=="Sarcophagus"){Sarcophagus();return;}
            string id=Game.Book.NoteIdFor(title);if(id==null)return;
            if(Forge.ReadLedger(id)&&LedgerHints.TryRelicFor(id,out var relic))Game.Say(Game.Book.RelicHints[(int)relic]);
            Game.Sound.Speak(id);
        }
        // ---- Item 14: seconds the investigator spends on each floor (crypt, ground, upper, tower) --------------------
        public readonly float[] FloorSeconds=new float[World.CastleFloors.Length];
        public static int FloorIndex(float y){float floor=World.FloorBelow(y+.1f);return Mathf.Max(0,System.Array.IndexOf(World.CastleFloors,floor));}
        /// <summary>The floor she has spent most time on inside the castle (-1 before 10 s of data).</summary>
        public int FavouriteFloor
        {
            get{int best=-1;float most=10;for(int i=0;i<FloorSeconds.Length;i++)if(FloorSeconds[i]>most){most=FloorSeconds[i];best=i;}return best;}
        }
        /// <summary>The favourite floor's share of the time inside (0-1).</summary>
        public float FavouriteFloorShare
        {
            get{int f=FavouriteFloor;if(f<0)return 0;float total=0;foreach(var v in FloorSeconds)total+=v;return total>0?FloorSeconds[f]/total:0;}
        }
        float floorLog;

        // ---- Item 18: the gamble at the sarcophagus --------------------------------------------------------------------
        // Reading an unread-aloud ledger page over the sarcophagus names the relic's room, costs 25 Dread and one time in
        // three starts a hunt at once. Called by InteractKind.Sarcophagus (Interactable.Use); while that case still only
        // shows F3's note, NoteRead("Sarcophagus") calls it. A 1 s cooldown makes a double route harmless.
        public const float GambleDread=25,GambleHuntChance=1f/3;
        static readonly string[] RelicRooms={"the Reliquary in the crypt, in the box the steward sealed","the Observatory at the top of the tower, in the orrery's empty socket","the Portrait hall, behind the painted door"};
        readonly bool[] readAloud=new bool[3];
        Transform sarcophagus;
        float gambleCooldown;
        public bool Sarcophagus()
        {
            var c=Game.Case;var at=SarcophagusPosition;
            if(!c.Equipped||c.Terminal||gambleCooldown>0)return false;
            gambleCooldown=1;
            int page=-1;
            for(int i=0;i<3;i++)if(Forge.HintKnown((Relic)i)&&!readAloud[i]){page=i;break;}
            if(page<0){Game.Say("Dispatch / The sarcophagus is cold. Read a ledger page first; then read it aloud here, if you dare.");Debug.Log("HC_COUNT_GAMBLE no unread-aloud page");return false;}
            readAloud[page]=true;
            string id=LedgerHints.EntryFor((Relic)page);
            Game.Sound.Speak(id,at+Vector3.up*1.2f);
            Game.Say("Dispatch / You read the page aloud over the sarcophagus. Something below answers: the "+Game.Book.RelicNames[page].ToLowerInvariant()+" is in "+RelicRooms[page]+".");
            float before=c.Dread;c.AddDread(GambleDread);
            var phase=c.Phase;bool canHunt=phase==Phase.Survey||phase==Phase.Disturbance||phase==Phase.Recovery;
            float roll=Random.value;bool hunt=canHunt&&roll<GambleHuntChance;
            // The cursed reading starts the hunt at once, with no Warning (F0 added Investigation.BeginNoTellHunt).
            if(hunt)hunt=c.BeginNoTellHunt();
            Debug.Log("HC_COUNT_GAMBLE page="+id+" relic="+page+" dread="+before.ToString("F1")+"->"+c.Dread.ToString("F1")+" roll="+roll.ToString("F2")+" hunt="+hunt+" via="+(hunt?"BeginNoTellHunt":"none")+" phase="+phase+"->"+c.Phase);
            return true;
        }
        Vector3 SarcophagusPosition=>sarcophagus?sarcophagus.position:Game.World.Root?Game.World.Root.TransformPoint(new Vector3(0,-3.2f,22.5f)):new Vector3(0,-3.2f,22.5f);

        // ---- Death penalty hook (integrator): move a relic not yet taken to its alternate place --------------------------
        /// <summary>Moves relic <paramref name="relicIndex"/> (0 blade, 1 hilt, 2 moonstone) and its art to
        /// CastleWorld.Points.RelicAltPositions[relicIndex]. False when it is already taken or no alternate exists.</summary>
        public bool MoveRelicToAlternate(int relicIndex)
        {
            if(relicIndex<0||relicIndex>2||Forge.HasRelic((Relic)relicIndex))return false;
            var alt=Game.World.Castle?.RelicAltPositions;
            if(alt==null||relicIndex>=alt.Length||alt[relicIndex]==Vector3.zero){Debug.Log("HC_RELIC_MOVE none relic="+relicIndex+" (no RelicAltPositions in this scene)");return false;}
            Interactable relic=null;
            foreach(var item in Game.World.Root.GetComponentsInChildren<Interactable>(true))if(item.Kind==InteractKind.Relic&&item.RelicIndex==relicIndex){relic=item;break;}
            if(!relic){Debug.Log("HC_RELIC_MOVE none relic="+relicIndex+" (no interactable)");return false;}
            var from=relic.transform.position;var delta=alt[relicIndex]-from;
            relic.transform.position+=delta;if(relic.Art&&!relic.Art.transform.IsChildOf(relic.transform))relic.Art.transform.position+=delta;
            // The blade leaves the claw-scored box, so the lantern gate no longer applies.
            if(relicIndex==0)BladeRevealed=true;
            Debug.Log("HC_RELIC_MOVE relic="+relicIndex+" from="+from+" to="+relic.transform.position);
            return true;
        }
        bool huntSeen;
        void Update()
        {
            if(Game==null||Game.Modal)return;
            float dt=Time.deltaTime;var spots=Game.World.Castle;if(spots==null)return;
            var player=Game.Player;var phase=Game.Case.Phase;bool hunting=Game.Case.HuntActive;
            gambleCooldown-=dt;
            if(player.Inside&&!Game.Case.Terminal)
            {
                FloorSeconds[FloorIndex(player.transform.position.y)]+=dt;
                floorLog-=dt;if(floorLog<=0){floorLog=30;Debug.Log("HC_COUNT_FLOORS seconds=["+string.Join(",",System.Array.ConvertAll(FloorSeconds,v=>v.ToString("F0")))+"] favourite="+FavouriteFloor);}
            }
            // Forge: hold interact at the altar with three relics for Forge.CraftSeconds.
            Forge.AtAltar=Game.Case.Equipped&&!player.Hidden&&Vector3.Distance(player.transform.position,spots.AltarPosition)<AltarReach&&player.UseHeld&&Mathf.Abs(player.transform.position.y-spots.AltarPosition.y)<2;
            Forge.Tick(dt);Fight.Tick(dt);
            // Grave-ash lantern (tool 2) held on the reliquary box for a second reveals the claw marks.
            if(!BladeRevealed&&claws&&Game.Case.Equipped&&player.Tool==2&&player.Ultraviolet.enabled)
            {
                var target=claws.transform.position;var eye=player.View.transform;var delta=target-eye.position;
                bool aimed=delta.magnitude<4.5f&&Vector3.Angle(eye.forward,delta)<25;
                if(aimed){claws.SetActive(true);revealHold+=dt;if(revealHold>1){BladeRevealed=true;Game.Sound.Cue("evidence_filed",target,.45f);}}
                else{revealHold=0;claws.SetActive(false);}
            }
            // Far bank: crossing the lowered drawbridge after the bell is sealed escapes (same as the winch).
            if(phase==Phase.Return&&spots.Drawbridge&&spots.Drawbridge.Lowered&&!spots.Drawbridge.Moving&&spots.DrawbridgeTrigger.Contains(player.transform.position+Vector3.up*.5f))Game.Case.FinishAtVan();
            // Strike: the instrument button with the sword, at the visible hunter, during a hunt.
            if(Forge.HasSword&&player.FirePressed&&hunting&&Fight.CanBeHit)
            {
                var delta=Game.Creature.transform.position+Vector3.up*1.2f-player.View.transform.position;
                if(delta.magnitude<StrikeReach+.8f&&Vector3.Angle(player.View.transform.forward,delta)<StrikeAngle)Fight.RegisterHit();
            }
            // Item 4: a hostile return (HuntActive in Phase Return) is a hunt for the flames, voice, wings and the strike.
            if(hunting!=huntSeen)
            {
                huntSeen=hunting;
                // r10 #5: no voice at hunt start (cc_hunt_smell is the near-miss line only). The flames already follow him
                // between hunts (Floater's automatic target), so this is no change on screen.
                if(hunting)Floater.SetFlameTarget(Game.Creature.transform);
                else ClearFlameTarget();
            }
            if(phase!=lastPhase)
            {
                if(phase==Phase.Return)Game.Sound.Cue("drawbridge_chains",spots.DrawbridgeWinch,.9f);
                if(phase==Phase.Dead){if(Fight.Active)Fight.PlayerGrabbed();Game.Sound.Cue("wing_wrap_grab",player.transform.position,.9f);}
                lastPhase=phase;
            }
            if(Game.Scares&&Game.Scares.Events!=seenScares){seenScares=Game.Scares.Events;if(Bats)Bats.Startle();}
            // Ringing (sealing) the chapel bell tolls once per stroke window.
            bellCooldown-=dt;
            if(Game.Case.SealProgress>.01f&&bellCooldown<=0){bellCooldown=7;Game.Sound.Cue("chapel_bell_toll",spots.Anchor,1);}
            // r10 #6: the wing loop follows his actual flight, in hunts and between them, never the hunt state.
            // r10b N3: the leg's loudness (silent glide / soft / full, same odds in and out of hunts); it rises in 0.4 s and fades over
            // 1.5 s as he lands, so it never cuts off.
            if(wings){float want=.6f*Game.Creature.WingLoudness;wings.volume=Mathf.MoveTowards(wings.volume,want,dt/(want>wings.volume?.4f:1.5f)*.6f);}
            // The candle bed swells when he is near the hall candles, visible, in any phase (same rule as the flames), not by hunt state.
            bool nearCandles=Game.Creature.Actor&&Game.Creature.Actor.gameObject.activeInHierarchy&&Vector3.Distance(Game.Creature.transform.position,candles?candles.transform.position:Vector3.zero)<12;
            if(candles)candles.volume=Mathf.MoveTowards(candles.volume,nearCandles?.6f:.25f,dt*.5f);
            if(forgeHum)forgeHum.volume=Mathf.MoveTowards(forgeHum.volume,Forge.CraftProgress01>0&&!Forge.HasSword?.6f:0,dt*1.5f);
        }
        void OnDestroy(){Floater.SetFlameTarget(null);if(Game&&Game.Creature)Game.Creature.Beat-=CountBeat;if(SurrealProps.Viewer&&Game&&Game.Player&&SurrealProps.Viewer==Game.Player.View.transform)SurrealProps.Viewer=null;}
    }
}
