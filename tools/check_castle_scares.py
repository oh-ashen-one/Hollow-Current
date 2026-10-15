#!/usr/bin/env python3
"""Static check of the castle scare set (pure python: no Unity, no build).

Counts the registered dread beats and throwables and asserts that every castle beat logs, every throw reject path logs, and
the lies and silence carry their HC_LIE / HC_SCARE_CASTLE lines. Exit 0 = pass, 1 = fail.
"""
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CASTLE = ROOT / "Assets/HollowCurrent/Runtime/Castle"
scares = (CASTLE / "CastleScares.cs").read_text()
world = (CASTLE / "CastleWorld.cs").read_text()
throwable = (CASTLE / "Throwable.cs").read_text()
director = (ROOT / "Assets/HollowCurrent/Runtime/ScareDirector.cs").read_text()
failures = []


def body(source, signature):
    """Text of the method whose declaration contains `signature` (brace matched), or None."""
    m = re.search(r"(?:bool|int|void)\s+" + re.escape(signature) + r"\(", source) if not signature.startswith("public ") and not signature.startswith("bool ") else None
    start = m.start() if m else source.find(signature)
    if start < 0:
        return None
    brace = source.find("{", start)
    depth, i = 0, brace
    while i < len(source):
        if source[i] == "{":
            depth += 1
        elif source[i] == "}":
            depth -= 1
            if depth == 0:
                return source[brace:i + 1]
        i += 1
    return None


# 1. every beat method exists and logs (Log("<beat>") or an HC_ line)
beats = {
    "BookDrop": "book_drop", "KnifeStick": "knife_stick", "BatPass": "bat_pass", "BellToll": "bell_single", "CandleGutter": "candle_gutter",
    "Poltergeist": "poltergeist", "WindowSilhouette": "window_silhouette", "BalustradeShape": "balustrade_shape", "FalseCandleTell": "false_candle_tell",
    "FalseCompassReading": "false_compass", "Silence": "silence",
}
lies = {"PhantomListen": "HC_LIE kind=phantom_listen", "FootstepsBehind": "HC_LIE kind=footsteps_behind"}
for method, tag in beats.items():
    text = body(scares, method)
    if text is None:
        failures.append("beat method missing: " + method)
    elif 'Log("' + tag not in text and not (method == "BalustradeShape" and 'Log("balustrade_shape")' in scares):  # armed by the call, logged when it is placed
        failures.append("beat %s has no Log(\"%s...\") line" % (method, tag))
for method, tag in lies.items():
    text = body(scares, method)
    if text is None:
        failures.append("lie method missing: " + method)
    elif tag not in text:
        failures.append("lie %s has no %s line" % (method, tag))
for tag in ("HC_LIE kind=false_candle", "HC_LIE kind=false_compass"):
    if tag not in scares:
        failures.append("missing log " + tag)

# 2. Beat() reaches the five beats and silence; DreadTick reaches lies, peripheral beats and Beat
try_beat = body(scares, "bool TryBeat(")
reached = re.findall(r"return (\w+)\(\);", try_beat or "")
registered = [b for b in reached if b in beats]
if len(registered) != 5:
    failures.append("TryBeat registers %d beats, expected 5: %s" % (len(registered), registered))
tick = (body(scares, "public bool DreadTick(") or "") + (body(scares, "bool TickChoice(") or "")
for needle in ("Silence()", "FalseCandleTell()", "FalseCompassReading()", "Lie()", "WindowSilhouette()", "BalustradeShape()", "Beat(false)"):
    if not tick or needle not in tick:
        failures.append("DreadTick does not call " + needle)

# 3. Poltergeist reject paths: every `continue;` branch logs HC_THROW_SKIP (through Throwable.LogSkip) and the early return logs too
polter = body(scares, "public int Poltergeist(")
if polter:
    for line in polter.splitlines():
        if "continue;" in line and "LogSkip" not in line and "foreach" not in line and "if(!item)continue;" not in line:
            failures.append("Poltergeist reject without a log: " + line.strip())
    if "HC_THROW_SKIP reason=not_ready" not in polter:
        failures.append("Poltergeist not-ready return has no HC_THROW_SKIP")
# the director's reject paths all go through Skip(...)
throw = body(director, "public bool ThrowNearbyProp(")
if throw:
    for line in throw.splitlines():
        if "continue;" in line and "Skip(" not in line and "foreach" not in line and "if(!prop)" not in line:
            failures.append("ThrowNearbyProp reject without Skip: " + line.strip())
if "HC_THROW_SKIP" not in throwable:
    failures.append("Throwable.LogSkip does not log HC_THROW_SKIP")

# 4. throwables: call sites (static) and the cap on bounds
sites = len(re.findall(r"MakeThrowable\(Small\.", world))
if sites < 20:
    failures.append("only %d MakeThrowable call sites" % sites)

# 4b. tell-safe lint: any code line in CastleScares.cs that tests HuntActive or a Phase (hunt state) must be justified by a
# `// tell-safe:` comment on the same line or in the comment block directly above it (a beat whose presence depends on the hunt
# state is a signal to the player). Also SilenceChance must key on FeltDread, never on real Dread (critic N11).
lines = scares.splitlines()
for number, line in enumerate(lines, 1):
    code = line.split("//")[0]
    if not re.search(r"HuntActive|\bPhase\.|\.Phase\b|NoTellHunt|FalseWarning", code):
        continue
    if "Debug.Log(" in code:
        continue  # logging the phase is not a gate
    justified = any("tell-safe:" in l for l in lines[max(0, number - 9):number])  # same line or the 8 lines above (a property's comment block)
    if not justified:
        failures.append("CastleScares.cs:%d tests hunt state without a `// tell-safe:` comment: %s" % (number, line.strip()))
silence = body(scares, "public float SilenceChance")
if not silence or "FeltDread" not in silence or re.search(r"\.Dread\b", silence):
    failures.append("SilenceChance must key on FeltDread and not on real Dread")
if "TickAmbient" not in scares or "HC_AMBIENT" not in scares:
    failures.append("the ambient substitute (TickAmbient / HC_AMBIENT) is missing")
foot = body(scares, "IEnumerator Footsteps(")
if foot is None or re.search(r"Phase|HuntActive|LieAllowed|Active", foot):
    failures.append("Footsteps coroutine must not stop on a phase, hunt or Active change (only Ready)")

# 4c. budgets (critic r11, bell_single 9 times in 18 minutes): every beat and lie has a BudgetTable entry (max per run, gap), the beat
# method checks Can("<key>") and a log spends it; the bell is capped at 2 per run with a gap >= 240 s, everything else gaps >= 90 s.
table = {}
m = re.search(r"BudgetTable\s*=\s*new Dictionary<string,int\[\]>\s*\{(.*?)\n        \};", scares, re.S)
if not m:
    failures.append("BudgetTable not found in CastleScares.cs")
else:
    for key, mx, gap in re.findall(r'\{"(\w+)",new\[\]\{(\d+),(\d+)\}\}', m.group(1)):
        table[key] = (int(mx), int(gap))
for tag in list(beats.values()) + [t.split("kind=")[1] for t in lies.values()]:
    if tag not in table:
        failures.append("beat %s has no BudgetTable entry" % tag)
        continue
    mx, gap = table[tag]
    if mx < 1 or gap < 90:
        failures.append("budget for %s is too loose: max=%d gap=%d (need max >= 1 and gap >= 90)" % (tag, mx, gap))
if table.get("bell_single", (99, 0)) > (2, 10 ** 9) or table.get("bell_single", (0, 0))[1] < 240 or table.get("bell_single", (99, 0))[0] > 2:
    failures.append("bell_single must be max 2 per run with gap >= 240 s: %s" % (table.get("bell_single"),))
for method, tag in list(beats.items()) + [("PhantomListen", "phantom_listen"), ("FootstepsBehind", "footsteps_behind")]:
    text = body(scares, method)
    if text is not None and 'Can("%s")' % tag not in text:
        failures.append("%s does not check Can(\"%s\") (budget/cooldown)" % (method, tag))
for tag in ("phantom_listen", "footsteps_behind"):
    if 'Spend("%s")' % tag not in scares:
        failures.append("lie %s never spends its budget" % tag)
if "string key=beat.Split" not in scares or "Spend(key)" not in scares or "BudgetText(key" not in scares:
    failures.append("Log() must spend the beat's budget and print pick= budget=")
if "no beat twice in a row" not in scares and "key==lastKey" not in scares:
    failures.append("Can() must refuse the beat used last")
esc = body(scares, "public bool Escalate(")
if not esc or "reason=" not in esc or "substitute=" not in esc:
    failures.append("Escalate must log reason= and substitute=")
print("budget table: %d entries %s" % (len(table), sorted(table)))

# 4d. hidden-player beats (critic r12): own budgets, own clock, and no-tell parity. The four hide_* kinds must have budget entries
# (gap >= 90 s), TickHidden must run a 40-70 s clock, and the only phases it may read are Briefing (no run) and Hunt (the 5 s guard);
# HiddenBeat and the kind coroutines must read no phase, HuntActive, NoTellHunt or FalseWarning (a hidden beat equally likely in Calm and Hunt).
for kind in ("hide_breath", "hide_clawstep", "hide_handle", "hide_whisper"):
    if kind not in table:
        failures.append("hidden beat %s has no BudgetTable entry" % kind)
    elif table[kind][1] < 90 or table[kind][0] < 1:
        failures.append("hidden beat %s budget too loose: %s" % (kind, table[kind]))
    if 'HideKeys' not in scares or '"%s"' % kind not in scares:
        failures.append("hidden beat %s is not in HideKeys" % kind)
tick_hidden = body(scares, "void TickHidden(")
if not tick_hidden:
    failures.append("TickHidden missing")
else:
    if "Random.Range(40f,70f)" not in tick_hidden:
        failures.append("TickHidden must re-arm its clock with Random.Range(40f,70f)")
    if "PhaseTime<5" not in tick_hidden:
        failures.append("TickHidden must keep hidden beats out of the first 5 s of a hunt")
    phases = set(re.findall(r"Phase\.(\w+)", tick_hidden))
    if phases - {"Briefing", "Hunt"}:
        failures.append("TickHidden reads phases beyond Briefing/Hunt: %s" % sorted(phases))
    if re.search(r"HuntActive|NoTellHunt|FalseWarning|Dread", tick_hidden):
        failures.append("TickHidden reads hunt state or Dread (parity)")
for name in ("public bool HiddenBeat(", "IEnumerator HideBreath(", "IEnumerator HideClaws(", "IEnumerator HideHandle("):
    text = body(scares, name)
    if text is None:
        failures.append("missing " + name)
    elif re.search(r"\bPhase\b|HuntActive|NoTellHunt|FalseWarning|\.Dread\b", re.sub(r'Debug\.Log\([^;]*;', '', text)):
        failures.append(name + " depends on phase or Dread (hidden beats must be equally likely in Calm and Hunt)")
# placeholder audio: every placeholder in HideClips exists on disk (the wanted castle_hide_* keys may not yet, F4)
audio_dir = ROOT / "Assets/HollowCurrent/Resources"
stems = set()
for d in ("Audio", "HiggsfieldAudio", "CastleAudio"):
    if (audio_dir / d).is_dir():
        stems |= {f.stem for f in (audio_dir / d).iterdir() if f.suffix.lower() in (".wav", ".ogg", ".mp3")}
clips = re.search(r"HideClips=\{(.*?)\};", scares, re.S)
pairs = re.findall(r'"([^"]+)"', clips.group(1)) if clips else []
if len(pairs) != 8:
    failures.append("HideClips must hold 4 (wanted, placeholder) pairs")
for wanted, placeholder in zip(pairs[0::2], pairs[1::2]):
    ok = placeholder in stems or (placeholder.endswith("_") and all((placeholder + str(i)) in stems for i in range(4)))
    if not ok:
        failures.append("placeholder %s for %s does not exist on disk" % (placeholder, wanted))
    present = (wanted + "0") in stems and (wanted + "1") in stems if wanted.endswith("_") else wanted in stems  # a trailing _ = two-variant set _0/_1
    print("hidden clip %s -> %s (%s)" % (wanted, "present" if present else "waiting on F4, placeholder " + placeholder, "ok" if ok else "MISSING"))

# 4e. escalation rule seam (critic r12: hunts chained to apparitions): Escalate must use F1's policy (wave L integrator) with
# dark = !LitNearby and must not add a roll of its own.
esc2 = body(scares, "public bool Escalate(")
if not esc2 or "EscalationChanceNow(dark)" not in esc2 or "TryEscalate(dark)" not in esc2 or "EscalationBlock(dark)" not in esc2 or "!game.LitNearby" not in esc2:
    failures.append("Escalate must use Investigation.EscalationChanceNow/TryEscalate/EscalationBlock(dark) with dark=!game.LitNearby")
if esc2 and re.search(r"Random\.value\s*(<|>=?)\s*(pass|chance)", esc2):
    failures.append("Escalate must not roll the escalation chance itself (TryEscalate rolls it)")

# 5. Bellwether isolation: CastleScares is only reached through the castle gate in the director
if "IsCastle" not in director:
    failures.append("ScareDirector has no IsCastle gate")

print("castle scares: %d beat methods, %d registered in TryBeat, %d lie methods, %d MakeThrowable call sites (the build logs HC_THROWABLES placed=<n> for the loop-expanded count)" % (len(beats), len(registered), len(lies), sites))
if failures:
    for f in failures:
        print("FAIL " + f)
    sys.exit(1)
print("PASS every castle beat and throw reject path logs")
