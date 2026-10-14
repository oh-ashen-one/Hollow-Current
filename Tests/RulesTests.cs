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
    // Piece 5: relic, forging and Count fight rules (Core/Forge.cs).
    static void ForgeAndFightTests()
    {
        var forge=new Forge();forge.AtAltar=true;
        Assert(forge.RelicCount==0&&!forge.CanCraft,"no relics, no craft");
        Assert(forge.Found(Relic.SilverBlade)&&!forge.Found(Relic.SilverBlade),"relic found once");
        forge.Found(Relic.Moonstone);Assert(forge.RelicCount==2&&forge.HasRelic(Relic.Moonstone)&&!forge.HasRelic(Relic.HallowedHilt),"relic bookkeeping");
        Assert(!forge.CanCraft&&!forge.Craft()&&!forge.HasSword,"cannot craft with two relics");
        forge.Tick(30);Assert(!forge.HasSword&&forge.CraftProgress01==0,"two relics never progress the forge");
        forge.Found(Relic.HallowedHilt);forge.AtAltar=false;Assert(!forge.CanCraft&&!forge.Craft(),"cannot craft away from the altar");
        forge.Tick(30);Assert(!forge.HasSword&&forge.CraftProgress01==0,"time away from altar does not forge");
        forge.AtAltar=true;Assert(forge.CanCraft,"three relics at altar can craft");
        forge.Tick(4.5f);Assert(Math.Abs(forge.CraftProgress01-.5f)<.001&&!forge.HasSword,"nine second hold: half way");
        forge.AtAltar=false;forge.Tick(.1f);Assert(forge.CraftProgress01==0,"leaving the altar resets progress");
        forge.AtAltar=true;forge.Tick(8.9f);Assert(!forge.HasSword&&forge.CraftProgress01<1,"8.9 seconds is not enough");
        int crafted=0;forge.Crafted+=()=>crafted++;forge.Tick(.2f);Assert(forge.HasSword&&forge.CraftProgress01==1&&crafted==1,"craft completes after nine seconds at altar");
        forge.Tick(5);forge.AtAltar=false;forge.Tick(5);Assert(forge.HasSword&&crafted==1&&!forge.CanCraft,"sword is permanent and crafts once");
        bool badForge=false;try{forge.Tick(float.NaN);}catch(ArgumentOutOfRangeException){badForge=true;}Assert(badForge,"forge rejects invalid time");
        var instant=new Forge();instant.AtAltar=true;foreach(Relic r in Enum.GetValues(typeof(Relic)))instant.Found(r);Assert(instant.Craft()&&instant.HasSword&&!instant.Craft(),"direct craft needs relics and works once");

        var fight=new Fight();Assert(!fight.CanBeHit&&!fight.RegisterHit()&&fight.Hits==0,"no hits before the fight begins");
        fight.Begin();Assert(fight.CanBeHit&&fight.Tier==0&&fight.SpeedMultiplier==1f&&!fight.Dead&&!fight.Lost,"fresh fight: tier 0 at normal speed");
        Assert(fight.RegisterHit()&&fight.Hits==1&&fight.Staggered&&!fight.Retreating&&!fight.CanBeHit,"first hit staggers");
        Assert(!fight.RegisterHit()&&fight.Hits==1,"hit during stagger does not count");
        fight.Tick(2.4f);Assert(fight.Staggered,"stagger lasts 2.5 seconds");fight.Tick(.2f);Assert(!fight.Staggered&&fight.Retreating&&!fight.CanBeHit,"stagger gives way to retreat");
        Assert(fight.Tier==0&&fight.SpeedMultiplier==1f,"tier rises only after the retreat ends");
        Assert(!fight.RegisterHit()&&fight.Hits==1,"hits during retreat do not count");
        fight.Tick(19.8f);Assert(fight.Retreating&&!fight.RegisterHit()&&fight.Hits==1,"retreat holds for 20 seconds");
        fight.Tick(.2f);Assert(!fight.Retreating&&fight.CanBeHit&&fight.Tier==1&&Math.Abs(fight.SpeedMultiplier-1.15f)<.0001f,"after retreat he returns enraged at 1.15x");
        Assert(fight.RegisterHit()&&fight.Hits==2,"second hit counts");fight.Tick(2.5f);fight.Tick(20);
        Assert(fight.CanBeHit&&fight.Tier==2&&Math.Abs(fight.SpeedMultiplier-1.3f)<.0001f,"second retreat raises tier 2 to 1.3x");
        bool killed=false;fight.Killed+=()=>killed=true;Assert(fight.RegisterHit()&&fight.Hits==3&&fight.Dead&&killed,"three counted hits kill");
        Assert(!fight.CanBeHit&&!fight.RegisterHit()&&fight.Hits==3,"dead hunter takes no more hits");fight.Tick(100);Assert(fight.Dead&&fight.Tier==2,"death is terminal");
        fight.PlayerGrabbed();Assert(!fight.Lost,"a dead hunter cannot grab");
        var lost=new Fight();lost.PlayerGrabbed();Assert(!lost.Lost,"grab before the fight begins is ignored");
        lost.Begin();lost.RegisterHit();lost.PlayerGrabbed();Assert(lost.Lost&&!lost.Dead&&!lost.CanBeHit&&!lost.Staggered&&!lost.Retreating,"grab ends the fight as Lost");
        Assert(!lost.RegisterHit()&&lost.Hits==1,"no hits after the player is lost");lost.Begin();Assert(!lost.Lost&&lost.Hits==0&&lost.Tier==0&&lost.CanBeHit,"Begin restarts a lost fight");
        var split=new Fight();split.Begin();split.RegisterHit();split.Tick(1);split.Tick(1.5f);split.Tick(1);Assert(split.Retreating&&!split.Staggered,"timers carry across ticks");
        bool badFight=false;try{split.Tick(-1);}catch(ArgumentOutOfRangeException){badFight=true;}Assert(badFight,"fight rejects invalid time");

        Assert(Enum.GetValues(typeof(Relic)).Length==3&&LedgerHints.EntryByRelic.Count==3,"one ledger hint per relic");
        var seen=new System.Collections.Generic.HashSet<string>();
        foreach(Relic r in Enum.GetValues(typeof(Relic))){string id=LedgerHints.EntryFor(r);Assert(!string.IsNullOrEmpty(id)&&id.StartsWith("cc_ledger_",StringComparison.Ordinal),"hint id is a cc_ledger entry");Assert(seen.Add(id),"hint ids are unique");Relic back;Assert(LedgerHints.TryRelicFor(id,out back)&&back==r,"hint id maps back to its relic");}
        Assert(LedgerHints.EntryFor(Relic.SilverBlade)=="cc_ledger_steward"&&LedgerHints.EntryFor(Relic.HallowedHilt)=="cc_ledger_orrery"&&LedgerHints.EntryFor(Relic.Moonstone)=="cc_ledger_portrait","hint ids match the design");
        Relic none;Assert(!LedgerHints.TryRelicFor("cc_ledger_unknown",out none)&&!LedgerHints.TryRelicFor(null,out none),"unknown ledger entry reveals nothing");
        var journal=new Forge();Assert(journal.ReadLedger("cc_ledger_orrery")&&journal.HintKnown(Relic.HallowedHilt)&&!journal.HintKnown(Relic.Moonstone),"reading an entry unlocks its hint");Assert(!journal.ReadLedger("cc_ledger_orrery")&&!journal.ReadLedger("nonsense"),"hints unlock once; unknown entries ignored");
    }
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
        ForgeAndFightTests();
        MapTests();
        DreadRulesTests();
        NoTellTriggerTests();
        WaveGTests();
        WaveHTests();
        Console.WriteLine("PASS "+checks+" deterministic gameplay assertions; no Unity runtime launched.");
    }
    // Map catalog (piece 0): one source for scene names, receipts and display names.
    static void MapTests()
    {
        Assert(MapCatalog.All.Count==2&&MapCatalog.Get(MapId.Bellwether).ScenePath=="Assets/HollowCurrent/Scenes/Bellwether.unity","Bellwether keeps its scene path");
        Assert(MapCatalog.Get(MapId.Bellwether).BakeReceiptPath=="Assets/HollowCurrent/Art/LayoutData/BellwetherBake.json","Bellwether keeps its bake receipt");
        Assert(MapCatalog.Get(MapId.Castle).BakeReceiptPath=="Assets/HollowCurrent/Art/LayoutData/CastleBake.json"&&MapCatalog.Get(MapId.Castle).SceneName=="Castle","castle receipt per contract");
        Assert(MapCatalog.Get(MapId.Castle).LayoutFolder!=MapCatalog.Get(MapId.Bellwether).LayoutFolder,"castle never overwrites Bellwether layout assets");
        Assert(MapCatalog.Get(MapId.Bellwether).HunterName=="Split Grin"&&MapCatalog.Get(MapId.Castle).HunterName=="Count Claude","hunter names");
        Assert(MapCatalog.TryParse("castle",out var parsed)&&parsed==MapId.Castle&&!MapCatalog.TryParse("Attic",out _),"map argument parsing");
        Assert(MapCatalog.TryFromSceneName("Castle",out var scene)&&scene==MapId.Castle&&!MapCatalog.TryFromSceneName("Untitled",out _),"scene name lookup");
        Assert(MapCatalog.FromPreference(1)==MapId.Castle&&MapCatalog.FromPreference(7)==MapId.Bellwether&&MapCatalog.FromPreference(-1)==MapId.Bellwether,"corrupt preference falls back to Bellwether");
        foreach(var book in new[]{CaseBook.Bellwether,CaseBook.Castle})
        {
            foreach(var field in typeof(CaseBook).GetFields())
            {
                var value=field.GetValue(book);
                if(field.FieldType==typeof(string))Assert(value!=null,book.Map+" CaseBook."+field.Name+" is set");
                if(field.FieldType==typeof(string[]))Assert(value!=null&&((string[])value).Length==3,book.Map+" CaseBook."+field.Name+" has three entries");
            }
        }
        Assert(new Investigation().Book==CaseBook.Bellwether&&CaseBook.For(MapId.Castle)==CaseBook.Castle,"default investigation is Bellwether");
        string said=null;var house=new Investigation();house.Message+=m=>said=m;house.Equip();house.EnterHouse();house.Record(Evidence.Handprint);
        Assert(said=="Evidence secured / Handprint. Journal updated.","Bellwether evidence message unchanged");
        Assert(house.Goal=="Follow activity. Record what the instruments reveal. Compare it with the family's notes.","Bellwether goal unchanged");
        var castle=new Investigation(CaseBook.Castle);castle.Message+=m=>said=m;castle.Equip();castle.EnterHouse();
        Assert(castle.Phase==Phase.Survey&&castle.Goal==CaseBook.Castle.GoalDefault,"castle goal text");
        castle.Record(Evidence.Induction);Assert(said=="Evidence secured / Compass. Journal updated.","castle evidence name");
        castle.Record(Evidence.Handprint);castle.Tick(95,true);castle.Tick(9,true);castle.Tick(38,true);castle.Record(Evidence.Voice);
        castle.Read("Family photograph");castle.Read("Child's drawing");castle.Read("Furnace diary");Assert(castle.ContextCount==0&&!castle.MysteryReady,"house notes do not count in the castle");
        foreach(var title in CaseBook.Castle.NoteTitles)castle.Read(title);Assert(castle.ContextCount==3&&castle.MysteryReady,"Blood Ledger pages make the castle ready");
        Assert(!castle.Conclude(AnchorKind.Doll,HauntingPattern.DomesticLoop,ClosureMethod.LetSongEnd)&&said==CaseBook.Castle.WrongAnchor,"castle wrong-anchor hint");
        Assert(castle.Conclude(AnchorKind.MusicBox,HauntingPattern.DomesticLoop,ClosureMethod.LetSongEnd)&&said==CaseBook.Castle.Diagnosed,"chapel bell slot solves the castle with the shared rules");
        var slay=new Investigation(CaseBook.Castle);slay.Equip();slay.EnterHouse();Assert(slay.WinByKill()&&slay.Phase==Phase.Won&&slay.HunterSlain,"slaying the Count wins");Assert(!slay.WinByKill(),"kill win is terminal once");
        var lost=Start();Hunt(lost);lost.Die();Assert(!lost.WinByKill()&&!lost.HunterSlain,"no kill win after death");
        Assert(CaseBook.Castle.AnchorChoices[(int)AnchorKind.MusicBox]=="The chapel bell"&&CaseBook.Castle.NoteIds[0]=="cc_ledger_steward","castle anchor and ledger ids");
    }

    // Piece F1: per-map HuntRules, Dread, dice hunts, broken tells, hostile Return, death penalty (Core/HuntRules.cs).
    static void NoTellTriggerTests()
    {
        var c=StartOn(CaseBook.Castle);c.Record(Evidence.Induction);
        Assert(c.BeginNoTellHunt()&&c.Phase==Phase.Hunt&&c.NoTellHunt&&!c.FalseWarning&&c.HuntActive&&c.Hunts==1,"castle: BeginNoTellHunt starts a no-tell hunt at once");
        Assert(!c.BeginNoTellHunt()&&c.Hunts==1,"castle: no second no-tell hunt during a hunt");
        var b=StartOn(CaseBook.Bellwether);b.Record(Evidence.Induction);
        Assert(!b.BeginNoTellHunt()&&b.Phase==Phase.Disturbance&&b.Hunts==0&&b.BreakerOn,"Bellwether ignores BeginNoTellHunt");
        var brief=new Investigation(CaseBook.Castle);Assert(!brief.BeginNoTellHunt()&&brief.Phase==Phase.Briefing,"castle: no no-tell hunt before the kit");
        // r10b N5: castle actions behave the same in a hunt; Bellwether keeps its hunt gates.
        var n5=StartOn(CaseBook.Castle);n5.Record(Evidence.Induction);n5.BeginNoTellHunt();n5.RestorePower();
        Assert(n5.Phase==Phase.Hunt&&n5.BreakerOn&&n5.Record(Evidence.Handprint),"castle hunt: the brazier relights and records succeed");
        var bw=StartOn(CaseBook.Bellwether);bw.Record(Evidence.Induction);bw.BeginWarning();bw.Tick(9,true);
        Assert(bw.Phase==Phase.Hunt&&!bw.Record(Evidence.Handprint),"Bellwether hunt still blocks records");
    }
    static Investigation StartOn(CaseBook book,Func<float> random=null,HuntRules rules=null){var c=new Investigation(book,random,rules);c.Equip();c.EnterHouse();return c;}
    static int TickUntilLeaves(Investigation c,Phase phase,int maxSeconds){int t=0;while(c.Phase==phase&&t<maxSeconds){c.Tick(1,false);t++;}return t;}
    static Investigation SealedCastle()
    {
        var c=StartOn(CaseBook.Castle);c.Record(Evidence.Induction);c.Record(Evidence.Handprint);c.Record(Evidence.Voice);
        c.BeginWarning();c.Tick(9,false);c.Tick(38,false);foreach(var title in CaseBook.Castle.NoteTitles)c.Read(title);
        Correct(c);c.RestorePower();c.Seal(12,true,true);return c;
    }
    static string Trace(Func<float> random)
    {
        var c=StartOn(CaseBook.Bellwether,random);var log=new System.Text.StringBuilder();c.Changed+=p=>log.Append(p).Append('@').Append(c.Elapsed.ToString("R")).Append(';');
        c.Record(Evidence.Induction);for(int i=0;i<400;i++){c.Tick(.5f,i%3!=0);c.AddDread(5);if(i==80)c.Record(Evidence.Handprint);if(i==300)c.TryEscalate();}
        log.Append(c.Phase).Append(c.Hunts).Append(c.Dread).Append(c.Lies).Append(c.DoorHeld).Append(c.NoTellHunt).Append(c.FalseWarning);return log.ToString();
    }
    static void DreadRulesTests()
    {
        var b=HuntRules.Bellwether;
        Assert(CaseBook.Bellwether.Rules==b&&CaseBook.Castle.Rules==HuntRules.Castle&&new Investigation().Rules==b&&new Investigation(CaseBook.Castle).Rules==HuntRules.Castle,"each book carries its map's rules");
        Assert(b.TimedHuntSeconds==95&&b.TimedHuntEvidence==2&&b.WarningSeconds==9&&b.HuntSeconds==38&&b.RecoverySeconds==32,"Bellwether keeps 95/9/38/32 with two records");
        Assert(!b.UsesDread&&b.DiceIntervalMax==0&&b.FirstHuntSeconds==0&&b.HuntDreadMin==0&&b.NoTellChance==0&&b.FalseWarningChance==0&&b.EventEscalationChance==0&&!b.HostileReturn&&!b.DeathPenalty&&b.LieThreshold==0&&b.DoorHeldThreshold==0&&b.NailedHides==0,"Bellwether has no dread, dice, broken tells, lies, hostile return or death penalty");
        Assert(HuntRules.For(MapId.Bellwether,true)==b&&HuntRules.For(MapId.Castle)==HuntRules.Castle&&HuntRules.For(MapId.Castle,true)==HuntRules.LongNight,"Long Night dial applies to the castle only");

        // The shared Hunt helper behaves identically under both books (castle: first-hunt guarantee at 95 s).
        foreach(var book in new[]{CaseBook.Bellwether,CaseBook.Castle})
        {
            var h=StartOn(book);Hunt(h);Assert(h.HuntActive&&!h.NoTellHunt&&!h.FalseWarning&&h.Hunts==1&&!h.BreakerOn,book.Map+": helper hunt is a told hunt");
            h.Tick(37,true);Assert(h.Phase==Phase.Hunt,book.Map+": hunt lasts 38 s");h.Tick(1,true);Assert(h.Phase==Phase.Recovery&&h.Survived&&!h.HuntActive,book.Map+": hunt ends at 38 s");
            h.Tick(31,true);Assert(h.Phase==Phase.Recovery,book.Map+": recovery protected");h.Tick(1,true);Assert(h.Phase==Phase.Disturbance,book.Map+": recovery ends at 32 s");
        }
        // Bellwether never consults chance and ignores dread: identical traces whatever the dice return.
        string calm=Trace(null);Assert(calm==Trace(()=>0f)&&calm==Trace(()=>.99f),"Bellwether trace is independent of the dice");
        var bell=StartOn(CaseBook.Bellwether,()=>0f);bell.Record(Evidence.Induction);bell.Record(Evidence.Handprint);
        Assert(TickUntilLeaves(bell,Phase.Disturbance,200)==95&&bell.Phase==Phase.Warning&&!bell.FalseWarning&&!bell.NoTellHunt,"Bellwether: timed hunt at exactly 95 s even with aggressive dice");
        var flat=StartOn(CaseBook.Bellwether);flat.Record(Evidence.Induction);flat.AddDread(80);flat.Tick(100,true,false,false);flat.Tick(100,false,false,true);
        Assert(flat.Dread==0&&!flat.Lies&&!flat.DoorHeld&&!flat.TryEscalate()&&flat.Phase==Phase.Disturbance,"Bellwether: no dread, no lies, no escalation");

        // Dread accrual and clamping; never shown in Goal.
        var c=StartOn(CaseBook.Castle);Assert(c.Dread==0,"castle dread starts at 0");
        c.Tick(10,true,false,false);Assert(Math.Abs(c.Dread-1.2f)<.001f,"dark inside rises 0.12/s");
        c.Tick(10,true,true,false);Assert(Math.Abs(c.Dread-1.7f)<.001f,"lit inside rises 0.05/s");
        c.Tick(10,false,false,false);Assert(Math.Abs(c.Dread-1.7f)<.001f,"outside the walls holds");
        c.Tick(2,false,false,true);Assert(Math.Abs(c.Dread-.7f)<.001f,"safe zone falls 0.5/s");c.Tick(10,false,false,true);Assert(c.Dread==0,"dread floor 0");
        c.AddDread(250);Assert(c.Dread==100,"dread ceiling 100");c.AddDread(-500);Assert(c.Dread==0,"AddDread clamps at 0");c.AddDread(float.NaN);Assert(c.Dread==0,"NaN dread ignored");
        string goal=c.Goal;c.AddDread(69.9f);Assert(!c.Lies&&!c.DoorHeld&&c.Goal==goal,"below 70 no lies; goal unchanged");
        c.AddDread(.1f);Assert(c.Lies&&!c.DoorHeld,"lies from 70");c.AddDread(20);Assert(c.Lies&&c.DoorHeld&&c.Goal==goal&&!c.Goal.Contains("ead"),"door held from 90; dread never in Goal");

        var r=HuntRules.Castle;
        Assert(Math.Abs(r.DiceChance(45,1)-.125f)<.0001f&&Math.Abs(r.DiceChance(65,1)-.25f)<.0001f&&Math.Abs(r.DiceChance(85,3)-.625f)<.0001f&&r.DiceChance(39.9f,3)==0,"dice chance by band times evidence");

        // Dice hunts occur / do not occur by band (one record: no first-hunt guarantee; dread held outside).
        var low=StartOn(CaseBook.Castle,()=>0f);low.Record(Evidence.Induction);low.AddDread(low.RunDreadMin-.1f);
        Assert(TickUntilLeaves(low,Phase.Disturbance,300)==300&&low.Hunts==0,"before the first hunt, below the run's boundary: no dice, no floor, even with a certain roll");
        var band=StartOn(CaseBook.Castle,()=>.2f);band.Record(Evidence.Induction);band.AddDread(45);
        Assert(TickUntilLeaves(band,Phase.Disturbance,90)==90&&band.Hunts==0,"40-60 band: 0.125 chance never beats a 0.2 roll");
        band.AddDread(20);int waited=TickUntilLeaves(band,Phase.Disturbance,30);
        Assert(waited<=7&&band.Phase==Phase.Hunt&&band.NoTellHunt&&band.HuntActive&&band.Hunts==1&&band.BreakerOn,"60-80 band: 0.25 chance beats 0.2; the 0.2 tell roll makes it a no-tell hunt (brazier untouched)");
        Assert(band.Record(Evidence.Voice),"castle (r10b N5): a no-tell hunt does not block records (the refusal was a hunt tell)");
        var high=StartOn(CaseBook.Castle);foreach(Evidence e in Enum.GetValues(typeof(Evidence)))high.Record(e);high.AddDread(85);
        Assert(TickUntilLeaves(high,Phase.Disturbance,30)<10&&high.Phase==Phase.Warning&&!high.FalseWarning&&!high.NoTellHunt,"80+ band with three records: deterministic 0.5 roll hunts, after a real Warning");
        high.Tick(9,false);Assert(high.Phase==Phase.Hunt&&high.Hunts==1,"rolled Warning becomes a hunt after 9 s");

        // Forced floor: 240 s at Dread >= 40 since the last hunt; the clock pauses below 40.
        var floor=StartOn(CaseBook.Castle,()=>.99f);floor.Record(Evidence.Induction);floor.AddDread(floor.RunDreadMin+5);
        Assert(TickUntilLeaves(floor,Phase.Disturbance,300)==240&&floor.Phase==Phase.Warning&&!floor.FalseWarning&&floor.CurrentWarningSeconds>=5&&floor.CurrentWarningSeconds<=14,"floor forces a hunt after 240 s just above the run's boundary");
        var pause=StartOn(CaseBook.Castle,()=>.99f);pause.Record(Evidence.Induction);pause.AddDread(pause.RunDreadMin+5);
        Assert(TickUntilLeaves(pause,Phase.Disturbance,200)==200,"200 s above the boundary is not enough");pause.AddDread(-10);
        Assert(TickUntilLeaves(pause,Phase.Disturbance,100)==100,"floor clock pauses below the boundary");pause.AddDread(10);
        Assert(TickUntilLeaves(pause,Phase.Disturbance,100)==40,"floor clock resumes where it stopped");

        // First-hunt guarantee: two records, deadline 95 s +-25 % by roll, independent of Dread.
        foreach(var pair in new[]{new[]{.5f,95},new[]{0f,72},new[]{.99f,119}})
        {
            float roll=pair[0];var first=StartOn(CaseBook.Castle,()=>roll);first.Record(Evidence.Induction);first.Record(Evidence.Handprint);
            Assert(TickUntilLeaves(first,Phase.Disturbance,200)==(int)pair[1]&&first.Dread==0,"first hunt guaranteed at "+pair[1]+" s for roll "+roll);
        }

        // Recovery below Dread 80 is no longer a guaranteed window (wave H, N7): certain dice also hunt from Recovery.
        var spaced=StartOn(CaseBook.Castle,()=>.01f);foreach(Evidence e in Enum.GetValues(typeof(Evidence)))spaced.Record(e);spaced.AddDread(79);
        int hunts=0,fromRecovery=0;spaced.Changed+=p=>{if(p==Phase.Hunt){hunts++;if(spaced.ThreatFrom==Phase.Recovery)fromRecovery++;}};
        for(int i=0;i<1200;i++)spaced.Tick(.5f,false);
        Assert(hunts>=5&&fromRecovery>=1&&spaced.Hunts==hunts,"dice hunts repeat, some of them out of Recovery below Dread 80");

        // False warning: Warning that ends back in Disturbance; records and MysteryReady treat it like Disturbance.
        var q=new System.Collections.Generic.Queue<float>();var fw=StartOn(CaseBook.Castle,()=>q.Count>0?q.Dequeue():.99f);
        fw.Record(Evidence.Induction);fw.Record(Evidence.Handprint);fw.BeginWarning();Assert(fw.Phase==Phase.Warning&&!fw.FalseWarning,"bell summons are never false alarms");
        TickUntilLeaves(fw,Phase.Warning,30);TickUntilLeaves(fw,Phase.Hunt,60);TickUntilLeaves(fw,Phase.Recovery,60);foreach(var title in CaseBook.Castle.NoteTitles)fw.Read(title);fw.AddDread(85);
        Assert(fw.Phase==Phase.Disturbance&&fw.Survived&&!fw.MysteryReady,"survived, two records: not ready");
        q.Enqueue(.5f);q.Enqueue(.1f);q.Enqueue(.9f);q.Enqueue(.1f); // interval, hunt roll, tell roll, false-alarm roll
        TickUntilLeaves(fw,Phase.Disturbance,30);
        Assert(fw.Phase==Phase.Warning&&fw.FalseWarning&&!fw.HuntActive&&fw.Hunts==1&&!fw.BreakerOn,"rolled false alarm: Warning that is not a hunt");
        Assert(fw.Record(Evidence.Voice)&&fw.MysteryReady,"false warning accepts records and keeps MysteryReady");
        float fwLen=fw.CurrentWarningSeconds;fw.Tick(fwLen-.5f,false);Assert(fw.Phase==Phase.Warning&&fw.FalseWarning,"false alarm lasts the full warning");
        fw.Tick(1,false);Assert(fw.Phase==Phase.Disturbance&&!fw.FalseWarning&&fw.Hunts==1&&fw.MysteryReady,"false alarm ends in Disturbance, no hunt, still ready");
        fw.BeginWarning();Assert(fw.MysteryReady&&!fw.FalseWarning,"castle (r10b N5): a real Warning does not block the journal (readiness is phase-blind)");TickUntilLeaves(fw,Phase.Warning,30);Assert(fw.Phase==Phase.Hunt&&fw.Hunts==2,"real Warning hunts");

        // Escalation: a close apparition becomes a hunt after a 1.5 s tell, with no dispatch line.
        Assert(!StartOn(CaseBook.Castle).TryEscalate(),"no escalation outside Disturbance");
        var calmEsc=StartOn(CaseBook.Castle);calmEsc.Record(Evidence.Induction);Assert(!calmEsc.TryEscalate()&&calmEsc.Phase==Phase.Disturbance,"0.5 roll does not escalate at 0.2");
        string said=null;var esc=StartOn(CaseBook.Castle,()=>.1f);esc.Record(Evidence.Induction);esc.Message+=m=>said=m;
        Assert(esc.TryEscalate()&&esc.Phase==Phase.Warning&&esc.WarningLength==1.5f&&!esc.FalseWarning&&said==null,"0.1 roll escalates into a silent 1.5 s Warning");
        Assert(!esc.TryEscalate(),"no escalation during a Warning");esc.Tick(1,false);Assert(esc.Phase==Phase.Warning,"tell still running at 1 s");
        esc.Tick(.5f,false);Assert(esc.Phase==Phase.Hunt&&!esc.NoTellHunt&&esc.Hunts==1,"escalated hunt starts at 1.5 s");

        // Hostile Return: a hunt until the bridge is crossed; Die works; FinishAtVan still wins.
        var ret=SealedCastle();Assert(ret.Phase==Phase.Return&&ret.HuntActive&&ret.NoTellHunt&&ret.Hunts==2,"castle Return is a tell-less hunt");
        ret.Tick(999,true);Assert(ret.Phase==Phase.Return&&ret.HuntActive,"hostile Return does not time out");
        ret.Die();Assert(ret.Phase==Phase.Dead&&ret.Deaths==1&&!ret.HuntActive,"dying on the bridge is a kill");
        var esc2=SealedCastle();esc2.Invulnerable=true;esc2.Die();Assert(esc2.Phase==Phase.Return&&esc2.BlockedDeaths==1,"playtest invulnerability still holds in Return");
        esc2.Invulnerable=false;Assert(esc2.FinishAtVan()&&esc2.Phase==Phase.Won&&!esc2.HuntActive,"crossing the bridge wins");esc2.Die();Assert(esc2.Phase==Phase.Won&&esc2.Deaths==0,"no death after the win");
        var house=Start();house.Record(Evidence.Induction);house.Record(Evidence.Handprint);house.Record(Evidence.Voice);house.BeginWarning();house.Tick(9,false);house.Tick(38,false);Context(house);Correct(house);house.RestorePower();house.Seal(12,true,true);
        Assert(house.Phase==Phase.Return&&!house.HuntActive&&house.Hunts==1&&!house.NoTellHunt,"Bellwether Return stays safe");house.Die();Assert(house.Phase==Phase.Return&&house.Deaths==0,"Bellwether Return cannot kill");

        // Death penalty: a restore after a death counts it and restarts Dread at 60 (castle only).
        var dying=StartOn(CaseBook.Castle);dying.Record(Evidence.Induction);dying.Record(Evidence.Handprint);dying.AddDread(20);var save=dying.SaveCheckpoint();
        Assert(save.Dread==20&&save.Deaths==0,"checkpoint stores Dread and Deaths");dying.BeginWarning();dying.Tick(9,false);dying.Die();Assert(dying.Phase==Phase.Dead&&dying.Deaths==1,"death counted");
        var back=new Investigation(CaseBook.Castle);back.RestoreCheckpoint(save);
        Assert(back.Phase==Phase.Disturbance&&back.Evidence.Count==2&&back.Dread==60&&back.Deaths==1&&back.DeathPenaltyApplied,"fresh restore after death: Dread 60, death counted, penalty flagged");
        dying.RestoreCheckpoint(save);Assert(dying.Dread==60&&dying.Deaths==1&&dying.DeathPenaltyApplied,"same-instance restore does not double count");
        var save2=back.SaveCheckpoint();Assert(save2.Deaths==1&&save2.Dread==60,"deaths carry into later checkpoints");var again=new Investigation(CaseBook.Castle);again.RestoreCheckpoint(save2);Assert(again.Deaths==2,"second death counted");
        var quiet=new Investigation(CaseBook.Castle);quiet.RestoreCheckpoint(save,false);Assert(quiet.Dread==20&&quiet.Deaths==0&&!quiet.DeathPenaltyApplied,"restore without a death keeps saved Dread");
        var bsave=Start();bsave.Record(Evidence.Induction);var bs=bsave.SaveCheckpoint();var bback=new Investigation();bback.RestoreCheckpoint(bs);
        Assert(bback.Dread==0&&!bback.DeathPenaltyApplied&&bback.Deaths==1&&bback.Phase==Phase.Disturbance,"Bellwether restore: no penalty");

        // Long Night: no Warning phase, Dread starts at 50, 50 s hunts, 2-5 s perches, two hides nailed.
        var ln=HuntRules.LongNight;
        Assert(ln.WarningSeconds==0&&ln.DreadStart==50&&ln.HuntSeconds==50&&ln.PerchMinSeconds==2&&ln.PerchMaxSeconds==5&&ln.NailedHides==2,"Long Night values");
        Assert(ln.UsesDread&&ln.HostileReturn&&ln.DeathPenalty&&ln.TimedHuntSeconds==r.TimedHuntSeconds&&ln.DiceIntervalMin==r.DiceIntervalMin&&ln.LieThreshold==70&&r.WarningSeconds==9&&r.HuntSeconds==38&&r.DreadStart==0,"Long Night is the castle with the dial; castle untouched");
        var night=StartOn(CaseBook.Castle,null,ln);Assert(night.Rules==ln&&night.Dread==50,"Long Night starts at Dread 50");
        night.BeginWarning();Assert(night.Phase==Phase.Hunt&&night.NoTellHunt&&night.Hunts==1,"Long Night: even the bell has no Warning");
        night.Tick(49,false);Assert(night.Phase==Phase.Hunt,"Long Night hunt lasts 50 s");night.Tick(1,false);Assert(night.Phase==Phase.Recovery,"Long Night hunt ends at 50 s");
        var night2=StartOn(CaseBook.Castle,null,ln);night2.Record(Evidence.Induction);night2.Record(Evidence.Handprint);
        Assert(TickUntilLeaves(night2,Phase.Disturbance,200)==95&&night2.Phase==Phase.Hunt&&night2.NoTellHunt,"Long Night first hunt arrives with no Warning");
    }

    // Wave G (critic r10 static #1, #10, #12, #15, FeltDread for #2/#3): randomised boundaries and lengths.
    static Func<float> Seeded(int seed){var rng=new Random(seed);return()=>(float)rng.NextDouble();}
    static float Median(System.Collections.Generic.List<float> v){v.Sort();return v[v.Count/2];}
    // A castle that has survived one bell hunt and is back in Disturbance at Dread 0.
    static Investigation AfterFirstHunt(Func<float> random)
    {
        var c=StartOn(CaseBook.Castle,random);c.Record(Evidence.Induction);c.BeginWarning();TickUntilLeaves(c,Phase.Warning,30);
        for(int t=0;t<900&&c.Phase!=Phase.Disturbance;t++)c.Tick(1,false);return c;
    }
    static void WaveGTests()
    {
        var r=HuntRules.Castle;var b=HuntRules.Bellwether;
        Assert(r.HuntDreadMin==40&&r.HuntDreadMinSpread==10&&Math.Abs(r.LowDreadChance-1f/40)<1e-6f,"castle boundary 30-50, low roll 1/40");
        Assert(r.HuntSecondsMin==25&&r.HuntSeconds==38&&r.HuntSecondsMax==50&&r.RecoverySecondsMin==10&&r.RecoverySeconds==32&&r.RecoverySecondsMax==40,"castle hunt 25-50, recovery 10-40");
        Assert(Math.Abs(r.RecoveryDiceChance-.125f)<1e-6f&&r.RecoveryDiceDread==80&&!r.AnnounceRecovery,"castle recovery dice 1/8 at 80, no quiet-window line");
        Assert(r.FeltLagMin==20&&r.FeltLagMax==30&&r.FeltNoise==12&&r.FeltCalm>0,"castle FeltDread lag 20-30 s, noise 12");
        Assert(b.HuntDreadMinSpread==0&&b.LowDreadChance==0&&b.HuntSecondsMin==38&&b.HuntSecondsMax==38&&b.RecoverySecondsMin==32&&b.RecoverySecondsMax==32&&b.RecoveryDiceChance==0&&b.AnnounceRecovery&&b.FeltNoise==0,"Bellwether: no wave G randomisation");
        Assert(HuntRules.LongNight.HuntSecondsMin==40&&HuntRules.LongNight.HuntSeconds==50&&HuntRules.LongNight.HuntSecondsMax==60,"Long Night hunts 40-60, median 50");
        Assert(HuntRules.Draw(25,38,50,0)==25&&HuntRules.Draw(25,38,50,.5f)==38&&HuntRules.Draw(25,38,50,1)==50&&Math.Abs(HuntRules.Draw(25,38,50,.25f)-31.5f)<1e-4f,"piecewise draw hits min, median, max");
        int calls=0;var bw=new Investigation(CaseBook.Bellwether,()=>{calls++;return 0f;});
        Assert(calls==0&&bw.RunDreadMin==0&&bw.FeltDread==0&&bw.FeltLagSeconds==0&&bw.CurrentHuntSeconds==38&&bw.CurrentRecoverySeconds==32,"Bellwether draws nothing at construction");
        string said=null;var bm=Start();bm.Message+=m=>said=m;Hunt(bm);bm.Tick(38,true);Assert(said==CaseBook.Bellwether.RecoveryMessage,"Bellwether still announces recovery");

        // #1 boundary per run: 30 + 20 x roll, constructor draws 3 rolls in a fixed order.
        int castleCalls=0;var cc=new Investigation(CaseBook.Castle,()=>{castleCalls++;return .5f;});
        Assert(castleCalls==3&&cc.RunDreadMin==40&&cc.FeltLagSeconds==25,"castle: 3 construction draws (boundary, lag, seed); 0.5 gives 40 and 25 s");
        Assert(new Investigation(CaseBook.Castle,()=>0f).RunDreadMin==30&&new Investigation(CaseBook.Castle,()=>.99999f).RunDreadMin>49.99f,"boundary spans 30-50");
        var mins=new System.Collections.Generic.List<float>();var draw=Seeded(11);
        for(int i=0;i<4000;i++)mins.Add(new Investigation(CaseBook.Castle,draw).RunDreadMin);
        float lo=float.MaxValue,hi=0,sum=0;int lowTail=0,highTail=0;foreach(var m in mins){lo=Math.Min(lo,m);hi=Math.Max(hi,m);sum+=m;if(m<32)lowTail++;if(m>48)highTail++;}
        Assert(lo>=30&&hi<50&&Math.Abs(sum/mins.Count-40)<.6f&&lowTail>250&&highTail>250,"boundary uniform over 30-50 across 4000 runs");

        // #1 low-Dread "demon" roll: none before the first hunt (covered above), 1/40 per roll after it.
        Assert(TickUntilLeaves(AfterFirstHunt(()=>.99f),Phase.Disturbance,600)==600,"low roll above 1/40 never hunts");
        var demon=AfterFirstHunt(()=>.99f);demon.Dice=()=>.02f;Assert(demon.Dread<demon.RunDreadMin&&TickUntilLeaves(demon,Phase.Disturbance,30)<10&&demon.Hunts==2,"low roll under 1/40 hunts at Dread 0 after the first hunt");
        int sessions=0,withHunt=0;float waitSum=0;int waits=0;
        for(int seed=0;seed<400;seed++)
        {
            var run=AfterFirstHunt(Seeded(1000+seed));if(run.Phase!=Phase.Disturbance)continue;
            sessions++;int t=TickUntilLeaves(run,Phase.Disturbance,600);if(t<600){withHunt++;waitSum+=t;waits++;}
            Assert(run.Dread<run.RunDreadMin,"session stays below the boundary");
        }
        float hitRate=withHunt/(float)sessions,meanWait=waitSum/Math.Max(1,waits);
        Console.WriteLine("WAVE_G low-Dread sessions="+sessions+" with_hunt_in_10min="+hitRate.ToString("F3")+" mean_wait_s="+meanWait.ToString("F1"));
        Assert(sessions>=395&&hitRate>.9f&&meanWait>100&&meanWait<220,"Dread 0, 10 min after the first hunt: >90 % of sessions hunt, mean wait about 160 s");
        // Crossing the boundary restarts the schedule: the playtest driver's queued false warning still lines up.
        var q=new System.Collections.Generic.Queue<float>();var drv=AfterFirstHunt(()=>q.Count>0?q.Dequeue():.99f);
        drv.Tick(17,false);drv.AddDread(55-drv.Dread+Math.Max(0,drv.RunDreadMin-50));foreach(var v in new[]{0f,0f,.9f,0f})q.Enqueue(v);
        TickUntilLeaves(drv,Phase.Disturbance,20);Assert(drv.Phase==Phase.Warning&&drv.FalseWarning&&q.Count==0,"driver queue [interval 0, hunt 0, tell .9, false 0] still gives a false warning");

        // #10 random lengths per hunt.
        var hunt=new System.Collections.Generic.List<float>();var rec=new System.Collections.Generic.List<float>();bool endsOk=true;
        for(int seed=0;seed<1500;seed++)
        {
            var c=StartOn(CaseBook.Castle,Seeded(5000+seed));c.Record(Evidence.Induction);c.BeginWarning();TickUntilLeaves(c,Phase.Warning,30);
            float h=c.CurrentHuntSeconds;hunt.Add(h);c.Tick(h-.5f,false);if(c.Phase!=Phase.Hunt)endsOk=false;c.Tick(1,false);if(c.Phase!=Phase.Recovery)endsOk=false;
            float rr=c.CurrentRecoverySeconds;rec.Add(rr);c.Tick(rr-.5f,false);if(c.Phase!=Phase.Recovery)endsOk=false;c.Tick(1,false);if(c.Phase!=Phase.Disturbance)endsOk=false;
        }
        Assert(endsOk,"each hunt and recovery lasts exactly its drawn length");
        float hMed=Median(hunt),rMed=Median(rec);
        Console.WriteLine("WAVE_G hunt_s min="+hunt[0].ToString("F1")+" median="+hMed.ToString("F1")+" max="+hunt[hunt.Count-1].ToString("F1")+"  recovery_s min="+rec[0].ToString("F1")+" median="+rMed.ToString("F1")+" max="+rec[rec.Count-1].ToString("F1"));
        Assert(hunt[0]>=25&&hunt[hunt.Count-1]<=50&&hunt[0]<26.5f&&hunt[hunt.Count-1]>48.5f&&Math.Abs(hMed-38)<1.2f,"hunt lengths spread 25-50 around 38");
        Assert(rec[0]>=10&&rec[rec.Count-1]<=40&&rec[0]<12&&rec[rec.Count-1]>39&&Math.Abs(rMed-32)<1.5f,"recovery lengths spread 10-40 around 32");
        int fixedHunts=0;foreach(var h in hunt)if(Math.Abs(h-38)<.05f)fixedHunts++;Assert(fixedHunts<15,"38 s is no longer the hunt length");
        // Recovery dice at Dread >= 80.
        var rd=AfterFirstHunt(null);rd.BeginWarning();rd.Tick(9,false);rd.AddDread(85);rd.Tick(38,false);Assert(rd.Phase==Phase.Recovery,"setup: recovery at Dread 85");
        var rq=new System.Collections.Generic.Queue<float>();var rdq=StartOn(CaseBook.Castle,()=>rq.Count>0?rq.Dequeue():.99f);rdq.Record(Evidence.Induction);rdq.BeginWarning();TickUntilLeaves(rdq,Phase.Warning,30);rdq.AddDread(85);
        TickUntilLeaves(rdq,Phase.Hunt,60);Assert(rdq.Phase==Phase.Recovery&&rdq.CurrentRecoverySeconds>39,"setup: long recovery at Dread 85");
        foreach(var v in new[]{.5f,.1f,.9f,.1f})rq.Enqueue(v); // interval, recovery roll under 1/8, tell, false alarm
        int inRecovery=TickUntilLeaves(rdq,Phase.Recovery,60);Assert(inRecovery<10&&rdq.Phase==Phase.Warning&&rdq.FalseWarning,"1/8 dice roll fires inside Recovery at Dread 85");
        TickUntilLeaves(rdq,Phase.Warning,30);Assert(rdq.Phase==Phase.Disturbance,"a false alarm from Recovery ends in Disturbance");
        int cut=0,trials=0;
        for(int seed=0;seed<600;seed++)
        {
            var c=StartOn(CaseBook.Castle,Seeded(9000+seed));c.Record(Evidence.Induction);c.BeginWarning();TickUntilLeaves(c,Phase.Warning,30);c.AddDread(90);
            TickUntilLeaves(c,Phase.Hunt,60);if(c.Phase!=Phase.Recovery)continue;trials++;float len=c.CurrentRecoverySeconds;int t=TickUntilLeaves(c,Phase.Recovery,60);if(t<len-.01f)cut++;
        }
        float cutRate=cut/(float)trials;Console.WriteLine("WAVE_G recovery_interrupted_at_dread90="+cutRate.ToString("F3")+" of "+trials);
        Assert(trials>550&&cutRate>.3f&&cutRate<.8f,"Recovery at Dread 90 is interrupted in a share of runs, not never");
        int lowCut=0;for(int seed=0;seed<400;seed++){var c=StartOn(CaseBook.Castle,Seeded(9900+seed));c.Record(Evidence.Induction);c.BeginWarning();TickUntilLeaves(c,Phase.Warning,30);c.AddDread(79);TickUntilLeaves(c,Phase.Hunt,60);float len=c.CurrentRecoverySeconds;if(TickUntilLeaves(c,Phase.Recovery,60)<len-.01f)lowCut++;}
        Console.WriteLine("WAVE_H recovery_interrupted_at_dread79="+(lowCut/400f).ToString("F3"));
        Assert(lowCut>40&&lowCut<220,"below Dread 80 Recovery is interrupted now and then (1/16 per roll), not never");
        string castleSaid=null;var cm=StartOn(CaseBook.Castle);cm.Record(Evidence.Induction);cm.BeginWarning();cm.Tick(9,false);cm.Message+=m=>castleSaid=m;cm.Tick(38,false);
        Assert(cm.Phase==Phase.Recovery&&castleSaid==null,"castle recovery is not announced");

        // #12 no-tell starts leave BreakerOn alone; told warnings still cut it.
        var nt=StartOn(CaseBook.Castle,()=>.1f);nt.Record(Evidence.Induction);nt.AddDread(90);TickUntilLeaves(nt,Phase.Disturbance,30);
        Assert(nt.Phase==Phase.Hunt&&nt.NoTellHunt&&nt.BreakerOn,"dice no-tell hunt keeps the brazier");
        var gamble=StartOn(CaseBook.Castle);gamble.Record(Evidence.Induction);Assert(gamble.BeginNoTellHunt()&&gamble.BreakerOn,"gamble no-tell hunt keeps the brazier");
        var lnb=StartOn(CaseBook.Castle,null,HuntRules.LongNight);lnb.BeginWarning();Assert(lnb.Phase==Phase.Hunt&&lnb.BreakerOn,"Long Night bell hunt keeps the brazier");
        var lne=StartOn(CaseBook.Castle,()=>.1f,HuntRules.LongNight);lne.Record(Evidence.Induction);Assert(lne.TryEscalate()&&lne.Phase==Phase.Hunt&&lne.BreakerOn,"Long Night escalation keeps the brazier");
        var told=StartOn(CaseBook.Castle);told.Record(Evidence.Induction);told.BeginWarning();Assert(!told.BreakerOn,"a told Warning still cuts the brazier");
        var bwt=Start();bwt.BeginWarning();Assert(!bwt.BreakerOn,"Bellwether warning still cuts the breaker");

        // #15 first hunt measured from the second record, not from Disturbance entry.
        var slow=StartOn(CaseBook.Castle);slow.Record(Evidence.Induction);Assert(TickUntilLeaves(slow,Phase.Disturbance,300)==300,"one record for 300 s: no hunt");
        slow.Record(Evidence.Handprint);slow.Tick(1,false);Assert(slow.Phase==Phase.Disturbance,"second record does not trigger the hunt on the next tick");
        Assert(TickUntilLeaves(slow,Phase.Disturbance,200)==94&&slow.Phase==Phase.Warning,"first hunt 95 s after the second record");
        var slowSave=StartOn(CaseBook.Castle);slowSave.Record(Evidence.Induction);slowSave.Record(Evidence.Handprint);slowSave.Tick(90,false);var ss=slowSave.SaveCheckpoint();
        var ssBack=new Investigation(CaseBook.Castle);ssBack.RestoreCheckpoint(ss,false);Assert(TickUntilLeaves(ssBack,Phase.Disturbance,200)==95,"a restore gives the full first-hunt delay again");

        // FeltDread: lagged, noisy, calm while Lies, blind to the boundary.
        var noiseless=HuntRules.Castle.Clone();noiseless.FeltNoise=0;
        var step=StartOn(CaseBook.Castle,null,noiseless);step.AddDread(60);Assert(step.FeltDread==0,"FeltDread does not jump with Dread");
        for(int i=0;i<25;i++)step.Tick(1,false);Assert(Math.Abs(step.FeltDread-60*(1-(float)Math.Exp(-1)))<.3f,"lag: 63 % after one time constant (25 s at a 0.5 roll)");
        var lag0=StartOn(CaseBook.Castle,()=>0f,noiseless);var lag1=StartOn(CaseBook.Castle,()=>.99999f,noiseless);Assert(lag0.FeltLagSeconds==20&&lag1.FeltLagSeconds>29.99f,"lag drawn from 20-30 s");
        var felt=StartOn(CaseBook.Castle,Seeded(77));felt.AddDread(80);felt.Tick(1,false);Assert(felt.FeltDread<12+80*(1-(float)Math.Exp(-1/20f))+.1f,"one second after a jump FeltDread has barely moved");
        var noEpisodes=HuntRules.Castle.Clone();noEpisodes.FeltEpisodeGapMax=0;
        var steady=StartOn(CaseBook.Castle,Seeded(78),noEpisodes);steady.AddDread(50);for(int i=0;i<200;i++)steady.Tick(1,false);
        float fMin=100,fMax=0,maxStep=0,prev=steady.FeltDread;bool inBand=true;
        for(int i=0;i<1200;i++){steady.Tick(1,false);float f=steady.FeltDread;if(Math.Abs(f-50)>12.05f)inBand=false;fMin=Math.Min(fMin,f);fMax=Math.Max(fMax,f);maxStep=Math.Max(maxStep,Math.Abs(f-prev));prev=f;}
        Console.WriteLine("WAVE_G felt_at_dread50 min="+fMin.ToString("F1")+" max="+fMax.ToString("F1")+" max_step_per_s="+maxStep.ToString("F2"));
        Assert(inBand&&fMax-fMin>14&&fMin<44&&fMax>56,"noise: +-12 around a steady Dread, and it really moves");
        Assert(maxStep<6.5f,"noise is slow: under 6.5 per second");
        var liar=StartOn(CaseBook.Castle,Seeded(79),noEpisodes);liar.AddDread(85);for(int i=0;i<150;i++)liar.Tick(1,false);
        Assert(liar.Lies&&Math.Abs(liar.FeltDread-85)<=12.6f,"Lies no longer fades the pulse to calm (N6): it follows Dread outside episodes");
        var low30=HuntRules.Castle.Clone();low30.HuntDreadMin=30;var high50=HuntRules.Castle.Clone();high50.HuntDreadMin=50;
        var fa=StartOn(CaseBook.Castle,Seeded(80),low30);var fb=StartOn(CaseBook.Castle,Seeded(80),high50);bool same=fa.RunDreadMin!=fb.RunDreadMin;float rampStep=0,rp=fa.FeltDread;
        for(int i=0;i<300;i++){fa.AddDread(.4f);fb.AddDread(.4f);fa.Tick(1,false);fb.Tick(1,false);if(fa.Phase!=Phase.Disturbance||fa.FeltDread!=fb.FeltDread)same=same&&fa.Phase!=Phase.Disturbance;rampStep=Math.Max(rampStep,Math.Abs(fa.FeltDread-rp));rp=fa.FeltDread;}
        Assert(same,"FeltDread is identical for runs that differ only in the boundary");Assert(rampStep<6.5f,"no step in FeltDread anywhere on a 0-100 ramp");
        int feltCalls=0;var quiet=new Investigation(CaseBook.Castle,()=>{feltCalls++;return .5f;});quiet.Equip();quiet.EnterHouse();for(int i=0;i<300;i++){quiet.AddDread(.3f);quiet.Tick(1,true,false,false);}
        Assert(feltCalls==3&&quiet.Phase==Phase.Survey,"FeltDread never consumes dice (only the 3 construction draws in Survey)");
        var cpA=new Investigation(CaseBook.Castle,()=>.9f);cpA.Equip();cpA.EnterHouse();cpA.Record(Evidence.Induction);var cpS=cpA.SaveCheckpoint();var cpB=new Investigation(CaseBook.Castle,()=>.1f);cpB.RestoreCheckpoint(cpS);
        Assert(Math.Abs(cpS.RunDreadMin-48)<.01f&&cpB.RunDreadMin==cpS.RunDreadMin,"the run's boundary survives a checkpoint restore");
    }

    // Wave H (critic r10b N6, N7, N10, N13): no refuge phase, felt lie episodes, drawn warnings, driver queues.
    static Investigation SurveyAfterHunt(Func<float> random)
    {
        var c=new Investigation(CaseBook.Castle,random);
        c.RestoreCheckpoint(new Investigation.Checkpoint{Evidence=new Evidence[0],Notes=new string[0],Equipped=true,BreakerOn=true,Survived=true,Hunts=1,Dread=10,RunDreadMin=40},false);return c;
    }
    static Investigation DiagnosedCastle(Func<float> random)
    {
        var c=StartOn(CaseBook.Castle,random);foreach(Evidence e in Enum.GetValues(typeof(Evidence)))c.Record(e);c.BeginWarning();
        for(int t=0;t<900&&c.Phase!=Phase.Disturbance;t++)c.Tick(1,false);foreach(var title in CaseBook.Castle.NoteTitles)c.Read(title);Correct(c);return c;
    }
    // Replays one PlaytestDriver dice step: low boundary, Dread anywhere at Disturbance entry, ForceDread, then the queue.
    static Investigation DriverStep(float boundaryRoll,float dreadAtEntry,float[] queue,out System.Collections.Generic.Queue<float> q)
    {
        var dq=new System.Collections.Generic.Queue<float>(new[]{boundaryRoll,.5f,.5f});q=dq;
        var c=new Investigation(CaseBook.Castle,()=>dq.Count>0?dq.Dequeue():.99f);c.Equip();c.EnterHouse();c.Record(Evidence.Induction);c.BeginWarning();
        for(int t=0;t<900&&c.Phase!=Phase.Disturbance;t++)c.Tick(1,false);
        c.AddDread(dreadAtEntry-c.Dread);for(int t=0;t<3;t++)c.Tick(1,false);
        c.ForceDread(Math.Max(55,c.RunDreadMin+5));foreach(var v in queue)dq.Enqueue(v);
        TickUntilLeaves(c,Phase.Disturbance,30);return c;
    }
    static void WaveHTests()
    {
        var r=HuntRules.Castle;var b=HuntRules.Bellwether;
        Assert(r.WarningSecondsMin==5&&r.WarningSeconds==9&&r.WarningSecondsMax==14&&Math.Abs(r.QuietPhaseChance-1f/80)<1e-6f&&Math.Abs(r.RecoveryLowChance-1f/16)<1e-6f,"castle: warning 5-14 s, quiet phases 1/80, recovery below 80 1/16");
        Assert(r.FeltEpisodeGapMin==200&&r.FeltEpisodeGapMax==600&&r.FeltEpisodeMin==20&&r.FeltEpisodeMax==40&&r.FeltCalmFrom==40&&r.FeltSpikeBelow==60,"castle felt episodes: 200-600 s apart, 20-40 s long");
        Assert(b.WarningSecondsMin==9&&b.WarningSecondsMax==9&&b.QuietPhaseChance==0&&b.RecoveryLowChance==0&&b.FeltEpisodeGapMax==0,"Bellwether: no wave H randomisation");

        // N7: Survey and Containment roll after the first hunt; Recovery below 80 rolls (distribution in the wave G block).
        Assert(TickUntilLeaves(StartOn(CaseBook.Castle,()=>0f),Phase.Survey,600)==600,"Survey before any hunt stays quiet even with certain dice");
        var sv=SurveyAfterHunt(()=>.005f);Assert(sv.Phase==Phase.Survey&&TickUntilLeaves(sv,Phase.Survey,30)<10&&sv.Phase==Phase.Hunt&&sv.ThreatFrom==Phase.Survey,"Survey after a hunt: a 1/80 roll hunts");
        Assert(TickUntilLeaves(SurveyAfterHunt(()=>.99f),Phase.Survey,600)==600,"Survey roll above 1/80 never hunts");
        var ct=DiagnosedCastle(()=>.99f);Assert(ct.Phase==Phase.Containment&&TickUntilLeaves(ct,Phase.Containment,600)==600,"setup: Containment with high rolls stays quiet");
        ct.Dice=()=>.005f;Assert(TickUntilLeaves(ct,Phase.Containment,30)<10&&ct.Phase==Phase.Hunt&&ct.ThreatFrom==Phase.Containment&&ct.NoTellHunt,"Containment: a 1/80 roll hunts");
        ct.Dice=()=>.99f;for(int t=0;t<200&&ct.Phase!=Phase.Containment;t++)ct.Tick(1,false);Assert(ct.Phase==Phase.Containment&&ct.Diagnosed,"after a Containment hunt the case returns to Containment");
        var cq=new System.Collections.Generic.Queue<float>();var cf=DiagnosedCastle(()=>cq.Count>0?cq.Dequeue():.99f);foreach(var v in new[]{.5f,.005f,.9f,.1f,.5f})cq.Enqueue(v);
        Assert(cf.Phase==Phase.Containment&&TickUntilLeaves(cf,Phase.Containment,30)<10&&cf.FalseWarning&&cf.CurrentWarningSeconds==9,"Containment roll can be a false alarm");
        TickUntilLeaves(cf,Phase.Warning,30);Assert(cf.Phase==Phase.Containment&&cf.Hunts==1,"a false alarm in Containment returns to Containment");
        var bc=Start();bc.Record(Evidence.Induction);bc.Record(Evidence.Handprint);bc.Record(Evidence.Voice);bc.BeginWarning();bc.Tick(9,false);bc.Tick(38,false);bc.Tick(32,false);Context(bc);Correct(bc);bc.Dice=()=>0f;
        Assert(bc.Phase==Phase.Containment&&TickUntilLeaves(bc,Phase.Containment,600)==600,"Bellwether Containment never rolls");
        int ctSessions=0,ctHunted=0;float ctWait=0;
        for(int seed=0;seed<300;seed++)
        {
            var c=DiagnosedCastle(Seeded(20000+seed));if(c.Phase!=Phase.Containment)continue;ctSessions++;
            int t=TickUntilLeaves(c,Phase.Containment,600);if(t<600){ctHunted++;ctWait+=t;}
        }
        float ctRate=ctHunted/(float)ctSessions,ctMean=ctWait/Math.Max(1,ctHunted);
        Console.WriteLine("WAVE_H containment sessions="+ctSessions+" hunted_in_10min="+ctRate.ToString("F3")+" mean_wait_s="+ctMean.ToString("F1"));
        Assert(ctSessions>=280&&ctRate>.65f&&ctRate<.97f&&ctMean>150&&ctMean<350,"Containment is rare but real: most 10-minute walks meet a hunt");

        // N10: every told or false Warning draws 5-14 s; Bellwether keeps 9 s with no draw.
        int bcalls=0;var bw=Start();bw.Dice=()=>{bcalls++;return 0f;};bw.BeginWarning();Assert(bw.CurrentWarningSeconds==9&&bcalls==0,"Bellwether warning 9 s, no draw");
        var w0=StartOn(CaseBook.Castle,()=>0f);w0.BeginWarning();var w1=StartOn(CaseBook.Castle,()=>.99999f);w1.BeginWarning();var wm=StartOn(CaseBook.Castle);wm.BeginWarning();
        Assert(w0.CurrentWarningSeconds==5&&w1.CurrentWarningSeconds>13.99f&&wm.CurrentWarningSeconds==9,"castle warning spans 5-14, 0.5 gives 9");
        var warn=new System.Collections.Generic.List<float>();bool warnOk=true;
        for(int seed=0;seed<1500;seed++)
        {
            var c=StartOn(CaseBook.Castle,Seeded(30000+seed));c.Record(Evidence.Induction);c.BeginWarning();float l=c.CurrentWarningSeconds;warn.Add(l);
            c.Tick(l-.5f,false);if(c.Phase!=Phase.Warning)warnOk=false;c.Tick(1,false);if(c.Phase!=Phase.Hunt)warnOk=false;
        }
        float wMed=Median(warn);Console.WriteLine("WAVE_H warning_s min="+warn[0].ToString("F1")+" median="+wMed.ToString("F1")+" max="+warn[warn.Count-1].ToString("F1"));
        Assert(warnOk&&warn[0]>=5&&warn[0]<5.5f&&warn[warn.Count-1]<=14&&warn[warn.Count-1]>13.5f&&Math.Abs(wMed-9)<.5f,"warning lengths spread 5-14 around 9, each lasts exactly its draw");
        var falseLen=new System.Collections.Generic.List<float>();var toldLen=new System.Collections.Generic.List<float>();
        for(int seed=0;seed<2500;seed++)
        {
            var c=StartOn(CaseBook.Castle,Seeded(40000+seed));foreach(Evidence e in Enum.GetValues(typeof(Evidence)))c.Record(e);c.AddDread(85);TickUntilLeaves(c,Phase.Disturbance,60);
            if(c.Phase==Phase.Warning)(c.FalseWarning?falseLen:toldLen).Add(c.CurrentWarningSeconds);
        }
        float fMed=Median(falseLen),tMed=Median(toldLen);Console.WriteLine("WAVE_H dice warnings false="+falseLen.Count+" median="+fMed.ToString("F2")+" told="+toldLen.Count+" median="+tMed.ToString("F2"));
        Assert(falseLen.Count>300&&toldLen.Count>900&&Math.Abs(fMed-tMed)<.6f&&falseLen[0]<6&&falseLen[falseLen.Count-1]>13&&toldLen[0]<6&&toldLen[toldLen.Count-1]>13,"false and told warnings share one length distribution");

        // N6: felt lie episodes instead of a fade-to-calm at Lies.
        var ep=StartOn(CaseBook.Castle,Seeded(50));ep.AddDread(50);var starts=new System.Collections.Generic.List<int>();string last="none";bool kinds=true;
        for(int t=0;t<6000;t++){ep.Tick(1,false);if(ep.FeltEpisode!="none"&&last=="none")starts.Add(t);if(ep.FeltEpisode!="none"&&ep.FeltEpisode!="calm"&&ep.FeltEpisode!="spike")kinds=false;last=ep.FeltEpisode;}
        bool gapsOk=starts.Count>1;for(int i=1;i<starts.Count;i++){int g=starts[i]-starts[i-1];if(g<199||g>601)gapsOk=false;}
        Console.WriteLine("WAVE_H felt_episodes_in_100min="+starts.Count+" per_10min="+(starts.Count/10f).ToString("F2"));
        Assert(kinds&&gapsOk&&ep.FeltEpisodes==starts.Count&&starts.Count>=10&&starts.Count<=30,"episodes start every 200-600 s: 1-3 per 10 minutes");
        int calmSeen=0,spikeSeen=0;
        for(int seed=0;seed<60;seed++){var m=StartOn(CaseBook.Castle,Seeded(60000+seed));m.AddDread(50);string prev="none";for(int t=0;t<1300;t++){m.Tick(1,false);if(m.FeltEpisode!=prev&&m.FeltEpisode=="calm")calmSeen++;if(m.FeltEpisode!=prev&&m.FeltEpisode=="spike")spikeSeen++;prev=m.FeltEpisode;}}
        Assert(calmSeen>20&&spikeSeen>20,"at Dread 40-60 both calm and spike episodes happen");
        bool onlyCalm=true,dipped=false,recovered=true;var hi=StartOn(CaseBook.Castle,Seeded(70));hi.AddDread(85);float endAt=-1;
        for(int t=0;t<3000;t++)
        {
            hi.Tick(1,false);if(hi.FeltEpisode=="spike")onlyCalm=false;if(hi.FeltEpisode=="calm"&&hi.FeltDread<55)dipped=true;
            if(hi.FeltEpisode=="calm")endAt=t;else if(endAt>=0&&t-endAt>100&&t-endAt<150&&Math.Abs(hi.FeltDread-85)>12.6f)recovered=false;
        }
        Assert(onlyCalm&&dipped&&recovered&&hi.FeltEpisodes>=4,"at Dread 85: calm episodes dip the pulse, then it returns to Dread");
        bool onlySpike=true;float lowMax=0;var lo=StartOn(CaseBook.Castle,Seeded(71));lo.AddDread(10);
        for(int t=0;t<3000;t++){lo.Tick(1,false);if(lo.FeltEpisode=="calm")onlySpike=false;lowMax=Math.Max(lowMax,lo.FeltDread);}
        Assert(onlySpike&&lowMax>35&&lo.Dread==10,"at Dread 10: false spikes lift the pulse well above Dread");
        var calmFloor=StartOn(CaseBook.Castle,Seeded(72));calmFloor.AddDread(85);float calmMin=100;for(int t=0;t<3000;t++){calmFloor.Tick(1,false);if(calmFloor.FeltEpisode=="none"&&t>150)calmMin=Math.Min(calmMin,calmFloor.FeltDread);}
        Assert(calmFloor.Lies&&calmMin<85,"Lies alone no longer drags the pulse to a constant");
        var bEp=Start();for(int t=0;t<3000;t++)bEp.Tick(1,false);Assert(bEp.FeltEpisodes==0&&bEp.FeltEpisode=="none"&&bEp.FeltDread==0,"Bellwether has no felt episodes");
        var late=StartOn(CaseBook.Castle,Seeded(73));late.Record(Evidence.Induction);var lateSave=late.SaveCheckpoint();lateSave.Elapsed=5000;var lateBack=new Investigation(CaseBook.Castle,Seeded(74));lateBack.RestoreCheckpoint(lateSave,false);
        int lateFirst=-1;for(int t=0;t<700&&lateFirst<0;t++){lateBack.ForceDread(50);lateBack.Tick(1,false);if(lateBack.FeltEpisode!="none")lateFirst=t;}
        Assert(lateFirst>=199&&lateFirst<=601,"a restore reschedules episodes from the restored time (no burst)");

        // N13: the driver's three queues are deterministic for any boundary and any Dread at Disturbance entry.
        System.Collections.Generic.Queue<float> dq;bool driverOk=true;string why="";
        foreach(var boundary in new[]{0f,.1f,.25f,.5f,.99f})foreach(var entry in new[]{0f,20f,32f,60f})
        {
            var told=DriverStep(boundary,entry,new[]{0f,0f,.9f,.9f,.5f,.5f},out dq);
            if(!(told.Phase==Phase.Warning&&!told.FalseWarning&&told.CurrentWarningSeconds==9&&dq.Count==1)){driverOk=false;why="told "+boundary+"/"+entry+" "+told.Phase;}
            TickUntilLeaves(told,Phase.Warning,30);if(!(told.Phase==Phase.Hunt&&!told.NoTellHunt&&told.CurrentHuntSeconds==38&&dq.Count==0)){driverOk=false;why="told hunt "+boundary+"/"+entry;}
            var fa=DriverStep(boundary,entry,new[]{0f,0f,.9f,0f,.5f},out dq);
            if(!(fa.Phase==Phase.Warning&&fa.FalseWarning&&fa.CurrentWarningSeconds==9&&dq.Count==0)){driverOk=false;why="false "+boundary+"/"+entry+" "+fa.Phase;}
            var nt=DriverStep(boundary,entry,new[]{0f,0f,0f,.5f},out dq);
            if(!(nt.Phase==Phase.Hunt&&nt.NoTellHunt&&nt.CurrentHuntSeconds==38&&dq.Count==0)){driverOk=false;why="no-tell "+boundary+"/"+entry+" "+nt.Phase;}
        }
        Assert(driverOk,"driver queues line up after ForceDread for boundaries 30-50 and any entry Dread "+why);
        bool escFlag=false;var ef=StartOn(CaseBook.Castle,()=>.1f);ef.Record(Evidence.Induction);ef.Changed+=p=>{if(p==Phase.Warning)escFlag=ef.EscalatedWarning;};
        Assert(ef.TryEscalate()&&escFlag&&ef.EscalatedWarning&&ef.CurrentWarningSeconds==1.5f,"escalation is flagged before Changed fires");TickUntilLeaves(ef,Phase.Warning,10);Assert(!ef.EscalatedWarning,"flag clears when the Warning ends");
        var shortTold=StartOn(CaseBook.Castle,()=>0f);shortTold.Record(Evidence.Induction);shortTold.BeginWarning();Assert(shortTold.CurrentWarningSeconds==5&&!shortTold.EscalatedWarning,"a short told Warning is not an escalation");
        var fd=StartOn(CaseBook.Castle);fd.ForceDread(150);Assert(fd.Dread==100,"ForceDread clamps");var fdb=Start();fdb.ForceDread(80);Assert(fdb.Dread==0,"ForceDread ignores Dread on Bellwether");
    }
}
