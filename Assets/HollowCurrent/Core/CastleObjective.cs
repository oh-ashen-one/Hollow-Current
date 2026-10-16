using System;
using System.Collections.Generic;

namespace HollowCurrent
{
    // fb-onboard (owner feedback 2026-10-15, item 12: "no idea what to do or where to go"): the castle HUD objective as one chain the
    // player can follow from spawn: the gate keeper's letter, the Blood Ledger pages (the rooms still to search are named), the
    // relics the ledger names, the forge, then the bell (the fight with the sword, or the seal once the bargain is reconstructed)
    // and the winch (Investigation.Goal's Containment / Return texts). Pure: Interface.HudGoal feeds it the case and the forge state.
    // Warning, Containment, Return and the endings keep Investigation.Goal's own texts; Hunt and Recovery keep the calm objective
    // (critic r10 #4: the HUD never flips to a hunt goal), with the hunt-survived state the caller froze at the hunt's start.
    public static class CastleObjective
    {
        public const string Letter = "letter", Ledger = "ledger", Relics = "relics", Forge = "forge", Fight = "fight", Reconstruct = "reconstruct",
            Warning = "warning", Contain = "contain", Return = "return", Won = "won", Dead = "dead", Briefing = "briefing";
        public struct State { public string Step, Text, Tracker; }
        // Rooms of the three ledger pages, in CaseBook.Castle.NoteTitles order (steward, astronomer, painter: the dining-hall compass,
        // the portrait-hall ear-trumpet and the chapel altar anchors of World.CastleCaseItems).
        public static readonly string[] PageRooms = { "the dining hall (west wing)", "the portrait hall (upstairs, east wing)", "the chapel (east of the courtyard)" };
        // Where each relic is (Forge.Relic order), for the relic step and the idle nudge.
        public static readonly string[] RelicPlaces = { "the reliquary box below the chapel (the grave-ash lantern shows the claw marks that open it)", "the orrery up in the observatory", "behind a portrait in the portrait hall" };
        public const string LetterText = "Read the gate keeper's letter on the gatehouse table [E].";
        public const string FightText = "Ring the chapel bell [E] to call him, then strike him three times with the sword. Or run for the drawbridge.";
        public const string ForgeText = "Take the three relics to the chapel altar and hold [E] there to forge the sword.";

        public static State For(Investigation c, bool[] relicsFound, bool sword, bool survived)
        {
            var b = c.Book;
            int relics = 0; if (relicsFound != null) foreach (bool f in relicsFound) if (f) relics++;
            int pages = c.ContextCount, records = c.Evidence.Count;
            string tracker = "RECORDS " + records + "/3   LEDGER " + pages + "/3   RELICS " + (sword ? "FORGED" : relics + "/3") + (survived ? "   HUNT SURVIVED" : "");
            State S(string step, string text) => new State { Step = step, Text = text, Tracker = tracker };
            switch (c.Phase)
            {
                case Phase.Briefing: return S(Briefing, c.Goal); // capture mode only: Game equips every other castle run at spawn
                case Phase.Won: return S(Won, c.Goal);
                case Phase.Dead: return S(Dead, c.Goal);
                case Phase.Warning: return S(Warning, c.Goal);
                case Phase.Containment: return S(Contain, c.Goal);
                case Phase.Return: return S(Return, c.Goal);
            }
            if (!c.Notes.Contains(b.ArrivalNoteTitle)) return S(Letter, LetterText);
            if (pages < 3)
            {
                var rooms = new List<string>();
                for (int i = 0; i < b.NoteTitles.Length && i < PageRooms.Length; i++) if (!c.Notes.Contains(b.NoteTitles[i])) rooms.Add(PageRooms[i]);
                return S(Ledger, "Find the Blood Ledger pages (" + pages + "/3): " + string.Join(", ", rooms) + ".");
            }
            if (records == 3 && survived && !c.Diagnosed) return S(Reconstruct, b.GoalReconstruct);
            if (sword) return S(Fight, FightText);
            if (relics >= 3) return S(Forge, ForgeText);
            int next = 0; while (next < 3 && relicsFound != null && next < relicsFound.Length && relicsFound[next]) next++;
            return S(Relics, "Find the relics the ledger names (" + relics + "/3). Next: " + RelicPlaces[Math.Min(next, 2)] + ".");
        }
        // The idle nudge for a step (fb-onboard: after about 60 s without progress), or null.
        public static string Nudge(string step, Investigation c, bool[] relicsFound)
        {
            switch (step)
            {
                case Letter: return "The gate keeper's letter lies open on the table beside you in the gatehouse. Look down at it and press [E].";
                case Ledger:
                    var b = c.Book; var rooms = new List<string>();
                    for (int i = 0; i < b.NoteTitles.Length && i < PageRooms.Length; i++) if (!c.Notes.Contains(b.NoteTitles[i])) rooms.Add(PageRooms[i]);
                    return rooms.Count == 0 ? null : "A ledger page is still waiting in " + rooms[0] + ". Pages lie open on tables and lecterns.";
                case Relics:
                    for (int i = 0; i < 3; i++) if (relicsFound == null || i >= relicsFound.Length || !relicsFound[i]) return "The ledger said where: " + RelicPlaces[i] + ".";
                    return null;
                case Forge: return "The chapel altar is on the ground floor, east of the courtyard. Stand at it with the relics and hold [E].";
                case Fight: return "The chapel bell calls him. Meet him in the great hall with the sword raised.";
                case Reconstruct: return "Open your journal [J] and reconstruct the bargain from the records and the ledger.";
                default: return null;
            }
        }
    }
}
