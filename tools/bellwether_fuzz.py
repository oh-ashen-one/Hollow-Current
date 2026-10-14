#!/usr/bin/env python3
"""Bellwether differential fuzz: the current rules must behave byte-for-byte like the reference snapshot.

Compiles Tests/BellwetherFuzz.cs twice, once against Tests/BellwetherReference/*.cs (the Core at
haunted-castle c4b24c1, before waves G and H) and once against Assets/HollowCurrent/Core/*.cs, runs both
over the same seeds with live random dice, and compares the SHA-256 of the full state trace. On a
mismatch it dumps both traces to evidence/bellwether-fuzz/ and prints the first differing line.
Exit 0 = identical, 1 = different, 2 = could not build or run.
Uses Unity's bundled compiler and Mono only. Does NOT launch Unity, render or use the network.

usage: python3 tools/bellwether_fuzz.py [--seeds 4000] [--steps 300]
"""
import argparse
import json
import os
from pathlib import Path
import subprocess
import sys
import time

ROOT = Path(__file__).resolve().parents[1]
UNITY = Path(os.environ.get("UNITY_CONTENTS", "/Applications/Unity/Hub/Editor/6000.3.20f1/Unity.app/Contents"))
SCRIPT = UNITY / "Resources/Scripting"
DOTNET = SCRIPT / "NetCoreRuntime/dotnet"
CSC = SCRIPT / "DotNetSdkRoslyn/csc.dll"
MONO = SCRIPT / "MonoBleedingEdge/bin/mono"
API = SCRIPT / "MonoBleedingEdge/lib/mono/4.8-api"
REFS = [API / "mscorlib.dll", API / "System.dll", API / "System.Core.dll", API / "Facades/netstandard.dll"]
OUT = ROOT / "evidence/bellwether-fuzz"
DRIVER = ROOT / "Tests/BellwetherFuzz.cs"
SOURCES = {
    "reference": sorted((ROOT / "Tests/BellwetherReference").glob("*.cs")),
    "current": sorted((ROOT / "Assets/HollowCurrent/Core").rglob("*.cs")),
}


def build(name, sources):
    exe = OUT / (name + ".exe")
    args = [str(DOTNET), str(CSC), "/nologo", "/noconfig", "/nostdlib+", "/langversion:9", "/optimize+", "/target:exe",
            "/nowarn:0649,0414,0169", "/out:" + str(exe)] + ["/reference:" + str(p) for p in REFS] + [str(p) for p in sources + [DRIVER]]
    result = subprocess.run(args, cwd=ROOT, text=True, capture_output=True, timeout=120)
    if result.returncode:
        print(result.stdout + result.stderr)
        sys.exit(2)
    return exe


def run(exe, seeds, steps, dump=None):
    args = [str(MONO), str(exe), str(seeds), str(steps)] + ([str(dump)] if dump else [])
    result = subprocess.run(args, cwd=ROOT, text=True, capture_output=True, timeout=1800)
    if result.returncode:
        print(result.stdout + result.stderr)
        sys.exit(2)
    return result.stdout.strip()


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--seeds", type=int, default=4000)
    parser.add_argument("--steps", type=int, default=300)
    opts = parser.parse_args()
    if not SOURCES["reference"]:
        print("Tests/BellwetherReference has no sources")
        sys.exit(2)
    OUT.mkdir(parents=True, exist_ok=True)
    started = time.time()
    exes = {name: build(name, files) for name, files in SOURCES.items()}
    results = {name: run(exe, opts.seeds, opts.steps) for name, exe in exes.items()}
    same = results["reference"] == results["current"]
    report = {"timestamp_utc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()), "seeds": opts.seeds, "steps": opts.steps,
              "reference": results["reference"], "current": results["current"], "identical": same,
              "seconds": round(time.time() - started, 1), "engine_launched": False}
    if not same:
        traces = {name: OUT / (name + ".trace") for name in exes}
        for name, exe in exes.items():
            run(exe, opts.seeds, opts.steps, traces[name])
        with open(traces["reference"], encoding="utf-8") as a, open(traces["current"], encoding="utf-8") as b:
            for number, (left, right) in enumerate(zip(a, b), 1):
                if left != right:
                    report["first_difference"] = {"line": number, "reference": left.rstrip("\n")[:400], "current": right.rstrip("\n")[:400]}
                    break
    (OUT / "report.json").write_text(json.dumps(report, indent=2) + "\n")
    print("reference " + results["reference"])
    print("current   " + results["current"])
    if same:
        print("PASS Bellwether identical to the c4b24c1 reference over %d seeds x %d steps (live random dice)." % (opts.seeds, opts.steps))
        return 0
    print("FAIL Bellwether differs from the reference: " + json.dumps(report.get("first_difference")))
    return 1


if __name__ == "__main__":
    sys.exit(main())
