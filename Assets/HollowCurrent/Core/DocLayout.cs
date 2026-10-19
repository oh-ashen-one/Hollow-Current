using System;
using System.Collections.Generic;

namespace HollowCurrent
{
    // Engine-independent rectangle (design space, 1600 x 900).
    public struct LRect
    {
        public float X, Y, W, H;
        public LRect(float x, float y, float w, float h) { X = x; Y = y; W = w; H = h; }
        public float XMax => X + W;
        public float YMax => Y + H;
        public bool Overlaps(LRect o) => X < o.XMax && o.X < XMax && Y < o.YMax && o.Y < YMax;
        public bool Inside(LRect o) => X >= o.X - .001f && Y >= o.Y - .001f && XMax <= o.XMax + .001f && YMax <= o.YMax + .001f;
        public LRect Inflate(float p) => new LRect(X - p, Y - p, W + 2 * p, H + 2 * p);
        public override string ToString() => X.ToString("0.#") + "," + Y.ToString("0.#") + "," + W.ToString("0.#") + "," + H.ToString("0.#");
    }

    // Piece fd-journal (r13 critic section 4): the castle's torn-paper card (Resources/UI/BloodLedger/Panel.png, 1448 x 1086) is
    // drawn as a nine-slice at ONE fixed scale, so its corner ornaments (filigree, feather, wax seal, blood blots) always sit in the
    // four corners and never travel under text when a card of another size is stretched. This class is the single source of truth for
    // where those ornaments are; text must keep `Pad` clear of them and inside the paper's safe rect. Layouts (journal, end card) are
    // built from it, and `Verify` is the assertion behind `HC_DOC_LAYOUT overlaps=0`.
    public static class DocPanel
    {
        public const float TexW = 1448f, TexH = 1426f;   // PanelCard.png = Panel.png with its plain middle row tripled (tools/docui_panel_card.py)
        public const float Scale = .28f;      // design units per texture pixel for the four corner cells
        public const float Pad = 8f;          // clear space kept between text and any ornament / paper edge
        // Nine-slice cut lines in texture pixels (y down). Left col [0,ColL], right col [ColR,TexW], top row [0,RowT], bottom row [RowB,TexH].
        public const float ColL = 300f, ColR = 960f, RowT = 390f, RowB = 900f;
        // Worst-case torn-edge depth per side in texture pixels: paper (and so text) never reaches beyond this.
        public const float BandL = 70f, BandT = 110f, BandR = 75f, BandB = 120f;   // (paper, ornament and band values are measured on Panel.png; bottom values shifted by the 340 px of extra middle row)
        // Ornament bounds in texture pixels (x0,y0,x1,y1) for TL, TR, BL, BR (filigree + feather + seal + the blots that sit with them).
        public static readonly float[][] Ornament =
        {
            new[] { 50f, 95f, 290f, 380f },      // TL: filigree + blot
            new[] { 1125f, 95f, 1390f, 370f },   // TR: filigree + blot
            new[] { 38f, 920f, 230f, 1345f },    // BL: feather
            new[] { 975f, 920f, 1400f, 1400f },  // BR: wax seal + two blots + filigree
        };

        // Card(r) in the old code drew the texture on r grown by (18, 42) per side; the new frame keeps that outer size.
        public static LRect FrameFor(float x, float y, float w, float h) => new LRect(x - 18, y - 42, w + 36, h + 72);

        // The four ornament rectangles in design space for a frame: corner cells are drawn unscaled by the frame, only by Scale.
        public static LRect OrnamentRect(LRect f, int corner)
        {
            var o = Ornament[corner];
            bool right = corner == 1 || corner == 3, bottom = corner == 2 || corner == 3;
            float x0 = right ? f.XMax - (TexW - o[0]) * Scale : f.X + o[0] * Scale;
            float x1 = right ? f.XMax - (TexW - o[2]) * Scale : f.X + o[2] * Scale;
            float y0 = bottom ? f.YMax - (TexH - o[1]) * Scale : f.Y + o[1] * Scale;
            float y1 = bottom ? f.YMax - (TexH - o[3]) * Scale : f.Y + o[3] * Scale;
            return new LRect(x0, y0, x1 - x0, y1 - y0);
        }

        // Where paper (not torn edge) can be, and the rect text must stay inside (paper minus Pad).
        public static LRect Paper(LRect f) => new LRect(f.X + BandL * Scale, f.Y + BandT * Scale, f.W - (BandL + BandR) * Scale, f.H - (BandT + BandB) * Scale);
        public static LRect Safe(LRect f) => Paper(f).Inflate(-Pad);

        // Edge positions of the nine cells in design space (cols: 0..3, rows: 0..3).
        public static float[] CellX(LRect f) => new[] { f.X, f.X + ColL * Scale, f.XMax - (TexW - ColR) * Scale, f.XMax };
        public static float[] CellY(LRect f) => new[] { f.Y, f.Y + RowT * Scale, f.YMax - (TexH - RowB) * Scale, f.YMax };

        // Leftmost text column that clears both left-hand ornaments, the right-hand limit of the paper, and the right limit for text
        // that sits level with a bottom ornament (the seal corner).
        public static float TextLeft(LRect f) => (float)Math.Ceiling(Math.Max(OrnamentRect(f, 0).XMax, OrnamentRect(f, 2).XMax) + Pad);
        public static float TextRight(LRect f) => (float)Math.Floor(Safe(f).XMax);
        public static float LowerRight(LRect f) => (float)Math.Floor(Math.Min(OrnamentRect(f, 3).X, OrnamentRect(f, 1).X) - Pad);
        public static float TopClear(LRect f) => (float)Math.Ceiling(Math.Max(OrnamentRect(f, 0).YMax, OrnamentRect(f, 1).YMax) + Pad);
    }

    // The set of frames and text/control rectangles one screen declares. Verify() is the layout assertion.
    public sealed class DocLayout
    {
        public readonly string Screen;
        public readonly List<LRect> Frames = new List<LRect>();
        public readonly List<string> Names = new List<string>();
        public readonly List<LRect> Texts = new List<LRect>();
        public DocLayout(string screen) { Screen = screen; }
        public LRect Frame(float x, float y, float w, float h) { var f = DocPanel.FrameFor(x, y, w, h); Frames.Add(f); return f; }
        public LRect Text(string name, float x, float y, float w, float h) { var r = new LRect(x, y, w, h); Names.Add(name); Texts.Add(r); return r; }

        // A text rect that touches the paper of a frame must lie inside that frame's safe rect and keep Pad clear of every ornament.
        // Returns one message per violation; overlaps = Count.
        public List<string> Verify()
        {
            var bad = new List<string>();
            foreach (var f in Frames)
            {
                var paper = DocPanel.Paper(f); var safe = DocPanel.Safe(f);
                for (int i = 0; i < Texts.Count; i++)
                {
                    var t = Texts[i];
                    if (!t.Overlaps(paper)) continue;
                    if (!t.Inside(safe)) bad.Add(Screen + ":" + Names[i] + " [" + t + "] leaves the safe rect [" + safe + "]");
                    for (int c = 0; c < 4; c++)
                    {
                        var o = DocPanel.OrnamentRect(f, c).Inflate(DocPanel.Pad);
                        if (t.Overlaps(o)) bad.Add(Screen + ":" + Names[i] + " [" + t + "] overlaps ornament " + c + " [" + o + "]");
                    }
                }
            }
            return bad;
        }

        // ---- the castle journal (two cards) ------------------------------------------------------------------------------------
        public sealed class JournalSpec
        {
            public DocLayout Layout;
            public LRect LeftFrame, RightFrame;
            public LRect LeftContent = new LRect(80, 229, 610, 568), RightContent = new LRect(728, 229, 792, 568);
            public float ColL, ColR, LowerR;                      // left card text column; LowerR = right limit beside the seal corner
            public float RecordY0 = 293, RecordPitch = 86;
            public LRect Kicker, Rule, LedgerKicker, PulseLabel, Pulse;
            public LRect[] Bar = new LRect[3], Tag = new LRect[3], Evidence = new LRect[3], Rows = new LRect[3];
            public float X0, ChoiceW = 226, ChoiceStep = 238;       // right card choice grid
            public float[] ChoiceY = { 264, 391, 518 };
            public LRect[] ChoiceLabel = new LRect[3];
            public LRect Compare, Submit;
        }

        public static JournalSpec Journal()
        {
            var s = new JournalSpec { Layout = new DocLayout("journal") };
            var L = s.Layout;
            s.LeftFrame = L.Frame(s.LeftContent.X, s.LeftContent.Y, s.LeftContent.W, s.LeftContent.H);
            s.RightFrame = L.Frame(s.RightContent.X, s.RightContent.Y, s.RightContent.W, s.RightContent.H);
            s.ColL = DocPanel.TextLeft(s.LeftFrame); s.ColR = DocPanel.TextRight(s.LeftFrame); s.LowerR = DocPanel.LowerRight(s.LeftFrame);
            s.Kicker = L.Text("records-kicker", s.ColL, 252, 260, 24);
            for (int i = 0; i < 3; i++)
            {
                float y = s.RecordY0 + i * s.RecordPitch;
                s.Bar[i] = L.Text("bar" + i, s.ColL, y + 4, 3, 62);
                s.Tag[i] = L.Text("tag" + i, s.ColL + 19, y, 200, 24);
                s.Evidence[i] = L.Text("evidence" + i, s.ColL + 19, y + 26, s.ColR - s.ColL - 19, 56);
            }
            float after = s.RecordY0 + 3 * s.RecordPitch;                // 551
            s.Rule = L.Text("rule", s.ColL, after, s.ColR - s.ColL, 1);
            s.LedgerKicker = L.Text("ledger-kicker", s.ColL, after + 23, 260, 24);
            for (int i = 0; i < 3; i++) s.Rows[i] = L.Text("doc-row" + i, s.ColL, after + 51 + i * 34, s.LowerR - s.ColL, 32);
            s.PulseLabel = L.Text("pulse-label", s.ColL, after + 176, 70, 24);
            s.Pulse = L.Text("pulse", s.ColL + 72, after + 161, s.LowerR - (s.ColL + 72), 54);
            // right card
            s.X0 = DocPanel.TextLeft(s.RightFrame);
            float rightLimit = DocPanel.TextRight(s.RightFrame), rowLimit = DocPanel.LowerRight(s.RightFrame);
            float gap = (rightLimit - s.X0 - 3 * s.ChoiceW) / 2; if (gap > 12) gap = 12; s.ChoiceStep = s.ChoiceW + gap;
            for (int i = 0; i < 3; i++)
            {
                s.ChoiceLabel[i] = L.Text("choice-label" + i, s.X0, s.ChoiceY[i], 420, 24);
                for (int k = 0; k < 3; k++) L.Text("choice" + i + k, s.X0 + k * s.ChoiceStep, s.ChoiceY[i] + 35, s.ChoiceW, 66);
            }
            s.Compare = L.Text("compare", s.X0, 636, rowLimit - s.X0, 44);
            s.Submit = L.Text("submit", s.X0, 694, rowLimit - s.X0, 46);
            return s;
        }

        // ---- the castle end card (won / killed) ---------------------------------------------------------------------------------
        public sealed class EndingSpec
        {
            public DocLayout Layout;
            public LRect Frame, Content = new LRect(45, 145, 850, 660);
            public float X;                                          // the text column
            public LRect Kicker, Hero, Body, New, Exit;
            public LRect Retry, NewSolo;                             // lost card only: RETRY CHECKPOINT beside NEW INVESTIGATION; NewSolo = NEW INVESTIGATION when there is no checkpoint
            public LRect[] Value = new LRect[3], Label = new LRect[3];
            public float[] MetricX = new float[3];
            public float MetricY = 578;
            public float[] Separator = new float[2];
        }

        // lost = the "Signal lost" card (piece fe-ui): the same nine-slice card and text column as the won / killed card, plus the checkpoint button.
        public static EndingSpec Ending(bool lost = false)
        {
            var s = new EndingSpec { Layout = new DocLayout(lost ? "ending_lost" : "ending") };
            var L = s.Layout;
            s.Frame = L.Frame(s.Content.X, s.Content.Y, s.Content.W, s.Content.H);
            s.X = DocPanel.TextLeft(s.Frame);
            float right = DocPanel.TextRight(s.Frame);
            s.Kicker = L.Text("kicker", s.X, 175, 320, 24);
            s.Hero = L.Text("hero", s.X, 214, 635, 188);
            s.Body = L.Text("body", s.X + 4, 428, 560, 125);
            float[] dx = { 4, 207, 427 };
            for (int i = 0; i < 3; i++)
            {
                s.MetricX[i] = s.X + dx[i];
                s.Value[i] = L.Text("metric" + i, s.MetricX[i], s.MetricY, 180, 55);
                s.Label[i] = L.Text("metric-label" + i, s.MetricX[i], s.MetricY + 65, 170, 24);
            }
            s.Separator[0] = s.MetricX[1] - 24; s.Separator[1] = s.MetricX[2] - 24;
            if (lost)
            {
                float lowerR = DocPanel.LowerRight(s.Frame), retryW = 280, gap = 12;
                s.Retry = L.Text("retry", s.X, 684, retryW, 52);
                s.New = L.Text("new", s.X + retryW + gap, 684, lowerR - (s.X + retryW + gap), 52);
                s.NewSolo = L.Text("new-solo", s.X, 684, 400, 52);
            }
            else s.New = L.Text("new", s.X, 684, 400, 52);
            s.Exit = L.Text("exit", s.X, 742, 310, 40);
            return s;
        }

        // ---- the settings card (castle, three pages; piece fe-ui) ---------------------------------------------------------------
        // Every control of every page is declared, so one verification covers all three pages. Rows that end below the seal corner's top
        // (LowY) are only LowW wide so their right-hand ON / OFF switch stays left of the seal and blots.
        public sealed class SettingsSpec
        {
            public DocLayout Layout;
            public LRect Frame, Content = new LRect(500, 173, 1020, 635);
            public float X0, W = 924, LowY, LowW;
            public const float TrackY = 202, RowH = 98, ToggleH = 37;
            public static readonly float[] P0Slider = { 244, 356, 468 };
            public const float P0Rule = 579;
            public static readonly float[] P0Toggle = { 597, 650, 703 };
            public static readonly float[] P1Slider = { 256, 372 };
            public const float P1Toggle = 487;
            public static readonly LRect P1Preview = new LRect(650, 555, 740, 220);
            public const float P2Toggle1 = 244, P2Note1 = 288, P2Rule = 360, P2Toggle2 = 380, P2Note2 = 424, P2Slider = 500, P2Input = 612;
            public LRect P2Bar;
            // Width of a toggle row whose top is y: full width above the seal corner, LowW beside it.
            public float ToggleW(float y) => y + ToggleH > LowY ? LowW : W;
        }

        public static SettingsSpec Settings()
        {
            var s = new SettingsSpec { Layout = new DocLayout("settings") };
            var L = s.Layout;
            s.Frame = L.Frame(s.Content.X, s.Content.Y, s.Content.W, s.Content.H);
            s.X0 = DocPanel.TextLeft(s.Frame);
            s.LowY = DocPanel.OrnamentRect(s.Frame, 3).Y - DocPanel.Pad;
            s.LowW = DocPanel.LowerRight(s.Frame) - s.X0;
            float x = s.X0, w = s.W, h = SettingsSpec.RowH;
            L.Text("p0-track", x, SettingsSpec.TrackY, 420, 24);
            for (int i = 0; i < 3; i++) L.Text("p0-slider" + i, x, SettingsSpec.P0Slider[i], w, h);
            L.Text("p0-rule", x, SettingsSpec.P0Rule, w, 1);
            for (int i = 0; i < 3; i++) L.Text("p0-toggle" + i, x, SettingsSpec.P0Toggle[i], s.ToggleW(SettingsSpec.P0Toggle[i]), SettingsSpec.ToggleH);
            L.Text("p1-track", x, SettingsSpec.TrackY, 420, 24);
            for (int i = 0; i < 2; i++) L.Text("p1-slider" + i, x, SettingsSpec.P1Slider[i], w, h);
            L.Text("p1-toggle", x, SettingsSpec.P1Toggle, s.ToggleW(SettingsSpec.P1Toggle), SettingsSpec.ToggleH);
            var pv = SettingsSpec.P1Preview; L.Text("p1-preview", pv.X, pv.Y, pv.W, pv.H);
            L.Text("p2-track", x, SettingsSpec.TrackY, 420, 24);
            L.Text("p2-toggle1", x, SettingsSpec.P2Toggle1, s.ToggleW(SettingsSpec.P2Toggle1), SettingsSpec.ToggleH);
            L.Text("p2-note1", x, SettingsSpec.P2Note1, 820, 60);
            L.Text("p2-rule", x, SettingsSpec.P2Rule, w, 1);
            L.Text("p2-toggle2", x, SettingsSpec.P2Toggle2, s.ToggleW(SettingsSpec.P2Toggle2), SettingsSpec.ToggleH);
            L.Text("p2-note2", x, SettingsSpec.P2Note2, 820, 60);
            L.Text("p2-slider", x, SettingsSpec.P2Slider, w, h);
            L.Text("p2-input-label", x, SettingsSpec.P2Input, 70, 24);
            s.P2Bar = L.Text("p2-bar", x + 82, SettingsSpec.P2Input + 2, w - 82, 20);
            return s;
        }
    }
}
