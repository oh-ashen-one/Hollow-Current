using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

namespace HollowCurrent
{
    // Opt-in automated playtest of the castle loop in the real player:
    //   -hc-playtest <outdir> escape|kill
    // Drives the investigator through Investigator.TestInput (the same path as keyboard and
    // pad input: look, use, hold, fire, tool select, torch), teleporting between stops instead
    // of pathing. Logs every phase as HC_PLAY and saves a screenshot per phase into <outdir>.
    // Driver-only safety: Case.Invulnerable counts a grab instead of ending the run; whether
    // it was needed is logged (blockedDeaths). Started from Bellwether, it switches to Castle.
    public sealed class PlaytestDriver : MonoBehaviour
    {
        public static string OutDir {get;private set;}
        public static string Mode {get;private set;}
        public static bool Requested
        {
            get
            {
                if(OutDir!=null)return true;
                var args=Environment.GetCommandLineArgs();
                for(int i=0;i<args.Length-1;i++)if(args[i]=="-hc-playtest"){OutDir=args[i+1];Mode=i+2<args.Length?args[i+2]:"escape";return true;}
                return false;
            }
        }
        Game game;FieldInput input;float start;int shot;
        readonly List<string> blockers=new List<string>();
        public void Initialize(Game owner)
        {
            game=owner;Application.runInBackground=true;
            Screen.SetResolution(1920,1080,FullScreenMode.FullScreenWindow);
            Directory.CreateDirectory(OutDir);StartCoroutine(Run());
        }
        void Log(string phase,string detail="")
        {
            var c=game.Case;var castle=game.Castle;
            Debug.Log("HC_PLAY phase="+phase+" t="+(Time.realtimeSinceStartup-start).ToString("F1")+" case="+c.Phase+" evidence="+c.Evidence.Count+" notes="+c.Notes.Count+" survived="+c.Survived+
                (castle?" relics="+castle.Forge.RelicCount+" sword="+castle.Forge.HasSword+" hits="+castle.Fight.Hits:"")+" blockedDeaths="+c.BlockedDeaths+(detail.Length>0?" "+detail:""));
        }
        void Blocker(string what){blockers.Add(what);Debug.Log("HC_PLAY_BLOCKER "+what);}
        IEnumerator Shot(string phase)
        {
            yield return new WaitForEndOfFrame();
            var tex=ScreenCapture.CaptureScreenshotAsTexture();shot++;
            File.WriteAllBytes(Path.Combine(OutDir,shot.ToString("00")+"_"+phase+".png"),tex.EncodeToPNG());Destroy(tex);
        }
        IEnumerator Step(string phase,string detail=""){Log(phase,detail);yield return Shot(phase);}
        IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        IEnumerator Wait(float seconds){float end=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<end)yield return null;}
        void Reset(){input.Use=input.UseHeld=input.Fire=input.FireHeld=input.Torch=input.Radio=input.Sprint=input.Crouch=false;input.SelectTool=0;input.Move=Vector2.zero;}
        // One frame only: a two-frame press re-uses the same E on the next frame (entering a
        // cupboard and leaving it again).
        IEnumerator Press(Action<FieldInput> set){set(input);yield return null;Reset();yield return null;}
        IEnumerator SelectTool(int tool){if(game.Player.Tool==tool)yield break;yield return Press(i=>i.SelectTool=tool);}
        IEnumerator Torch(bool on){if(game.Player.TorchOn!=on)yield return Press(i=>i.Torch=true);}
        // Stand on the nearest walkable spot ~distance from the target (same floor), facing it.
        bool StandNear(Vector3 target,float distance,float floorY)
        {
            var best=Vector3.zero;bool found=false;float score=float.MaxValue;
            for(int k=0;k<16;k++)
            {
                float a=k*Mathf.PI/8;var probe=new Vector3(target.x+Mathf.Cos(a)*distance,floorY+.1f,target.z+Mathf.Sin(a)*distance);
                if(!NavMesh.SamplePosition(probe,out var hit,.8f,NavMesh.AllAreas)||Mathf.Abs(hit.position.y-floorY)>.5f)continue;
                var eye=hit.position+Vector3.up*1.64f;var d=target-eye;
                bool clear=!Physics.Raycast(eye,d.normalized,Mathf.Max(0,d.magnitude-.15f),~((1<<2)|(1<<30)),QueryTriggerInteraction.Ignore);
                float s=Mathf.Abs(Vector3.Distance(hit.position,new Vector3(target.x,floorY,target.z))-distance)+(clear?0:10);
                if(s<score){score=s;best=hit.position;found=true;}
            }
            if(found){game.Player.Teleport(best+Vector3.up*.05f);input.Look=target;}
            return found;
        }
        static float Floor(float y)=>World.FloorBelow(y);
        Interactable Find(InteractKind kind,string label=null,int relic=-1)
        {
            foreach(var item in game.World.Root.GetComponentsInChildren<Interactable>(true))
                if(item.Kind==kind&&(label==null||item.Label==label)&&(relic<0||item.RelicIndex==relic))return item;
            return null;
        }
        // Focus the item through the investigator's own interaction ray, then press E.
        IEnumerator Use(Interactable item,string what,float distance=1.4f,bool hold=false,float holdSeconds=0)
        {
            if(!item){Blocker(what+": interactable missing");yield break;}
            var col=item.GetComponent<Collider>();var point=col?col.bounds.center:item.transform.position;
            if(!StandNear(point,distance,Floor(point.y-.2f)))Blocker(what+": no walkable stand point near "+point);
            yield return Frames(4);
            if(game.Player.Focus!=item)
            {
                // Try the other side, then fall back to a direct Use (logged as a blocker).
                StandNear(point,distance*.7f,Floor(point.y-.2f));yield return Frames(4);
            }
            bool focused=game.Player.Focus==item;
            if(!focused)Blocker(what+": not focusable by the interaction ray (focus="+(game.Player.Focus?game.Player.Focus.Label:"none")+")");
            if(hold){input.UseHeld=true;float end=Time.realtimeSinceStartup+holdSeconds;while(Time.realtimeSinceStartup<end){yield return null;}Reset();yield break;}
            if(focused)yield return Press(i=>{i.Use=true;i.UseHeld=true;});else item.Use(game);
            yield return Frames(2);
            if(game.NoteBody!=null){yield return Shot("note_"+item.Label.Replace(' ','_').Replace(':','_'));game.NoteBody=null;game.ApplyFocus();}
        }
        IEnumerator Record(Evidence evidence,Vector3 point,int tool,string what)
        {
            yield return SelectTool(tool);
            if(!StandNear(point,tool==3?2f:2.2f,Floor(point.y-.3f)))Blocker(what+": no stand point");
            yield return Frames(6);
            if(tool==3){yield return Press(i=>i.Fire=true);yield return Wait(4.6f);}
            else{input.FireHeld=true;float end=Time.realtimeSinceStartup+4f;while(Time.realtimeSinceStartup<end&&!game.Case.Evidence.Contains(evidence))yield return null;Reset();}
            if(!game.Case.Evidence.Contains(evidence))Blocker(what+": evidence not recorded (instrument: "+game.Player.Instrument+")");
            yield return Step("evidence_"+evidence,game.Player.Instrument);
        }
        IEnumerator SurviveHunt()
        {
            // Hide (torch off) in a cupboard away from the crypt during the warning.
            Interactable hide=null;float far=0;
            foreach(var h in game.World.HidingPlaces){float d=Vector3.Distance(h.HidePoint,game.World.HunterSpawn);if(d>far){far=d;hide=h;}}
            yield return Torch(false);
            if(hide)yield return Use(hide,"hiding place "+hide.Label);
            if(!game.Player.Hidden)Blocker("could not hide");
            yield return Step("hidden",hide?hide.Label:"none");
            while(game.Case.Phase==Phase.Warning)yield return null;
            yield return Step("hunt_started",game.Creature.DiagnosticState);
            int blocked=game.Case.BlockedDeaths;
            while(game.Case.Phase==Phase.Hunt)yield return null;
            yield return Step("hunt_survived",game.Case.BlockedDeaths>blocked?"grabs blocked by driver invulnerability: "+(game.Case.BlockedDeaths-blocked):"no grab");
            if(game.Player.Hidden)yield return Press(i=>i.Use=true);
            yield return Torch(true);
        }
        IEnumerator Run()
        {
            start=Time.realtimeSinceStartup;
            while(game.Loading)yield return null;yield return Wait(1);
            if(game.Map!=MapId.Castle)
            {
                Debug.Log("HC_PLAY phase=switch_to_castle from="+game.Map);
                game.SelectMap(MapId.Castle);game.BeginInvestigation();yield break; // the castle scene's Game starts a new driver
            }
            if(!game.Started)game.StartCase();
            game.Case.Invulnerable=true;
            input=new FieldInput();game.Player.TestInput=input;
            yield return Step("begin_"+Mode);
            // Field kit from the lantern cart.
            yield return Use(Find(InteractKind.Kit),"field kit");
            if(!game.Case.Equipped)Blocker("kit not equipped");
            yield return Step("kit_taken");
            // Arrival letter (gatehouse).
            yield return Use(Find(InteractKind.Note,game.Book.ArrivalNoteTitle),"arrival letter");
            // Enter the castle proper (Survey).
            game.Player.Teleport(new Vector3(0,.1f,10));yield return Frames(4);
            yield return Step("entered",game.Case.Phase.ToString());
            yield return Record(Evidence.Induction,game.World.Induction,1,"compass");
            yield return Record(Evidence.Handprint,game.World.Print,2,"grave-ash lantern");
            yield return Record(Evidence.Voice,game.World.Voice,3,"ear-trumpet");
            foreach(var title in game.Book.NoteTitles)yield return Use(Find(InteractKind.Note,title),"ledger "+title);
            yield return Step("ledger_read",string.Join(",",game.Case.Notes));
            if(Mode=="kill")yield return Relics();
            // Ring the chapel bell: the bargain hears you; survive the hunt.
            yield return Use(Find(InteractKind.Anchor),"chapel bell");
            yield return Step("bell_rung",game.Case.Phase.ToString());
            if(game.Case.Phase!=Phase.Warning)Blocker("bell did not start the warning (phase "+game.Case.Phase+")");
            if(Mode=="kill"){yield return Fight();yield return Finish();yield break;}
            yield return SurviveHunt();
            // Close the ledger: the journal reconstruction (same call as the journal's FILE RECONSTRUCTION).
            game.Journal=true;game.ApplyFocus();yield return Shot("journal");
            bool solved=game.Case.Conclude(AnchorKind.MusicBox,HauntingPattern.DomesticLoop,ClosureMethod.LetSongEnd);game.Journal=false;game.ApplyFocus();
            yield return Step("ledger_closed","conclude="+solved);
            if(!solved)Blocker("reconstruction rejected (mysteryReady="+game.Case.MysteryReady+")");
            // Relight the brazier, then hold the bell rope for the seal.
            yield return Use(Find(InteractKind.Breaker),"chapel brazier");
            yield return Step("brazier_lit","breaker="+game.Case.BreakerOn);
            yield return Use(Find(InteractKind.Anchor),"chapel bell (seal)",1.4f,true,13.5f);
            yield return Step("bell_sealed",game.Case.Phase.ToString());
            if(game.Case.Phase!=Phase.Return)Blocker("seal did not complete (progress "+game.Case.SealProgress.ToString("F2")+")");
            yield return Wait(3.5f); // drawbridge lowers at 30 deg/s
            var bridge=game.World.Castle.Drawbridge;
            yield return Step("drawbridge_lowered","lowered="+(bridge&&bridge.Lowered)+" moving="+(bridge&&bridge.Moving));
            // Walk out over the bridge to the far bank.
            game.Player.Teleport(new Vector3(0,.1f,1.2f));input.Look=new Vector3(0,1.6f,-20);yield return Frames(3);
            input.Move=new Vector2(0,1);float walk=Time.realtimeSinceStartup+12;
            while(Time.realtimeSinceStartup<walk&&game.Case.Phase!=Phase.Won)yield return null;
            Reset();
            if(game.Case.Phase!=Phase.Won)
            {
                Blocker("walking over the bridge did not reach the far-bank trigger (stopped at "+game.Player.transform.position+")");
                game.Player.Teleport(game.World.Castle.DrawbridgeTrigger.center-Vector3.up*1.0f);yield return Frames(5);
            }
            yield return Finish();
        }
        IEnumerator Relics()
        {
            var castle=game.Castle;
            // Silver blade: the lantern on the reliquary's claw marks, then open the box.
            var box=Find(InteractKind.Relic,null,0);
            yield return Use(box,"reliquary (sealed)");
            yield return Step("reliquary_sealed","bladeRevealed="+castle.BladeRevealed);
            yield return SelectTool(2);
            Transform claws=null;foreach(var t in game.World.Root.GetComponentsInChildren<Transform>(true))if(t.name=="Reliquary claw-scores")claws=t;
            if(claws){StandNear(claws.position,1.8f,Floor(claws.position.y-.2f));float end=Time.realtimeSinceStartup+3;while(Time.realtimeSinceStartup<end&&!castle.BladeRevealed)yield return null;}
            if(!castle.BladeRevealed)Blocker("lantern did not reveal the reliquary claw marks");
            yield return Step("claws_revealed","bladeRevealed="+castle.BladeRevealed);
            yield return Use(box,"reliquary (open)");
            // Hallowed hilt: the orrery socket. Moonstone: behind the portrait door.
            yield return Use(Find(InteractKind.Relic,null,1),"orrery socket",1.2f);
            Interactable portrait=null;foreach(var item in game.World.Root.GetComponentsInChildren<Interactable>(true))if(item.Kind==InteractKind.Door&&item.Label=="Portrait door")portrait=item;
            if(portrait&&portrait.Door&&!portrait.Door.Open)yield return Use(portrait,"portrait door");
            yield return Wait(1.2f);
            yield return Use(Find(InteractKind.Relic,null,2),"moonstone pedestal",1.2f);
            yield return Step("relics_found","relics="+castle.Forge.RelicCount);
            if(castle.Forge.RelicCount<3)Blocker("relics found "+castle.Forge.RelicCount+"/3");
            // Forge: hold E at the altar for the 9 s craft.
            yield return Use(Find(InteractKind.Altar),"forge altar",1.4f,true,10.5f);
            yield return Step("sword_forged","sword="+castle.Forge.HasSword+" progress="+castle.Forge.CraftProgress01.ToString("F2"));
            if(!castle.Forge.HasSword)Blocker("forge did not complete");
        }
        IEnumerator Fight()
        {
            var castle=game.Castle;int rings=1;float limit=Time.realtimeSinceStartup+300,nextApproach=0;
            while(!castle.Fight.Dead&&Time.realtimeSinceStartup<limit)
            {
                if(game.Case.Phase!=Phase.Warning&&game.Case.Phase!=Phase.Hunt)
                {
                    if(game.Case.Terminal)break;
                    yield return Use(Find(InteractKind.Anchor),"chapel bell (call)");rings++;
                    continue;
                }
                if(game.Case.Phase==Phase.Warning){yield return null;continue;}
                // Track him; strike (the instrument button) whenever he is in reach and hittable.
                // If he stays far (perched/retreating) step to a walkable spot below him.
                var him=game.Creature.transform.position;input.Look=him+Vector3.up*1.2f;
                float reach=Vector3.Distance(game.Player.View.transform.position,him+Vector3.up*1.2f);
                if(castle.Fight.CanBeHit&&reach<3.0f)
                {
                    int before=castle.Fight.Hits;yield return Press(i=>i.Fire=true);
                    if(castle.Fight.Hits>before)yield return Step("hit_"+castle.Fight.Hits,"staggered="+castle.Fight.Staggered+" tier="+castle.Fight.Tier+" countPos="+him+" reach="+reach.ToString("F2"));
                    continue;
                }
                if(castle.Fight.CanBeHit&&Time.realtimeSinceStartup>nextApproach&&NavMesh.SamplePosition(new Vector3(him.x,World.FloorBelow(him.y),him.z),out var near,3f,NavMesh.AllAreas)&&him.y-near.position.y<2.2f)
                {
                    nextApproach=Time.realtimeSinceStartup+1.5f;
                    var dir=Vector3.ProjectOnPlane(game.Player.transform.position-him,Vector3.up);if(dir.sqrMagnitude<.01f)dir=Vector3.back;
                    var stand=near.position+dir.normalized*1.4f;if(NavMesh.SamplePosition(stand,out var s2,1.5f,NavMesh.AllAreas))stand=s2.position;
                    game.Player.Teleport(stand+Vector3.up*.05f);
                }
                yield return null;
            }
            if(!castle.Fight.Dead)Blocker("fight not finished in 300 s (hits "+castle.Fight.Hits+", bell rings "+rings+")");
            yield return Wait(2.5f);
        }
        IEnumerator Finish()
        {
            yield return Wait(1.5f);
            var c=game.Case;
            yield return Step(c.Phase==Phase.Won?(c.HunterSlain?"won_kill":"won_escape"):"not_won","goal="+c.Goal);
            Debug.Log("HC_PLAY_RESULT mode="+Mode+" won="+(c.Phase==Phase.Won)+" slain="+c.HunterSlain+" blockedDeaths="+c.BlockedDeaths+" blockers="+blockers.Count+(blockers.Count>0?" ["+string.Join(" | ",blockers)+"]":""));
            yield return Wait(1);Application.Quit();
        }
    }
}
