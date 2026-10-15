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
        WaveITests();
        WaveJTests();
        WaveKTests();
        WaveLTests();
        WaveL3Tests();
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
    // The castle with the wave I damping off (no event decay, no hunt cap, no post-hunt grace): for tests of the
    // Recovery dice mechanism itself, which the grace now mostly covers (WaveITests asserts the interplay).
    // The castle without the wave K "decay above 60" (FeltDread mechanism tests that hold Dread at 85).
    static HuntRules NoAllDecay(HuntRules from){var r=from.Clone();r.DreadDecayAllAbove=0;return r;}
    // The castle without the wave L3 "no escalation before the first hunt" gate: for tests of the escalation mechanism itself.
    static HuntRules EscMech(HuntRules from){var r=from.Clone();r.EscalationNeedsFirstHunt=false;r.FirstHuntAfterHold=0;r.LullHoldSeconds=0;return r;}
    // The castle without the wave L3 holds (for wave I-K mechanism tests that roll dice right after the first hunt).
    static HuntRules NoHold(HuntRules from){var r=from.Clone();r.FirstHuntAfterHold=0;r.LullHoldSeconds=0;return r;}
    static HuntRules RecoveryMech(){var r=HuntRules.Castle.Clone();r.Name="Castle (wave I damping off)";r.GraceMin=0;r.GraceMax=0;r.DreadDecayRecovery=0;r.DreadDecayCalm=0;r.HuntDreadCap=0;r.GovernorGain=0;r.FirstHuntAfterHold=0;r.LullHoldSeconds=0;return r;}
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
        c.Tick(10,true,false,false);Assert(Math.Abs(c.Dread-.8f)<.001f,"dark inside rises 0.08/s (wave K, was 0.12)");
        c.Tick(10,true,true,false);Assert(Math.Abs(c.Dread-1.1f)<.001f,"lit inside rises 0.03/s (wave K, was 0.05)");
        c.Tick(10,false,false,false);Assert(Math.Abs(c.Dread-1.1f)<.001f,"outside the walls holds");
        c.Tick(2,false,false,true);Assert(Math.Abs(c.Dread-.1f)<.001f,"safe zone falls 0.5/s");c.Tick(10,false,false,true);Assert(c.Dread==0,"dread floor 0");
        c.AddDread(250);Assert(c.Dread==100,"dread ceiling 100");c.AddDread(-500);Assert(c.Dread==0,"AddDread clamps at 0");c.AddDread(float.NaN);Assert(c.Dread==0,"NaN dread ignored");
        string goal=c.Goal;c.AddDread(69.9f);Assert(!c.Lies&&!c.DoorHeld&&c.Goal==goal,"below 70 no lies; goal unchanged");
        c.AddDread(.1f);Assert(c.Lies&&!c.DoorHeld,"lies from 70");c.AddDread(20);Assert(c.Lies&&!c.DoorHeld&&c.Goal==goal&&!c.Goal.Contains("ead"),"castle door-held is a per-attempt roll (wave K), never a blanket state; dread never in Goal");

        var r=HuntRules.Castle;
        Assert(Math.Abs(r.DiceChance(45,1)-.125f)<.0001f&&Math.Abs(r.DiceChance(65,1)-.25f)<.0001f&&Math.Abs(r.DiceChance(85,3)-.625f)<.0001f&&r.DiceChance(39.9f,3)==0,"dice chance by band times evidence");

        // Dice hunts occur / do not occur by band (one record: no first-hunt guarantee; dread held outside).
        var low=StartOn(CaseBook.Castle,()=>0f);low.Record(Evidence.Induction);low.AddDread(low.RunDreadMin-.1f);
        Assert(TickUntilLeaves(low,Phase.Disturbance,300)==300&&low.Hunts==0,"before the first hunt, below the run's boundary: no dice, no floor, even with a certain roll");
        var band=StartOn(CaseBook.Castle,()=>.2f);band.Record(Evidence.Induction);band.ForceDread(45);
        Assert(TickUntilLeaves(band,Phase.Disturbance,90)==90&&band.Hunts==0,"40-60 band: 0.125 chance never beats a 0.2 roll");
        band.ForceDread(65);int waited=TickUntilLeaves(band,Phase.Disturbance,30);
        Assert(waited<=7&&band.Phase==Phase.Hunt&&band.NoTellHunt&&band.HuntActive&&band.Hunts==1&&band.BreakerOn,"60-80 band: 0.25 chance beats 0.2; the 0.2 tell roll makes it a no-tell hunt (brazier untouched)");
        Assert(band.Record(Evidence.Voice),"castle (r10b N5): a no-tell hunt does not block records (the refusal was a hunt tell)");
        var high=StartOn(CaseBook.Castle);foreach(Evidence e in Enum.GetValues(typeof(Evidence)))high.Record(e);high.AddDread(85);
        Assert(TickUntilLeaves(high,Phase.Disturbance,30)<10&&high.Phase==Phase.Warning&&!high.FalseWarning&&!high.NoTellHunt,"80+ band with three records: deterministic 0.5 roll hunts, after a real Warning");
        high.Tick(9,false);Assert(high.Phase==Phase.Hunt&&high.Hunts==1,"rolled Warning becomes a hunt after 9 s");

        // Forced floor: 240 s at Dread >= 40 since the last hunt; the clock pauses below 40.
        var floor=StartOn(CaseBook.Castle,()=>.99f);floor.Record(Evidence.Induction);floor.ForceDread(floor.RunDreadMin+5);
        Assert(TickUntilLeaves(floor,Phase.Disturbance,300)==240&&floor.Phase==Phase.Warning&&!floor.FalseWarning&&floor.CurrentWarningSeconds>=5&&floor.CurrentWarningSeconds<=14,"floor forces a hunt after 240 s just above the run's boundary");
        var pause=StartOn(CaseBook.Castle,()=>.99f);pause.Record(Evidence.Induction);pause.ForceDread(pause.RunDreadMin+5);
        Assert(TickUntilLeaves(pause,Phase.Disturbance,200)==200,"200 s above the boundary is not enough");pause.ForceDread(pause.Dread-10);
        Assert(TickUntilLeaves(pause,Phase.Disturbance,100)==100,"floor clock pauses below the boundary");pause.ForceDread(pause.Dread+10);
        Assert(TickUntilLeaves(pause,Phase.Disturbance,100)==40,"floor clock resumes where it stopped");

        // First-hunt guarantee: two records, deadline 95 s +-25 % by roll, independent of Dread.
        foreach(var pair in new[]{new[]{.5f,95},new[]{0f,60},new[]{.99f,179}}) // wave K: 60-240 s, wave L2: 60-180 s, median 95
        {
            float roll=pair[0];var first=StartOn(CaseBook.Castle,()=>roll);first.Record(Evidence.Induction);first.Record(Evidence.Handprint);
            Assert(TickUntilLeaves(first,Phase.Disturbance,300)==(int)pair[1]&&first.Dread==0,"first hunt guaranteed at "+pair[1]+" s for roll "+roll);
        }

        // Recovery below Dread 80 is no longer a guaranteed window (wave H, N7): certain dice also hunt from Recovery.
        var spaced=StartOn(CaseBook.Castle,()=>.01f,RecoveryMech());foreach(Evidence e in Enum.GetValues(typeof(Evidence)))spaced.Record(e);spaced.AddDread(79);
        int hunts=0,fromRecovery=0;spaced.Changed+=p=>{if(p==Phase.Hunt){hunts++;if(spaced.ThreatFrom==Phase.Recovery)fromRecovery++;}};
        for(int i=0;i<1200;i++)spaced.Tick(.5f,false);
        Assert(hunts>=5&&fromRecovery>=1&&spaced.Hunts==hunts,"dice hunts repeat, some of them out of Recovery below Dread 80");

        // False warning: Warning that ends back in Disturbance; records and MysteryReady treat it like Disturbance.
        var q=new System.Collections.Generic.Queue<float>();var fw=StartOn(CaseBook.Castle,()=>q.Count>0?q.Dequeue():.99f);
        fw.Record(Evidence.Induction);fw.Record(Evidence.Handprint);fw.BeginWarning();Assert(fw.Phase==Phase.Warning&&!fw.FalseWarning,"bell summons are never false alarms");
        TickUntilLeaves(fw,Phase.Warning,30);TickUntilLeaves(fw,Phase.Hunt,60);TickUntilLeaves(fw,Phase.Recovery,60);foreach(var title in CaseBook.Castle.NoteTitles)fw.Read(title);fw.ForceDread(85);
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
        var calmEsc=StartOn(CaseBook.Castle);calmEsc.Record(Evidence.Induction);Assert(!calmEsc.TryEscalate()&&calmEsc.Phase==Phase.Disturbance,"0.5 roll does not escalate (0.35 since wave K)");
        string said=null;var esc=StartOn(CaseBook.Castle,()=>.1f,EscMech(HuntRules.Castle));esc.Record(Evidence.Induction);esc.NoteApparition();esc.NoteApparition();esc.Message+=m=>said=m;
        Assert(esc.TryEscalate()&&esc.Phase==Phase.Warning&&esc.WarningLength==1.5f&&!esc.FalseWarning&&said==null,"0.1 roll escalates into a silent 1.5 s Warning");
        Assert(!esc.TryEscalate(),"no escalation during a Warning");esc.Tick(1,false);Assert(esc.Phase==Phase.Warning,"tell still running at 1 s");
        esc.Tick(.5f,false);Assert(esc.Phase==Phase.Hunt&&!esc.NoTellHunt&&esc.Hunts==1,"escalated hunt starts at 1.5 s");

        // Hostile Return: a hunt until the bridge is crossed; Die works; FinishAtVan still wins.
        var ret=SealedCastle();Assert(ret.Phase==Phase.Return&&ret.HuntActive&&ret.NoTellHunt&&ret.Hunts==2,"castle Return is a tell-less hunt");
        ret.Tick(999,true);Assert(ret.Phase==Phase.Return&&ret.HuntActive,"hostile Return does not time out");
        ret.Die();Assert(ret.Phase==Phase.Dead&&ret.Deaths==1&&!ret.HuntActive,"dying on the bridge is a kill");
        var esc2=SealedCastle();esc2.Invulnerable=true;esc2.Die();Assert(esc2.Phase==Phase.Return&&esc2.BlockedDeaths==1,"playtest invulnerability still holds in Return");
        esc2.Invulnerable=false;Assert(esc2.FinishAtVan()&&esc2.Phase==Phase.Won&&!esc2.HuntActive,"crossing the bridge wins");esc2.Tick(.02f,false);esc2.Die();Assert(esc2.Phase==Phase.Won&&esc2.Deaths==0,"no death a tick after the win (same-tick deaths: WaveKTests)");
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
        for(int t=0;t<900&&(c.Phase!=Phase.Disturbance||c.GraceRemaining>0||c.DiceHoldRemaining>0);t++)c.Tick(1,false);return c;
    }
    static void WaveGTests()
    {
        var r=HuntRules.Castle;var b=HuntRules.Bellwether;
        Assert(r.HuntDreadMin==40&&r.HuntDreadMinSpread==10&&Math.Abs(r.LowDreadChance-1f/15)<1e-6f,"castle boundary 30-50, low roll 1/15 (wave L3; 1/24 L2, 1/30 L, 1/40 before)");
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
        Assert(sessions>=395&&hitRate>.9f&&meanWait>45&&meanWait<100,"Dread 0, 10 min after the first hunt: >90 % of sessions hunt, mean wait about 70 s (1/15 roll since wave L3; was 160 s at 1/40)");
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
        var rq=new System.Collections.Generic.Queue<float>();var rdq=StartOn(CaseBook.Castle,()=>rq.Count>0?rq.Dequeue():.99f,RecoveryMech());rdq.Record(Evidence.Induction);rdq.BeginWarning();TickUntilLeaves(rdq,Phase.Warning,30);rdq.AddDread(85);
        TickUntilLeaves(rdq,Phase.Hunt,60);Assert(rdq.Phase==Phase.Recovery&&rdq.CurrentRecoverySeconds>39,"setup: long recovery at Dread 85");
        foreach(var v in new[]{.5f,.1f,.9f,.1f})rq.Enqueue(v); // interval, recovery roll under 1/8, tell, false alarm
        int inRecovery=TickUntilLeaves(rdq,Phase.Recovery,60);Assert(inRecovery<10&&rdq.Phase==Phase.Warning&&rdq.FalseWarning,"1/8 dice roll fires inside Recovery at Dread 85");
        TickUntilLeaves(rdq,Phase.Warning,30);Assert(rdq.Phase==Phase.Disturbance,"a false alarm from Recovery ends in Disturbance");
        int cut=0,trials=0;
        for(int seed=0;seed<600;seed++)
        {
            var c=StartOn(CaseBook.Castle,Seeded(9000+seed),RecoveryMech());c.Record(Evidence.Induction);c.BeginWarning();TickUntilLeaves(c,Phase.Warning,30);c.AddDread(90);
            TickUntilLeaves(c,Phase.Hunt,60);if(c.Phase!=Phase.Recovery)continue;trials++;float len=c.CurrentRecoverySeconds;int t=TickUntilLeaves(c,Phase.Recovery,60);if(t<len-.01f)cut++;
        }
        float cutRate=cut/(float)trials;Console.WriteLine("WAVE_G recovery_interrupted_at_dread90="+cutRate.ToString("F3")+" of "+trials);
        Assert(trials>550&&cutRate>.3f&&cutRate<.8f,"Recovery at Dread 90 is interrupted in a share of runs, not never");
        int lowCut=0;for(int seed=0;seed<400;seed++){var c=StartOn(CaseBook.Castle,Seeded(9900+seed),RecoveryMech());c.Record(Evidence.Induction);c.BeginWarning();TickUntilLeaves(c,Phase.Warning,30);c.AddDread(79);TickUntilLeaves(c,Phase.Hunt,60);float len=c.CurrentRecoverySeconds;if(TickUntilLeaves(c,Phase.Recovery,60)<len-.01f)lowCut++;}
        Console.WriteLine("WAVE_H recovery_interrupted_at_dread79="+(lowCut/400f).ToString("F3"));
        Assert(lowCut>40&&lowCut<220,"below Dread 80 Recovery is interrupted now and then (1/16 per roll), not never");
        string castleSaid=null;var cm=StartOn(CaseBook.Castle);cm.Record(Evidence.Induction);cm.BeginWarning();cm.Tick(9,false);cm.Message+=m=>castleSaid=m;cm.Tick(38,false);
        Assert(cm.Phase==Phase.Recovery&&castleSaid==null,"castle recovery is not announced");

        // #12 no-tell starts leave BreakerOn alone; told warnings still cut it.
        var nt=StartOn(CaseBook.Castle,()=>.1f);nt.Record(Evidence.Induction);nt.AddDread(90);TickUntilLeaves(nt,Phase.Disturbance,30);
        Assert(nt.Phase==Phase.Hunt&&nt.NoTellHunt&&nt.BreakerOn,"dice no-tell hunt keeps the brazier");
        var gamble=StartOn(CaseBook.Castle);gamble.Record(Evidence.Induction);Assert(gamble.BeginNoTellHunt()&&gamble.BreakerOn,"gamble no-tell hunt keeps the brazier");
        var lnb=StartOn(CaseBook.Castle,null,HuntRules.LongNight);lnb.BeginWarning();Assert(lnb.Phase==Phase.Hunt&&lnb.BreakerOn,"Long Night bell hunt keeps the brazier");
        var lne=StartOn(CaseBook.Castle,()=>.1f,EscMech(HuntRules.LongNight));lne.Record(Evidence.Induction);lne.NoteApparition();lne.NoteApparition();Assert(lne.TryEscalate()&&lne.Phase==Phase.Hunt&&lne.BreakerOn,"Long Night escalation keeps the brazier");
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
        var step=StartOn(CaseBook.Castle,null,noiseless);step.ForceDread(60);Assert(step.FeltDread==0,"FeltDread does not jump with Dread");
        for(int i=0;i<25;i++)step.Tick(1,false);Assert(Math.Abs(step.FeltDread-60*(1-(float)Math.Exp(-1)))<.3f,"lag: 63 % after one time constant (25 s at a 0.5 roll)");
        var lag0=StartOn(CaseBook.Castle,()=>0f,noiseless);var lag1=StartOn(CaseBook.Castle,()=>.99999f,noiseless);Assert(lag0.FeltLagSeconds==20&&lag1.FeltLagSeconds>29.99f,"lag drawn from 20-30 s");
        var felt=StartOn(CaseBook.Castle,Seeded(77));felt.AddDread(80);felt.Tick(1,false);Assert(felt.FeltDread<12+80*(1-(float)Math.Exp(-1/20f))+.1f,"one second after a jump FeltDread has barely moved");
        var noEpisodes=HuntRules.Castle.Clone();noEpisodes.FeltEpisodeGapMax=0;
        var steady=StartOn(CaseBook.Castle,Seeded(78),noEpisodes);steady.ForceDread(50);for(int i=0;i<200;i++)steady.Tick(1,false);
        float fMin=100,fMax=0,maxStep=0,prev=steady.FeltDread;bool inBand=true;
        for(int i=0;i<1200;i++){steady.Tick(1,false);float f=steady.FeltDread;if(Math.Abs(f-50)>12.05f)inBand=false;fMin=Math.Min(fMin,f);fMax=Math.Max(fMax,f);maxStep=Math.Max(maxStep,Math.Abs(f-prev));prev=f;}
        Console.WriteLine("WAVE_G felt_at_dread50 min="+fMin.ToString("F1")+" max="+fMax.ToString("F1")+" max_step_per_s="+maxStep.ToString("F2"));
        Assert(inBand&&fMax-fMin>14&&fMin<44&&fMax>56,"noise: +-12 around a steady Dread, and it really moves");
        Assert(maxStep<6.5f,"noise is slow: under 6.5 per second");
        var liar=StartOn(CaseBook.Castle,Seeded(79),NoAllDecay(noEpisodes));liar.ForceDread(85);for(int i=0;i<150;i++)liar.Tick(1,false);
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
        c.ForceDread(dreadAtEntry);for(int t=0;t<3;t++)c.Tick(1,false);
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
        var cq=new System.Collections.Generic.Queue<float>();var cf=DiagnosedCastle(()=>cq.Count>0?cq.Dequeue():.99f);cf.ForceDread(cf.Dread);foreach(var v in new[]{.5f,.005f,.9f,.1f,.5f})cq.Enqueue(v);
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
        var ep=StartOn(CaseBook.Castle,Seeded(50));ep.ForceDread(50);var starts=new System.Collections.Generic.List<int>();string last="none";bool kinds=true;
        for(int t=0;t<6000;t++){ep.Tick(1,false);if(ep.FeltEpisode!="none"&&last=="none")starts.Add(t);if(ep.FeltEpisode!="none"&&ep.FeltEpisode!="calm"&&ep.FeltEpisode!="spike")kinds=false;last=ep.FeltEpisode;}
        bool gapsOk=starts.Count>1;for(int i=1;i<starts.Count;i++){int g=starts[i]-starts[i-1];if(g<199||g>601)gapsOk=false;}
        Console.WriteLine("WAVE_H felt_episodes_in_100min="+starts.Count+" per_10min="+(starts.Count/10f).ToString("F2"));
        Assert(kinds&&gapsOk&&ep.FeltEpisodes==starts.Count&&starts.Count>=10&&starts.Count<=30,"episodes start every 200-600 s: 1-3 per 10 minutes");
        int calmSeen=0,spikeSeen=0;
        for(int seed=0;seed<60;seed++){var m=StartOn(CaseBook.Castle,Seeded(60000+seed));m.ForceDread(50);string prev="none";for(int t=0;t<1300;t++){m.Tick(1,false);if(m.FeltEpisode!=prev&&m.FeltEpisode=="calm")calmSeen++;if(m.FeltEpisode!=prev&&m.FeltEpisode=="spike")spikeSeen++;prev=m.FeltEpisode;}}
        Assert(calmSeen>20&&spikeSeen>20,"at Dread 40-60 both calm and spike episodes happen");
        bool onlyCalm=true,dipped=false,recovered=true;var hi=StartOn(CaseBook.Castle,Seeded(70),NoAllDecay(HuntRules.Castle));hi.ForceDread(85);float endAt=-1;
        for(int t=0;t<3000;t++)
        {
            hi.Tick(1,false);if(hi.FeltEpisode=="spike")onlyCalm=false;if(hi.FeltEpisode=="calm"&&hi.FeltDread<55)dipped=true;
            if(hi.FeltEpisode=="calm")endAt=t;else if(endAt>=0&&t-endAt>100&&t-endAt<150&&Math.Abs(hi.FeltDread-85)>12.6f)recovered=false;
        }
        Assert(onlyCalm&&dipped&&recovered&&hi.FeltEpisodes>=4,"at Dread 85: calm episodes dip the pulse, then it returns to Dread");
        bool onlySpike=true;float lowMax=0;var lo=StartOn(CaseBook.Castle,Seeded(71));lo.ForceDread(10);
        for(int t=0;t<3000;t++){lo.Tick(1,false);if(lo.FeltEpisode=="calm")onlySpike=false;lowMax=Math.Max(lowMax,lo.FeltDread);}
        Assert(onlySpike&&lowMax>35&&lo.Dread==10,"at Dread 10: false spikes lift the pulse well above Dread");
        var calmFloor=StartOn(CaseBook.Castle,Seeded(72),NoAllDecay(HuntRules.Castle));calmFloor.ForceDread(85);float calmMin=100;for(int t=0;t<3000;t++){calmFloor.Tick(1,false);if(calmFloor.FeltEpisode=="none"&&t>150)calmMin=Math.Min(calmMin,calmFloor.FeltDread);}
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
        bool escFlag=false;var ef=StartOn(CaseBook.Castle,()=>.1f,EscMech(HuntRules.Castle));ef.Record(Evidence.Induction);ef.NoteApparition();ef.NoteApparition();ef.Changed+=p=>{if(p==Phase.Warning)escFlag=ef.EscalatedWarning;};
        Assert(ef.TryEscalate()&&escFlag&&ef.EscalatedWarning&&ef.CurrentWarningSeconds==1.5f,"escalation is flagged before Changed fires");TickUntilLeaves(ef,Phase.Warning,10);Assert(!ef.EscalatedWarning,"flag clears when the Warning ends");
        var shortTold=StartOn(CaseBook.Castle,()=>0f);shortTold.Record(Evidence.Induction);shortTold.BeginWarning();Assert(shortTold.CurrentWarningSeconds==5&&!shortTold.EscalatedWarning,"a short told Warning is not an escalation");
        var fd=StartOn(CaseBook.Castle);fd.ForceDread(150);Assert(fd.Dread==100,"ForceDread clamps");var fdb=Start();fdb.ForceDread(80);Assert(fdb.Dread==0,"ForceDread ignores Dread on Bellwether");
    }

    // Wave I (blind critic r10 P3): Dread decay, per-hunt cap, post-hunt grace, HuntShare, and a 10-minute session model.
    sealed class SessionResult{public float ShareAfterFirst,ShareRun,PinnedSeconds,FirstAt=-1,DreadAt480=-1,MaxQuietAfterFirst,FirstDelayAfterSecondRecord=-1;public bool NoTellBelow70;public System.Collections.Generic.List<float> ApparitionTimes=new System.Collections.Generic.List<float>(),HuntStartTimes=new System.Collections.Generic.List<float>();public int Escalations,EscalationTries,Hunts,LowStarts,NoTellHunts,HuntStarts;public System.Collections.Generic.List<float> Gaps=new System.Collections.Generic.List<float>();}
    // Player model: lit inside (+0.05/s), dark for darkShare of the time (+0.12/s), a 20-40 s gatehouse visit every
    // 150-300 s of calm play (DreadRelief 3), evidence at 10-40 / 40-120 / 120-240 s, ledger read by 240 s, apparitions
    // every 60-110 s in Disturbance (+12, every second one throws +8, one in three lands close: TryEscalate). In a hunt
    // the player is seen in 60 % of hunts (DreadSeen once, +0.3/s for up to 8 s) and the hide fails in 30 % (grab survived +20).
    // immortal: the r11 runtime driver: invulnerable, standing in the open, Dread pinned at 100 (ForceDread at the start,
    // seen every hunt, a grab every hunt), never in the gatehouse.
    // driver (wave K): the r11 natural route as measured: invulnerable, torch on, standing in the open, starting at Dread 0,
    // seen every hunt and grabbed every hunt (the cap holds it to +25), never in the gatehouse.
    // Apparitions (wave L): the castle ScareRhythm clock (UseCastleClock), checked every tick, only in Disturbance/Recovery outside the
    // gatehouse, 20-40 s rest after each Recovery (ScareDirector); each visible apparition calls NoteApparition, and a share closeShare of
    // them land close enough to try TryEscalate(dark).
    static float SessionCloseShare=1/3f;
    // Wave L2: evidence timing. false = a slower investigator (records at 10-40 / 40-120 / 120-240 s); true = the critic's natural
    // route (kit, letter, three records and the ledger by about 13 s: records at 5-15 s).
    static bool SessionCriticRoute=false;
    static SessionResult Session(HuntRules rules,int seed,float darkShare,bool immortal=false,bool driver=false,RunPlan plan=null)
    {
        var res=new SessionResult();var dice=new Random(seed*31+7);var p=new Random(seed*17+3);
        var c=new Investigation(CaseBook.Castle,()=>(float)dice.NextDouble(),rules);c.Equip();c.EnterHouse();if(immortal)c.ForceDread(100);
        float[] ev={10+30*(float)p.NextDouble(),40+80*(float)p.NextDouble(),120+120*(float)p.NextDouble()};
        if(SessionCriticRoute){ev[0]=5+3*(float)p.NextDouble();ev[1]=8+3*(float)p.NextDouble();ev[2]=11+4*(float)p.NextDouble();}int evi=0,apps=0;
        float nextApp=40+60*(float)p.NextDouble(),nextGate=150+100*(float)p.NextDouble(),gateLeft=0,darkSwitch=20,seenLeft=0,grabAt=-1,t=0,dt=.5f,threat=0,lastEnd=-1;
        bool dark=false,seenGiven=false,grabGiven=false,inThreat=false;var rhythm=new ScareRhythm();var ar=new Random(seed*13+5);
        // Wave L3: the block model. Apparitions only outside CastleBlocked (Warning, Hunt, Recovery, grace) and the gatehouse; an expired due
        // time inside a block draws a fresh burst gap when the block lifts; each lull holds the dice for 120 s (HoldDice).
        if(plan!=null)c.UseRunPlan(plan);rhythm.UseCastleClock(()=>(float)ar.NextDouble(),plan!=null?plan.BurstStart:ar.NextDouble()<.5);if(rhythm.TakeLullStart())c.HoldDiceForLull();
        c.Changed+=ph=>{bool th=ph==Phase.Warning||ph==Phase.Hunt;
            if(th&&!inThreat){if(res.FirstAt<0)res.FirstAt=t;if(lastEnd>=0)res.Gaps.Add(t-lastEnd);inThreat=true;if(c.Dread<c.RunDreadMin)res.LowStarts++;}
            if(!th&&inThreat){inThreat=false;lastEnd=t;}
            if(ph==Phase.Hunt){res.HuntStarts++;res.HuntStartTimes.Add(t);if(c.NoTellHunt){res.NoTellHunts++;if(c.Dread<70)res.NoTellBelow70=true;}if(res.FirstDelayAfterSecondRecord<0&&evi>=2)res.FirstDelayAfterSecondRecord=t-ev[1];}
            if(ph==Phase.Hunt){seenLeft=immortal||driver?999:p.NextDouble()<.6?(float)(p.NextDouble()*8):0;seenGiven=false;grabGiven=false;grabAt=immortal||driver?10:p.NextDouble()<.3?(float)(5+p.NextDouble()*20):-1;}};
        while(t<600)
        {
            t+=dt;
            while(evi<3&&t>=ev[evi]){c.Record((Evidence)evi);evi++;}
            if((SessionCriticRoute?t>13:t>240)&&c.ContextCount<3)foreach(var n in CaseBook.Castle.NoteTitles)c.Read(n);
            darkSwitch-=dt;if(darkSwitch<=0){dark=p.NextDouble()<darkShare;darkSwitch=10+20*(float)p.NextDouble();}
            bool calm=c.Phase==Phase.Disturbance||c.Phase==Phase.Survey||c.Phase==Phase.Recovery;
            if(gateLeft>0)gateLeft-=dt;else if(!immortal&&!driver&&calm&&t>=nextGate){gateLeft=20+20*(float)p.NextDouble();nextGate=t+150+150*(float)p.NextDouble();}
            bool safe=gateLeft>0;c.DreadRelief=safe?3:1;
            if(c.Phase==Phase.Hunt)
            {
                if(seenLeft>0){seenLeft-=dt;c.AddDread(.3f*dt);if(!seenGiven){seenGiven=true;c.AddDread(c.Rules.DreadSeen);}}
                if(grabAt>=0&&c.PhaseTime>=grabAt&&!grabGiven){grabGiven=true;c.AddDread(c.Rules.DreadGrabSurvived);}
            }
            bool blocked=ScareRhythm.CastleBlocked(c.Phase,c.GraceRemaining,false,false)||safe;rhythm.Tick(dt,blocked);
            if(!blocked&&(c.Phase==Phase.Disturbance||c.Phase==Phase.Survey)&&rhythm.AppearanceDue)
            {
                c.NoteApparition();res.ApparitionTimes.Add(t);rhythm.RecordAppearance(0);if(rhythm.TakeLullStart())c.HoldDiceForLull();apps++;if(apps%2==0)c.AddDread(c.Rules.DreadThrow);
                if(p.NextDouble()<SessionCloseShare){res.EscalationTries++;if(c.TryEscalate(dark))res.Escalations++;}
            }
            c.Tick(dt,!safe,!dark,safe);
            if(inThreat)threat+=dt;
            if(c.Dread>=95)res.PinnedSeconds+=dt;
            if(res.DreadAt480<0&&t>=480)res.DreadAt480=c.Dread;
        }
        foreach(var g in res.Gaps)res.MaxQuietAfterFirst=Math.Max(res.MaxQuietAfterFirst,g);
        if(!inThreat&&lastEnd>=0)res.MaxQuietAfterFirst=Math.Max(res.MaxQuietAfterFirst,600-lastEnd);
        res.ShareAfterFirst=res.FirstAt<0?0:threat/(600-res.FirstAt);res.ShareRun=c.HuntShare;res.Hunts=c.Hunts;return res;
    }
    static float Quantile(System.Collections.Generic.List<float> v,float q){v.Sort();return v[Math.Min(v.Count-1,(int)(q*v.Count))];}
    static string SessionReport(string name,HuntRules rules,float dark,out float meanAfter,out float runP50,out float runP90,out float pooledCv,out System.Collections.Generic.List<float> gaps,out float pinnedPerRun,out int low,out int threats)
    {
        var after=new System.Collections.Generic.List<float>();var run=new System.Collections.Generic.List<float>();gaps=new System.Collections.Generic.List<float>();var cvs=new System.Collections.Generic.List<float>();
        float pinned=0,sumAfter=0;low=0;threats=0;int hunts=0;
        for(int seed=0;seed<2000;seed++)
        {
            var r=Session(rules,seed,dark);after.Add(r.ShareAfterFirst);run.Add(r.ShareRun);sumAfter+=r.ShareAfterFirst;gaps.AddRange(r.Gaps);pinned+=r.PinnedSeconds;low+=r.LowStarts;threats+=r.Gaps.Count+(r.FirstAt>=0?1:0);hunts+=r.Hunts;
            if(r.Gaps.Count>=3){float m=0;foreach(var g in r.Gaps)m+=g;m/=r.Gaps.Count;float sd=0;foreach(var g in r.Gaps)sd+=(g-m)*(g-m);cvs.Add((float)Math.Sqrt(sd/r.Gaps.Count)/m);}
        }
        meanAfter=sumAfter/2000;runP50=Quantile(run,.5f);runP90=Quantile(run,.9f);pinnedPerRun=pinned/2000;
        float gm=0;foreach(var g in gaps)gm+=g;gm/=Math.Max(1,gaps.Count);float gsd=0;foreach(var g in gaps)gsd+=(g-gm)*(g-gm);pooledCv=gaps.Count>0?(float)Math.Sqrt(gsd/gaps.Count)/gm:0;
        return "WAVE_I "+name+" share_after_first_hunt mean="+meanAfter.ToString("F3")+" p10="+Quantile(after,.1f).ToString("F3")+" p50="+Quantile(after,.5f).ToString("F3")+" p90="+Quantile(after,.9f).ToString("F3")
            +" | HuntShare_10min p50="+runP50.ToString("F3")+" p90="+runP90.ToString("F3")
            +" | gaps n="+gaps.Count+" p10="+Quantile(gaps,.1f).ToString("F0")+" p50="+Quantile(gaps,.5f).ToString("F0")+" p90="+Quantile(gaps,.9f).ToString("F0")+" max="+Quantile(gaps,1f).ToString("F0")
            +" cv_pooled="+pooledCv.ToString("F2")+" cv_seed_median="+(cvs.Count>0?Quantile(cvs,.5f):0).ToString("F2")
            +" | hunts/run="+(hunts/2000f).ToString("F1")+" starts_below_boundary="+low+"/"+threats+" dread>=95 s/run="+pinnedPerRun.ToString("F1");
    }
    // Wave J: pacing governor.
    // Wave K: the r11 critic's natural-play test, as a 2,000-seed model for the mortal player and the r11 driver.
    static string WaveKReport(string name,HuntRules rules,bool driver,out float shareMean,out float shareP90,out float quiet120,out float dread480P90,out float noTell70,out float delaySpread)
    {
        var share=new System.Collections.Generic.List<float>();var d480=new System.Collections.Generic.List<float>();var delay=new System.Collections.Generic.List<float>();var maxq=new System.Collections.Generic.List<float>();
        int q120=0,nt70=0,ran=0,noTell=0,starts=0,esc=0,escTries=0;float sum=0;
        for(int seed=0;seed<2000;seed++)
        {
            var r=Session(rules,seed,0,false,driver);ran++;share.Add(r.ShareRun);sum+=r.ShareRun;d480.Add(r.DreadAt480);maxq.Add(r.MaxQuietAfterFirst);
            if(r.MaxQuietAfterFirst>=120)q120++;if(r.NoTellBelow70)nt70++;if(r.FirstDelayAfterSecondRecord>=0)delay.Add(r.FirstDelayAfterSecondRecord);noTell+=r.NoTellHunts;starts+=r.HuntStarts;esc+=r.Escalations;escTries+=r.EscalationTries;
        }
        shareMean=sum/ran;shareP90=Quantile(share,.9f);quiet120=q120/(float)ran;dread480P90=Quantile(d480,.9f);noTell70=nt70/(float)ran;delaySpread=Quantile(delay,.9f)-Quantile(delay,.1f);
        return "WAVE_K "+name+" share(warning+hunt+return / 600 s) mean="+shareMean.ToString("F3")+" p50="+Quantile(share,.5f).ToString("F3")+" p90="+shareP90.ToString("F3")
            +" | runs with a quiet stretch >=120 s after the first hunt="+quiet120.ToString("F3")+" (max quiet p50="+Quantile(maxq,.5f).ToString("F0")+" s)"
            +" | Dread@480 p50="+Quantile(d480,.5f).ToString("F1")+" p90="+dread480P90.ToString("F1")+" max="+Quantile(d480,1f).ToString("F1")
            +" | no-tell hunts "+noTell+"/"+starts+", runs with a no-tell hunt below Dread 70="+noTell70.ToString("F3")
            +" | escalations "+esc+"/"+escTries+" close apparitions ("+(esc/(float)ran).ToString("F2")+" per run)"
            +" | first hunt after 2nd record p10="+Quantile(delay,.1f).ToString("F0")+" p50="+Quantile(delay,.5f).ToString("F0")+" p90="+Quantile(delay,.9f).ToString("F0")+" s";
    }
    // Wave L (r12 critic): unchain hunts from the apparition clock.
    static float realized12,poolSizes;
    static string WaveLReport(string name,float closeShare,out float within10,out float appCv,out float firstSpread3,out float shareMean,out float shareP10,out float shareP90,out float gap120,out bool capHeld)
    {
        var perRun=new System.Collections.Generic.List<System.Collections.Generic.List<float>>();poolSizes=0;
        float saved=SessionCloseShare;SessionCloseShare=closeShare;
        int hunts=0,near=0,q120=0;var share=new System.Collections.Generic.List<float>();var inter=new System.Collections.Generic.List<float>();var first=new System.Collections.Generic.List<float>();float sum=0;capHeld=true;int esc=0;
        for(int seed=0;seed<2000;seed++)
        {
            var r=Session(HuntRules.Castle,seed,0);share.Add(r.ShareRun);sum+=r.ShareRun;if(r.MaxQuietAfterFirst>=120)q120++;esc+=r.Escalations;
            var mine=new System.Collections.Generic.List<float>();for(int i=1;i<r.ApparitionTimes.Count;i++){inter.Add(r.ApparitionTimes[i]-r.ApparitionTimes[i-1]);mine.Add(r.ApparitionTimes[i]-r.ApparitionTimes[i-1]);}perRun.Add(mine);
            if(r.HuntStartTimes.Count>0)first.Add(r.HuntStartTimes[0]);
            foreach(var h in r.HuntStartTimes){hunts++;float last=float.NegativeInfinity;foreach(var a in r.ApparitionTimes)if(a<=h)last=a;if(h-last<=10)near++;}
        }
        SessionCloseShare=saved;
        within10=near/(float)Math.Max(1,hunts);float m=0;foreach(var v in inter)m+=v;m/=inter.Count;float sd=0;foreach(var v in inter)sd+=(v-m)*(v-m);appCv=(float)Math.Sqrt(sd/inter.Count)/m;
        int wide=0,triples=0;for(int i=0;i+2<first.Count;i+=3){triples++;float lo=Math.Min(first[i],Math.Min(first[i+1],first[i+2])),hi=Math.Max(first[i],Math.Max(first[i+1],first[i+2]));if(hi-lo>=45)wide++;}
        firstSpread3=wide/(float)triples;
        // Realized clock as the critic measures it: the apparition intervals of three consecutive runs pooled (about 12), CV of that set.
        realized12=0;int sets=0;for(int i=0;i+2<perRun.Count;i+=3){var pool=new System.Collections.Generic.List<float>();pool.AddRange(perRun[i]);pool.AddRange(perRun[i+1]);pool.AddRange(perRun[i+2]);if(pool.Count<6)continue;sets++;
            float pm=0;foreach(var v in pool)pm+=v;pm/=pool.Count;float ps=0;foreach(var v in pool)ps+=(v-pm)*(v-pm);if((float)Math.Sqrt(ps/pool.Count)/pm>=.5f)realized12++;poolSizes+=pool.Count;}
        realized12/=Math.Max(1,sets);poolSizes/=Math.Max(1,sets);shareMean=sum/2000;shareP10=Quantile(share,.1f);shareP90=Quantile(share,.9f);gap120=q120/2000f;
        return "WAVE_L "+name+" hunts starting <=10 s after an apparition="+within10.ToString("F3")+" ("+near+"/"+hunts+") | apparition inter-arrival n="+inter.Count+" mean="+m.ToString("F0")+" CV="+appCv.ToString("F2")
            +" | 3-run pooled sets (mean "+poolSizes.ToString("F1")+" intervals) with CV>=0.5="+realized12.ToString("F3")
            +" | first hunt p10="+Quantile(first,.1f).ToString("F0")+" p50="+Quantile(first,.5f).ToString("F0")+" p90="+Quantile(first,.9f).ToString("F0")+" s, 3-run sets spanning >=45 s="+firstSpread3.ToString("F3")
            +" | share mean="+shareMean.ToString("F3")+" p10="+shareP10.ToString("F3")+" p90="+shareP90.ToString("F3")+" | runs with a >=120 s gap="+gap120.ToString("F3")+" | escalations/run="+(esc/2000f).ToString("F2");
    }
    static void WaveLTests()
    {
        var r=HuntRules.Castle;var b=HuntRules.Bellwether;
        Assert(r.EscalationLitChance==.15f&&r.EscalationDarkChance==.25f&&r.ApparitionDiceShadowSeconds==20&&r.DoorHeldBag==5,"castle wave L values");
        Assert(b.EscalationLitChance==0&&b.EscalationDarkChance==0&&b.ApparitionDiceShadowSeconds==0&&b.DoorHeldBag==0&&b.NearMissOpenChance(100)==0,"Bellwether: no wave L changes");
        // 1. Apparition clock: pure inter-arrival draw, 10,000 samples.
        var rng=new Random(4242);var xs=new System.Collections.Generic.List<float>();float sum=0;
        for(int i=0;i<10000;i++){float x=ScareRhythm.CastleInterval((float)rng.NextDouble(),(float)rng.NextDouble());xs.Add(x);sum+=x;}
        float mean=sum/xs.Count,sd=0;foreach(var x in xs)sd+=(x-mean)*(x-mean);float cv=(float)Math.Sqrt(sd/xs.Count)/mean;
        Console.WriteLine("WAVE_L CastleInterval 10k mean="+mean.ToString("F1")+" CV="+cv.ToString("F3")+" min="+Quantile(xs,0).ToString("F1")+" p10="+Quantile(xs,.1f).ToString("F0")+" p50="+Quantile(xs,.5f).ToString("F0")+" p90="+Quantile(xs,.9f).ToString("F0")+" max="+Quantile(xs,1).ToString("F1"));
        Assert(mean>100&&mean<120&&cv>=.7f&&Quantile(xs,0)>=45&&Quantile(xs,1)<=300&&Quantile(xs,1)>280,"inter-arrival mean about 110 s, CV >= 0.7 (wave L2, was 0.54), within 45-300 s");
        Assert(ScareRhythm.CastleInterval(.1f,.3f)==ScareRhythm.CastleInterval(.1f,.3f)&&ScareRhythm.CastleInterval(0,0)==45&&ScareRhythm.CastleInterval(.6f,0)==130&&ScareRhythm.CastleInterval(.9f,.99999f)<=300&&ScareRhythm.CastleInterval(float.NaN,.5f)>=45,"pure, bounded, NaN-safe");
        // Wave L2: the critic measures ~12 intervals over three runs. 12-sample CV of the pure draw (10,000 groups of 12).
        int cv12ok=0;var g12=new Random(1212);var cv12=new System.Collections.Generic.List<float>();
        for(int k=0;k<10000;k++){float m12=0,s12=0;var w=new float[12];for(int i=0;i<12;i++){w[i]=ScareRhythm.CastleInterval((float)g12.NextDouble(),(float)g12.NextDouble());m12+=w[i];}m12/=12;foreach(var x in w)s12+=(x-m12)*(x-m12);float c12=(float)Math.Sqrt(s12/12)/m12;cv12.Add(c12);if(c12>=.5f)cv12ok++;}
        Console.WriteLine("WAVE_L2 12-sample CV of the pure draw: P(CV>=0.5)="+(cv12ok/10000f).ToString("F3")+" p10="+Quantile(cv12,.1f).ToString("F2")+" p50="+Quantile(cv12,.5f).ToString("F2"));
        Assert(cv12ok>=9000,"12 samples of the pure draw give CV >= 0.5 in >= 90 % of groups");
        // (wave L3: the castle clock is bursts and lulls; CastleInterval above stays as a pure function)
        var firstBurst=new System.Collections.Generic.List<float>();var firstLull=new System.Collections.Generic.List<float>();var rr=new Random(7);
        for(int i=0;i<2000;i++){var rh=new ScareRhythm();rh.UseCastleClock(()=>(float)rr.NextDouble(),i%2==0);int t=0;while(!rh.AppearanceDue&&t<400){rh.Tick(1);t++;}(i%2==0?firstBurst:firstLull).Add(t);}
        Assert(Quantile(firstBurst,0)>=40&&Quantile(firstBurst,1)<=56&&Quantile(firstLull,0)>=150&&Quantile(firstLull,1)<=281,"first castle apparition: 40-55 s into a burst start, 150-280 s into a lull start");
        var rc=new ScareRhythm();rc.UseCastleClock(()=>.5f,true);while(!rc.AppearanceDue)rc.Tick(1);rc.RecordAppearance(.9f);rc.Tick(39.9f);Assert(!rc.CanSurprise,"castle spacing 40 s");rc.Tick(.2f);Assert(rc.CanSurprise,"spacing ends at 40 s");
        var bwr=new ScareRhythm();bwr.RecordAppearance(0);Assert(bwr.Spacing==90&&bwr.LastInterval==0,"Bellwether rhythm unchanged without UseCastleClock");
        // 2. Escalation policy.
        var e=StartOn(CaseBook.Castle,()=>0f,EscMech(HuntRules.Castle));e.Record(Evidence.Induction);Assert(e.EscalationBlock(false)=="first_apparition"&&!e.TryEscalate(true),"no apparition noted yet: blocked");
        e.NoteApparition();Assert(e.Apparitions==1&&e.EscalationChanceNow(true)==0&&e.EscalationBlock(true)=="first_apparition"&&!e.TryEscalate(true),"never on the run's first apparition, even with a certain roll");
        e.NoteApparition();Assert(e.EscalationChanceNow(false)==.15f&&e.EscalationChanceNow(true)==.25f&&e.TryEscalate(true)&&e.EscalatedWarning,"second apparition: 15 % lit / 25 % dark");
        TickUntilLeaves(e,Phase.Warning,5);Assert(e.Phase==Phase.Hunt&&e.LastHuntFromEscalation,"the hunt is marked escalation-born");
        for(int t=0;t<400&&e.Phase!=Phase.Disturbance||e.GraceRemaining>0&&t<400;t++)e.Tick(1,false);
        e.NoteApparition();Assert(e.EscalationBlock(false)=="escalation_cap"&&!e.TryEscalate(false),"after an escalation-born hunt the next hunt cannot be one");
        e.BeginNoTellHunt();Assert(!e.LastHuntFromEscalation,"a gamble hunt is not escalation-born");for(int t=0;t<400&&(e.Phase!=Phase.Disturbance||e.GraceRemaining>0);t++)e.Tick(1,false);
        e.NoteApparition();Assert(e.EscalationBlock(false)=="escalation_cap","still blocked: one of the last two hunts was escalation-born");
        e.BeginNoTellHunt();for(int t=0;t<400&&(e.Phase!=Phase.Disturbance||e.GraceRemaining>0);t++)e.Tick(1,false);
        e.NoteApparition();Assert(e.EscalationBlock(false)=="ok"&&e.TryEscalate(false),"after two other hunts escalation is possible again");
        var g=StartOn(CaseBook.Castle,()=>0f);g.Record(Evidence.Induction);g.NoteApparition();g.NoteApparition();g.BeginNoTellHunt();for(int t=0;t<60&&g.Phase==Phase.Hunt;t++)g.Tick(1,false);
        Assert(g.EscalationBlock(true)=="grace"&&!g.TryEscalate(true),"never during the grace");
        bool capOk=true;int escBorn=0,huntsSeen=0;
        for(int seed=0;seed<500;seed++)
        {
            var c=StartOn(CaseBook.Castle,Seeded(95000+seed));c.Record(Evidence.Induction);c.Record(Evidence.Handprint);var born=new System.Collections.Generic.List<bool>();c.Changed+=ph=>{if(ph==Phase.Hunt)born.Add(c.LastHuntFromEscalation);};
            for(int t=0;t<1800;t++){if(t%20==0&&(c.Phase==Phase.Disturbance||c.Phase==Phase.Recovery)){c.NoteApparition();c.TryEscalate(t%40==0);}c.Tick(1,false);}
            for(int i=0;i+2<born.Count;i++)if((born[i]?1:0)+(born[i+1]?1:0)+(born[i+2]?1:0)>1)capOk=false;foreach(var x in born){huntsSeen++;if(x)escBorn++;}
        }
        Assert(capOk&&escBorn>50,"over 500 half-hour runs with an apparition every 20 s: at most 1 escalation-born hunt in any 3 consecutive ("+escBorn+"/"+huntsSeen+")");
        // 3. The dice ignore a fresh apparition's Dread for 20 s.
        var dd=StartOn(CaseBook.Castle);dd.Record(Evidence.Induction);dd.BeginNoTellHunt();for(int t=0;t<400&&(dd.Phase!=Phase.Disturbance||dd.GraceRemaining>0);t++)dd.Tick(1,false);
        dd.Dice=()=>0f;dd.ForceDread(dd.RunDreadMin-1);dd.NoteApparition();Assert(dd.Dread>=dd.RunDreadMin&&dd.DiceDread<dd.RunDreadMin,"right after an apparition the dice read the Dread without its bump");
        dd.Tick(19,false);Assert(dd.DiceDread<dd.RunDreadMin,"for 20 s");dd.Tick(1.5f,false);Assert(dd.DiceDread==dd.Dread,"then the bump counts (if it has not decayed)");
        int bandedFromBump=0;
        for(int seed=0;seed<400;seed++){var c=StartOn(CaseBook.Castle,Seeded(96000+seed));c.Record(Evidence.Induction);c.BeginNoTellHunt();for(int t=0;t<400&&(c.Phase!=Phase.Disturbance||c.GraceRemaining>0);t++)c.Tick(1,false);
            c.Dice=()=>.09f;c.ForceDread(c.RunDreadMin-2);int hunts=c.Hunts;c.NoteApparition();for(int t=0;t<19;t++)c.Tick(1,false);if(c.Hunts>hunts||c.Phase==Phase.Warning)bandedFromBump++;}
        Assert(bandedFromBump==0,"a roll that only the banded chance would pass never fires inside the 20 s after an apparition (0.09 > 1/15 low roll)");
        // 4. Door-held bag: exactly one in every five eligible attempts; the first five always include one.
        bool firstFive=true;int heldTotal=0;
        for(int seed=0;seed<1000;seed++){var c=StartOn(CaseBook.Castle,Seeded(97000+seed));c.Record(Evidence.Induction);c.ForceDread(65);int h=0;for(int i=0;i<5;i++)if(c.TryDoorHeld())h++;if(h!=1)firstFive=false;heldTotal+=h;}
        Assert(firstFive&&heldTotal==1000,"any 5 hides at Dread >= 60 from the start include exactly one held door");
        var low=StartOn(CaseBook.Castle,()=>0f);low.Record(Evidence.Induction);low.ForceDread(59);for(int i=0;i<10;i++)low.TryDoorHeld();Assert(low.DoorAttempts==0&&low.DoorsHeld==0,"attempts below 60 neither hold nor use the bag");
        // 5. Near-miss open chance for Presence (F2).
        Assert(Math.Abs(r.NearMissOpenChance(0)-.02f)<1e-5f&&Math.Abs(r.NearMissOpenChance(40)-.05f)<1e-5f&&Math.Abs(r.NearMissOpenChance(60)-.15f)<1e-5f&&Math.Abs(r.NearMissOpenChance(80)-.25f)<1e-5f&&Math.Abs(r.NearMissOpenChance(100)-.4f)<1e-5f&&r.NearMissOpenChance(float.NaN)==.02f,"near-miss open: 5 % at 40, 25 % at 80, 40 % at 100");
        // 6. The r12 pass tests, as 2,000-seed models (one in three apparitions close, and every apparition close), on the critic's natural
        // route (records by ~15 s) as the build measures them, and on a slower investigator (records 10-240 s).
        SessionCriticRoute=true;
        string a=WaveLReport("critic route, close 1/3",1/3f,out float w10,out float acv,out float fs3,out float sm,out float s10,out float s90,out float g120,out _);float r12a=realized12;Console.WriteLine(a);
        string c2=WaveLReport("critic route, all close",1f,out float w10b,out float acvb,out float fs3b,out float smb,out float s10b,out float s90b,out float g120b,out _);float r12b=realized12;Console.WriteLine(c2);
        SessionCriticRoute=false;
        string d=WaveLReport("slow investigator, close 1/3",1/3f,out float w10c,out float acvc,out _,out float smc,out float s10c,out _,out float g120c,out _);Console.WriteLine(d);
        string e2=WaveLReport("slow investigator, all close",1f,out float w10d,out _,out _,out float smd,out float s10d,out _,out float g120d,out _);Console.WriteLine(e2);
        Assert(w10<=.35f&&w10b<=.35f&&w10c<=.35f&&w10d<=.35f,"<= 35 % of hunts start within 10 s after an apparition (both routes)");
        Assert(acv>=.5f&&acvb>=.5f&&acvc>=.5f&&r12a>=.9f&&r12b>=.9f,"apparition CV >= 0.5, and >= 90 % of 3-run pooled sets (about 12 intervals) reach CV 0.5");
        Assert(fs3>=.8f&&fs3b>=.8f,"three runs' first hunts span >= 45 s in >= 80 % of 3-run sets");
        Assert(sm>=.15f&&sm<=.30f&&s90<=.30f&&smb>=.15f&&smb<=.30f&&s90b<=.30f&&s10>=.13f&&s10b>=.13f,"critic route: share 15-30 %, p10 >= 0.13");
        Assert(g120>=.9f&&g120b>=.9f&&g120c>=.9f&&g120d>=.9f,"a >= 120 s gap in >= 90 % of runs (both routes)");
        Assert(smc>=.15f&&smd>=.15f,"slow investigator: mean share >= 15 % (its p10 is reported, not asserted)");
    }
    // Wave L3: block model, bursts and lulls, cross-run plans, lull dice hold. Consecutive seeds are consecutive runs sharing a RunHistory.
    static string WaveL3Report(string name,float closeShare,out float cv50,out float firstSpread,out float everyGap,out float shareMean,out float shareP10,out float within10)
    {
        float saved=SessionCloseShare;SessionCloseShare=closeShare;var history=new RunHistory();var pr=new Random(31337);
        var runs=new System.Collections.Generic.List<SessionResult>();var share=new System.Collections.Generic.List<float>();float sum=0;int hunts=0,near=0,gapOk=0;var pooledCv=new System.Collections.Generic.List<float>();
        var maxq=new System.Collections.Generic.List<float>();
        for(int seed=0;seed<2000;seed++)
        {
            var plan=history.Next((float)pr.NextDouble(),(float)pr.NextDouble());var r=Session(HuntRules.Castle,seed,0,false,false,plan);history.Push(plan);runs.Add(r);
            share.Add(r.ShareRun);sum+=r.ShareRun;maxq.Add(r.MaxQuietAfterFirst);if(r.HuntStartTimes.Count==0||r.MaxQuietAfterFirst>=120)gapOk++;
            foreach(var h in r.HuntStartTimes){hunts++;float last=float.NegativeInfinity;foreach(var a in r.ApparitionTimes)if(a<=h)last=a;if(h-last<=10)near++;}
        }
        SessionCloseShare=saved;
        int sets=0,cvOk=0;for(int i=0;i+2<runs.Count;i+=3){var pool=new System.Collections.Generic.List<float>();for(int k=i;k<i+3;k++)for(int j=1;j<runs[k].ApparitionTimes.Count;j++)pool.Add(runs[k].ApparitionTimes[j]-runs[k].ApparitionTimes[j-1]);
            if(pool.Count<4)continue;sets++;float pm=0;foreach(var v in pool)pm+=v;pm/=pool.Count;float ps=0;foreach(var v in pool)ps+=(v-pm)*(v-pm);float cv=(float)Math.Sqrt(ps/pool.Count)/pm;pooledCv.Add(cv);if(cv>=.5f)cvOk++;}
        int tri=0,wide=0;for(int i=0;i+2<runs.Count;i++){if(runs[i].HuntStartTimes.Count==0||runs[i+1].HuntStartTimes.Count==0||runs[i+2].HuntStartTimes.Count==0)continue;tri++;
            float a0=runs[i].HuntStartTimes[0],a1=runs[i+1].HuntStartTimes[0],a2=runs[i+2].HuntStartTimes[0];if(Math.Max(a0,Math.Max(a1,a2))-Math.Min(a0,Math.Min(a1,a2))>=45)wide++;}
        cv50=cvOk/(float)Math.Max(1,sets);firstSpread=wide/(float)Math.Max(1,tri);everyGap=gapOk/2000f;shareMean=sum/2000;shareP10=Quantile(share,.1f);within10=near/(float)Math.Max(1,hunts);
        var firsts=new System.Collections.Generic.List<float>();int resched=0,apps=0;foreach(var r in runs){if(r.HuntStartTimes.Count>0)firsts.Add(r.HuntStartTimes[0]);apps+=r.ApparitionTimes.Count;}
        return "WAVE_L3 "+name+" | 3-run pooled apparition CV p10="+Quantile(pooledCv,.1f).ToString("F2")+" p50="+Quantile(pooledCv,.5f).ToString("F2")+", sets with CV>=0.5="+cv50.ToString("F3")+" (apparitions/run "+(apps/2000f).ToString("F1")+")"
            +" | first hunt p10/p50/p90="+Quantile(firsts,.1f).ToString("F0")+"/"+Quantile(firsts,.5f).ToString("F0")+"/"+Quantile(firsts,.9f).ToString("F0")+" s, consecutive 3-run windows spanning >=45 s="+firstSpread.ToString("F3")
            +" | runs with a >=120 s gap after the first hunt="+everyGap.ToString("F4")+" (min longest gap "+Quantile(maxq,0).ToString("F0")+" s)"
            +" | share mean="+shareMean.ToString("F3")+" p10="+shareP10.ToString("F3")+" p90="+Quantile(share,.9f).ToString("F3")+" | hunts <=10 s after an apparition="+within10.ToString("F3");
    }
    static void WaveL3Tests()
    {
        // Block model: a due time that expires inside a block is not fired at the block's end; a fresh 40-55 s gap is drawn.
        var b=new ScareRhythm();b.UseCastleClock(()=>.5f,true);float due=b.DueIn;b.Tick(due+30,true);Assert(!b.AppearanceDue,"not due while blocked, even past the due time");b.Tick(.1f,false);
        Assert(!b.AppearanceDue&&b.Rescheduled==1&&Math.Abs(b.DueIn-47.5f)<.01f,"expiry inside a block: a fresh 47.5 s gap (0.5 roll) from the block's end, not a fire at its edge");
        var nb=new ScareRhythm();nb.UseCastleClock(()=>.5f,true);nb.Tick(nb.DueIn-5,true);nb.Tick(1,false);Assert(nb.Rescheduled==0&&!nb.AppearanceDue,"a block that ends before the due time changes nothing");nb.Tick(4.1f,false);Assert(nb.AppearanceDue,"and it comes due on time");
        Assert(ScareRhythm.CastleBlocked(Phase.Recovery,0,false,false)&&ScareRhythm.CastleBlocked(Phase.Disturbance,5,false,false)&&ScareRhythm.CastleBlocked(Phase.Disturbance,0,true,false)&&!ScareRhythm.CastleBlocked(Phase.Disturbance,0,false,false)&&!ScareRhythm.CastleBlocked(Phase.Survey,0,false,false),"castle block: Warning/Hunt/Recovery/grace/hidden/modal");
        // Bursts and lulls: 0.9 rolls = bursts of 3, gaps 53.5, lulls 267.
        var bl=new ScareRhythm();bl.UseCastleClock(()=>.9f,true);var gaps=new System.Collections.Generic.List<float>();int lulls=0;
        for(int i=0;i<8;i++){while(!bl.AppearanceDue)bl.Tick(.5f);bl.RecordAppearance(0);gaps.Add(bl.LastInterval);if(bl.TakeLullStart())lulls++;}
        Assert(gaps[0]==53.5f&&gaps[1]==53.5f&&gaps[2]==267&&gaps[3]==53.5f&&gaps[5]==267&&lulls==2&&!bl.TakeLullStart(),"burst of 3 (gaps 40-55 s), then a 150-280 s lull, reported once");
        var ls=new ScareRhythm();ls.UseCastleClock(()=>.1f,false);Assert(ls.TakeLullStart()&&!ls.StartedWithBurst&&Math.Abs(ls.DueIn-163)<.01f,"a lull start is reported for the dice hold");
        // Run planner: a band used in neither of the last two runs, and the other rhythm start.
        var h=new RunHistory();var seen=new System.Collections.Generic.List<FirstHuntBand>();bool alternate=true;bool? lastStart=null;var pr=new Random(5);
        for(int i=0;i<300;i++){var plan=h.Next((float)pr.NextDouble(),(float)pr.NextDouble());seen.Add(plan.Band);if(lastStart.HasValue&&plan.BurstStart==lastStart.Value)alternate=false;lastStart=plan.BurstStart;h.Push(plan);h=RunHistory.Parse(h.Serialize());}
        bool distinct=true;for(int i=0;i+2<seen.Count;i++)if(seen[i]==seen[i+1]||seen[i]==seen[i+2]||seen[i+1]==seen[i+2])distinct=false;
        Assert(distinct&&alternate,"any three consecutive runs use all three bands; rhythm start alternates (through Serialize/Parse)");
        Assert(RunHistory.Parse("junk").LastBand==null&&RunHistory.Parse(null).LastBurstStart==null&&RunHistory.Parse("E,M,B").Serialize()=="E,M,B","history parses safely");
        var pe=new RunPlan{Band=FirstHuntBand.Early};var pl=new RunPlan{Band=FirstHuntBand.Late};Assert(pe.FirstHuntMin==60&&pe.FirstHuntMax==100&&pl.FirstHuntMin==160&&pl.FirstHuntMax==200,"bands 60-100 / 110-150 / 160-200 s");
        foreach(var band in new[]{FirstHuntBand.Early,FirstHuntBand.Mid,FirstHuntBand.Late})foreach(var u in new[]{0f,.5f,.99f})
        {var c=StartOn(CaseBook.Castle,()=>u);var plan=new RunPlan{Band=band};c.UseRunPlan(plan);c.Record(Evidence.Induction);c.Record(Evidence.Handprint);int t=TickUntilLeaves(c,Phase.Disturbance,400);
         Assert(t>=plan.FirstHuntMin&&t<=plan.FirstHuntMax+1,"first hunt inside its planned band ("+band+", roll "+u+")");}
        // Escalation never before the first hunt.
        var eb=StartOn(CaseBook.Castle,()=>0f);eb.Record(Evidence.Induction);eb.NoteApparition();eb.NoteApparition();Assert(eb.EscalationBlock(false)=="before_first_hunt"&&!eb.TryEscalate(true),"no escalation before the run's first hunt");
        // Lull dice hold: 120 s of non-threat time, unless Dread >= 60; the countdown stops in a hunt.
        var dh=StartOn(CaseBook.Castle);dh.Record(Evidence.Induction);dh.BeginNoTellHunt();for(int t=0;t<400&&(dh.Phase!=Phase.Disturbance||dh.GraceRemaining>0);t++)dh.Tick(1,false);
        dh.Dice=()=>0f;dh.ForceDread(10);dh.HoldDice(120);Assert(TickUntilLeaves(dh,Phase.Disturbance,300)>=120,"certain dice wait out the 120 s hold at low Dread");
        var dh2=StartOn(CaseBook.Castle);dh2.Record(Evidence.Induction);dh2.BeginNoTellHunt();for(int t=0;t<400&&(dh2.Phase!=Phase.Disturbance||dh2.GraceRemaining>0);t++)dh2.Tick(1,false);
        dh2.Dice=()=>0f;dh2.ForceDread(65);dh2.HoldDice(120);Assert(!dh2.DiceHeld&&TickUntilLeaves(dh2,Phase.Disturbance,300)<10,"at Dread >= 60 the hold does not apply");
        var dh3=StartOn(CaseBook.Castle);dh3.Record(Evidence.Induction);dh3.HoldDice(120);dh3.BeginNoTellHunt();dh3.Tick(30,false);Assert(dh3.DiceHoldRemaining==120,"the hold does not count down in a hunt");
        var bwh=Start();bwh.HoldDice(120);Assert(bwh.DiceHoldRemaining==0,"Bellwether ignores HoldDice");
        // The r13 pass tests, critic route, 2,000 consecutive runs with plans and the block model.
        SessionCriticRoute=true;
        string a=WaveL3Report("critic route, 1 in 3 close",1/3f,out float cv,out float fs,out float eg,out float sm,out float s10,out float w10);Console.WriteLine(a);
        string c2=WaveL3Report("critic route, all close",1f,out float cvb,out float fsb,out float egb,out float smb,out float s10b,out float w10b);Console.WriteLine(c2);
        SessionCriticRoute=false;
        Assert(cv>=.9f&&cvb>=.9f,"3-run pooled apparition CV >= 0.5 in >= 90 % of sets, with the block model");
        Assert(fs==1&&fsb==1,"every 3 consecutive runs' first hunts span >= 45 s");
        Assert(eg==1&&egb==1,"every run has a >= 120 s gap after its first hunt");
        Assert(sm>=.15f&&sm<=.30f&&smb>=.15f&&smb<=.30f&&s10>=.13f&&s10b>=.13f,"share 15-30 %, p10 >= 0.13");
        Assert(w10<=.35f&&w10b<=.35f,"<= 35 % of hunts within 10 s after an apparition");
    }
    static void WaveKTests()
    {
        var r=HuntRules.Castle;
        Assert(r.DreadInsideRate==.03f&&r.DreadDarkRate==.08f&&r.DreadDecayAllAbove==60&&r.NoTellChance==.35f&&r.FirstHuntMin==60&&r.FirstHuntMax==180&&r.DoorHeldThreshold==60&&Math.Abs(r.DoorHeldChance-1f/6)<1e-6f,"castle wave K values");
        Assert(HuntRules.Bellwether.DreadDecayAllAbove==0&&HuntRules.Bellwether.DoorHeldChance==0&&HuntRules.Bellwether.FirstHuntMax==0,"Bellwether: no wave K changes");
        // 1. Decay above 60 takes ambient Dread too.
        var a=StartOn(CaseBook.Castle);a.Record(Evidence.Induction);a.ForceDread(90);a.Tick(50,false);Assert(Math.Abs(a.Dread-75)<.01f&&a.DreadEvent==0,"calm: ambient Dread above 60 decays 0.3/s");
        a.Tick(1000,false);Assert(Math.Abs(a.Dread-60)<.01f,"and stops at 60");
        var a2=StartOn(CaseBook.Castle);a2.Record(Evidence.Induction);a2.ForceDread(90);a2.DreadRelief=3;a2.Tick(10,false);Assert(Math.Abs(a2.Dread-81)<.01f,"relief speeds it up");
        var a3=StartOn(CaseBook.Castle);a3.Record(Evidence.Induction);a3.ForceDread(50);a3.Tick(300,false);Assert(a3.Dread==50,"ambient Dread at or below 60 does not decay");
        var a4=StartOn(CaseBook.Castle);a4.Record(Evidence.Induction);a4.ForceDread(70);a4.AddDread(20);a4.Tick(10,false);Assert(Math.Abs(a4.Dread-87)<.01f&&Math.Abs(a4.DreadEvent-17)<.01f,"event part decays first");
        // 2. No-tell is a flat roll, independent of Dread.
        foreach(var level in new[]{10f,45f,65f,95f})
        {
            int nt=0,n=0;
            for(int seed=0;seed<1500;seed++){var c=StartOn(CaseBook.Castle,Seeded(80000+seed));c.Record(Evidence.Induction);c.Record(Evidence.Handprint);c.ForceDread(level);
                for(int t=0;t<400&&c.Phase==Phase.Disturbance;t++){c.ForceDread(level);c.Tick(1,false);}if(c.Phase==Phase.Hunt||c.Phase==Phase.Warning&&!c.FalseWarning){n++;if(c.NoTellHunt)nt++;}}
            Assert(n>1000&&Math.Abs(nt/(float)n-.35f/(1-.25f*.65f*0))<.05f||n>1000&&Math.Abs(nt/(float)n-.35f)<.06f,"no-tell share is about 35 % at Dread "+level);
        }
        var ln=StartOn(CaseBook.Castle,null,HuntRules.LongNight);ln.BeginWarning();Assert(ln.NoTellHunt,"Long Night: every hunt is no-tell");
        // 3. First hunt 60-240 s after the second record.
        var fd=new System.Collections.Generic.List<float>();
        for(int seed=0;seed<1000;seed++){var c=StartOn(CaseBook.Castle,Seeded(81000+seed));c.Record(Evidence.Induction);c.Record(Evidence.Handprint);fd.Add(TickUntilLeaves(c,Phase.Disturbance,400));}
        Assert(Quantile(fd,0)>=60&&Quantile(fd,0)<64&&Quantile(fd,1)<=181&&Quantile(fd,1)>170&&Quantile(fd,.9f)-Quantile(fd,.1f)>=60,"first hunt 60-180 s after the second record (wave L2, was 240), spread >= 60 s");
        // 5. Door held: a 1-in-6 roll per hide attempt at Dread >= 60.
        int calls=0;var dh=StartOn(CaseBook.Castle,()=>{calls++;return 0f;});dh.Record(Evidence.Induction);dh.ForceDread(59);int before=calls;
        Assert(!dh.TryDoorHeld()&&calls==before&&!dh.DoorHeld,"below 60: never held, no roll");dh.ForceDread(60);Assert(dh.TryDoorHeld()&&calls==before+1&&dh.DoorsHeld==1&&!dh.DoorHeld,"at 60: a roll under 1/6 holds the door");
        int held=0;var dr=StartOn(CaseBook.Castle,Seeded(82000));dr.Record(Evidence.Induction);dr.ForceDread(75);for(int i=0;i<6000;i++)if(dr.TryDoorHeld())held++;
        Assert(held==1200,"wave L bag of 5: exactly one eligible attempt in five is held (was an independent 1 in 6)");
        int bcalls=0;var bdh=Start();bdh.Dice=()=>{bcalls++;return 0f;};Assert(!bdh.TryDoorHeld()&&bcalls==0&&!bdh.DoorHeld,"Bellwether: no door-held");
        // Addendum: escalation 35 % in Disturbance, 15 % in Recovery after the grace, never during the grace.
        // (wave L: lit 15 % / dark 25 %, never the run's first apparition; was 35 % in wave K)
        int ok=0,okDark=0,tries=0;for(int seed=0;seed<3000;seed++){var c=StartOn(CaseBook.Castle,Seeded(83000+seed),EscMech(HuntRules.Castle));c.Record(Evidence.Induction);c.NoteApparition();c.NoteApparition();tries++;if(c.TryEscalate(false))ok++;
            var cd=StartOn(CaseBook.Castle,Seeded(93000+seed),EscMech(HuntRules.Castle));cd.Record(Evidence.Induction);cd.NoteApparition();cd.NoteApparition();if(cd.TryEscalate(true))okDark++;}
        Assert(Math.Abs(ok/(float)tries-.15f)<.025f&&Math.Abs(okDark/(float)tries-.25f)<.03f,"Disturbance: a close apparition escalates about 15 % lit, 25 % dark");
        int rok=0,rtries=0,inGrace=0;
        for(int seed=0;seed<3000;seed++)
        {
            var c=StartOn(CaseBook.Castle,Seeded(84000+seed));c.Record(Evidence.Induction);c.BeginNoTellHunt();for(int t=0;t<60&&c.Phase==Phase.Hunt;t++)c.Tick(1,false);
            if(c.Phase!=Phase.Recovery)continue;c.NoteApparition();c.NoteApparition();if(c.TryEscalate())inGrace++;c.ForceDread(c.Dread);rtries++;if(c.TryEscalate()){rok++;if(c.ThreatFrom!=Phase.Recovery||!c.EscalatedWarning)rok+=100000;}
        }
        Assert(inGrace==0,"never during the post-hunt grace");Assert(rtries>2500&&Math.Abs(rok/(float)rtries-.15f)<.03f,"Recovery after the grace: about 15 % lit, threat from Recovery, 1.5 s tell");
        // 6. Death first: a death in the same tick as a win wins over it.
        var w=SealedCastle();int phases=0;w.Changed+=_=>phases++;Assert(w.FinishAtVan()&&w.Phase==Phase.Won,"crossing wins");w.Die();
        Assert(w.Phase==Phase.Dead&&w.Deaths==1&&w.DeathOverrodeWin&&phases==2,"a grab in the same tick as the crossing kills (Won, then Dead)");
        var wi=SealedCastle();wi.Invulnerable=true;wi.FinishAtVan();wi.Die();Assert(wi.Phase==Phase.Won&&wi.BlockedDeaths==1,"the driver's invulnerability counts it as a blocked death");
        var wd=SealedCastle();wd.Die();Assert(wd.Phase==Phase.Dead&&!wd.FinishAtVan(),"death before the crossing: no win");
        var wt=SealedCastle();wt.FinishAtVan();wt.Tick(.016f,false);wt.Die();Assert(wt.Phase==Phase.Won&&wt.Deaths==0,"a tick later the win stands");
        var wk=StartOn(CaseBook.Castle);wk.Record(Evidence.Induction);wk.BeginNoTellHunt();Assert(wk.WinByKill()&&wk.HunterSlain,"killing blow wins");wk.Die();
        Assert(wk.Phase==Phase.Dead&&!wk.HunterSlain&&wk.Deaths==1,"a grab in the same tick as the killing blow kills");
        int frame=10;var wf=SealedCastle();wf.FrameSource=()=>frame;wf.FinishAtVan();wf.Tick(.016f,false);wf.Die();
        Assert(wf.Phase==Phase.Dead&&wf.DeathOverrodeWin,"with a frame source: a Case.Tick between the crossing and the grab in the same frame still lets death win");
        var wf2=SealedCastle();wf2.FrameSource=()=>frame;wf2.FinishAtVan();frame++;wf2.Die();Assert(wf2.Phase==Phase.Won&&wf2.Deaths==0,"with a frame source: a grab one frame later does not undo the win");
        var bwin=Start();bwin.Record(Evidence.Induction);bwin.Record(Evidence.Handprint);bwin.Record(Evidence.Voice);bwin.BeginWarning();bwin.Tick(9,false);bwin.Tick(38,false);Context(bwin);Correct(bwin);bwin.RestorePower();bwin.Seal(12,true,true);
        bwin.FinishAtVan();bwin.Die();Assert(bwin.Phase==Phase.Won,"Bellwether: the win is final");
        // 4. Natural play, 2,000 seeds: mortal player and the r11 driver.
        string m=WaveKReport("mortal",HuntRules.Castle,false,out float sm,out float s90,out float q120,out float d90,out float nt70,out float spread);Console.WriteLine(m);
        Assert(sm<=.22f&&s90<=.28f,"mortal: warning + hunt + return <= 22 % mean, p90 <= 28 %");
        Assert(q120>=.9f,"mortal: a quiet stretch of 120 s or more after the first hunt in >= 90 % of runs");
        Assert(d90<85,"mortal: Dread < 85 at 8 minutes (p90)");
        Assert(nt70>=.4f&&spread>=60,"mortal: no-tell hunts below Dread 70 happen in many runs; first-hunt delay spread >= 60 s");
        string dv=WaveKReport("r11 driver (immortal, open)",HuntRules.Castle,true,out float dm,out float dp90,out _,out float dd90,out _,out _);Console.WriteLine(dv);
        Assert(dm<=.30f&&dp90<=.33f&&dd90<95,"r11 driver: share <= 0.30 mean, Dread < 95 at 8 minutes (p90)");
    }
    static void WaveJTests()
    {
        var r=HuntRules.Castle;
        Assert(r.GovernorStart==.25f&&r.GovernorGain==12&&r.GovernorMax==3&&r.GovernorMinWindow==300&&r.GovernorQuietShare==.12f&&r.GovernorQuietSeconds==180&&r.GovernorQuietScale==.8f,"castle governor: 1 + 12 x (share - 0.25) up to 3; 0.8 after 3 quiet minutes");
        Assert(HuntRules.Bellwether.GovernorGain==0,"Bellwether has no governor");
        var bw=Start();Hunt(bw);bw.Tick(38,true);Assert(bw.Governor==1&&bw.GovernorAtLastGrace==1,"Bellwether governor stays neutral");
        // Neutral, then quiet.
        var q=StartOn(CaseBook.Castle);q.Record(Evidence.Induction);Assert(q.GovernorShare==0&&q.Governor==1,"no threat yet: neutral");
        for(int t=0;t<179;t++)q.Tick(1,false);Assert(q.Governor==1,"quiet for under 3 minutes: neutral");q.Tick(1,false);Assert(q.Governor==.8f,"quiet for 3 minutes: graces shorten (0.8)");
        q.BeginNoTellHunt();for(int t=0;t<60&&q.Phase==Phase.Hunt;t++)q.Tick(1,false);
        Assert(q.Phase==Phase.Recovery&&q.GovernorAtLastGrace==.8f&&Math.Abs(q.GraceSeconds-48)<.01f,"a hunt that began after 3 quiet minutes gets 0.8 x the 60 s grace");
        // Dense: three bell hunts back to back.
        var d=StartOn(CaseBook.Castle);d.Record(Evidence.Induction);
        for(int h=0;h<3;h++){d.BeginNoTellHunt();for(int t=0;t<60&&d.Phase==Phase.Hunt;t++)d.Tick(1,false);if(h<2)for(int t=0;t<10;t++)d.Tick(1,false);}
        Assert(Math.Abs(d.GovernorShare-114f/300)<.01f&&Math.Abs(d.GovernorAtLastGrace-(1+12*(114f/300-.25f)))<.02f,"three 38 s hunts in 134 s: share over the 300 s minimum window drives the multiplier");
        Assert(Math.Abs(d.GraceSeconds-60*d.GovernorAtLastGrace)<.05f,"the grace is the 30-90 s draw times the multiplier");
        var m=StartOn(CaseBook.Castle,()=>0f);m.Record(Evidence.Induction);for(int h=0;h<6;h++){m.BeginNoTellHunt();for(int t=0;t<60&&m.Phase==Phase.Hunt;t++)m.Tick(1,false);}
        Assert(m.GovernorAtLastGrace<=3&&m.Governor>2,"the multiplier is bounded at 3");
        m.ForceDread(100);int wait=0;while(m.Phase!=Phase.Warning&&m.Phase!=Phase.Hunt&&wait<400){m.Tick(1,false);wait++;}
        Assert(wait<400&&m.Hunts==7,"never a hard cap: with certain dice a hunt still comes at the top multiplier");
        // Runtime case (r11, build 0b08422): immortal driver in the open, Dread 100. And the mortal model, 2,000 seeds each.
        var imm=new System.Collections.Generic.List<float>();int fewHunts=0,minHunts=99;
        for(int seed=0;seed<2000;seed++){var s=Session(HuntRules.Castle,seed,0,true);imm.Add(s.ShareRun);if(s.Hunts<3)fewHunts++;minHunts=Math.Min(minHunts,s.Hunts);}
        float immMean=0;foreach(var v in imm)immMean+=v;immMean/=imm.Count;
        var immOff=new System.Collections.Generic.List<float>();var offRules=HuntRules.Castle.Clone();offRules.GovernorGain=0;
        for(int seed=0;seed<2000;seed++)immOff.Add(Session(offRules,seed,0,true).ShareRun);
        Console.WriteLine("WAVE_J immortal_open HuntShare_10min mean="+immMean.ToString("F3")+" p10="+Quantile(imm,.1f).ToString("F3")+" p50="+Quantile(imm,.5f).ToString("F3")+" p90="+Quantile(imm,.9f).ToString("F3")+" max="+Quantile(imm,1f).ToString("F3")+" runs_under_3_hunts="+fewHunts+" min_hunts="+minHunts+" | governor off p50="+Quantile(immOff,.5f).ToString("F3")+" p90="+Quantile(immOff,.9f).ToString("F3"));
        Assert(Quantile(immOff,.5f)>.36f,"without the governor the immortal model reproduces the runtime failure (0.362)");
        Assert(immMean<=.30f&&Quantile(imm,.9f)<=.33f&&fewHunts<=20&&minHunts>=2,"immortal in the open at Dread 100: 10-minute HuntShare p90 <= 0.33; 99 % of runs still have 3+ hunts, every run 2+");
        string mortal=SessionReport("castle lit (wave J governor)",HuntRules.Castle,0,out float meanAfter,out _,out _,out float cv,out var gaps,out _,out _,out _);Console.WriteLine(mortal);
        float p90After=Quantile(GovernorAfterShares(HuntRules.Castle),.9f);float cvSeed=SeedCvMedian(HuntRules.Castle),cvSeedOff=SeedCvMedian(offRules);
        Console.WriteLine("WAVE_J mortal share_after_first_hunt p90="+p90After.ToString("F3")+" cv_seed_median="+cvSeed.ToString("F2")+" (governor off "+cvSeedOff.ToString("F2")+") cv_pooled="+cv.ToString("F2"));
        Assert(meanAfter<=.30f&&p90After<=.33f,"mortal: hunt + warning after the first hunt mean <= 30 %, p90 <= 33 %");
        Assert(cvSeed>=cvSeedOff&&cv>=.4f&&Quantile(gaps,.1f)>=30,"gap variability within a run is unchanged or higher; pooled CV stays >= 0.4");
    }
    static System.Collections.Generic.List<float> GovernorAfterShares(HuntRules rules){var l=new System.Collections.Generic.List<float>();for(int seed=0;seed<2000;seed++)l.Add(Session(rules,seed,0).ShareAfterFirst);return l;}
    static float SeedCvMedian(HuntRules rules)
    {
        var cvs=new System.Collections.Generic.List<float>();
        for(int seed=0;seed<2000;seed++){var g=Session(rules,seed,0).Gaps;if(g.Count<3)continue;float m=0;foreach(var x in g)m+=x;m/=g.Count;float sd=0;foreach(var x in g)sd+=(x-m)*(x-m);cvs.Add((float)Math.Sqrt(sd/g.Count)/m);}
        return Quantile(cvs,.5f);
    }
    static void WaveITests()
    {
        var r=HuntRules.Castle;var b=HuntRules.Bellwether;
        Assert(Math.Abs(r.DreadDecayRecovery-.4f)<1e-6f&&Math.Abs(r.DreadDecayCalm-.3f)<1e-6f&&r.DreadDecayFloor==20&&r.HuntDreadCap==25&&r.GraceMin==30&&r.GraceMax==90,"castle: decay 0.4/0.3 (wave K, was 0.15) to 20, cap 25, grace 30-90 s");
        Assert(b.DreadDecayRecovery==0&&b.DreadDecayCalm==0&&b.HuntDreadCap==0&&b.GraceMax==0,"Bellwether: no wave I damping");
        var bw=Start();Hunt(bw);bw.Tick(38,true);Assert(bw.GraceSeconds==0&&bw.GraceRemaining==0,"Bellwether draws no grace");

        // 1. Event Dread decays; ambient Dread (darkness, time, ForceDread) does not.
        var d=StartOn(CaseBook.Castle);d.Record(Evidence.Induction);d.AddDread(30);Assert(Math.Abs(d.DreadEvent-30)<1e-4f,"AddDread is event Dread");
        d.Tick(10,false);Assert(Math.Abs(d.Dread-27)<.01f,"calm phases: event Dread decays 0.3/s");
        d.Tick(300,false);Assert(d.Dread==0&&d.DreadEvent==0,"before the first hunt event Dread decays to nothing");
        var amb=StartOn(CaseBook.Castle);amb.Record(Evidence.Induction);amb.Tick(100,true,false,false);float dark=amb.Dread;amb.Tick(300,false);
        Assert(Math.Abs(dark-8)<.01f&&amb.Dread==dark&&amb.DreadEvent==0,"darkness builds Dread that time does not take away (below 60)");
        amb.Tick(10,false,false,true);Assert(Math.Abs(amb.Dread-3)<.01f,"the safe zone still lowers it 0.5/s");
        var rel=StartOn(CaseBook.Castle);rel.Record(Evidence.Induction);rel.AddDread(30);rel.DreadRelief=3;rel.Tick(10,false);Assert(Math.Abs(rel.Dread-21)<.01f,"DreadRelief 3 triples the decay");
        var rec=StartOn(CaseBook.Castle);rec.Record(Evidence.Induction);rec.BeginWarning();TickUntilLeaves(rec,Phase.Warning,30);
        rec.AddDread(20);float inHunt=rec.Dread;rec.Tick(10,false);Assert(rec.Phase==Phase.Hunt&&rec.Dread==inHunt,"no decay during a hunt");
        for(int t=0;t<60&&rec.Phase==Phase.Hunt;t++)rec.Tick(1,false);rec.ForceDread(10);rec.AddDread(50);rec.Tick(10,false);
        Assert(rec.Phase==Phase.Recovery&&Math.Abs(rec.Dread-56)<.01f,"Recovery: event Dread decays 0.4/s");
        for(int t=0;t<400;t++)rec.Tick(1,false);Assert(Math.Abs(rec.Dread-20)<.01f&&rec.Hunts>=1,"after the first hunt decay stops at 20");
        var floorAmb=StartOn(CaseBook.Castle);floorAmb.Record(Evidence.Induction);floorAmb.BeginNoTellHunt();for(int t=0;t<200;t++)floorAmb.Tick(1,false);floorAmb.ForceDread(5);floorAmb.AddDread(10);
        for(int t=0;t<200;t++)floorAmb.Tick(1,false);Assert(Math.Abs(floorAmb.Dread-15)<.01f,"the floor never raises Dread; it only stops the decay");
        var cp=StartOn(CaseBook.Castle);cp.Record(Evidence.Induction);cp.ForceDread(10);cp.AddDread(30);var cs=cp.SaveCheckpoint();var cb=new Investigation(CaseBook.Castle);cb.RestoreCheckpoint(cs,false);
        Assert(Math.Abs(cs.DreadEvent-30)<1e-4f&&Math.Abs(cb.DreadEvent-30)<1e-4f&&Math.Abs(cb.Dread-40)<1e-4f,"checkpoints keep the event part");
        var pen=new Investigation(CaseBook.Castle);pen.RestoreCheckpoint(cs,true);Assert(pen.Dread==60&&Math.Abs(pen.DreadEvent-50)<1e-4f,"death penalty: 60, of which everything above the saved ambient part decays");

        // 2. Per-hunt cap on AddDread.
        var cap=StartOn(CaseBook.Castle);cap.Record(Evidence.Induction);cap.BeginNoTellHunt();float c0=cap.Dread;
        cap.AddDread(15);cap.AddDread(20);Assert(Math.Abs(cap.Dread-c0-25)<1e-4f&&cap.HuntDreadAdded==25,"a hunt adds at most 25");
        cap.AddDread(10);for(int i=0;i<100;i++)cap.AddDread(.3f);Assert(Math.Abs(cap.Dread-c0-25)<1e-4f,"no caller gets past the cap");
        cap.AddDread(-5);Assert(Math.Abs(cap.Dread-c0-20)<1e-4f,"negative AddDread still works in a hunt");
        for(int t=0;t<60&&cap.Phase==Phase.Hunt;t++)cap.Tick(1,false);cap.AddDread(30);Assert(cap.Phase==Phase.Recovery&&cap.Dread>=c0+49,"no cap outside a hunt");
        cap.ForceDread(0);cap.BeginNoTellHunt();Assert(cap.HuntDreadAdded==0,"the cap resets each hunt");cap.AddDread(40);Assert(Math.Abs(cap.Dread-25)<1e-4f,"second hunt: 25 again");
        var hr=SealedCastle();hr.AddDread(15);hr.AddDread(15);Assert(hr.HuntActive&&hr.HuntDreadAdded==25,"the hostile Return is capped too");
        var bcap=Start();Hunt(bcap);bcap.AddDread(80);Assert(bcap.Dread==0,"Bellwether still ignores Dread");

        // 3. Post-hunt grace: 30-90 s drawn at hunt end (after the recovery length), dice and escalation disarmed.
        var g0=StartOn(CaseBook.Castle,()=>0f,NoHold(HuntRules.Castle));g0.Record(Evidence.Induction);g0.BeginWarning();TickUntilLeaves(g0,Phase.Warning,30);TickUntilLeaves(g0,Phase.Hunt,60);
        Assert(g0.Phase==Phase.Recovery&&g0.GraceSeconds==30&&g0.GraceRemaining==30&&g0.CurrentRecoverySeconds==10,"roll 0: recovery 10 s, grace 30 s");
        int quietFor=0;while((g0.Phase==Phase.Recovery||g0.Phase==Phase.Disturbance)&&quietFor<120){g0.Tick(1,false);quietFor++;}
        Assert(quietFor>=30&&quietFor<=40&&g0.Hunts==2,"certain dice stay disarmed for the whole grace, then hunt");
        var gm=StartOn(CaseBook.Castle);gm.Record(Evidence.Induction);gm.BeginWarning();TickUntilLeaves(gm,Phase.Warning,30);TickUntilLeaves(gm,Phase.Hunt,60);Assert(gm.GraceSeconds==60,"0.5 roll: 60 s");
        int gcalls=0;var go=new Investigation(CaseBook.Castle,()=>{gcalls++;return .5f;});go.Equip();go.EnterHouse();go.Record(Evidence.Induction);go.BeginWarning();TickUntilLeaves(go,Phase.Warning,30);int beforeEnd=gcalls;TickUntilLeaves(go,Phase.Hunt,60);
        Assert(gcalls-beforeEnd==2,"hunt end draws exactly two rolls: recovery length, then grace");
        var ge=StartOn(CaseBook.Castle,()=>0f);ge.Record(Evidence.Induction);ge.BeginNoTellHunt();for(int t=0;t<60&&ge.Phase!=Phase.Disturbance;t++)ge.Tick(1,false);
        Assert(ge.Phase==Phase.Disturbance&&ge.GraceRemaining>0&&!ge.TryEscalate(),"no escalation during the grace");
        ge.BeginWarning();Assert(ge.Phase==Phase.Warning,"the bell still summons during the grace");
        var gf=StartOn(CaseBook.Castle,()=>0f);gf.Record(Evidence.Induction);gf.BeginNoTellHunt();for(int t=0;t<60&&gf.Phase==Phase.Hunt;t++)gf.Tick(1,false);gf.ForceDread(50);Assert(gf.GraceRemaining==0,"ForceDread (driver) clears the grace");
        var graces=new System.Collections.Generic.List<float>();
        for(int seed=0;seed<1000;seed++){var c=StartOn(CaseBook.Castle,Seeded(70000+seed));c.Record(Evidence.Induction);c.BeginNoTellHunt();for(int t=0;t<60&&c.Phase==Phase.Hunt;t++)c.Tick(1,false);graces.Add(c.GraceSeconds);}
        Assert(Quantile(graces,0)>=30&&Quantile(graces,0)<32&&Quantile(graces,1)<=90&&Quantile(graces,1)>88&&Math.Abs(Quantile(graces,.5f)-60)<3,"grace uniform over 30-90 s");
        int cutCastle=0;for(int seed=0;seed<300;seed++){var c=StartOn(CaseBook.Castle,Seeded(71000+seed));c.Record(Evidence.Induction);c.BeginNoTellHunt();c.ForceDread(90);for(int t=0;t<60&&c.Phase==Phase.Hunt;t++)c.Tick(1,false);
            float len=c.CurrentRecoverySeconds,grace=c.GraceSeconds;if(TickUntilLeaves(c,Phase.Recovery,60)<len-.01f){cutCastle++;if(grace>len)cutCastle+=1000;}}
        Assert(cutCastle<1000&&cutCastle<60,"Recovery dice only fire after the grace, so a Recovery is rarely cut short");

        // 5. HuntShare: rolling share of the last 10 minutes in Warning, Hunt or hostile Return.
        var hs=StartOn(CaseBook.Castle);hs.Record(Evidence.Induction);for(int t=0;t<53;t++)hs.Tick(1,false);
        Assert(hs.HuntShare==0,"no threat yet");hs.BeginWarning();for(int t=0;t<47;t++)hs.Tick(1,false);
        Assert(hs.Phase==Phase.Recovery&&Math.Abs(hs.HuntShare-.47f)<.011f,"9 s warning + 38 s hunt in the first 100 s = 0.47");
        for(int t=0;t<500;t++){hs.ForceDread(0);hs.Tick(1,false);}Assert(Math.Abs(hs.HuntShare-47/600f)<.003f,"after 600 s the share is over the last 10 minutes");
        for(int t=0;t<120;t++){hs.ForceDread(0);hs.Tick(1,false);}Assert(hs.HuntShare<.001f&&hs.Hunts==1,"a hunt more than 10 minutes ago no longer counts");
        var hsr=StartOn(CaseBook.Castle);hsr.Record(Evidence.Induction);hsr.Record(Evidence.Handprint);hsr.Record(Evidence.Voice);hsr.BeginWarning();hsr.Tick(9,false);hsr.Tick(38,false);
        foreach(var title in CaseBook.Castle.NoteTitles)hsr.Read(title);Correct(hsr);hsr.RestorePower();for(int t=0;t<100;t++)hsr.Tick(1,false);hsr.Seal(12,true,true);for(int t=0;t<30;t++)hsr.Tick(1,false);
        Assert(hsr.Phase==Phase.Return&&Math.Abs(hsr.HuntShare-77/177f)<.01f,"the hostile Return counts as threat time ((47 + 30) / 177 s)");

        // 4. The 10-minute session model, 2,000 seeds: wave I against the same castle with the damping off.
        float meanAfter,p50,p90,cv,pinned;System.Collections.Generic.List<float> gaps;int low,threats;
        Console.WriteLine(SessionReport("off(pre-wave-I)",RecoveryMech(),0,out float offAfter,out _,out _,out _,out _,out float offPinned,out _,out _));
        string lit=SessionReport("castle lit",HuntRules.Castle,0,out meanAfter,out p50,out p90,out cv,out gaps,out pinned,out low,out threats);Console.WriteLine(lit);
        Assert(offAfter>.5f&&offPinned>60,"the model reproduces the runaway without wave I (critic: 52-63 %, Dread pinned)");
        Assert(meanAfter<=.30f&&p50<=.25f&&p90<=.30f,"hunt + warning time is at most 30 % of a 10-minute run");
        Assert(cv>=.4f&&Quantile(gaps,.1f)>=30&&Quantile(gaps,.9f)>=150,"quiet gaps vary from about 40 s to several minutes (CV >= 0.4)");
        Assert(pinned<5&&low>threats/4,"Dread no longer pins at 100, and hunts still start below the boundary");
        string dk=SessionReport("castle 25% dark",HuntRules.Castle,.25f,out meanAfter,out p50,out p90,out cv,out gaps,out pinned,out low,out threats);Console.WriteLine(dk);
        Assert(meanAfter<=.32f&&p90<=.32f&&cv>=.4f,"a darker player stays near the target too");
    }
}
