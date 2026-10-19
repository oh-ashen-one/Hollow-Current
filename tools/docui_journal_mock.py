#!/usr/bin/env python3
"""Offline layout check + mock for the castle journal and end card (piece fd-journal, r13 critic section 4).

The layout (frames, ornament keep-out rects, safe rects, every text/control rect) is NOT duplicated here: this script builds
Core/DocLayout.cs with tools/DocLayoutDump.cs using the Unity-bundled csc and runs it with the Unity-bundled mono (same toolchain as
tools/check_source.py), then draws the screens from that JSON with the real PanelCard.png (nine-slice), the real note/evidence strings from
Core/CaseBook.cs and Georgia / the bundled IM Fell fonts. It then asserts, on the LONGEST record set:
  1. the C# verifier reports overlaps=0,
  2. every drawn text (measured with the real font, word-wrapped to its declared width) fits inside its declared rect,
  3. no measured text box touches an ornament (+ pad) or leaves the safe rect,
  4. text contrast against the paper under it: >= 7:1 on the median paper pixel and >= 6.5:1 on the darkest 5 % (blots no longer sit under text).
It prints `HC_DOC_LAYOUT overlaps=0` and writes docui_mock_journal_fd.png / docui_mock_ending_fd.png.
Differences from the engine: PIL kerning/baselines (1-2 px), Georgia is the in-game OS serif, the pulse strip is a fixed sample.
Run:  python3 tools/docui_journal_mock.py [outdir]
"""
import json
import math
import os
import re
import subprocess
import sys
import tempfile
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / "Assets/HollowCurrent/Resources"
OUT = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "docs/haunted-castle/rounds/r13"
W, H = 1920, 1080
S = W / 1600.0
GEORGIA = "/System/Library/Fonts/Supplemental/Georgia.ttf"
GEORGIA_I = "/System/Library/Fonts/Supplemental/Georgia Italic.ttf"
FELLI = RES / "UI/Fonts/IMFellEnglish-Italic.ttf"
FELL = RES / "UI/Fonts/IMFellEnglish-Regular.ttf"

INK = (232, 227, 209)          # Interface.ink
MUTED = (163, 168, 161)
PAPER_INK = (20, 13, 12)       # Interface.castleInk (castle cards only)
OXBLOOD = (32, 4, 4)           # Interface.castleKicker: kickers on the castle cards (near-black oxblood, >= 7:1)
LINE = (107, 92, 74, 166)
VELLUM_INK, VELLUM_RED, VELLUM_KICK = (20, 13, 12), (32, 4, 4), (32, 4, 4)   # DocumentUi.JournalInk / JournalRed on the card
TEX_W, TEX_H = 1448.0, 1426.0
COLS, ROWS = (300.0, 960.0), (390.0, 900.0)
SCALE = 0.28


# ---- layout from the real C# --------------------------------------------------------------------------------------------

def layout():
    unity = Path(os.environ.get("UNITY_CONTENTS", "/Applications/Unity/Hub/Editor/6000.3.20f1/Unity.app/Contents"))
    sc = unity / "Resources/Scripting"
    api = sc / "MonoBleedingEdge/lib/mono/4.8-api"
    refs = [api / "mscorlib.dll", api / "System.dll", api / "System.Core.dll", api / "Facades/netstandard.dll"]
    with tempfile.TemporaryDirectory() as td:
        exe = Path(td) / "DocLayoutDump.exe"
        args = [str(sc / "NetCoreRuntime/dotnet"), str(sc / "DotNetSdkRoslyn/csc.dll"), "/nologo", "/noconfig", "/nostdlib+",
                "/langversion:9", "/target:exe", "/out:" + str(exe)] + ["/reference:" + str(r) for r in refs] + [
            str(ROOT / "Assets/HollowCurrent/Core/DocLayout.cs"), str(ROOT / "tools/DocLayoutDump.cs")]
        r = subprocess.run(args, cwd=ROOT, text=True, capture_output=True, timeout=90)
        if r.returncode:
            sys.exit(r.stdout + r.stderr)
        r = subprocess.run([str(sc / "MonoBleedingEdge/bin/mono"), str(exe)], text=True, capture_output=True, timeout=30)
        if r.returncode:
            sys.exit(r.stdout + r.stderr)
    return json.loads(r.stdout)


# ---- strings from the source ---------------------------------------------------------------------------------------------

LIT = r'"((?:[^"\\]|\\.)*)"'


def unesc(s):
    return s.replace("\\n", "\n").replace('\\"', '"').replace("\\\\", "\\")


def castle_book():
    src = (ROOT / "Assets/HollowCurrent/Core/CaseBook.cs").read_text()
    castle = src[src.index('Backdrop="UI/CastleBackdrop"'):]
    castle = src[src.rfind("EvidenceRecorded", 0, src.index('Backdrop="UI/CastleBackdrop"')):]

    def arr(name):
        i = castle.index(name + "=new[]{")
        j = castle.index("}", i)
        return [unesc(m) for m in re.findall(LIT, castle[i:j])]

    def one(name):
        i = castle.index(name + '="')
        return unesc(re.match(LIT, castle[i + len(name) + 1:]).group(1))
    return dict(rec=arr("EvidenceRecorded"), miss=arr("EvidenceMissing"), notes=arr("NoteTitles"), anchor=arr("AnchorChoices"),
                pattern=arr("PatternChoices"), closure=arr("ClosureChoices"), question=one("JournalQuestion"),
                ready=one("JournalReady"), notready=one("JournalNotReady"), kill_hero=one("KillHero"), kill_body=one("KillBody"),
                won_hero=one("WonHero"), won_body=one("WonBody"), kill_footer=one("KillFooter"), case=one("CaseTag"), place=one("PlaceTag"),
                context=one("ContextTrack"))


# ---- drawing helpers -----------------------------------------------------------------------------------------------------

def F(path, size):
    return ImageFont.truetype(str(path), max(8, round(size * S)))


def px(v):
    return v * S


def lum(c):
    def f(v):
        v /= 255.0
        return v / 12.92 if v <= .04045 else ((v + .055) / 1.055) ** 2.4
    return .2126 * f(c[0]) + .7152 * f(c[1]) + .0722 * f(c[2])


def contrast(a, b):
    la, lb = lum(a), lum(b)
    return (max(la, lb) + .05) / (min(la, lb) + .05)


def backdrop(shade):
    im = Image.open(RES / "UI/CastleBackdrop.png").convert("RGB")
    f = max(W / im.width, H / im.height)
    im = im.resize((round(im.width * f), round(im.height * f)), Image.LANCZOS)
    im = im.crop(((im.width - W) // 2, (im.height - H) // 2, (im.width - W) // 2 + W, (im.height - H) // 2 + H)).convert("RGBA")
    if shade > 0:
        im.alpha_composite(Image.new("RGBA", (W, H), (2, 3, 4, round(255 * shade))))
    return im


def nine_slice(canvas, frame):
    """Mirror of Interface.CastleCard: corner cells at DocPanel.Scale, edges and centre stretched."""
    tex = Image.open(RES / "UI/BloodLedger/PanelCard.png").convert("RGBA")
    fx, fy, fw, fh = frame
    cx = [fx, fx + COLS[0] * SCALE, fx + fw - (TEX_W - COLS[1]) * SCALE, fx + fw]
    cy = [fy, fy + ROWS[0] * SCALE, fy + fh - (TEX_H - ROWS[1]) * SCALE, fy + fh]
    tx = [0, COLS[0], COLS[1], TEX_W]
    ty = [0, ROWS[0], ROWS[1], TEX_H]
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    for j in range(3):
        for i in range(3):
            x0, x1, y0, y1 = round(px(cx[i])), round(px(cx[i + 1])), round(px(cy[j])), round(px(cy[j + 1]))
            box = (int(tx[i]), int(ty[j]), int(tx[i + 1]), int(ty[j + 1]))
            cell = tex.crop(box).resize((x1 - x0, y1 - y0), Image.LANCZOS)
            layer.alpha_composite(cell, (x0, y0))
    a = layer.getchannel("A").point(lambda v: round(v * .97))
    layer.putalpha(a)
    canvas.alpha_composite(layer)


class Screen:
    def __init__(self, shade):
        self.im = backdrop(shade)
        self.clean = None
        self.d = None
        self.items = []     # (name, measured (x,y,w,h) design, declared (x,y,w,h), color, on_paper)
        self.paper_rects = []
        self.disabled = set()   # inactive controls (WCAG exempt): "not yet found"

    def begin_text(self):
        self.clean = self.im.copy()          # paper + backdrop only, for the contrast measurement
        self.d = ImageDraw.Draw(self.im)

    # plain label: Georgia, word wrap inside the declared width; returns measured box
    def label(self, name, rect, text, size, color, font=GEORGIA, on_paper=True, line=1.136, align="l"):
        x, y, w, h = rect
        f = F(font, size)
        lines = []
        for para in text.split("\n"):
            cur = ""
            for word in para.split(" "):
                t = word if not cur else cur + " " + word
                if cur and f.getlength(t) / S > w - 6:
                    lines.append(cur)
                    cur = word
                else:
                    cur = t
            lines.append(cur)
        lh = size * line
        for i, l in enumerate(lines):
            self.d.text((px(x + 3), px(y + i * lh)), l, font=f, fill=color + (255,))
        mw = max(f.getlength(l) for l in lines) / S + 3
        self.items.append((name, (x, y, mw, len(lines) * lh), rect, color, on_paper))
        return lines

    def tracked(self, name, rect, text, size, color, spacing=2.4, font=GEORGIA, on_paper=True):
        x, y, w, h = rect
        f = F(font, size)
        cx = px(x)
        total = 0
        for c in text:
            self.d.text((cx, px(y + 2)), c, font=f, fill=color + (255,))
            adv = f.getlength(c) + px(spacing)
            cx += adv
            total += adv
        self.items.append((name, (x, y, total / S, 24), rect, color, on_paper))

    def rect(self, r, rgba):
        x, y, w, h = r
        self.d.rectangle([px(x), px(y), px(x + w) - 1, px(max(y + h, y + 1 / S)) - 1], fill=rgba)


def ecg(scr, rect):
    x, y, w, h = rect
    pts = []
    base = y + h / 2
    for i in range(111):
        u = i / 110.0
        phase = (u * w / 90.0 * 1.4) % 1
        v = (math.exp(-((phase - .18) / .035) ** 2) * .18 - math.exp(-((phase - .30) / .012) ** 2) * .25 + math.exp(-((phase - .34) / .014) ** 2)
             - math.exp(-((phase - .39) / .014) ** 2) * .35 + math.exp(-((phase - .58) / .06) ** 2) * .28)
        pts.append((px(x + u * w), px(base - v * h * .45)))
    scr.d.line(pts, fill=PAPER_INK + (217,), width=max(2, round(S * 1.6)))
    scr.items.append(("pulse", (x, y, w, h), rect, PAPER_INK, True))


def button(scr, name, rect, text, key, selected=False, plain=False, color=None, on_paper=True):
    x, y, w, h = rect
    if not plain:
        warn = Image.open(RES / "UI/BloodLedger/Warning.png").convert("RGBA").resize((round(px(w)), round(px(h))), Image.LANCZOS)
        scr.im.alpha_composite(warn, (round(px(x)), round(px(y))))
        scr.d = ImageDraw.Draw(scr.im)
    f = F(GEORGIA, 18)
    pad = max(22, min(68, round(w * .14)))
    col = color or (PAPER_INK if plain else INK)
    right = 80 if key else 22
    # a wrapping label (word wrap, middle-left) inside w - pad - right
    avail = w - pad - right
    lines, cur = [], ""
    for word in text.split(" "):
        t = word if not cur else cur + " " + word
        if cur and f.getlength(t) / S > avail:
            lines.append(cur)
            cur = word
        else:
            cur = t
    lines.append(cur)
    lh = 18 * 1.136
    top = y + (h - len(lines) * lh) / 2
    for i, l in enumerate(lines):
        scr.d.text((px(x + pad), px(top + i * lh)), l, font=f, fill=col + (255,))
    if key:
        kx = (x + w - 66, y + (h - 24) / 2, 48, 24)
        scr.d.rectangle([px(kx[0]), px(kx[1]), px(kx[0] + kx[2]) - 1, px(kx[1] + kx[3]) - 1], outline=LINE)
        kf = F(GEORGIA, 12)
        scr.d.text((px(kx[0] + 24), px(kx[1] + 12)), key, font=kf, fill=col + (255,), anchor="mm")
    mw = max(f.getlength(l) for l in lines) / S
    scr.items.append((name + ":label", (x + pad, top, mw, len(lines) * lh), (x + pad, y, avail, h), col, on_paper))
    scr.items.append((name, (x, y, w, h), rect, col, on_paper))


def chrome(scr, bk, right, footer):
    scr.tracked("brand1", (80, 48, 300, 24), "HOLLOW CURRENT", 13, INK, 2.4, on_paper=False)
    scr.tracked("brand2", (80, 78, 400, 24), "FIELD INVESTIGATION OFFICE", 13, MUTED, 2.4, on_paper=False)
    scr.label("brand-right", (1110, 51, 410, 35), right, 15, MUTED, on_paper=False)
    scr.rect((80, 843, 1440, 1), LINE)
    scr.label("footer", (80, 863, 1160, 25), footer, 15, MUTED, on_paper=False)
    scr.tracked("footer-name", (1260, 863, 260, 25), "HAUNTED CASTLE", 13, (160, 130, 90), 2.4, on_paper=False)


# ---- the screens -----------------------------------------------------------------------------------------------------------

def longest(bk):
    return [max(a, b, key=len) for a, b in zip(bk["rec"], bk["miss"])]


def journal(L, bk, mode):
    j = L["journal"]
    t = {k: v for k, v in j["texts"].items()}
    scr = Screen(.52)
    for f in j["frames"]:
        nine_slice(scr.im, f)
    scr.begin_text()
    chrome(scr, bk, "FIELD JOURNAL / " + bk["case"], "1–9 choose a conclusion   ·   ENTER file reconstruction   ·   J or ESC close")
    scr.label("heading", (77, 147, 1100, 62), bk["question"], 36, INK, on_paper=False, line=1.136)
    button(scr, "close-journal", (1240, 149, 280, 48), "CLOSE JOURNAL", "J", plain=True, color=INK, on_paper=False)
    texts = longest(bk)
    scr.tracked("records-kicker", t["records-kicker"], "INSTRUMENT RECORDS", 13, OXBLOOD)
    for i in range(3):
        yes = (i != 1)
        r = t["bar%d" % i]
        scr.rect(r, (160, 130, 90, 255) if yes else LINE)
        scr.tracked("tag%d" % i, t["tag%d" % i], "RECORDED" if yes else "NOT RECORDED", 13, OXBLOOD)
        scr.label("evidence%d" % i, t["evidence%d" % i], texts[i], 17, PAPER_INK)
    scr.rect(t["rule"], LINE)
    scr.tracked("ledger-kicker", t["ledger-kicker"], bk["context"], 16, VELLUM_KICK, 3.2, font=FELL)
    notes = bk["notes"]
    longest_note = max(notes, key=len)
    for i in range(3):
        x, y, w, h = t["doc-row%d" % i]
        filed = i < 2
        name = longest_note if i == 2 else notes[i]
        col = VELLUM_RED if filed else VELLUM_INK
        if filed:
            scr.rect((x, y + 3, 2.5, h - 6), VELLUM_RED + (255,))
        f = F(FELLI, 24)
        scr.d.text((px(x + 14), px(y + (h - 24 * 1.25) / 2 - 1)), name, font=f, fill=col + (255 if filed else 115,))
        tag = "read again" if filed else "not yet found"
        f2 = F(FELLI, 16)
        scr.d.text((px(x + w - 14), px(y + (h - 24 * 1.25) / 2 + 3)), tag, font=f2, fill=col + (230 if filed else 128,), anchor="ra")
        tw = f.getlength(name) / S
        gw = f2.getlength(tag) / S
        if not filed:
            scr.disabled.update(("doc-row%d:title" % i, "doc-row%d:tag" % i))
        scr.items.append(("doc-row%d:title" % i, (x + 14, y + 3, tw, 26), t["doc-row%d" % i], col, True))
        scr.items.append(("doc-row%d:tag" % i, (x + w - 14 - gw, y + 6, gw, 20), t["doc-row%d" % i], col, True))
        scr.items.append(("doc-row%d:gap" % i, (x + 14, y, tw + gw + 28 - 28, 1), (x, y, w, h), col, True))
        # title and tag must not run into each other
        assert 14 + tw + 10 < w - 14 - gw, "doc row %d title %.0f + tag %.0f overlap in %.0f" % (i, tw, gw, w)
    scr.tracked("pulse-label", t["pulse-label"], "PULSE", 13, OXBLOOD)
    ecg(scr, t["pulse"])
    for i, (label, ch, key0) in enumerate((("ANCHOR / WHAT KEEPS RETURNING?", bk["anchor"], 1), ("PATTERN / WHAT IS REPEATED?", bk["pattern"], 4),
                                           ("CLOSURE / WHAT SHOULD WE DO?", bk["closure"], 7))):
        scr.tracked("choice-label%d" % i, t["choice-label%d" % i], label, 13, OXBLOOD)
        for k in range(3):
            button(scr, "choice%d%d" % (i, k), t["choice%d%d" % (i, k)], ch[k], str(key0 + k), selected=(i == 1 and k == 1))
    scr.label("compare", t["compare"], bk["notready"] if mode == "notready" else bk["ready"], 15, PAPER_INK)
    button(scr, "submit", t["submit"], "FILE RECONSTRUCTION", "ENTER")
    return scr, j


def ending(L, bk):
    e = L["ending"]
    t = e["texts"]
    scr = Screen(0)
    for f in e["frames"]:
        nine_slice(scr.im, f)
    scr.begin_text()
    chrome(scr, bk, bk["place"] + "  /  " + bk["case"], bk["kill_footer"])
    scr.tracked("kicker", t["kicker"], bk["case"] + " / CLOSED", 13, OXBLOOD)
    scr.label("hero", t["hero"], bk["kill_hero"], 76, PAPER_INK)
    body = max(bk["kill_body"], bk["won_body"], key=len)
    scr.label("body", t["body"], body, 20, PAPER_INK)
    for i, (v, lab) in enumerate((("3 / 3", "EVIDENCE"), ("2", "HUNTS SURVIVED"), ("01:40", "FIELD TIME"))):
        scr.label("metric%d" % i, t["metric%d" % i], v, 40, PAPER_INK, line=1.136)
        scr.tracked("metric-label%d" % i, t["metric-label%d" % i], lab, 13, OXBLOOD)
    for sx in e["sep"]:
        scr.rect((sx, e["metricY"] + 9, 1, 76), LINE)
    button(scr, "new", t["new"], "NEW INVESTIGATION", "N")
    button(scr, "exit", t["exit"], "EXIT TO DESKTOP", "Q", plain=True)
    return scr, e


def clear(a, b):
    return a[0] >= b[0] - .5 and a[1] >= b[1] - .5 and a[0] + a[2] <= b[0] + b[2] + .5 and a[1] + a[3] <= b[1] + b[3] + .5


def overlaps(a, b):
    return a[0] < b[0] + b[2] and b[0] < a[0] + a[2] and a[1] < b[1] + b[3] and b[1] < a[1] + a[3]


def check(scr, scrjson, label, pad=8.0):
    """measured text boxes: inside their declared rect, clear of ornaments(+pad), inside the safe rect; contrast of body text."""
    problems = []
    orn = [tuple(o) for o in scrjson["ornaments"]]
    safe = [tuple(s) for s in scrjson["safe"]]
    paper = [tuple(p) for p in scrjson["paper"]]
    worst = {}
    for name, meas, decl, col, on_paper in scr.items:
        if not on_paper:
            continue
        if not clear(meas, decl):
            problems.append("%s: measured %s exceeds declared %s" % (name, tuple(round(v, 1) for v in meas), tuple(round(v, 1) for v in decl)))
        if not any(overlaps(meas, p) for p in paper):
            continue
        if not any(clear(meas, s) for s in safe):
            problems.append("%s: measured %s leaves the safe rect" % (name, tuple(round(v, 1) for v in meas)))
        for o in orn:
            if overlaps(meas, (o[0] - pad, o[1] - pad, o[2] + 2 * pad, o[3] + 2 * pad)):
                problems.append("%s: measured %s touches an ornament" % (name, tuple(round(v, 1) for v in meas)))
        # contrast against the darkest 0.5 % of the clean paper under the declared rect
        x, y, w, h = [round(px(v)) for v in meas]
        if name.startswith(("choice", "submit", "new")) or ":gap" in name:
            continue   # light text on the dark-red button art: measured separately below
        reg = scr.clean.crop((x, y, max(x + 1, x + w), max(y + 1, y + h))).convert("RGB")
        data = reg.get_flattened_data() if hasattr(reg, "get_flattened_data") else reg.getdata()
        px_l = sorted(lum(p) for p in data)
        dark = px_l[min(len(px_l) - 1, int(len(px_l) * .05))]      # darkest 5 % of the paper under the text
        mid = px_l[len(px_l) // 2]
        if name in scr.disabled:
            continue
        worst[name] = ((dark + .05) / (lum(col) + .05), (mid + .05) / (lum(col) + .05))
    return problems, worst


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    L = layout()
    bk = castle_book()
    ok = True
    for tag, builder, args in (("journal", journal, (L, bk, "notready")), ("ending", ending, (L, bk))):
        scr, sj = builder(*args)
        problems, contrast_by = check(scr, sj, tag)
        csharp = L[tag]["overlaps"]
        lo5 = min(v[0] for v in contrast_by.values())
        lom = min(v[1] for v in contrast_by.values())
        wk = min(contrast_by, key=lambda k: contrast_by[k][0])
        print("%-8s C# verifier overlaps=%d | measured-text problems=%d | worst text contrast: median paper %.1f:1, darkest 5%% of the paper %.1f:1 (%s)"
              % (tag, csharp, len(problems), lom, lo5, wk))
        for p in problems:
            print("   ", p)
        ok = ok and csharp == 0 and not problems and lom >= 7.0 and lo5 >= 6.5
        name = "docui_mock_journal_fd.png" if tag == "journal" else "docui_mock_ending_fd.png"
        scr.im.convert("RGB").save(OUT / name)
        print("wrote", OUT / name)
    total = L["journal"]["overlaps"] + L["ending"]["overlaps"]
    print("HC_DOC_LAYOUT overlaps=%d  (journal=%d ending=%d; longest record set, longest note title, kill-card body)"
          % (total, L["journal"]["overlaps"], L["ending"]["overlaps"]))
    if not ok:
        sys.exit("FAIL: layout check")


if __name__ == "__main__":
    main()
