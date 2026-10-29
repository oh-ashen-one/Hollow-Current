#!/usr/bin/env python3
"""Compile against installed Unity assemblies; execute only engine-independent rules.
Does NOT launch Unity, render, acquire a renderer, install packages or use the network.
"""
import json
import sys
import os
from pathlib import Path
import subprocess
import time

ROOT = Path(__file__).resolve().parents[1]
UNITY = Path(os.environ.get("UNITY_CONTENTS", "/Applications/Unity/Hub/Editor/6000.3.20f1/Unity.app/Contents"))
SCRIPT = UNITY / "Resources/Scripting"
OUT = ROOT / "evidence/compile"
OUT.mkdir(parents=True, exist_ok=True)
DOTNET = SCRIPT / "NetCoreRuntime/dotnet"
CSC = SCRIPT / "DotNetSdkRoslyn/csc.dll"
MONO = SCRIPT / "MonoBleedingEdge/bin/mono"
API = SCRIPT / "MonoBleedingEdge/lib/mono/4.8-api"
base = [API / "mscorlib.dll", API / "System.dll", API / "System.Core.dll", API / "Facades/netstandard.dll"]
managed = SCRIPT / "Managed/UnityEngine"
results = []
def compile(name, sources, references, target="library"):
    args = [str(DOTNET), str(CSC), "/nologo", "/noconfig", "/nostdlib+", "/langversion:9", "/target:" + target,
            "/out:" + str(OUT / name)]
    args += ["/reference:" + str(p) for p in references]
    args += [str(p) for p in sources]
    result = subprocess.run(args, cwd=ROOT, text=True, capture_output=True, timeout=60)
    print(result.stdout + result.stderr, end="")
    results.append({"target": name, "exit": result.returncode, "output": result.stdout + result.stderr})
    if result.returncode:
        write_report()
        raise SystemExit(result.returncode)
def write_report():
    (ROOT / "evidence/source-verification.json").write_text(json.dumps({"timestamp_utc":time.strftime("%Y-%m-%dT%H:%M:%SZ",time.gmtime()),"unity_version":"6000.3.20f1","engine_launched":False,"rendered":False,"input_system_reference_override":os.environ.get("INPUT_SYSTEM_REFERENCE"),"checks":results},indent=2)+"\n")
core = sorted((ROOT / "Assets/HollowCurrent/Core").rglob("*.cs"))
runtime = sorted((ROOT / "Assets/HollowCurrent/Runtime").rglob("*.cs"))
compile("RulesTests.exe", core + [ROOT / "Tests/RulesTests.cs"], base, "exe")
test = subprocess.run([str(MONO), str(OUT / "RulesTests.exe")],cwd=ROOT,text=True,capture_output=True,timeout=90) if "--compile-only" not in sys.argv else subprocess.CompletedProcess([],0,"Gameplay execution explicitly skipped (compile-only).\n","")
print(test.stdout+test.stderr,end="")
results.append({"target":"rules-execution","exit":test.returncode,"output":test.stdout+test.stderr})
if test.returncode:
    write_report()
    raise SystemExit(test.returncode)
engine = sorted(managed.glob("UnityEngine*.dll"))
unity_base = [SCRIPT / "NetStandard/ref/2.1.0/netstandard.dll"]
input_system = Path(os.environ.get("INPUT_SYSTEM_REFERENCE",str(ROOT / "Library/ScriptAssemblies/Unity.InputSystem.dll")))
if not input_system.is_file():
    raise SystemExit("Unity.InputSystem.dll not found. Import the pinned package once through the guarded editor, or set "
                     "INPUT_SYSTEM_REFERENCE to an imported checkout's Library/ScriptAssemblies/Unity.InputSystem.dll "
                     "(builder worktrees: /Users/midir/hollow-current-haunted-castle/Library/ScriptAssemblies/Unity.InputSystem.dll)")
unity_base.append(input_system)
player_managed = UNITY / "PlaybackEngines/MacStandaloneSupport/Variations/mono/Managed"
player_engine = sorted(player_managed.glob("UnityEngine*.dll"))
if not player_engine:
    raise SystemExit("Installed macOS Mono player assemblies missing; player compatibility is unverified")
# Editor engine assemblies expose some APIs stripped from the actual player.
# Compile against both surfaces so an editor-only Light API cannot slip through.
compile("HollowCurrent.Player.dll", core + runtime, unity_base + player_engine)
compile("HollowCurrent.dll", core + runtime, unity_base + engine)
editor = sorted(managed.glob("UnityEditor*.dll"))
compile("HollowCurrent.Editor.dll", sorted((ROOT / "Assets/HollowCurrent/Editor").rglob("*.cs")), unity_base+engine+editor+[OUT / "HollowCurrent.dll"])
write_report()
print("PASS runtime C# against installed macOS Mono player and editor assemblies; editor C# also compiled. This check does not launch or validate graphics.")
