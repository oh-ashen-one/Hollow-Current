using System;
using HollowCurrent;

static class RulesTests
{
    static int checks;
    static void Assert(bool value,string label){checks++;if(!value)throw new Exception(label);}
    static Investigation Start(){var c=new Investigation();c.Equip();c.EnterHouse();return c;}
    static void Context(Investigation c){c.Read("Family photograph");c.Read("Child's drawing");c.Read("Furnace diary");}
    static bool Correct(Investigation c)=>c.Conclude(AnchorKind.MusicBox,HauntingPattern.DomesticLoop,ClosureMethod.LetSongEnd);
    static void Hunt(Investigation c){c.Record(Evidence.Induction);c.Record(Evidence.Handprint);c.Tick(95,true);Assert(c.Phase==Phase.Warning,"telegraphed warning");c.Tick(8,true);Assert(c.Phase==Phase.Warning,"eight seconds is still safe");c.Tick(1,true);Assert(c.Phase==Phase.Hunt,"hunt begins after nine seconds");}
    static void Main()
    {
        // Every source of a full apparition shares the same cooldown.
        var rhythm=new ScareRhythm();Assert(!rhythm.CanSurprise,"opening is quiet");rhythm.Tick(35);Assert(rhythm.CanSurprise&&!rhythm.AppearanceDue,"basement eligible before ordinary apparition");
        rhythm.RecordAppearance(0);rhythm.Tick(89.9f);Assert(!rhythm.CanSurprise,"scripted basement cannot stack with ordinary scare");rhythm.Tick(.2f);Assert(rhythm.CanSurprise&&!rhythm.AppearanceDue,"ordinary scares have a longer variable interval");rhythm.Tick(15);Assert(rhythm.AppearanceDue,"ordinary apparition eventually becomes due");
        rhythm.RecordAppearance(1);rhythm.Tick(120);Assert(!rhythm.AppearanceDue,"long anticipation interval remains clear after two minutes");rhythm.Rest(65);rhythm.Tick(64);Assert(!rhythm.CanSurprise,"post-hunt quiet window");rhythm.Tick(1);Assert(rhythm.CanSurprise,"quiet window expires");
        foreach(string caption in new[]{"[She lunges through the dark.]","[A shape flashes in the basement.]","Concealed / E to leave","Filed / Furnace diary","Power restored / The field instruments stabilize."})Assert(!FieldMessages.ShowCard(caption),"environment narration does not create a HUD card");
        Assert(FieldMessages.ShowCard("Radio / Find cover."),"spoken radio remains accessible");Assert(FieldMessages.ShowCard("Evidence secured / Voice. Journal updated."),"player evidence action gives brief confirmation");Assert(!FieldMessages.ShowCard(null),"empty card suppressed");
        Assert(FieldMessages.CardSeconds("Evidence secured / Voice.")<3,"evidence card clears quickly");
        var budget=new ScareRhythm();var events=new System.Collections.Generic.List<float>();
        for(int tick=0;tick<9000;tick++){budget.Tick(.1f);if(budget.AppearanceDue||(tick%317==0&&budget.CanSurprise)){events.Add(budget.Age);budget.RecordAppearance((tick%10)/10f);}}
        bool spaced=true,limited=true;
        for(int i=1;i<events.Count;i++)if(events[i]-events[i-1]<89.99f)spaced=false;
        foreach(float begin in events){int count=0;foreach(float t in events)if(t>=begin&&t<begin+120)count++;if(count>2)limited=false;}
        Assert(spaced,"all apparition requests obey the shared 90-second gap");Assert(limited,"no more than two full apparitions in any two-minute window");
        var c=new Investigation();c.EnterHouse();Assert(c.Phase==Phase.Briefing,"cannot leave briefing unequipped");Assert(!c.Record(Evidence.Voice),"no evidence without kit");
        Assert(c.Equip(),"kit once");Assert(!c.Equip(),"kit deduplicates");c.EnterHouse();Assert(c.Phase==Phase.Survey,"entered house");
        Assert(c.Record(Evidence.Induction),"record unique evidence");Assert(!c.Record(Evidence.Induction),"evidence cannot farm progression");
        Assert(!Correct(c),"premature diagnosis rejected");Assert(!c.FinishAtVan(),"premature ending rejected");
        c.Tick(1000,false);Assert(c.Phase==Phase.Disturbance,"one evidence does not start timed hunt");
        Hunt(c);Assert(!c.Record(Evidence.Voice),"hunt blocks capture");c.RestorePower();Assert(!c.BreakerOn,"cannot cancel hunt with breaker");
        c.Tick(38,false);Assert(c.Survived&&c.Phase==Phase.Recovery,"escape survives hunt");c.Tick(31,true);Assert(c.Phase==Phase.Recovery,"quiet recovery protected");c.Tick(1,true);Assert(c.Phase==Phase.Disturbance,"recovery ends at 32 seconds");
        c.Record(Evidence.Voice);Assert(!Correct(c),"records without family context cannot conclude");Context(c);
        Assert(c.ContextCount==3&&c.MysteryReady,"three independent context clues make reconstruction ready");
        Assert(!c.Conclude(AnchorKind.MusicBox,HauntingPattern.Imitation,ClosureMethod.LetSongEnd),"wrong behavioral interpretation rejected");
        Assert(!c.Conclude(AnchorKind.MusicBox,HauntingPattern.DomesticLoop,ClosureMethod.BurnObject),"unsupported treatment rejected");
        Assert(!c.Conclude(AnchorKind.Doll,HauntingPattern.DomesticLoop,ClosureMethod.LetSongEnd),"wrong diagnosis does not lock out");Assert(Correct(c),"correct diagnosis");
        c.Seal(20,true,true);Assert(c.Phase==Phase.Containment&&c.SealProgress==0,"power needed to seal");
        c.RestorePower();c.Seal(6,true,true);Assert(Math.Abs(c.SealProgress-.5f)<.001,"twelve second seal");c.Seal(1,false,true);Assert(c.SealProgress<.5,"interruption decays progress");
        c.Seal(12,true,false);Assert(c.Phase==Phase.Containment,"remote sealing rejected");c.Seal(12,true,true);Assert(c.Phase==Phase.Return,"seal opens return phase");
        c.Tick(999,true);Assert(c.Phase==Phase.Return,"no hunt after seal");Assert(!Correct(c),"journal cannot reopen a sealed case");Assert(c.Phase==Phase.Return,"repeat diagnosis preserves return phase");Assert(c.FinishAtVan()&&c.Phase==Phase.Won,"complete loop");c.Die();Assert(c.Phase==Phase.Won,"terminal success preserved");
        var dead=Start();Hunt(dead);dead.Die();Assert(dead.Phase==Phase.Dead,"hunt capture kills");dead.Tick(999,true);Assert(dead.Phase==Phase.Dead,"death terminal");Assert(!dead.FinishAtVan(),"death cannot finish");Assert(Start().Evidence.Count==0,"retry has clean state");
        var memory=new ObservationMemory();Assert(!memory.Knows,"starts ignorant");memory.Observe(3,0,7);memory.Tick(6.9f);Assert(memory.Knows,"limited observation memory");memory.Tick(.2f);Assert(!memory.Knows,"forgets without new observation");
        bool bad=false;try{c.Tick(float.NaN,true);}catch(ArgumentOutOfRangeException){bad=true;}Assert(bad,"reject invalid time");
        var orders=new[]{
            new[]{Evidence.Induction,Evidence.Handprint,Evidence.Voice},
            new[]{Evidence.Induction,Evidence.Voice,Evidence.Handprint},
            new[]{Evidence.Handprint,Evidence.Induction,Evidence.Voice},
            new[]{Evidence.Handprint,Evidence.Voice,Evidence.Induction},
            new[]{Evidence.Voice,Evidence.Induction,Evidence.Handprint},
            new[]{Evidence.Voice,Evidence.Handprint,Evidence.Induction}};
        foreach(var order in orders)
        {
            var route=Start();foreach(var evidence in order)Assert(route.Record(evidence),"arbitrary evidence order accepted");
            route.Tick(95,true);Assert(route.Phase==Phase.Warning,"all evidence orders reach warning");
            Assert(!route.Record(Evidence.Voice),"warning rejects record");route.Seal(50,true,true);Assert(route.SealProgress==0,"early seal rejected");
            route.Tick(9,true);route.Tick(38,true);Context(route);Assert(Correct(route),"all evidence orders can diagnose after survival");
            route.RestorePower();route.Seal(12,true,true);Assert(route.FinishAtVan(),"all evidence orders complete");
        }
        var away=Start();away.Record(Evidence.Induction);away.Record(Evidence.Handprint);away.Tick(95,false);Assert(away.Phase==Phase.Warning,"van does not freeze threat progression");
        var saved=away.SaveCheckpoint();away.Tick(9,false);away.Die();away.RestoreCheckpoint(saved);
        Assert(away.Phase==Phase.Disturbance&&!away.Terminal&&away.Evidence.Count==2,"checkpoint restores investigation progress after death without resuming an active hunt");
        Assert(away.PhaseTime==0&&away.SealProgress==0,"checkpoint provides fresh reaction time");
        var fake=Start();fake.Die();Assert(!fake.Terminal,"fake manifestation cannot kill outside a hunt");
        var fast=new float[5000];for(int i=0;i<fast.Length;i++)fast[i]=10;
        var timing=FrameStatistics.Measure(fast);Assert(timing.meets_60_fps_floor,"steady 100 FPS meets the floor");
        Assert(Math.Abs(timing.minimum_one_second_fps-100)<.01,"rolling one-second rate uses actual elapsed frame time");
        fast[2500]=50;timing=FrameStatistics.Measure(fast);
        Assert(timing.mean_fps>99&&Math.Abs(timing.minimum_frame_fps-20)<.01,"average FPS does not hide a single slow frame");
        Assert(!timing.meets_60_fps_floor&&timing.frames_over_16ms==1,"a slow frame fails the strict 60 FPS floor");
        Assert(Math.Abs(timing.minimum_one_second_fps-96)<.01,"rolling interval includes the hitch");
        Assert(timing.p99_ms==10&&timing.one_percent_low_fps<100,"percentile and one-percent-low remain distinct metrics");
        Assert(!FrameStatistics.Measure(new float[0]).meets_60_fps_floor,"no rendered samples cannot pass performance");
        Console.WriteLine("PASS "+checks+" deterministic gameplay assertions; no Unity runtime launched.");
    }
}
