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
        int seenScares;
        float bellCooldown;
        Phase lastPhase;
        float revealHold;
        GameObject claws;
        // The grave-ash lantern has shown the claw marks on the reliquary box (opens relic 0).
        public bool BladeRevealed {get;private set;}

        public void Initialize(Game game)
        {
            Game=game;
            Forge.Crafted+=()=>{Game.Say(Game.Book.SwordForged);Fight.Begin();if(Rune)Rune.Ignite();Game.Sound.Cue("evidence_filed",Game.World.Castle.AltarPosition,.7f);};
            Fight.Killed+=()=>{Game.Creature.Defeat();Game.Sound.Cue("death_wail_sunrise",Game.Player.transform.position,.8f);Game.Case.WinByKill();};
            Fight.HitLanded+=()=>{Game.Creature.Stagger();Game.Sound.Cue("stagger_screech",Game.Creature.transform.position+Vector3.up,.9f);if(!Fight.Dead)Game.Say(Game.Book.HitLanded);};
        }
        void Start()
        {
            var spots=Game.World.Castle;var root=Game.World.Root;var view=Game.Player.View.transform;
            SurrealProps.Viewer=view;
            if(spots==null)return;
            foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name=="Reliquary claw-scores")claws=t.gameObject;
            // Props on piece 1's anchors (piece 3's factories).
            if(spots.RuneRingAnchor)Rune=SurrealProps.MakeRuneRing(spots.RuneRingAnchor,Vector3.zero,spots.RuneRingRadius>0?spots.RuneRingRadius:3,28,Game.Player.transform);
            if(spots.KnifeOrbitAnchor)SurrealProps.MakeKnifeOrbit(spots.KnifeOrbitAnchor,Vector3.zero);
            if(spots.StairLoop)
            {
                var drop=new GameObject("Stair loop destination").transform;drop.SetParent(root,false);drop.position=spots.StairLoopReturn;
                spots.StairLoop.gameObject.layer=2;
                Loop=spots.StairLoop.GetComponent<StairLoop>();if(!Loop)Loop=spots.StairLoop.gameObject.AddComponent<StairLoop>();
                Loop.destination=drop;Loop.player=Game.Player.transform;Loop.ResetForNewRun();
            }
            Bats=SurrealProps.MakeGiantBat(root,root.InverseTransformPoint(new Vector3(0,0,10)),6,6,7.5f);
            // Floating candelabras ring the great-hall rune circle; books drift over the library.
            var hall=spots.RuneRingCenter;
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI/3;var p=hall+new Vector3(Mathf.Cos(a)*4.6f,2.7f,Mathf.Sin(a)*4.6f);
                SurrealProps.MakeFloater(root,root.InverseTransformPoint(p),FloaterKind.Candelabra,i,i%3==0?1:0);
            }
            var library=spots.Compass+new Vector3(0,3.2f+1.6f,0);
            for(int i=0;i<5;i++)SurrealProps.MakeFloater(root,root.InverseTransformPoint(library+new Vector3((i-2)*.9f,(i%2)*.4f,(i%3-1)*.8f)),i%2==0?FloaterKind.OpenBook:FloaterKind.ClosedBook,10+i,0);
            // Loops (docs/haunted-castle/AUDIO.md).
            wings=Game.Sound.SpatialLoop("wing_beats_loop",Game.Creature.transform,Game.Creature.transform.position+Vector3.up*1.6f,0,2,24);
            candles=Game.Sound.SpatialLoop("candle_whisper_bed",root,hall+Vector3.up*1.5f,.25f,3,18);
            forgeHum=Game.Sound.SpatialLoop("forge_hold_hum",root,spots.AltarPosition,0,1.5f,12);
            trickle=Game.Sound.SpatialLoop("crypt_blood_trickle",root,new Vector3(spots.HunterSpawn.x+2.4f,-3.2f+.3f,spots.HunterSpawn.z),.5f,1.5f,14);
        }
        public void TakeRelic(Interactable item)
        {
            if(item.RelicIndex<0||item.RelicIndex>2||!Game.Case.Equipped)return;
            if(item.RelicIndex==0&&!BladeRevealed){Game.Say(Game.Book.BladeSealed);return;}
            if(Forge.Found((Relic)item.RelicIndex))
            {
                Game.Say(Game.Book.RelicFound[item.RelicIndex]);Game.Sound.Cue("evidence_filed",item.transform.position,.5f);
                item.gameObject.SetActive(false);
            }
        }
        public void AltarHint(){if(Forge.RelicCount<3&&!Forge.HasSword)Game.ShowNote(Game.Book.AltarLabel,Game.Book.AltarHint);}
        // A ledger page was opened: unlock its relic hint and read it aloud.
        public void NoteRead(string title)
        {
            string id=Game.Book.NoteIdFor(title);if(id==null)return;
            if(Forge.ReadLedger(id)&&LedgerHints.TryRelicFor(id,out var relic))Game.Say(Game.Book.RelicHints[(int)relic]);
            Game.Sound.Speak(id);
        }
        void Update()
        {
            if(Game==null||Game.Modal)return;
            float dt=Time.deltaTime;var spots=Game.World.Castle;if(spots==null)return;
            var player=Game.Player;var phase=Game.Case.Phase;
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
            if(Forge.HasSword&&player.FirePressed&&phase==Phase.Hunt&&Fight.CanBeHit)
            {
                var delta=Game.Creature.transform.position+Vector3.up*1.2f-player.View.transform.position;
                if(delta.magnitude<StrikeReach+.8f&&Vector3.Angle(player.View.transform.forward,delta)<StrikeAngle)Fight.RegisterHit();
            }
            if(phase!=lastPhase)
            {
                if(phase==Phase.Hunt){Floater.SetFlameTarget(Game.Creature.transform);Game.Sound.Speak("cc_hunt_smell",Game.Creature.transform.position+Vector3.up*1.8f);}
                if(lastPhase==Phase.Hunt)Floater.SetFlameTarget(null);
                if(phase==Phase.Return)Game.Sound.Cue("drawbridge_chains",spots.DrawbridgeWinch,.9f);
                if(phase==Phase.Dead){if(Fight.Active)Fight.PlayerGrabbed();Game.Sound.Cue("wing_wrap_grab",player.transform.position,.9f);}
                lastPhase=phase;
            }
            if(Game.Scares&&Game.Scares.Events!=seenScares){seenScares=Game.Scares.Events;if(Bats)Bats.Startle();}
            // Ringing (sealing) the chapel bell tolls once per stroke window.
            bellCooldown-=dt;
            if(Game.Case.SealProgress>.01f&&bellCooldown<=0){bellCooldown=7;Game.Sound.Cue("chapel_bell_toll",spots.Anchor,1);}
            bool flying=phase==Phase.Hunt&&Game.Creature.isActiveAndEnabled;
            if(wings)wings.volume=Mathf.MoveTowards(wings.volume,flying?.7f:0,dt/.4f*.7f);
            if(candles)candles.volume=Mathf.MoveTowards(candles.volume,phase==Phase.Hunt&&Vector3.Distance(Game.Creature.transform.position,candles.transform.position)<12?.6f:.25f,dt*.5f);
            if(forgeHum)forgeHum.volume=Mathf.MoveTowards(forgeHum.volume,Forge.CraftProgress01>0&&!Forge.HasSword?.6f:0,dt*1.5f);
        }
        void OnDestroy(){Floater.SetFlameTarget(null);if(SurrealProps.Viewer&&Game&&Game.Player&&SurrealProps.Viewer==Game.Player.View.transform)SurrealProps.Viewer=null;}
    }
}
