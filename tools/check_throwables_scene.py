#!/usr/bin/env python3
"""Static check (no Unity): every "Throwable / *" object in the prepared castle scene and the static editor flags on its
mesh parts. Batching-static parts (flag bit 4) cannot be thrown: the build merges them into a static batch.

usage: check_throwables_scene.py [Assets/HollowCurrent/Scenes/Castle.unity]
Exit 1 when any Throwable part carries BatchingStatic or the count is not 38.
"""
import re,sys,collections
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
path=sys.argv[1] if len(sys.argv)>1 else str(ROOT/"Assets/HollowCurrent/Scenes/Castle.unity")
src=open(path).read()
docs=re.split(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n',src,flags=re.M)
objs={}
for i in range(1,len(docs),3):
    objs[docs[i+1]]=(int(docs[i]),docs[i+2])
go_name={};go_flags={};go_comps={};tr_go={};tr_children={};go_tr={}
for fid,(cls,body) in objs.items():
    if cls==1:
        m=re.search(r'm_Name: (.*)',body);go_name[fid]=m.group(1) if m else ''
        m=re.search(r'm_StaticEditorFlags: (\d+)',body);go_flags[fid]=int(m.group(1)) if m else 0
        go_comps[fid]=re.findall(r'component: \{fileID: (-?\d+)\}',body)
    elif cls==4:
        g=re.search(r'm_GameObject: \{fileID: (-?\d+)\}',body).group(1)
        tr_go[fid]=g;go_tr[g]=fid
        ch=re.search(r'm_Children:\n((?:  - \{fileID: -?\d+\}\n)*)',body)
        tr_children[fid]=re.findall(r'fileID: (-?\d+)',ch.group(1)) if ch else []
# class 114 MonoBehaviour, check script guid
def comps_of(g):return [(objs[c][0],objs[c][1]) for c in go_comps.get(g,[]) if c in objs]
roots=[g for g,n in go_name.items() if n.startswith('Throwable / ')]
summary=collections.Counter();bodies=0;cols=0
for g in roots:
    flags=[]
    stack=list(tr_children.get(go_tr[g],[]))
    while stack:
        t=stack.pop();cg=tr_go[t];flags.append(go_flags[cg]);stack+=tr_children.get(t,[])
        for cls,_ in comps_of(cg):
            if cls==54:bodies+=1
            if cls in(65,64,136,135):cols+=1
    for cls,_ in comps_of(g):
        if cls==54:bodies+=1
        if cls in(65,64,136,135):cols+=1
    summary[(go_name[g],go_flags[g],tuple(sorted(set(flags))))]+=1
for k,v in sorted(summary.items()):print(v,k)
print('roots',len(roots),'rigidbodies',bodies,'colliders',cols)
batched=sum(v for (n,f,fl),v in summary.items() if any(x&4 for x in fl))
print('batching_static_throwables',batched)
# r13: the drawbridge leaf turns about "Drawbridge hinge"; any batching-static part under it stays raised on screen.
hinges=[g for g,n in go_name.items() if n=='Drawbridge hinge']
bridge_batched=0
for g in hinges:
    stack=list(tr_children.get(go_tr[g],[]))
    while stack:
        t=stack.pop();cg=tr_go[t];stack+=tr_children.get(t,[])
        if go_flags[cg]&4:bridge_batched+=1
print('drawbridge_hinges',len(hinges),'batching_static_drawbridge_parts',bridge_batched)
sys.exit(1 if batched or len(roots)!=38 or bridge_batched or len(hinges)!=1 else 0)
