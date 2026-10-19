using System;
using System.Collections.Generic;

namespace HollowCurrent
{
    // fe3-grab (r13 FE2 final build: left side snatch m10 0 of 24 open grabs, right m11 and drop-spin m12 3 each). Pure rules for the
    // grab variety deck and the side snatch's side choice and commit gate, shared by Presence (Unity geometry) and RulesTests (a
    // 2D model). Angles are degrees, yaw is Unity's (positive = clockwise seen from above = toward her right), a bearing is
    // positive on her right.
    //
    // The deck (PlayerPrefs hc.castle.grabdeck): committed open-floor grabs by mode, plus, per mode, how many plans in a row were made
    // and not committed. It persisted five left-snatch commits from FE2 r1/r2 into r3, so the right side was always tried first and
    // left plans came only where the right flank was shut. Now:
    //   - a mode planned and not committed StarveAfter times in a row is rebalanced (a side drops below its pair, so it is tried first;
    //     any other mode loses one count), logged by the caller;
    //   - the saved state is normalised: a side never leads its pair by more than PairMaxLead, and no scored group sits more than
    //     MaxSurplus counts over its target share, so a saved deck cannot shut a mode out of the next run.
    public sealed class GrabDeck
    {
        public readonly int[] Commits, Streak;
        public int StarveAfter = 2, FadeTotal = 60, PairMaxLead = 1;
        public float MaxSurplus = 2f;
        public GrabDeck(int modes) { Commits = new int[modes]; Streak = new int[modes]; }
        public int Modes => Commits.Length;
        bool Valid(int m) => m >= 0 && m < Commits.Length;

        // "c0,c1,...,cN" (the FD/FE format) or "c0,...,cN;s0,...,sN".
        public static GrabDeck Parse(string text, int modes)
        {
            var d = new GrabDeck(modes);
            if (string.IsNullOrEmpty(text)) return d;
            var halves = text.Split(';');
            Fill(halves[0], d.Commits);
            if (halves.Length > 1) Fill(halves[1], d.Streak);
            return d;
        }
        static void Fill(string csv, int[] into)
        {
            var parts = csv.Split(',');
            for (int i = 0; i < parts.Length && i < into.Length; i++) { int.TryParse(parts[i], out int v); into[i] = Math.Max(0, v); }
        }
        public string Serialize() => string.Join(",", Array.ConvertAll(Commits, n => n.ToString())) + ";" + string.Join(",", Array.ConvertAll(Streak, n => n.ToString()));

        public void Committed(int mode)
        {
            if (!Valid(mode)) return;
            Commits[mode]++; Streak[mode] = 0;
            int total = 0; foreach (var n in Commits) total += n;
            if (total > FadeTotal) for (int i = 0; i < Commits.Length; i++) Commits[i] /= 2;   // old history fades
        }

        // A plan for this mode ended without its commit. Returns a note when it rebalanced (null otherwise). pair = the other side's
        // mode for a side snatch, -1 for any other mode.
        public string PlannedNotCommitted(int mode, int pair)
        {
            if (!Valid(mode)) return null;
            Streak[mode]++;
            if (Streak[mode] < StarveAfter) return null;
            int before = Commits[mode];
            if (Valid(pair)) Commits[mode] = Math.Min(Commits[mode], Math.Max(0, Commits[pair] - 1));
            else Commits[mode] = Math.Max(0, Commits[mode] - 1);
            string note = "mode " + mode + " planned and not committed " + Streak[mode] + " times: count " + before + " -> " + Commits[mode];
            Streak[mode] = 0;
            return note;
        }

        // Normalise before saving. pairs: side modes that are ordered against each other; groups: scored modes (a group may hold
        // several modes, e.g. both sides) with their target shares.
        public void Normalize(int[][] pairs, int[][] groups, float[] targets)
        {
            for (int pass = 0; pass < 64; pass++)
            {
                bool changed = false;
                if (pairs != null)
                    foreach (var p in pairs)
                    {
                        if (p.Length != 2 || !Valid(p[0]) || !Valid(p[1])) continue;
                        int lo = Math.Min(Commits[p[0]], Commits[p[1]]);
                        for (int k = 0; k < 2; k++) if (Commits[p[k]] > lo + PairMaxLead) { Commits[p[k]] = lo + PairMaxLead; changed = true; }
                    }
                if (groups != null && targets != null)
                {
                    int total = 0; foreach (var g in groups) foreach (int m in g) if (Valid(m)) total += Commits[m];
                    for (int gi = 0; gi < groups.Length && gi < targets.Length; gi++)
                    {
                        int have = 0; foreach (int m in groups[gi]) if (Valid(m)) have += Commits[m];
                        // have <= t * (1 + rest + have) + MaxSurplus, i.e. have <= (t * (1 + rest) + MaxSurplus) / (1 - t).
                        float t = Math.Min(.95f, Math.Max(0, targets[gi]));int rest = total - have;
                        int cap = (int)Math.Floor((t * (1 + rest) + MaxSurplus) / (1 - t) + 1e-4f);
                        int over = have - cap;
                        while (over-- > 0)
                        {
                            int big = -1; foreach (int m in groups[gi]) if (Valid(m) && (big < 0 || Commits[m] > Commits[big])) big = m;
                            if (big < 0 || Commits[big] == 0) break;
                            Commits[big]--; total--; changed = true;
                        }
                    }
                }
                if (!changed) break;
            }
        }
    }

    public static class SideGrabRules
    {
        // Unity yaw rotation of a flat vector (x, z) by deg (clockwise from above).
        public static void Rotate(float x, float z, float deg, out float rx, out float rz)
        {
            double a = deg * Math.PI / 180; float c = (float)Math.Cos(a), s = (float)Math.Sin(a);
            rx = x * c + z * s; rz = -x * s + z * c;
        }
        // Unity's Vector3.SignedAngle(from, to, up) on the flat plane: + when 'to' is to the right of 'from'.
        public static float Bearing(float fx, float fz, float tx, float tz)
        {
            if (fx * fx + fz * fz < 1e-8f || tx * tx + tz * tz < 1e-8f) return 0;
            return (float)(Math.Atan2(fz * tx - fx * tz, fx * tx + fz * tz) * 180 / Math.PI);
        }
        // The side frame: her view (fx, fz, normalised) turned yaw toward sign (-1 = her left, +1 = her right).
        public static void Frame(float fx, float fz, int sign, float yaw, out float ffx, out float ffz) => Rotate(fx, fz, yaw * sign, out ffx, out ffz);
        // The side clip's flank start relative to her feet in a frame (ffx, ffz): the clip's origin is standOff ahead; he starts
        // lateral to the sign side and back toward her (the clip's sampled skull start, his local frame facing her).
        public static void Start(float ffx, float ffz, int sign, float standOff, float lateral, float back, out float sx, out float sz)
        {
            float rx = ffz, rz = -ffx;   // Cross(up, f): her right
            sx = ffx * (standOff - back) + rx * sign * lateral;
            sz = ffz * (standOff - back) + rz * sign * lateral;
        }
        // The start point's bearing in its own frame (the same on both sides, mirrored).
        public static float StartFrameBearing(float standOff, float lateral, float back) => (float)(Math.Atan2(lateral, standOff - back) * 180 / Math.PI);

        // Which side is tried first: the one committed less often; a tie goes to the side he is on, an exact tie (bearing within 1 deg)
        // alternates with the deck's parity. Mirror-symmetric: mirrored inputs give the mirrored side.
        public static int FirstSide(int commitsLeft, int commitsRight, float bearing, int parity)
        {
            if (commitsLeft != commitsRight) return commitsLeft < commitsRight ? -1 : 1;
            if (Math.Abs(bearing) >= 1) return bearing < 0 ? -1 : 1;
            return (parity & 1) == 0 ? -1 : 1;
        }

        // Plan a side: feasible(sign, yaw) returns null when that frame's flank is open and its start point reachable, else the reason.
        // Both sides are evaluated (every side plan logs both); the first side that is feasible is chosen, at its first feasible yaw.
        public sealed class SidePlan
        {
            public int Sign; public float Yaw;
            public bool LeftOk, RightOk; public float LeftYaw, RightYaw; public string LeftWhy = "", RightWhy = "";
        }
        public static SidePlan Choose(Func<int, float, string> feasible, float[] yaws, int first)
        {
            var plan = new SidePlan();
            foreach (int sg in new[] { -1, 1 })
            {
                string firstWhy = null; bool ok = false; float okYaw = 0;
                foreach (float y in yaws)
                {
                    string w = feasible(sg, y);
                    if (w == null) { ok = true; okYaw = y; break; }
                    if (firstWhy == null) firstWhy = w;
                }
                if (sg < 0) { plan.LeftOk = ok; plan.LeftYaw = okYaw; plan.LeftWhy = ok ? "" : firstWhy ?? "no frame"; }
                else { plan.RightOk = ok; plan.RightYaw = okYaw; plan.RightWhy = ok ? "" : firstWhy ?? "no frame"; }
            }
            foreach (int sg in new[] { first, -first })
            {
                bool ok = sg < 0 ? plan.LeftOk : plan.RightOk;
                if (ok) { plan.Sign = sg; plan.Yaw = sg < 0 ? plan.LeftYaw : plan.RightYaw; break; }
            }
            return plan;
        }

        // The commit gate of a planned side snatch, judged in the frame it was planned in (the frame the clip is placed in), on the planned
        // side. FE2 judged his bearing from her view and took the side from its sign: a frame turned away from his side (-15) put the start
        // at 39 deg of view bearing, 4 deg over the 35 deg gate, and an approach across her front entered the commit radius under it.
        public static bool CommitOk(int planSign, float frameBearing, float min, float max, out string why)
        {
            why = "";
            if (planSign == 0) { why = "no planned side"; return false; }
            if (Math.Sign(frameBearing) != planSign) { why = "he is on the " + (frameBearing < 0 ? "left" : "right") + " of the planned " + (planSign < 0 ? "left" : "right") + " frame (" + frameBearing.ToString("F0") + ")"; return false; }
            float ab = Math.Abs(frameBearing);
            if (ab < min) { why = "frame bearing " + ab.ToString("F0") + " under " + min.ToString("F0"); return false; }
            if (ab > max) { why = "frame bearing " + ab.ToString("F0") + " over " + max.ToString("F0"); return false; }
            return true;
        }

        // fe3-grab seat rule for side and spin grabs (the drop/crawl rule of fe-count): the clip's first skull must be within maxGap of the
        // last dive sample. Over it the commit is held (he keeps flying onto the clip's start) for at most holdSeconds; after that the
        // grab commits with a hard cut on frame 0 (no slide).
        public enum Seat { Commit, Hold, Cut }
        public static Seat SeatDecision(float gap, float heldSeconds, float maxGap, float holdSeconds)
        {
            if (gap <= maxGap) return Seat.Commit;
            return heldSeconds < holdSeconds ? Seat.Hold : Seat.Cut;
        }
    }
}
