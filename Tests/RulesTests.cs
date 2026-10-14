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
        var low=StartOn(CaseBook.Castle,()=>0f);low.Record(Evidence.Induction);low.AddDread(39.9f);
        Assert(TickUntilLeaves(low,Phase.Disturbance,300)==300&&low.Hunts==0,"below 40 Dread: no dice, no floor, even with a certain roll");
        var band=StartOn(CaseBook.Castle,()=>.2f);band.Record(Evidence.Induction);band.AddDread(45);
        Assert(TickUntilLeaves(band,Phase.Disturbance,90)==90&&band.Hunts==0,"40-60 band: 0.125 chance never beats a 0.2 roll");
        band.AddDread(20);int waited=TickUntilLeaves(band,Phase.Disturbance,30);
        Assert(waited<=7&&band.Phase==Phase.Hunt&&band.NoTellHunt&&band.HuntActive&&band.Hunts==1&&!band.BreakerOn,"60-80 band: 0.25 chance beats 0.2; the 0.2 tell roll makes it a no-tell hunt");
        Assert(!band.Record(Evidence.Voice),"no-tell hunt still blocks records");
        var high=StartOn(CaseBook.Castle);foreach(Evidence e in Enum.GetValues(typeof(Evidence)))high.Record(e);high.AddDread(85);
        Assert(TickUntilLeaves(high,Phase.Disturbance,30)<10&&high.Phase==Phase.Warning&&!high.FalseWarning&&!high.NoTellHunt,"80+ band with three records: deterministic 0.5 roll hunts, after a real Warning");
        high.Tick(9,false);Assert(high.Phase==Phase.Hunt&&high.Hunts==1,"rolled Warning becomes a hunt after 9 s");

        // Forced floor: 240 s at Dread >= 40 since the last hunt; the clock pauses below 40.
        var floor=StartOn(CaseBook.Castle,()=>.99f);floor.Record(Evidence.Induction);floor.AddDread(45);
        Assert(TickUntilLeaves(floor,Phase.Disturbance,300)==240&&floor.Phase==Phase.Warning&&!floor.FalseWarning&&floor.WarningLength==9,"floor forces a hunt after 240 s at Dread 45");
        var pause=StartOn(CaseBook.Castle,()=>.99f);pause.Record(Evidence.Induction);pause.AddDread(45);
        Assert(TickUntilLeaves(pause,Phase.Disturbance,200)==200,"200 s at Dread 45 is not enough");pause.AddDread(-10);
        Assert(TickUntilLeaves(pause,Phase.Disturbance,100)==100,"floor clock pauses below 40");pause.AddDread(10);
        Assert(TickUntilLeaves(pause,Phase.Disturbance,100)==40,"floor clock resumes where it stopped");

        // First-hunt guarantee: two records, deadline 95 s +-25 % by roll, independent of Dread.
        foreach(var pair in new[]{new[]{.5f,95},new[]{0f,72},new[]{.99f,119}})
        {
            float roll=pair[0];var first=StartOn(CaseBook.Castle,()=>roll);first.Record(Evidence.Induction);first.Record(Evidence.Handprint);
            Assert(TickUntilLeaves(first,Phase.Disturbance,200)==(int)pair[1]&&first.Dread==0,"first hunt guaranteed at "+pair[1]+" s for roll "+roll);
        }

        // Spacing: with certain dice the next hunt never starts before Recovery (32 s) has passed.
        var spaced=StartOn(CaseBook.Castle,()=>.01f);foreach(Evidence e in Enum.GetValues(typeof(Evidence)))spaced.Record(e);spaced.AddDread(100);
        float now=0,lastEnd=-1;int hunts=0;bool gapOk=true;spaced.Changed+=p=>{if(p==Phase.Hunt){hunts++;if(lastEnd>=0&&now-lastEnd<32)gapOk=false;}if(p==Phase.Recovery)lastEnd=now;};
        for(int i=0;i<1200;i++){spaced.Tick(.5f,false);now+=.5f;}
        Assert(hunts>=5&&gapOk&&spaced.Hunts==hunts,"dice hunts repeat but never within the 32 s recovery");

        // False warning: Warning that ends back in Disturbance; records and MysteryReady treat it like Disturbance.
        var q=new System.Collections.Generic.Queue<float>();var fw=StartOn(CaseBook.Castle,()=>q.Count>0?q.Dequeue():.99f);
        fw.Record(Evidence.Induction);fw.Record(Evidence.Handprint);fw.BeginWarning();Assert(fw.Phase==Phase.Warning&&!fw.FalseWarning,"bell summons are never false alarms");
        fw.Tick(9,false);fw.Tick(38,false);fw.Tick(32,false);foreach(var title in CaseBook.Castle.NoteTitles)fw.Read(title);fw.AddDread(85);
        Assert(fw.Phase==Phase.Disturbance&&fw.Survived&&!fw.MysteryReady,"survived, two records: not ready");
        q.Enqueue(.5f);q.Enqueue(.1f);q.Enqueue(.9f);q.Enqueue(.1f); // interval, hunt roll, tell roll, false-alarm roll
        TickUntilLeaves(fw,Phase.Disturbance,30);
        Assert(fw.Phase==Phase.Warning&&fw.FalseWarning&&!fw.HuntActive&&fw.Hunts==1&&!fw.BreakerOn,"rolled false alarm: Warning that is not a hunt");
        Assert(fw.Record(Evidence.Voice)&&fw.MysteryReady,"false warning accepts records and keeps MysteryReady");
        fw.Tick(8,false);Assert(fw.Phase==Phase.Warning&&fw.FalseWarning,"false alarm lasts the full warning");
        fw.Tick(1,false);Assert(fw.Phase==Phase.Disturbance&&!fw.FalseWarning&&fw.Hunts==1&&fw.MysteryReady,"false alarm ends in Disturbance, no hunt, still ready");
        fw.BeginWarning();Assert(!fw.MysteryReady&&!fw.FalseWarning,"a real Warning blocks the journal");fw.Tick(9,false);Assert(fw.Phase==Phase.Hunt&&fw.Hunts==2,"real Warning hunts");

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
}
