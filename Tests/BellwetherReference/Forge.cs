using System;
using System.Collections.Generic;

namespace HollowCurrent
{
    public enum Relic { SilverBlade, HallowedHilt, Moonstone }

    // Which Blood Ledger entry reveals where each relic is hidden. The journal unlocks
    // a hint when the matching entry id is read.
    public static class LedgerHints
    {
        public static readonly IReadOnlyDictionary<Relic, string> EntryByRelic = new Dictionary<Relic, string>
        {
            { Relic.SilverBlade, "cc_ledger_steward" },
            { Relic.HallowedHilt, "cc_ledger_orrery" },
            { Relic.Moonstone, "cc_ledger_portrait" },
        };
        public static string EntryFor(Relic relic) => EntryByRelic[relic];
        public static bool TryRelicFor(string entryId, out Relic relic)
        {
            foreach (var pair in EntryByRelic)
                if (string.Equals(pair.Value, entryId, StringComparison.Ordinal)) { relic = pair.Key; return true; }
            relic = default; return false;
        }
    }

    // Pure deterministic relic and forging rules: no scene, provider, clock or random dependency.
    public sealed class Forge
    {
        public const float CraftSeconds = 9f;
        readonly HashSet<Relic> found = new HashSet<Relic>();
        readonly HashSet<Relic> hinted = new HashSet<Relic>();
        public bool AtAltar { get; set; }
        public bool HasSword { get; private set; }
        public float CraftProgress01 { get; private set; }
        public int RelicCount => found.Count;
        public bool CanCraft => !HasSword && found.Count == 3 && AtAltar;
        public bool Crafting => CanCraft && CraftProgress01 > 0;
        public event Action<Relic> RelicFound;
        public event Action Crafted;

        public bool Found(Relic relic)
        {
            if (!found.Add(relic)) return false;
            RelicFound?.Invoke(relic); return true;
        }
        public bool HasRelic(Relic relic) => found.Contains(relic);

        // Reading a ledger entry reveals the matching relic's hiding place. Returns true on first reveal.
        public bool ReadLedger(string entryId) => LedgerHints.TryRelicFor(entryId, out var relic) && hinted.Add(relic);
        public bool HintKnown(Relic relic) => hinted.Contains(relic);

        // Instant commit used by Tick when the hold completes (also callable directly by tests).
        public bool Craft()
        {
            if (!CanCraft) return false;
            HasSword = true; CraftProgress01 = 1;
            Crafted?.Invoke(); return true;
        }
        // Hold at the altar for CraftSeconds. Leaving the altar (or losing a relic precondition) resets progress.
        public void Tick(float dt)
        {
            if (dt < 0 || float.IsNaN(dt) || float.IsInfinity(dt)) throw new ArgumentOutOfRangeException(nameof(dt));
            if (HasSword) return;
            if (!CanCraft) { CraftProgress01 = 0; return; }
            CraftProgress01 = Math.Min(1, CraftProgress01 + dt / CraftSeconds);
            if (CraftProgress01 >= 1) Craft();
        }
        public void Reset() { found.Clear(); hinted.Clear(); HasSword = false; CraftProgress01 = 0; AtAltar = false; }
    }

    // Pure combat rules for the Count. The hunter AI polls SpeedMultiplier, CanBeHit and Tier;
    // the player-side code calls RegisterHit on a clean strike and PlayerGrabbed when he catches you.
    public sealed class Fight
    {
        public const int HitsToKill = 3;
        public const float StaggerSeconds = 2.5f;
        public const float RetreatSeconds = 20f;
        static readonly float[] Speeds = { 1.0f, 1.15f, 1.3f };

        public bool Active { get; private set; }
        public bool Dead { get; private set; }
        public bool Lost { get; private set; }
        public int Hits { get; private set; }
        public bool Staggered { get; private set; }
        public bool Retreating { get; private set; }
        public float StateTime { get; private set; }
        // Enraged tier = completed retreats (0 = first approach, 1 = after first hit, 2 = after second).
        public int Tier { get; private set; }
        public float SpeedMultiplier => Speeds[Math.Min(Tier, Speeds.Length - 1)];
        public bool Over => Dead || Lost;
        public bool CanBeHit => Active && !Over && !Staggered && !Retreating;
        public event Action HitLanded;
        public event Action Killed;
        public event Action PlayerLost;

        public void Begin()
        {
            Active = true; Dead = false; Lost = false; Hits = 0; Tier = 0;
            Staggered = false; Retreating = false; StateTime = 0;
        }
        // Returns true if the hit counted. Hits while staggered, retreating, over or before Begin are ignored.
        public bool RegisterHit()
        {
            if (!CanBeHit) return false;
            Hits++;
            HitLanded?.Invoke();
            if (Hits >= HitsToKill) { Dead = true; Staggered = false; Retreating = false; StateTime = 0; Killed?.Invoke(); return true; }
            Staggered = true; Retreating = false; StateTime = 0;
            return true;
        }
        public void PlayerGrabbed()
        {
            if (!Active || Over) return;
            Lost = true; Staggered = false; Retreating = false;
            PlayerLost?.Invoke();
        }
        public void Tick(float dt)
        {
            if (dt < 0 || float.IsNaN(dt) || float.IsInfinity(dt)) throw new ArgumentOutOfRangeException(nameof(dt));
            if (!Active || Over) return;
            if (!Staggered && !Retreating) return;
            StateTime += dt;
            if (Staggered && StateTime >= StaggerSeconds)
            {
                Staggered = false; Retreating = true; StateTime -= StaggerSeconds;
            }
            if (Retreating && StateTime >= RetreatSeconds)
            {
                Retreating = false; StateTime = 0; Tier++;
            }
        }
    }
}
