using System;

namespace HollowCurrent
{
    // Wave FD (r13 blind critic, hiding: "the open is a bag of three with one open; after he opens your door the next two near-misses
    // are guaranteed rattles"). The hide the Count keeps finding her in opens on a per-visit roll instead of a bag:
    //   - every visit rolls; the chance is BaseChance,
    //   - right after an open the next visit rolls AfterOpenChance (higher: he comes back hungrier), so an open is never followed by a safe
    //     window and back-to-back opens happen,
    //   - a soft pity floor starts only after PityAfter consecutive closed visits: each further closed visit adds PityStep (capped at 1).
    // Long-run open rate about 0.37 (0.32 over a 6-visit siege from a fresh start); no visit is ever certain to stay closed. Pure: the
    // caller passes the uniform roll in [0,1).
    public sealed class NearMissDice
    {
        public float BaseChance = .24f, AfterOpenChance = .45f, PityStep = .25f;
        public int PityAfter = 4;
        // Visits since the last open (or since the start).
        public int ClosedStreak { get; private set; }
        public bool LastOpened { get; private set; }
        public int Visits { get; private set; }
        public int Opens { get; private set; }
        // The chance the next visit opens.
        public float ChanceNow
        {
            get
            {
                if (LastOpened) return AfterOpenChance;
                if (ClosedStreak < PityAfter) return BaseChance;
                return Math.Min(1f, BaseChance + PityStep * (ClosedStreak - PityAfter + 1));
            }
        }
        // One visit: returns whether it opens; p is the chance it was rolled at.
        public bool Roll(float u, out float p)
        {
            p = ChanceNow;
            float roll = float.IsNaN(u) ? .5f : Math.Max(0, Math.Min(.99999f, u));
            bool open = roll < p;
            Visits++;
            if (open) { Opens++; ClosedStreak = 0; LastOpened = true; }
            else { ClosedStreak++; LastOpened = false; }
            return open;
        }
        public void Reset() { ClosedStreak = 0; LastOpened = false; }
    }
}
