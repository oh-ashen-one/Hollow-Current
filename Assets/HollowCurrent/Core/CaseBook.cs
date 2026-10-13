using System;

namespace HollowCurrent
{
    // Everything a map renames in the shared investigation: evidence names and
    // ids, note titles, journal choices, dispatch lines, goal text and art paths.
    // Rule logic stays in Investigation; only words, ids and anchors differ.
    // Journal choices keep the enum slots: the correct answer for every map is
    // AnchorKind.MusicBox (slot 2), HauntingPattern.DomesticLoop (slot 0) and
    // ClosureMethod.LetSongEnd (slot 0); each map labels those slots itself.
    // Bellwether strings must stay byte-for-byte identical to the shipped house.
    public sealed class CaseBook
    {
        public MapId Map;
        // Evidence, indexed by (int)Evidence: display name (also used in
        // "Evidence secured / <name>."), stable id, and instrument name.
        public string[] EvidenceNames, EvidenceIds, InstrumentNames;
        // Journal instrument record text, indexed by (int)Evidence.
        public string[] EvidenceRecorded, EvidenceMissing;
        // Context notes that make the mystery ready (titles are Interactable labels).
        public string[] NoteTitles, NoteIds, NoteBodies;
        // Resources path of the art shown beside each note; null = text only.
        public string[] NoteArt;
        public string ArrivalNoteTitle, ArrivalNoteBody;
        // Journal choice labels per slot.
        public string[] AnchorChoices, PatternChoices, ClosureChoices;
        public string WrongAnchor, WrongPattern, WrongClosure, Diagnosed;
        public string EquipMessage, PowerRestored, RecoveryMessage, WarningMessage, SealedMessage;
        public string GoalNoKit, GoalBriefing, GoalWarning, GoalHunt, GoalContainmentOn, GoalContainmentOff, GoalReturn, GoalWon, GoalDead, GoalReacting, GoalNeedContext, GoalReconstruct, GoalDefault;
        // Game dispatch / radio. Cue phrases trigger the matching spoken line.
        public string Arrival, ArrivalCue, KitCue, RadioInterference, RadioHunt, HintSignal, HintTrace, HintVoice;
        // Investigator instrument status lines and EVP exchange.
        public string ReaderNear, ReaderClose, ReaderIdle, TraceFound, TraceIdle, AskPad, AskKey, Listening, Question, Reply, NoReply;
        // Interactables.
        public string KitLabel, FinishLabel, AnchorLabel, BreakerLabel, FinishNoteTitle, FinishNoteSuffix, AnchorNotReady;
        // Interface.
        public string CaseTag, PlaceTag, FooterName, TitleName, Tagline, LoadingTrack, ArchiveTrack, ContextTrack, JournalQuestion, CardStyle, UiArtFolder;
        public string JournalReady, JournalNotReady, WonHero, WonBody, LostBody, WonFooter, LostFooter;
        // Location labels: safe zone, outside, below ground, ground, upper, tower (tower used above TowerY).
        public string LocSafe, LocOutside, LocLower, LocGround, LocUpper, LocTower;
        public float TowerY=float.MaxValue;

        public string EvidenceName(Evidence e)=>EvidenceNames[(int)e];
        public string EvidenceId(Evidence e)=>EvidenceIds[(int)e];
        public string NoteArtFor(string title)
        {
            for(int i=0;i<NoteTitles.Length;i++)if(NoteTitles[i]==title)return NoteArt[i];
            return null;
        }
        public string NoteIdFor(string title)
        {
            for(int i=0;i<NoteTitles.Length;i++)if(NoteTitles[i]==title)return NoteIds[i];
            return null;
        }
        public static CaseBook For(MapId map)=>map==MapId.Castle?Castle:Bellwether;

        public static readonly CaseBook Bellwether=new CaseBook
        {
            Map=MapId.Bellwether,
            EvidenceNames=new[]{"Induction","Handprint","Voice"},
            EvidenceIds=new[]{"induction","handprint","voice"},
            InstrumentNames=new[]{"INDUCTION READER","ULTRAVIOLET","EVP RECORDER"},
            EvidenceRecorded=new[]{"Pulse / A regular signal at the empty place setting. It repeats even when nobody moves.","Trace / A hand beside the bedroom window. The mark stays after the figure is gone.","Reply / 'Her chair. Do not put it away. Keep the house warm.' A domestic instruction."},
            EvidenceMissing=new[]{"Pulse / Sweep for persistent activity, then hold the reader steady.","Trace / Examine surfaces the family describes with ultraviolet.","Reply / Record where activity persists. Ask once, stay still and listen."},
            NoteTitles=new[]{"Family photograph","Child's drawing","Furnace diary"},
            NoteIds=new[]{"family_photograph","child_drawing","furnace_diary"},
            NoteBodies=new[]{"","",""},
            NoteArt=new[]{"Surfaces/FamilyPhoto","Surfaces/ChildDrawing","Surfaces/FurnaceDiary"},
            ArrivalNoteTitle="Arrival note",ArrivalNoteBody="",
            AnchorChoices=new[]{"The doll","The furnace","The music box"},
            PatternChoices=new[]{"An evening routine","Our own words","A person's journey"},
            ClosureChoices=new[]{"Let the song end","Burn the anchor","Move the anchor"},
            WrongAnchor="Case reconstruction / The diary describes an object returning to the same place. Reconsider the anchor.",
            WrongPattern="Case reconstruction / Compare the repeated instruction with the family's evening routine.",
            WrongClosure="Case reconstruction / The drawing says what happens when the tune is interrupted. Reconsider the closure.",
            Diagnosed="Dispatch / A domestic loop, held by the music box. Stabilize the power, then hold the lid gently while the last notes end. Do not wind it again.",
            EquipMessage="Dispatch / Three instruments checked. Read the family notes. Find a hiding place before you need one.",
            PowerRestored="Power restored / The field instruments stabilize. A little tune carries through the pipes.",
            RecoveryMessage="Dispatch / Signal restored. Breathe. You have a quiet window. Finish collecting evidence.",
            WarningMessage="Dispatch / Nine seconds. Get out of its sight. Hide, switch off your torch, stay quiet. The front door is still open.",
            SealedMessage="Dispatch / Anchor is sealed. Leave it where it is. Return to the van and close the case.",
            GoalNoKit="Collect the field kit from the van rack [E].",
            GoalBriefing="Enter 14 Bellwether Lane. Read the note in the foyer.",
            GoalWarning="BREAK LINE OF SIGHT · hide [E] · torch off [F] · stay quiet",
            GoalHunt="Break sight and hide. She follows sight and sound, even outside.",
            GoalContainmentOn="Hold the music-box lid [E]. Let the last notes end.",
            GoalContainmentOff="Restore the basement breaker [E].",
            GoalReturn="Return to the van. Close the case at the desk [E].",
            GoalWon="CASE CLOSED",GoalDead="SIGNAL LOST",
            GoalReacting="The house is reacting. Find cover before it finds you.",
            GoalNeedContext="The instrument records need context. Examine the family's photograph, drawing and diary.",
            GoalReconstruct="Reconstruct the haunting in your journal [J].",
            GoalDefault="Follow activity. Record what the instruments reveal. Compare it with the family's notes.",
            Arrival="Dispatch / Bellwether House. Family evacuated. Keep an escape route. She can reach the van. Collect the kit and read your brief.",
            ArrivalCue="Family evacuated",KitCue="Three instruments",
            RadioInterference="Radio / ...keep... the house... warm...",
            RadioHunt="Radio / Find cover. Light off. Stay quiet.",
            HintSignal="Follow the meter through the rooms they used most.",
            HintTrace="Look at what the family left behind. Ultraviolet may reveal a trace.",
            HintVoice="The recorder needs a quiet room. Ask once, then listen.",
            ReaderNear="INDUCTION  5 / 5 · HOLD STEADY TO RECORD",ReaderClose="INDUCTION  2 / 5 · SIGNAL NEARBY",ReaderIdle="INDUCTION  0 / 5 · SWEEP FOR ACTIVITY",
            TraceFound="ULTRAVIOLET · HANDPRINT · HOLD TO RECORD",TraceIdle="ULTRAVIOLET · INSPECT TOUCHED SURFACES",
            AskPad="EVP RECORDER · R2 TO ASK: WHO IS HERE?",AskKey="EVP RECORDER · CLICK TO ASK: WHO IS HERE?",Listening="EVP · LISTENING… STAY STILL",
            Question="You / Who is keeping this house warm?",
            Reply="Recorder / Her chair. Do not put it away. Keep the house warm.",
            NoReply="Recorder / Only room noise. Find persistent activity, ask again, and stay still while listening.",
            KitLabel="Field kit · torch, induction, UV, recorder, radio",FinishLabel="Case terminal",AnchorLabel="The music box",BreakerLabel="Basement breaker",
            FinishNoteTitle="CASE 014",FinishNoteSuffix="\n\nThe van is safe. Equipment never runs out of charge. Take the time you need.",
            AnchorNotReady="A tiny tune circles inside the box. Reconstruct what holds the house in your journal before attempting a seal.",
            CaseTag="CASE 014",PlaceTag="BELLWETHER LANE",FooterName="THE BELLWETHER HOUSE",TitleName="The Bellwether House",
            Tagline="An empty family home. A tune beneath the floor.\nSomeone is still keeping the house warm.",
            LoadingTrack="RETURNING TO BELLWETHER",ArchiveTrack="FAMILY ARCHIVE",ContextTrack="FAMILY CONTEXT",JournalQuestion="What holds the house?",
            CardStyle="MOURNING NOTE",UiArtFolder="UI/MourningNote",
            JournalReady="Compare the records with the family's notes. Each conclusion needs support.",
            JournalNotReady="Reconstruction needs three records, the photograph, drawing and diary, and a hunt survived.",
            WonHero="The house\nfalls silent.",
            WonBody="The music box is quiet. The furnace cools.\nFor the first time in weeks, no one sets the fourth chair.\n\nThe Harrows can begin again.",
            LostBody="Your last transmission dissolves into static.\n\nBreak sight before hiding. Silence the torch and radio. She can follow you to the van.",
            WonFooter="CONTAINMENT CONFIRMED   /   FIELD RECORD ARCHIVED",LostFooter="CASE UNRESOLVED   /   A NEW INVESTIGATION CAN BEGIN",
            LocSafe="FIELD VAN",LocOutside="BELLWETHER LANE",LocLower="BASEMENT",LocGround="GROUND FLOOR",LocUpper="UPPER FLOOR",LocTower="UPPER FLOOR",
        };

        // The Blood Ledger (DESIGN.md): compass / grave-ash lantern / ear-trumpet,
        // the chapel bell instead of the music box, the drawbridge instead of the van.
        public static readonly CaseBook Castle=new CaseBook
        {
            Map=MapId.Castle,
            EvidenceNames=new[]{"Compass","Claw-scores","Whisper"},
            EvidenceIds=new[]{"compass","lantern","trumpet"},
            InstrumentNames=new[]{"HOLLOW COMPASS","GRAVE-ASH LANTERN","VELLUM EAR-TRUMPET"},
            EvidenceRecorded=new[]{"Needle / The compass spins where he lands. It keeps spinning after he has flown.","Ash / Claw-scores and the drag of a wing along the stone. The ash keeps them after he is gone.","Whisper / 'Ring, and I wake. Ring, and they stay mine.' A bargain, not a threat."},
            EvidenceMissing=new[]{"Needle / Sweep the halls with the compass, then hold it steady where it spins.","Ash / Raise the grave-ash lantern to walls and floors where he might land.","Whisper / Find where he lingers. Ask once into the ear-trumpet, stay still and listen."},
            NoteTitles=new[]{"Ledger: the steward","Ledger: the astronomer","Ledger: the painter"},
            NoteIds=new[]{"cc_ledger_steward","cc_ledger_orrery","cc_ledger_portrait"},
            NoteBodies=new[]{
                "BLOOD LEDGER / THE STEWARD\n\nEvery midnight the chapel bell rings with no hand on the rope, and every midnight one of us is written out of the book.\n\nI hid the silver blade where he cannot bear to look: in the reliquary box below, under the marks his own claws left. Light the ash and you will see it.",
                "BLOOD LEDGER / THE ASTRONOMER\n\nThe bargain is a toll, not a hunger. The bell rings, he wakes, the night goes on.\n\nIf the last stroke is ever allowed to fade without a second ring, the toll is paid out. I set the hilt where my missing planet should turn.",
                "BLOOD LEDGER / THE PAINTER\n\nHe sits for his portrait each night and each night the eyes are wrong.\n\nI kept the moonstone behind the one painting that is also a door. He walks past it as if it were a wall."},
            NoteArt=new[]{"Castle/LedgerSteward","Castle/LedgerAstronomer","Castle/LedgerPainter"},
            ArrivalNoteTitle="Steward's note",
            ArrivalNoteBody="THE GATEHOUSE\n\nThe household is gone. The night has not ended for a very long time.\n\nThe ledger pages are scattered through the castle. Hide under the galleries, not in the open halls: he watches from above. Outside the walls you are safe. Never hide while he is watching you.",
            AnchorChoices=new[]{"The portrait","The sarcophagus","The chapel bell"},
            PatternChoices=new[]{"The bargain's toll","Our own words","A guest's journey"},
            ClosureChoices=new[]{"Let the last stroke fade","Burn the anchor","Move the anchor"},
            WrongAnchor="Case reconstruction / The steward's page says something rings at midnight with no hand on it. Reconsider the anchor.",
            WrongPattern="Case reconstruction / Compare the ledger's repeating entries with the hours of the bargain.",
            WrongClosure="Case reconstruction / The astronomer's page says what happens when the last stroke fades. Reconsider the closure.",
            Diagnosed="Dispatch / The bargain's toll, held by the chapel bell. Keep the brazier lit, then hold the rope still while the last stroke fades. Do not ring it again.",
            EquipMessage="Dispatch / Three instruments checked. Compass, lantern, ear-trumpet. Read the ledger pages. Find a hiding place before you need one.",
            PowerRestored="Fire restored / The chapel brazier catches. The instruments steady. Above you, the bell hums by itself.",
            RecoveryMessage="Dispatch / He has gone back to his perch. Breathe. You have a quiet window. Finish collecting evidence.",
            WarningMessage="Dispatch / Nine seconds. Wings in the rafters. Get under cover, lantern off, stay quiet. The gatehouse is still open.",
            SealedMessage="Dispatch / The bell is still. Leave it where it hangs. Lower the drawbridge and cross it.",
            GoalNoKit="Take the instruments from the lantern cart [E].",
            GoalBriefing="Cross into the courtyard. Read the steward's note in the gatehouse.",
            GoalWarning="BREAK LINE OF SIGHT · hide [E] · lantern off [F] · stay quiet",
            GoalHunt="Break sight and get under cover. He watches from above and hears every step.",
            GoalContainmentOn="Hold the bell rope [E]. Let the last stroke fade.",
            GoalContainmentOff="Relight the chapel brazier [E].",
            GoalReturn="Return to the gatehouse. Lower the drawbridge [E].",
            GoalWon="LEDGER CLOSED",GoalDead="THE COUNT FEEDS",
            GoalReacting="The castle is waking. Find cover before he finds you.",
            GoalNeedContext="The instrument records need context. Find the steward's, astronomer's and painter's ledger pages.",
            GoalReconstruct="Reconstruct the bargain in your journal [J].",
            GoalDefault="Follow his residue. Record what the instruments reveal. Compare it with the Blood Ledger.",
            Arrival="Dispatch / Castle gate. The household is long gone. Keep the drawbridge in reach. He can fly the walls. Take the instruments and read the steward's note.",
            ArrivalCue="household is long gone",KitCue="Three instruments",
            RadioInterference="Radio / ...the bell... ring... the bell...",
            RadioHunt="Radio / Get under cover. Lantern off. Stay quiet.",
            HintSignal="Follow the compass through the halls he crosses most.",
            HintTrace="Look where he lands. The grave-ash lantern shows claw-scores.",
            HintVoice="The ear-trumpet needs a still room. Ask once, then listen.",
            ReaderNear="COMPASS  5 / 5 · HOLD STEADY TO RECORD",ReaderClose="COMPASS  2 / 5 · RESIDUE NEARBY",ReaderIdle="COMPASS  0 / 5 · SWEEP FOR RESIDUE",
            TraceFound="GRAVE-ASH · CLAW-SCORES · HOLD TO RECORD",TraceIdle="GRAVE-ASH · INSPECT WALLS AND FLOORS",
            AskPad="EAR-TRUMPET · R2 TO ASK: WHO IS HERE?",AskKey="EAR-TRUMPET · CLICK TO ASK: WHO IS HERE?",Listening="EAR-TRUMPET · LISTENING… STAY STILL",
            Question="You / Who keeps this castle's bargain?",
            Reply="Recorder / Ring, and I wake. Ring, and they stay mine.",
            NoReply="Recorder / Only wind in the stones. Find where he lingers, ask again, and stay still while listening.",
            KitLabel="Lantern cart · compass, grave-ash lantern, ear-trumpet, radio",FinishLabel="Drawbridge winch",AnchorLabel="The chapel bell",BreakerLabel="Chapel brazier",
            FinishNoteTitle="THE GATEHOUSE",FinishNoteSuffix="\n\nOutside the walls is safe. The lantern never gutters here. Take the time you need.",
            AnchorNotReady="The bell hums without being struck. Reconstruct what holds the castle in your journal before attempting a seal.",
            CaseTag="LEDGER 015",PlaceTag="CASTLE GATE",FooterName="HAUNTED CASTLE",TitleName="Haunted Castle",
            Tagline="A castle half out of the world. Two moons over a red moat.\nA bell no one rings keeps the bargain.",
            LoadingTrack="RETURNING TO THE CASTLE",ArchiveTrack="BLOOD LEDGER",ContextTrack="BLOOD LEDGER",JournalQuestion="What holds the castle?",
            CardStyle="BLOOD LEDGER",UiArtFolder="UI/BloodLedger",
            JournalReady="Compare the records with the Blood Ledger. Each conclusion needs support.",
            JournalNotReady="Reconstruction needs three records, the steward's, astronomer's and painter's pages, and a hunt survived.",
            WonHero="The castle\nfalls silent.",
            WonBody="The bell is still. The candles settle onto the tables.\nFor the first time in centuries, the night is allowed to end.\n\nThe ledger can be closed.",
            LostBody="Your lantern gutters into the dark.\n\nBreak sight before hiding. Douse the lantern and radio. He can follow you to the gatehouse.",
            WonFooter="BARGAIN BROKEN   /   BLOOD LEDGER CLOSED",LostFooter="BARGAIN UNBROKEN   /   A NEW INVESTIGATION CAN BEGIN",
            LocSafe="GATEHOUSE",LocOutside="OUTSIDE THE WALLS",LocLower="CRYPT",LocGround="GROUND FLOOR",LocUpper="GALLERIES",LocTower="TOWER",TowerY=5,
        };
    }
}
