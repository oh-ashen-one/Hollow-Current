using System;
using System.Collections.Generic;
using UnityEngine;

namespace HollowCurrent
{
    // Found-document presentation (piece fb-docui, owner feedback 10). Replaces the web-style torn card, big plain serif
    // title, repeated header and boxed FILE & CLOSE button with a full-bleed paper sheet per kind of document:
    //   castle ledger / inscription : aged vellum, IM Fell English, drop cap, red ledger margins, ruled lines
    //   castle letter / tally       : lighter vellum with folds and a broken seal, La Belle Aurore hand with line jitter
    //   Bellwether field report     : modern ruled sheet, Courier Prime typescript, typed subject line, pinned print
    // Everything is drawn in screen pixels (GUI.matrix is reset) so the type stays crisp: layout is authored in the
    // 1600 x 900 design space and multiplied by the same scale Interface.OnGUI uses. Text is laid out line by line
    // (own word wrap, <= ~70 characters, 26 to 28 design units = 31 to 34 px at 1920 x 1080), so long notes scroll in whole
    // lines (wheel, arrow keys, PageUp/PageDown, controller stick) instead of overflowing. Missing fonts fall back to
    // Cinzel and then the interface serif; a missing paper sheet returns false from Draw and Interface uses its old card.
    public sealed class DocumentUi
    {
        const string FontFolder = "UI/Fonts/", PaperFolder = "UI/Documents/";
        // Ascent / em of each face (hhea), used to put a baseline exactly on the ruled line.
        const float AscFell = .905f, AscBelle = 1.077f, AscCourier = .781f;

        Font fell, fellItalic, belle, courier, courierBold, cinzel, fallback;
        Texture2D ledgerPaper, letterPaper, fieldPaper;
        bool loaded;
        public bool FontsComplete { get; private set; }
        public bool Ready => loaded && ledgerPaper && letterPaper && fieldPaper;

        // Frame state (pixels).
        float s, ox, oy;
        Material material;
        Rect closeRect;
        string closeText = "", cacheKey = "";
        Color closeColor;
        GUIStyle closeStyle;
        public Rect CloseRect => closeRect;

        // Layout cache and scroll state.
        sealed class Row { public string text; public float pos; public float indent; }
        readonly List<Row> rows = new List<Row>(64);
        DocumentText doc;
        string layoutKey = "", lastDocId = "";
        int scroll, maxScroll, visibleRows;
        float nextPad;
        float pitch, bodyTop, bodyBottom, colX, colW, gap, size, asc;
        bool hand, centered, dropCap, ruled, art;
        Font bodyFont;
        float capSize;
        string capChar = "";
        readonly Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();
        readonly Dictionary<long, float> glyph = new Dictionary<long, float>();
        readonly GUIContent probe = new GUIContent();

        // Ink colours (sRGB). tools/docui_paper.py --check measures the contrast of exactly these against each sheet.
        static readonly Color VellumInk = new Color(.17f, .11f, .075f), VellumRed = new Color(.36f, .05f, .045f), VellumKicker = new Color(.31f, .10f, .07f);
        static readonly Color FieldInk = new Color(.10f, .11f, .16f), FieldRed = new Color(.50f, .08f, .08f), FieldKicker = new Color(.22f, .24f, .30f);
        static readonly Color VellumRule = new Color(.36f, .25f, .16f, .24f), VellumMargin = new Color(.50f, .10f, .08f, .60f);
        static readonly Color FieldRule = new Color(.33f, .47f, .72f, .30f), FieldMargin = new Color(.80f, .28f, .28f, .50f);

        // Idempotent. `serif` is the interface's dynamic serif, the last fallback.
        public void Load(Font serif)
        {
            if (loaded) return;
            loaded = true;
            fallback = serif;
            cinzel = Resources.Load<Font>(FontFolder + "Cinzel-Medium");
            fell = Resources.Load<Font>(FontFolder + "IMFellEnglish-Regular");
            fellItalic = Resources.Load<Font>(FontFolder + "IMFellEnglish-Italic");
            belle = Resources.Load<Font>(FontFolder + "LaBelleAurore-Regular");
            courier = Resources.Load<Font>(FontFolder + "CourierPrime-Regular");
            courierBold = Resources.Load<Font>(FontFolder + "CourierPrime-Bold");
            FontsComplete = fell && fellItalic && belle && courier && courierBold;
            if (!fell) fell = cinzel ? cinzel : serif;
            if (!fellItalic) fellItalic = fell;
            if (!belle) belle = fellItalic;
            if (!courier) courier = cinzel ? cinzel : serif;
            if (!courierBold) courierBold = courier;
            ledgerPaper = Resources.Load<Texture2D>(PaperFolder + "DocLedger");
            letterPaper = Resources.Load<Texture2D>(PaperFolder + "DocLetter");
            fieldPaper = Resources.Load<Texture2D>(PaperFolder + "DocField");
            Debug.Log("HC_DOC_UI fonts_complete=" + FontsComplete + " fell=" + (Resources.Load<Font>(FontFolder + "IMFellEnglish-Regular") != null) + " belle=" + (Resources.Load<Font>(FontFolder + "LaBelleAurore-Regular") != null) +
                      " courier=" + (Resources.Load<Font>(FontFolder + "CourierPrime-Regular") != null) + " paper=" + (ledgerPaper != null && letterPaper != null && fieldPaper != null));
        }

        // ---- small drawing helpers (pixels) ---------------------------------------------------------------

        Rect R(float x, float y, float w, float h) => new Rect(ox + x * s, oy + y * s, w * s, h * s);

        static void Fill(Rect r, Color c)
        {
            var old = GUI.color; GUI.color = c.linear; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old;
        }

        GUIStyle Style(Font f, float design, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            string key = (f ? f.GetInstanceID() : 0) + "/" + design + "/" + (int)anchor;
            if (!styles.TryGetValue(key, out var st))
            {
                st = new GUIStyle(GUI.skin.label) { wordWrap = false, clipping = TextClipping.Overflow, richText = false, alignment = anchor };
                st.padding = new RectOffset(); st.margin = new RectOffset(); st.font = f;
                styles[key] = st;
            }
            st.fontSize = Mathf.Max(8, Mathf.RoundToInt(design * s));
            return st;
        }

        float Measure(GUIStyle st, string t)
        {
            probe.text = t; return st.CalcSize(probe).x / s;
        }

        void Text(float x, float y, float w, float h, string t, GUIStyle st, Color c)
        {
            st.normal.textColor = c; GUI.Label(R(x, y, w, h), t, st);
        }

        // Letter-spaced capitals; returns the design width.
        float Tracked(float x, float y, string t, Font f, float design, Color c, float spacing, bool right = false, bool draw = true)
        {
            var st = Style(f, design);
            float total = 0;
            var widths = new float[t.Length];
            for (int i = 0; i < t.Length; i++)
            {
                long key = ((long)(f ? f.GetInstanceID() & 0xFFFFF : 0) << 32) | ((long)st.fontSize << 16) | t[i];
                if (!glyph.TryGetValue(key, out float w)) { w = Measure(st, t[i].ToString()); glyph[key] = w; }
                widths[i] = w + spacing; total += widths[i];
            }
            if (!draw) return total;
            if (right) x -= total;
            float cx = x;
            for (int i = 0; i < t.Length; i++) { if (t[i] != ' ') Text(cx, y, 40, design * 1.6f, t[i].ToString(), st, c); cx += widths[i]; }
            return total;
        }

        static float Hash(int i, int k) => Mathf.Repeat(Mathf.Sin(i * 12.9898f + k * 78.233f) * 43758.5453f, 1f);

        // ---- layout ----------------------------------------------------------------------------------------

        void Frame(DocKind kind, bool hasArt)
        {
            art = hasArt;
            hand = kind == DocKind.Letter || kind == DocKind.Tally;
            centered = kind == DocKind.Inscription;
            dropCap = kind == DocKind.Ledger;
            ruled = kind == DocKind.Ledger || kind == DocKind.FieldReport;
            gap = ruled ? 1f : .6f;
            switch (kind)
            {
                case DocKind.FieldReport:
                    bodyFont = courier; size = 25; asc = AscCourier; pitch = 36; bodyTop = art ? 282 : 252;
                    colX = art ? 700 : 350; colW = art ? 750 : 900; break;
                case DocKind.Letter:
                case DocKind.Tally:
                    bodyFont = belle; size = 28; asc = AscBelle; pitch = 44; bodyTop = 238; colX = 370; colW = 860; break;
                default: // Ledger, Inscription
                    bodyFont = fell; size = 27; asc = AscFell; pitch = 39; bodyTop = 236; colX = 400; colW = 800; break;
            }
            bodyBottom = 790;
            visibleRows = Mathf.Max(1, Mathf.FloorToInt((bodyBottom - bodyTop) / pitch));
        }

        void Layout(string key)
        {
            layoutKey = key; rows.Clear();
            var st = Style(bodyFont, size);
            float pos = 0;
            capChar = ""; capIndent = 0;
            if (dropCap && doc.Lines.Count > 0 && doc.Lines[0].Length > 30 && char.IsLetter(doc.Lines[0][0]))
            {
                capChar = doc.Lines[0].Substring(0, 1);
                capSize = pitch * 2.05f;
                var cs = Style(fell, capSize);
                // the cap sits in a 2-line box; the text of the first two lines starts after it
                capIndent = Measure(cs, capChar) + 10;
            }
            bool firstPara = true; int paraRows = 0;
            for (int li = 0; li < doc.Lines.Count; li++)
            {
                string line = doc.Lines[li];
                if (line.Length == 0) { pos += gap; firstPara = false; continue; }
                bool capped = firstPara && capChar.Length > 0;
                if (capped && li == 0) line = line.Substring(1);
                string cur = "";
                foreach (var word in line.Split(' '))
                {
                    float indent = capped && paraRows < 2 ? capIndent : 0;
                    string trial = cur.Length == 0 ? word : cur + " " + word;
                    if (cur.Length > 0 && Measure(st, trial) > colW - indent - (hand ? 6 : 0))
                    {
                        rows.Add(new Row { text = cur, pos = pos, indent = indent }); pos += 1; paraRows++; cur = word;
                    }
                    else cur = trial;
                }
                if (cur.Length > 0)
                {
                    float indent = capped && paraRows < 2 ? capIndent : 0;
                    rows.Add(new Row { text = cur, pos = pos, indent = indent }); pos += 1; paraRows++;
                }
            }
            float total = rows.Count == 0 ? 0 : rows[rows.Count - 1].pos + 1;
            maxScroll = Mathf.Max(0, Mathf.CeilToInt(total - visibleRows));
            scroll = Mathf.Clamp(scroll, 0, maxScroll);
        }
        float capIndent;

        // ---- the page --------------------------------------------------------------------------------------

        // Paints the whole sheet. Returns false when the paper art is missing (caller falls back to the old card).
        public bool Draw(Material artwork, bool castle, string title, string body, Texture2D print, bool padActive, Vector2 padNavigate, bool padPlayStation)
        {
            if (!Ready || artwork == null) return false;
            material = artwork;
            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;
            s = Mathf.Min(Screen.width / 1600f, Screen.height / 900f);
            ox = (Screen.width - 1600 * s) * .5f; oy = (Screen.height - 900 * s) * .5f;

            string docId = (castle ? "c|" : "b|") + title + "|" + (body ?? "").GetHashCode();
            string key = docId + "|" + Mathf.RoundToInt(s * 1000) + "|" + (print != null);
            if (key != cacheKey)
            {
                if (docId != lastDocId) { scroll = 0; lastDocId = docId; }
                cacheKey = key;
                doc = DocumentText.Parse(castle, title, body);
                Frame(doc.Kind, print != null);
                Layout(key);
                Debug.Log("HC_DOC_OPEN kind=" + doc.Kind + " title=\"" + title + "\" kicker=\"" + doc.Kicker + "\" heading=\"" + doc.Heading + "\" lines=" + rows.Count + " visible=" + visibleRows + " scrollable=" + (maxScroll > 0) + " fonts_complete=" + FontsComplete);
            }

            HandleScroll(padActive, padNavigate);

            Paper(doc.Kind);
            if (doc.Kind == DocKind.FieldReport) FieldPage(print, padActive, padPlayStation); else CastlePage(padActive, padPlayStation);
            GUI.matrix = oldMatrix;
            return true;
        }

        void HandleScroll(bool padActive, Vector2 padNav)
        {
            if (maxScroll <= 0) return;
            var e = Event.current;
            int before = scroll;
            if (e.type == EventType.ScrollWheel) { scroll += e.delta.y > 0 ? 2 : -2; e.Use(); }
            else if (e.type == EventType.KeyDown)
            {
                switch (e.keyCode)
                {
                    case KeyCode.DownArrow: scroll += 1; e.Use(); break;
                    case KeyCode.UpArrow: scroll -= 1; e.Use(); break;
                    case KeyCode.PageDown: case KeyCode.Space: scroll += visibleRows - 2; e.Use(); break;
                    case KeyCode.PageUp: scroll -= visibleRows - 2; e.Use(); break;
                    case KeyCode.Home: scroll = 0; e.Use(); break;
                    case KeyCode.End: scroll = maxScroll; e.Use(); break;
                }
            }
            else if (padActive && e.type == EventType.Layout && Mathf.Abs(padNav.y) > .55f && Time.unscaledTime >= nextPad)
            {
                scroll += padNav.y < 0 ? 1 : -1; nextPad = Time.unscaledTime + .16f;
            }
            scroll = Mathf.Clamp(scroll, 0, maxScroll);
            if (before != scroll) Debug.Log("HC_DOC_SCROLL line=" + scroll + "/" + maxScroll);
        }

        void Paper(DocKind kind)
        {
            Texture2D tex = kind == DocKind.FieldReport ? fieldPaper : kind == DocKind.Letter || kind == DocKind.Tally ? letterPaper : ledgerPaper;
            if (Event.current.type != EventType.Repaint) return;
            float factor = Mathf.Max(Screen.width / (float)tex.width, Screen.height / (float)tex.height);
            var draw = new Rect(Screen.width * .5f - tex.width * factor * .5f, Screen.height * .5f - tex.height * factor * .5f, tex.width * factor, tex.height * factor);
            // inscriptions read as a cooler stone rubbing on the same vellum
            Color tint = kind == DocKind.Inscription ? new Color(.90f, .93f, 1f) : Color.white;
            material.SetColor("_Tint", tint.linear);
            Graphics.DrawTexture(draw, tex, material);
        }

        void CastlePage(bool padActive, bool ps)
        {
            bool letter = doc.Kind == DocKind.Letter || doc.Kind == DocKind.Tally;
            // Ledger furniture first: ruled lines, red double margins (all outside or under the text, never over it).
            if (ruled)
            {
                for (int i = 0; i <= visibleRows; i++)
                {
                    float y = bodyTop + i * pitch + pitch * .72f + 6f;
                    if (y < bodyBottom + 6) Fill(R(colX - 34, y, colW + 68, 1), VellumRule);
                }
                Fill(R(colX - 52, 74, 1.4f, 746), VellumMargin); Fill(R(colX - 47, 74, 1, 746), VellumMargin);
                Fill(R(colX + colW + 46, 74, 1, 746), VellumMargin); Fill(R(colX + colW + 51, 74, 1.4f, 746), VellumMargin);
            }
            // Kicker, heading, rule.
            float cx = colX;
            string kick = doc.Kicker;
            float kw = Tracked(0, 0, kick, fell, 16, Color.clear, 3.2f, false, false);
            Tracked(doc.Kind == DocKind.Inscription ? colX + (colW - kw) * .5f : cx, 108, kick, fell, 16, VellumKicker, 3.2f);
            Font hf = letter ? belle : fellItalic;
            float hs = letter ? 56 : 52, hw;
            var hst = Style(hf, hs);
            while ((hw = Measure(hst, doc.Heading)) > colW && hs > 30) { hs -= 2; hst = Style(hf, hs); }
            float hx = doc.Kind == DocKind.Inscription ? colX + (colW - hw) * .5f : cx;
            Text(hx, 128 + (letter ? -6 : 0), colW + 40, hs * 1.6f, doc.Heading, hst, VellumRed);
            Fill(R(colX, 206, colW, 1.6f), new Color(VellumRed.r, VellumRed.g, VellumRed.b, .70f));
            Fill(R(colX, 211, colW, 1f), new Color(VellumRed.r, VellumRed.g, VellumRed.b, .45f));
            Body(VellumInk, letter ? 1 : 0);
            // Drop cap, visible only at the top of the page.
            if (capChar.Length > 0 && scroll == 0)
            {
                float baseline2 = bodyTop + pitch * 1 + pitch * .72f;
                var cs = Style(fell, capSize);
                Text(colX - 2, baseline2 - AscFell * capSize, capSize * 1.2f, capSize * 1.6f, capChar, cs, VellumRed);
            }
            Cues(VellumKicker, fellItalic, colX + colW);
            QuietClose(fellItalic, "set the page down", VellumInk, padActive, ps, true);
        }

        void FieldPage(Texture2D print, bool padActive, bool ps)
        {
            float x0 = art ? 150 : colX, w = art ? 1300 : colW;
            // Notebook ruling and margin line.
            for (int i = 0; i <= visibleRows; i++)
            {
                float y = bodyTop + i * pitch + pitch * .72f + 4f;
                if (y < bodyBottom + 8) Fill(R(95, y, 1410, 1), FieldRule);
            }
            Fill(R(118, 0, 1.4f, 900), FieldMargin);
            // Typed form header: office line, exhibit tag, rule, heading, subject.
            Tracked(x0, 92, "HOLLOW CURRENT  /  FIELD INVESTIGATION OFFICE", courier, 14, FieldKicker, 1.6f);
            Tracked(x0 + w, 92, doc.Kicker, courier, 14, FieldKicker, 1.6f, true);
            Fill(R(x0, 118, w, 1.6f), FieldInk);
            var hst = Style(courierBold, 38);
            float hs = 38;
            while (Measure(hst, doc.Heading.ToUpperInvariant()) > w && hs > 22) { hs -= 2; hst = Style(courierBold, hs); }
            Text(x0, 130, w, hs * 1.5f, doc.Heading.ToUpperInvariant(), hst, FieldRed);
            if (doc.Subject.Length > 0) Text(x0, 188, w, 30, "SUBJECT: " + doc.Subject, Style(courier, 17), FieldInk);
            Fill(R(x0, 226, w, 1f), new Color(FieldInk.r, FieldInk.g, FieldInk.b, .55f));
            if (print != null) Print(print);
            if (art) Tracked(colX, 246, "FIELD TRANSCRIPT", courier, 14, FieldKicker, 1.6f);
            Body(FieldInk, 0);
            Cues(FieldKicker, courier, colX + colW);
            QuietClose(courier, "close the file", FieldInk, padActive, ps, false);
        }

        // The photograph / drawing: a white-bordered print, taped at two corners, with a soft shadow.
        void Print(Texture2D tex)
        {
            var area = new Rect(150, 262, 490, 470);
            float fit = Mathf.Min(area.width / tex.width, area.height / tex.height);
            float w = tex.width * fit, h = tex.height * fit;
            var img = new Rect(area.x + (area.width - w) * .5f, area.y + (area.height - h) * .5f, w, h);
            var border = new Rect(img.x - 12, img.y - 12, img.width + 24, img.height + 40);
            Fill(R(border.x + 7, border.y + 9, border.width, border.height), new Color(0, 0, 0, .22f));
            Fill(R(border.x, border.y, border.width, border.height), new Color(.96f, .95f, .91f));
            if (Event.current.type == EventType.Repaint)
            {
                material.SetColor("_Tint", Color.white.linear);
                Graphics.DrawTexture(R(img.x, img.y, img.width, img.height), tex, material);
            }
            var m = GUI.matrix;
            for (int i = 0; i < 2; i++)
            {
                var c = new Vector2(i == 0 ? border.x + 8 : border.xMax - 8, border.y + 2);
                GUIUtility.RotateAroundPivot(i == 0 ? -34 : 34, new Vector2(ox + c.x * s, oy + c.y * s));
                Fill(R(c.x - 34, c.y - 8, 68, 16), new Color(.85f, .80f, .62f, .72f));
                GUI.matrix = m;
            }
        }

        void Body(Color ink, int jitterKind)
        {
            var st = Style(bodyFont, size, centered ? TextAnchor.UpperCenter : TextAnchor.UpperLeft);
            var m = GUI.matrix;
            int seedBase = (doc.Heading.GetHashCode() & 0xFFFF) % 997;
            foreach (var row in rows)
            {
                float rel = row.pos - scroll;
                if (rel < -.01f || rel > visibleRows - .01f) continue;
                float lineTop = bodyTop + rel * pitch;
                float baseline = lineTop + pitch * .72f;
                float x = colX + row.indent, y = baseline - asc * size, w = colW - row.indent;
                if (hand)
                {
                    int idx = Mathf.RoundToInt(row.pos * 10) + seedBase;
                    float dy = (Hash(idx, 1) - .5f) * 2.8f, dx = (Hash(idx, 2) - .5f) * 5f, rot = (Hash(idx, 3) - .5f) * 1.0f;
                    float fade = .94f + Hash(idx, 4) * .06f;
                    GUIUtility.RotateAroundPivot(rot, new Vector2(ox + (x + 30) * s, oy + baseline * s));
                    Text(x + dx, y + dy, w, size * 2.2f, row.text, st, new Color(ink.r, ink.g, ink.b, fade));
                    GUI.matrix = m;
                }
                else Text(x, y, w, size * 2f, row.text, st, ink);
            }
        }

        void Cues(Color c, Font f, float right)
        {
            if (maxScroll <= 0) return;
            var st = Style(f, 15, TextAnchor.UpperRight);
            if (scroll < maxScroll) Text(right - 500, bodyBottom + 2, 500, 22, "more below   (wheel, arrow keys)", st, c);
            if (scroll > 0) Text(right - 500, bodyTop - 28, 500, 22, "earlier lines above", st, c);
        }

        // ---- the quiet close line -------------------------------------------------------------------------

        void QuietClose(Font f, string verb, Color ink, bool padActive, bool ps, bool castle)
        {
            string key = padActive ? (ps ? "○" : "B") : "ESC";
            closeText = key + "  ·  " + verb;
            closeColor = ink;
            var st = Style(f, 18, TextAnchor.UpperCenter);
            closeStyle = st;
            float w = Mathf.Max(260, Measure(st, closeText) + 60);
            closeRect = R(800 - w * .5f, 800, w, 34);
        }

        // Called by Interface after it has hit-tested CloseRect.
        public void DrawClose(bool lit)
        {
            if (closeStyle == null) return;
            var m = GUI.matrix; GUI.matrix = Matrix4x4.identity;
            var c = new Color(closeColor.r, closeColor.g, closeColor.b, lit ? 1f : .92f);
            closeStyle.normal.textColor = c;
            GUI.Label(new Rect(closeRect.x, closeRect.y + 5 * s, closeRect.width, closeRect.height), closeText, closeStyle);
            if (lit) { float w = Measure(closeStyle, closeText) * s; Fill(new Rect(closeRect.center.x - w * .5f, closeRect.y + 28 * s, w, 1), c); }
            GUI.matrix = m;
        }

        // ---- journal list ----------------------------------------------------------------------------------

        // One recovered-document row in the field journal, in the document faces. Design-space rect; the caller owns hit testing.
        // Dark ink for the castle journal card (fd-journal): the card's paper is darker than the vellum sheets, so the journal rows and
        // kicker use a near-black oxblood / ink that hold >= 7:1 on it. `onCard` is false everywhere else (Bellwether keeps its faces).
        static readonly Color JournalRed = new Color32(32, 4, 4, 255), JournalInk = new Color32(20, 13, 12, 255);
        public void JournalRow(Rect r, string title, bool filed, bool lit, bool castle, bool onCard = false)
        {
            if (!loaded) return;
            var m = GUI.matrix; GUI.matrix = Matrix4x4.identity;
            s = Mathf.Min(Screen.width / 1600f, Screen.height / 900f);
            ox = (Screen.width - 1600 * s) * .5f; oy = (Screen.height - 900 * s) * .5f;
            Color ink = castle ? (onCard ? JournalInk : VellumInk) : FieldInk, red = castle ? (onCard ? JournalRed : VellumRed) : FieldRed;
            if (filed) Fill(R(r.x, r.y + 3, 2.5f, r.height - 6), red);
            if (lit && filed) Fill(R(r.x, r.y, r.width, r.height), new Color(red.r, red.g, red.b, .10f));
            var st = Style(castle ? fellItalic : courierBold, castle ? 24 : 18);
            float ty = r.y + (r.height - (castle ? 24 : 18) * 1.25f) * .5f - (castle ? 1 : 0);
            string shown = castle ? title : title.ToUpperInvariant();
            Text(r.x + 14, ty, r.width - 120, 34, shown, st, filed ? (lit ? red : ink) : new Color(ink.r, ink.g, ink.b, .45f));
            var tag = Style(castle ? fellItalic : courier, 16, TextAnchor.UpperRight);
            Text(r.xMax - 134, ty + 3, 120, 24, filed ? (castle ? "read again" : "OPEN") : (castle ? "not yet found" : "NOT FOUND"), tag, filed ? new Color(red.r, red.g, red.b, .9f) : new Color(ink.r, ink.g, ink.b, .5f));
            if (lit && filed) Fill(R(r.x + 14, r.yMax - 4, Measure(st, shown) , 1), red);
            GUI.matrix = m;
        }

        public void JournalKicker(float x, float y, string text, bool castle, bool onCard = false)
        {
            if (!loaded) return;
            var m = GUI.matrix; GUI.matrix = Matrix4x4.identity;
            s = Mathf.Min(Screen.width / 1600f, Screen.height / 900f);
            ox = (Screen.width - 1600 * s) * .5f; oy = (Screen.height - 900 * s) * .5f;
            Tracked(x, y, text, castle ? fell : courier, castle ? 16 : 13, castle ? (onCard ? JournalRed : VellumKicker) : FieldKicker, castle ? 3.2f : 2f);
            GUI.matrix = m;
        }
    }
}
