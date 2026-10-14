using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using HollowCurrent;

// Bellwether differential fuzz (tools/bellwether_fuzz.py). Compiled twice: against the reference
// Core snapshot (Tests/BellwetherReference, c4b24c1) and against the current Core. Uses only API
// both share. Each seed drives a Bellwether Investigation with live random dice through random
// operations and logs the full observable state after every step, plus every message and phase
// event. Prints "lines=<n> sha256=<hex>"; with a third argument, also writes the trace to that file.
static class BellwetherFuzz
{
    static Investigation Make(StringBuilder sb, int seed)
    {
        var dice = new Random(seed * 7 + 1);
        var c = new Investigation(CaseBook.Bellwether, () => (float)dice.NextDouble());
        c.Message += m => sb.Append("M:").Append(m).Append('|');
        c.Changed += p => sb.Append("C:").Append(p).Append('|');
        return c;
    }
    static int Main(string[] args)
    {
        int seeds = args.Length > 0 ? int.Parse(args[0]) : 4000, steps = args.Length > 1 ? int.Parse(args[1]) : 300;
        StreamWriter dump = args.Length > 2 ? new StreamWriter(args[2]) : null;
        var sha = SHA256.Create(); long lines = 0; var sb = new StringBuilder();
        for (int seed = 0; seed < seeds; seed++)
        {
            var rng = new Random(seed); var c = Make(sb, seed); Investigation.Checkpoint cp = null;
            for (int step = 0; step < steps; step++)
            {
                int op = rng.Next(100);
                if (op < 40) c.Tick((float)(rng.NextDouble() * (rng.Next(10) == 0 ? 60 : 3)), rng.Next(2) == 0);
                else if (op < 45) c.Tick((float)(rng.NextDouble() * 3), rng.Next(2) == 0, rng.Next(2) == 0, rng.Next(2) == 0);
                else if (op < 51) c.Record((Evidence)rng.Next(3));
                else if (op < 54) c.Equip(); else if (op < 57) c.EnterHouse();
                else if (op < 61) c.Read(rng.Next(2) == 0 ? CaseBook.Bellwether.NoteTitles[rng.Next(3)] : "x");
                else if (op < 65) c.Conclude((AnchorKind)rng.Next(3), (HauntingPattern)rng.Next(3), (ClosureMethod)rng.Next(3));
                else if (op < 67) c.RestorePower();
                else if (op < 72) c.Seal((float)(rng.NextDouble() * 8), rng.Next(4) != 0, rng.Next(4) != 0);
                else if (op < 74) c.BeginWarning();
                else if (op < 77) c.Die();
                else if (op < 79) c.FinishAtVan();
                else if (op < 82) cp = c.SaveCheckpoint();
                else if (op < 85) { if (cp != null) { if (rng.Next(2) == 0) c.RestoreCheckpoint(cp, rng.Next(3) != 0); else { c = Make(sb, seed + step); c.RestoreCheckpoint(cp); } } }
                else if (op < 87) c.Invulnerable = !c.Invulnerable;
                else if (op < 88) c.WinByKill();
                else if (op < 92) c.AddDread((float)(rng.NextDouble() * 60 - 20));
                else if (op < 95) sb.Append(c.TryEscalate());
                else if (op < 97) sb.Append(c.BeginNoTellHunt());
                sb.Append(c.Phase).Append(',').Append(c.PhaseTime.ToString("R")).Append(',').Append(c.Elapsed.ToString("R")).Append(',').Append(c.Hunts).Append(c.Survived).Append(c.BreakerOn).Append(c.Diagnosed).Append(c.Equipped).Append(',')
                  .Append(c.SealProgress.ToString("R")).Append(c.MysteryReady).Append(c.WrongDiagnoses).Append(c.BlockedDeaths).Append(c.ContextCount).Append(c.Evidence.Count).Append(c.Goal)
                  .Append(c.Dread.ToString("R")).Append(c.Lies).Append(c.DoorHeld).Append(c.FalseWarning).Append(c.NoTellHunt).Append(c.HuntActive).Append(c.Deaths).Append(c.DeathPenaltyApplied).Append(c.WarningLength.ToString("R")).Append('\n');
                lines++;
                var bytes = Encoding.UTF8.GetBytes(sb.ToString()); sha.TransformBlock(bytes, 0, bytes.Length, null, 0);
                if (dump != null) dump.Write(sb.ToString());
                sb.Clear();
            }
        }
        sha.TransformFinalBlock(new byte[0], 0, 0);
        dump?.Dispose();
        Console.WriteLine("lines=" + lines + " sha256=" + BitConverter.ToString(sha.Hash).Replace("-", "").ToLowerInvariant());
        return 0;
    }
}
