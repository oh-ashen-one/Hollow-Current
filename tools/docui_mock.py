#!/usr/bin/env python3
"""Static 1920x1080 mock of the new document UI (piece fb-docui), rendered with the REAL note texts, the bundled OFL
fonts and the generated paper sheets. It mirrors Runtime/DocumentUi.cs (same design-space coordinates, word wrap, drop
cap, ruled lines, jitter, close line) so the owner can approve the look before a player build exists.

Differences from the in-game result (stated honestly): PIL anti-aliases and kerns slightly differently from Unity's
dynamic fonts, a 0.45 px blur imitates ink soaking into the page (the game draws crisp type), and baselines can sit 1 to
2 px differently. Run:  python3 tools/docui_mock.py [outdir]
"""
import re
import sys
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / "Assets/HollowCurrent/Resources"
FONTS = RES / "UI/Fonts"
PAPER = RES / "UI/Documents"
OUT = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "docs/haunted-castle/rounds/r13"
W, H = 1920, 1080
S = min(W / 1600, H / 900)

FELL, FELLI = FONTS / "IMFellEnglish-Regular.ttf", FONTS / "IMFellEnglish-Italic.ttf"
BELLE, COUR, COURB = FONTS / "LaBelleAurore-Regular.ttf", FONTS / "CourierPrime-Regular.ttf", FONTS / "CourierPrime-Bold.ttf"
ASC = {str(FELL): .905, str(FELLI): .905, str(BELLE): 1.077, str(COUR): .781, str(COURB): .781}

V_INK, V_RED, V_KICK = (43, 28, 19), (92, 13, 11), (79, 26, 18)
F_INK, F_RED, F_KICK = (26, 28, 41), (128, 20, 20), (56, 61, 77)


def font(path, design):
    return ImageFont.truetype(str(path), max(8, round(design * S)))


def meas(f, t):
    return f.getlength(t) / S


# ---- port of Core/DocumentText.cs ------------------------------------------------------------------------------

def is_header(line):
    if not line or len(line) > 70:
        return False
    letters = False
    for c in line:
        if c.islower():
            return False
        if c.isalpha():
            letters = True
    return letters


PROPER = ["Margrave", "Bellwether", "Harrow", "Harrows", "Count"]


def sentence(caps):
    words = caps.strip().split(" ")
    for i, w in enumerate(words):
        w = w.lower()
        for p in PROPER:
            pl = p.lower()
            if w == pl or w == pl + "'s":
                w = p + w[len(pl):]
                break
        if i == 0 and w:
            w = w[0].upper() + w[1:]
        words[i] = w
    return " ".join(words)


def parse(castle, title, body):
    raw = [l.rstrip() for l in body.replace("\r", "").split("\n")]
    while raw and not raw[0]:
        raw.pop(0)
    header = lead = None
    if raw:
        gapnext = len(raw) == 1 or not raw[1]
        if gapnext and is_header(raw[0]):
            header = raw.pop(0)
        elif gapnext and raw[0].endswith(":"):
            lead = raw.pop(0)[:-1]
    while raw and not raw[0]:
        raw.pop(0)
    lines, gap = [], False
    for l in raw:
        if not l:
            gap = bool(lines)
            continue
        if gap:
            lines.append("")
            gap = False
        lines.append(l)
    d = dict(lines=lines, lead=lead or "", subject="")
    lt = title.lower()
    norm = lambda s: re.sub(r"[^a-z0-9]", "", s.lower())
    if not castle:
        d.update(kind="field", kicker="EXHIBIT", heading=title)
        if header and norm(header) != norm(title):
            d["subject"] = header
        return d
    ledger = lt.startswith("ledger") or "blood ledger" in lt or "last entry" in lt or (header or "").startswith("BLOOD LEDGER")
    kind = "inscription" if "inscription" in lt else "tally" if "tally" in lt else "ledger" if ledger else "letter"
    d["kind"] = kind
    if kind == "ledger":
        d["kicker"] = "BLOOD LEDGER"
        rest = None
        if header and header.startswith("BLOOD LEDGER"):
            rest = header[len("BLOOD LEDGER"):].lstrip(" /,:-")
        elif header:
            rest = header
        if not rest:
            rest = title.split(":", 1)[1].strip() if ":" in title else title
            if rest.lower() == "blood ledger page":
                rest = ""
        d["heading"] = sentence(rest) if rest else "A page of the ledger"
    elif kind == "letter":
        d["kicker"] = title.upper()
        d["heading"] = sentence(header) if header else title
    elif kind == "inscription":
        d["kicker"] = (lead or "CUT INTO STONE").upper()
        d["heading"] = title
    else:
        d["kicker"] = (lead or "SCRATCHED ON A TABLE").upper()
        d["heading"] = title
    return d


# ---- real note texts, read from the source ---------------------------------------------------------------------

def cs_string(src, anchor, after=0):
    i = src.index(anchor, after)
    j = src.index('"', i + len(anchor) - 1)
    k = j + 1
    out = []
    while True:
        c = src[k]
        if c == "\\":
            n = src[k + 1]
            out.append({"n": "\n", '"': '"', "\\": "\\", "'": "'"}.get(n, n))
            k += 2
            continue
        if c == '"':
            break
        out.append(c)
        k += 1
    return "".join(out)


def texts():
    cb = (ROOT / "Assets/HollowCurrent/Core/CaseBook.cs").read_text()
    cw = (ROOT / "Assets/HollowCurrent/Runtime/Castle/CastleWorld.cs").read_text()
    wd = (ROOT / "Assets/HollowCurrent/Runtime/World.cs").read_text()
    i = cb.index("NoteBodies=new[]{", cb.index("Ledger: the steward"))
    t = {}
    t["ledger"] = ("Ledger: the steward", cs_string(cb, 'NoteBodies=new[]{\n                "', i - 5) if False else cs_string(cb[i:], '{\n                "'))
    t["letter"] = ("Gate keeper's letter", cs_string(cw, 'Note("Gate keeper\'s letter", new Vector3(-1.4f, .82f, 5.3f), "Gate keeper\'s letter",\n                "'))
    t["inscription"] = ("Chapel inscription", cs_string(cw, 'Note("Chapel inscription", new Vector3(12.75f, 1.44f, 5.7f), "Chapel inscription",\n                "'))
    t["tally"] = ("Kitchen tally", cs_string(cw, 'Note("Kitchen tally", new Vector3(-8.5f, .92f, 7.5f), "Kitchen tally",\n                "'))
    t["photo"] = ("Family photograph", cs_string(wd, '"Family photograph",\n                "', wd.index('Interact("Family photograph"')))
    t["briefing"] = ("Case briefing", cs_string(wd, '"Case briefing",\n                "'))
    return t


# ---- drawing ---------------------------------------------------------------------------------------------------

def hashf(i, k):
    v = math.sin(i * 12.9898 + k * 78.233) * 43758.5453
    return v - math.floor(v)


def px(v):
    return v * S


class Page:
    def __init__(self, paper, tint=None):
        im = Image.open(PAPER / paper).convert("RGB").resize((W, H), Image.LANCZOS)
        if tint:
            im = Image.eval(im, lambda v: v)  # placeholder, tint applied below
            r, g, b = im.split()
            im = Image.merge("RGB", (r.point(lambda v: v * tint[0]), g.point(lambda v: v * tint[1]), b.point(lambda v: v * tint[2])))
        self.im = im.convert("RGBA")
        self.fx = Image.new("RGBA", (W, H), (0, 0, 0, 0))   # rules, tape, print (no ink bleed)
        self.ink = Image.new("RGBA", (W, H), (0, 0, 0, 0))  # type, slightly bled
        self.fd = ImageDraw.Draw(self.fx)
        self.id = ImageDraw.Draw(self.ink)

    def rect(self, x, y, w, h, rgba):
        self.fd.rectangle([px(x), px(y), px(x + w) - 1, px(y + h) - 1], fill=rgba)

    def text(self, x, y_base, t, f, rgba, anchor="ls"):
        self.id.text((px(x), px(y_base)), t, font=f, fill=rgba, anchor=anchor)

    def rot_text(self, x, y_base, t, f, rgba, deg):
        w = int(f.getlength(t) + 80)
        h = int(f.size * 3)
        tile = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        ImageDraw.Draw(tile).text((40, h // 2), t, font=f, fill=rgba, anchor="ls")
        tile = tile.rotate(-deg, resample=Image.BICUBIC, center=(70, h // 2))
        self.ink.alpha_composite(tile, (int(px(x) - 40), int(px(y_base) - h // 2)))

    def tracked(self, x, y_base, t, f, rgba, spacing, right=False):
        widths = [f.getlength(c) + spacing * S for c in t]
        total = sum(widths)
        cx = px(x) - (total if right else 0)
        for c, w in zip(t, widths):
            self.id.text((cx, px(y_base)), c, font=f, fill=rgba, anchor="ls")
            cx += w
        return total / S

    def save(self, path):
        out = self.im.copy()
        out.alpha_composite(self.fx)
        bleed = self.ink.filter(ImageFilter.GaussianBlur(.45))
        out.alpha_composite(bleed)
        out.alpha_composite(self.ink)
        out.convert("RGB").save(path)
        print("wrote", path)


def wrap(lines, f, colw, hand, gap, cap_indent=0):
    rows, pos, para_rows, first = [], 0.0, 0, True
    cap = bool(cap_indent)
    for li, line in enumerate(lines):
        if not line:
            pos += gap
            first = False
            continue
        capped = first and cap
        if capped and li == 0:
            line = line[1:]
        cur = ""
        for word in line.split(" "):
            ind = cap_indent if capped and para_rows < 2 else 0
            trial = word if not cur else cur + " " + word
            if cur and meas(f, trial) > colw - ind - (6 if hand else 0):
                rows.append((cur, pos, ind)); pos += 1; para_rows += 1; cur = word
            else:
                cur = trial
        if cur:
            ind = cap_indent if capped and para_rows < 2 else 0
            rows.append((cur, pos, ind)); pos += 1; para_rows += 1
    return rows


def body(page, d, f, fpath, size, pitch, body_top, colx, colw, ink, hand=False, centered=False, gap=1.0, cap_indent=0, scroll=0):
    rows = wrap(d["lines"], f, colw, hand, gap, cap_indent)
    visible = int((790 - body_top) // pitch)
    asc = ASC[str(fpath)]
    seed = (hash(d["heading"]) & 0xFFFF) % 997  # python hash differs from C#; jitter pattern only
    for text, pos, ind in rows:
        rel = pos - scroll
        if rel < -.01 or rel > visible - .01:
            continue
        base = body_top + rel * pitch + pitch * .72
        x = colx + ind
        if hand:
            idx = round(pos * 10) + seed
            dy, dx, rot = (hashf(idx, 1) - .5) * 2.8, (hashf(idx, 2) - .5) * 5, (hashf(idx, 3) - .5) * 1.0
            fade = .94 + hashf(idx, 4) * .06
            page.rot_text(x + dx, base + dy, text, f, ink + (int(255 * fade),), rot)
        elif centered:
            w = meas(f, text)
            page.text(colx + (colw - w) / 2, base, text, f, ink + (255,))
        else:
            page.text(x, base, text, f, ink + (255,))
    total = rows[-1][1] + 1 if rows else 0
    return rows, visible, total


def close_line(page, fpath, verb, ink, key="ESC"):
    f = font(fpath, 18)
    t = "%s  ·  %s" % (key, verb)
    w = meas(f, t)
    page.text(800 - w / 2, 800 + 5 + 18 * ASC[str(fpath)], t, f, ink + (235,))


def castle_page(kind, title, text, out, scroll=0):
    d = parse(True, title, text)
    letter = d["kind"] in ("letter", "tally")
    page = Page("DocLetter.jpg" if letter else "DocLedger.jpg", tint=(.90, .93, 1.0) if d["kind"] == "inscription" else None)
    colx, colw = (370, 860) if letter else (400, 800)
    pitch, bt = (44, 238) if letter else (39, 236)
    ruled = d["kind"] == "ledger"
    visible = int((790 - bt) // pitch)
    if ruled:
        for i in range(visible + 1):
            y = bt + i * pitch + pitch * .72 + 6
            if y < 796:
                page.rect(colx - 34, y, colw + 68, 1, (92, 64, 41, 61))
        for xx, ww in ((colx - 52, 1.4), (colx - 47, 1), (colx + colw + 46, 1), (colx + colw + 51, 1.4)):
            page.rect(xx, 74, ww, 746, (127, 26, 20, 153))
    kf = font(FELL, 16)
    kw = page.tracked(0, -999, d["kicker"], kf, (0, 0, 0, 0), 3.2)
    kx = colx + (colw - kw) / 2 if d["kind"] == "inscription" else colx
    page.tracked(kx, 108 + 16 * .905, d["kicker"], kf, V_KICK + (255,), 3.2)
    hp = BELLE if letter else FELLI
    hs = 56 if letter else 52
    hf = font(hp, hs)
    while meas(hf, d["heading"]) > colw and hs > 30:
        hs -= 2
        hf = font(hp, hs)
    hw = meas(hf, d["heading"])
    hx = colx + (colw - hw) / 2 if d["kind"] == "inscription" else colx
    page.text(hx, 128 + (-6 if letter else 0) + hs * ASC[str(hp)], d["heading"], hf, V_RED + (255,))
    page.rect(colx, 206, colw, 1.6, V_RED + (178,))
    page.rect(colx, 211, colw, 1, V_RED + (115,))
    bp, size = (BELLE, 28) if letter else (FELL, 27)
    bf = font(bp, size)
    cap_indent, cap_char, cap_size = 0, "", 0
    if d["kind"] == "ledger" and d["lines"] and len(d["lines"][0]) > 30 and d["lines"][0][0].isalpha():
        cap_char = d["lines"][0][0]
        cap_size = pitch * 2.05
        cap_indent = meas(font(FELL, cap_size), cap_char) + 10
    rows, vis, total = body(page, d, bf, bp, size, pitch, bt, colx, colw, V_INK, hand=letter, centered=d["kind"] == "inscription",
                            gap=.6 if not ruled else 1.0, cap_indent=cap_indent, scroll=scroll)
    if cap_char and scroll == 0:
        base2 = bt + pitch + pitch * .72
        page.text(colx - 2, base2, cap_char, font(FELL, cap_size), V_RED + (255,))
    if total - vis > 0:
        cf = font(FELLI, 15)
        page.text(colx + colw - meas(cf, "more below   (wheel, arrow keys)"), 790 + 2 + 15 * .905, "more below   (wheel, arrow keys)", cf, V_KICK + (255,))
    close_line(page, FELLI, "set the page down", V_INK)
    page.save(out)
    print("  %s: %d rows, %d visible, %.1f total, scroll needed %s" % (d["kind"], len(rows), vis, total, total > vis))
    return d


def field_page(title, text, out, art=None, scroll=0):
    d = parse(False, title, text)
    page = Page("DocField.jpg")
    pitch, size = 36, 25
    art_on = art is not None
    bt = 282 if art_on else 252
    colx, colw = (700, 750) if art_on else (350, 900)
    x0, w = (150, 1300) if art_on else (colx, colw)
    visible = int((790 - bt) // pitch)
    for i in range(visible + 1):
        y = bt + i * pitch + pitch * .72 + 4
        if y < 798:
            page.rect(95, y, 1410, 1, (84, 120, 184, 77))
    page.rect(118, 0, 1.4, 900, (204, 71, 71, 128))
    kf = font(COUR, 14)
    page.tracked(x0, 92 + 14 * .781, "HOLLOW CURRENT  /  FIELD INVESTIGATION OFFICE", kf, F_KICK + (255,), 1.6)
    page.tracked(x0 + w, 92 + 14 * .781, d["kicker"], kf, F_KICK + (255,), 1.6, right=True)
    page.rect(x0, 118, w, 1.6, F_INK + (255,))
    hs = 38
    hf = font(COURB, hs)
    head = d["heading"].upper()
    while meas(hf, head) > w and hs > 22:
        hs -= 2
        hf = font(COURB, hs)
    page.text(x0, 130 + hs * .781, head, hf, F_RED + (255,))
    if d["subject"]:
        page.text(x0, 188 + 17 * .781, "SUBJECT: " + d["subject"], font(COUR, 17), F_INK + (255,))
    page.rect(x0, 226, w, 1, F_INK + (140,))
    if art_on:
        im = Image.open(art).convert("RGB")
        area = (150, 262, 490, 470)
        fit = min(area[2] / im.width, area[3] / im.height)
        iw, ih = im.width * fit, im.height * fit
        ix, iy = area[0] + (area[2] - iw) / 2, area[1] + (area[3] - ih) / 2
        page.rect(ix - 12 + 7, iy - 12 + 9, iw + 24, ih + 40, (0, 0, 0, 56))
        page.rect(ix - 12, iy - 12, iw + 24, ih + 40, (245, 242, 232, 255))
        page.fx.paste(im.resize((int(px(iw)), int(px(ih))), Image.LANCZOS), (int(px(ix)), int(px(iy))))
        for cx in (ix - 12 + 8, ix + iw + 12 - 8):
            tape = Image.new("RGBA", (int(px(68)), int(px(16))), (217, 204, 158, 184))
            tape = tape.rotate(34 if cx < ix + 50 else -34, expand=True, resample=Image.BICUBIC)
            page.fx.alpha_composite(tape, (int(px(cx) - tape.width / 2), int(px(iy - 10) - tape.height / 2)))
        page.tracked(colx, 246 + 14 * .781, "FIELD TRANSCRIPT", kf, F_KICK + (255,), 1.6)
    bf = font(COUR, size)
    rows, vis, total = body(page, d, bf, COUR, size, pitch, bt, colx, colw, F_INK, scroll=scroll)
    if total - vis > 0:
        cf = font(COUR, 15)
        page.text(colx + colw - meas(cf, "more below   (wheel, arrow keys)"), 790 + 2 + 15 * .781, "more below   (wheel, arrow keys)", cf, F_KICK + (255,))
    close_line(page, COUR, "close the file", F_INK)
    page.save(out)
    print("  field: %d rows, %d visible, %.1f total" % (len(rows), vis, total))


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    t = texts()
    castle_page("ledger", *t["ledger"], OUT / "docui_mock_castle_ledger.png")
    castle_page("letter", *t["letter"], OUT / "docui_mock_castle_letter.png")
    castle_page("inscription", *t["inscription"], OUT / "docui_mock_castle_inscription.png")
    castle_page("tally", *t["tally"], OUT / "docui_mock_castle_tally.png")
    field_page(*t["photo"], OUT / "docui_mock_bellwether_photo.png", art=RES / "Surfaces/FamilyPhoto.png")
    field_page(*t["briefing"], OUT / "docui_mock_bellwether_briefing.png")


if __name__ == "__main__":
    main()
