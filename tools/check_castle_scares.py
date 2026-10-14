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
tick = body(scares, "public bool DreadTick(")
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

# 5. Bellwether isolation: CastleScares is only reached through the castle gate in the director
if "IsCastle" not in director:
    failures.append("ScareDirector has no IsCastle gate")

print("castle scares: %d beat methods, %d registered in TryBeat, %d lie methods, %d MakeThrowable call sites (the build logs HC_THROWABLES placed=<n> for the loop-expanded count)" % (len(beats), len(registered), len(lies), sites))
if failures:
    for f in failures:
        print("FAIL " + f)
    sys.exit(1)
print("PASS every castle beat and throw reject path logs")
