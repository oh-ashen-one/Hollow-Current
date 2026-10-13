#!/usr/bin/env python3
"""Structural checks only, deliberately not a substitute for a Unity playtest."""
import json
from pathlib import Path
import re
ROOT=Path(__file__).resolve().parents[1]
EDITOR=Path("/Applications/Unity/Hub/Editor/6000.3.20f1/Unity.app/Contents")
checks=[]
def check(ok,label):
    if not ok: raise AssertionError(label)
    checks.append(label)
manifest=json.loads((ROOT/"Packages/manifest.json").read_text())
for name,version in manifest["dependencies"].items():
    package=EDITOR/"Resources/PackageManager/BuiltInPackages"/name/"package.json"
    check(package.is_file(),"Installed official module: "+name)
    check(json.loads(package.read_text())["version"]==version,"Pinned module version: "+name)
guids=set()
for asset in sorted((ROOT/"Assets").rglob("*")):
    if asset.suffix==".meta": continue
    meta=Path(str(asset)+".meta")
    check(meta.is_file(),"Metadata: "+asset.relative_to(ROOT).as_posix())
    match=re.search(r"^guid: ([0-9a-f]{32})$",meta.read_text(),re.M)
    check(bool(match),"Valid GUID: "+asset.name)
    check(match[1] not in guids,"Unique GUID: "+asset.name)
    guids.add(match[1])
# Scene names come from the single C# map catalog (Core/MapId.cs), never a second list here.
catalog=(ROOT/"Assets/HollowCurrent/Core/MapId.cs").read_text()
maps=re.findall(r'new MapInfo\(MapId\.(\w+),"([^"]+)"',catalog)
check(len(maps)>=1 and maps[0]==("Bellwether","Bellwether"),"Map catalog lists Bellwether first")
settings=(ROOT/"ProjectSettings/EditorBuildSettings.asset").read_text()
registered=re.findall(r"path: (Assets/\S+\.unity)\s+guid: ([0-9a-f]{32})",settings)
check(bool(registered) and registered[0][0]=="Assets/HollowCurrent/Scenes/Bellwether.unity","Bellwether is the startup scene")
for map_id,scene_name in maps:
    scene=ROOT/"Assets/HollowCurrent/Scenes"/(scene_name+".unity")
    check(scene.is_file(),"Scene present for map "+map_id)
    scene_guid=re.search(r"^guid: (.+)$",Path(str(scene)+".meta").read_text(),re.M)[1]
    check((scene.relative_to(ROOT).as_posix(),scene_guid) in registered,"Scene GUID registered for map "+map_id)
axes=(ROOT/"ProjectSettings/InputManager.asset").read_text()
for axis in ["Horizontal","Vertical","Mouse X","Mouse Y"]:check("m_Name: "+axis in axes,"Legacy input axis: "+axis)
check("[Ll]ibrary/" in (ROOT/".gitignore").read_text(),"Regenerable Unity Library cache excluded from source control")
report={"kind":"plain-file structural audit","assertions":len(checks),"passed":True,"runtime_validation":False,"checks":checks}
(ROOT/"evidence/project-audit.json").write_text(json.dumps(report,indent=2)+"\n")
print("PASS",len(checks),"structural assertions; runtime acceptance is recorded separately.")
