#!/usr/bin/env python3
"""Static audio-key check: every sound the C# asks for must exist on disk. Exits 1 on any missing key.

usage: python3 tools/check_audio_keys.py [-v]

Scans Assets/HollowCurrent/Runtime/**/*.cs and Core/**/*.cs (no Unity needed) for:
  * Speak(id)                 -> castle: voice_<mapped id> in CastleAudio (mapping copied from Soundscape.Speak);
                                 Bellwether (non-cc_ id): voice_<id> in Audio
  * Cue / Loop / CastleLoop / SpatialLoop / UI / Has / ContainsKey (clip id)
                              -> clip stem in Audio, HiggsfieldAudio or CastleAudio. All string literals in the
                                 first argument count, so ternaries (phase==Won?"a":"b") and a variable assigned
                                 from literals in the same file (Speak(hint)) are covered. "step" expands to the
                                 step_<surface>_0..3 set (wood/tile/carpet/gravel in Audio; stone, the castle's indoor surface, in CastleAudio), "voice" to voice_evp, "breath" to breath_presence.
                                 A literal followed by "+" (e.g. "claw_steps_"+i) is a prefix: <prefix>0..3 must exist.
  * `.Key="..."`              -> clip id (CueOnImpact)
  * any whole string literal  "cc_*" -> voice_cc_* in CastleAudio (Presence.Voice, Forge/CaseBook note ids, ...)
  * the required-id arrays in Soundscape.Initialize
Reasons it can still miss something: ids built at run time from non-literals (the script prints each such call as
'dynamic' so it can be read), and anything outside Runtime/ and Core/. Keep new keys literal.
"""
import re,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
ASSETS=ROOT/'Assets/HollowCurrent'
EXT=('.wav','.ogg','.mp3','.aif','.aiff')
def stems(d):
    p=ASSETS/'Resources'/d
    return {f.stem for f in p.iterdir() if f.suffix.lower() in EXT} if p.is_dir() else set()
AUDIO,HIGGS,CASTLE=stems('Audio'),stems('HiggsfieldAudio'),stems('CastleAudio')
ANY=AUDIO|HIGGS|CASTLE
SURFACES=('wood','tile','carpet','gravel','stone')   # stone = the castle's indoor surface (CastleAudio only; World.StepSurface)

def strip_comments(s):
    out=[];i=0;n=len(s)
    while i<n:
        c=s[i]
        if c=='"':
            j=i+1
            while j<n and s[j]!='"':j+=2 if s[j]=='\\' else 1
            out.append(s[i:j+1]);i=j+1
        elif s.startswith('//',i):
            while i<n and s[i]!='\n':i+=1
        elif s.startswith('/*',i):
            j=s.find('*/',i+2);i=n if j<0 else j+2
        else:out.append(c);i+=1
    return ''.join(out)
LIT=re.compile(r'"((?:[^"\\]|\\.)*)"')
def first_arg(s,start):
    """text of the first argument of the call whose '(' is at s[start-1]"""
    depth=0;i=start;n=len(s)
    while i<n:
        c=s[i]
        if c=='"':
            j=i+1
            while j<n and s[j]!='"':j+=2 if s[j]=='\\' else 1
            i=j+1;continue
        if c in '([{':depth+=1
        elif c in ')]}':
            if depth==0:return s[start:i]
            depth-=1
        elif c==',' and depth==0:return s[start:i]
        i+=1
    return s[start:]
def speak_key(i):  # copied from Soundscape.Speak (castle branch)
    if i.startswith('cc_'):return i
    return {'evp':'cc_bargain','whisper':'cc_listen'}.get(i)   # every other non-cc_ id is dropped in the castle (owner item 13)

CALL=re.compile(r'\b(Speak|BellwetherSpeak|Cue|Loop|CastleLoop|SpatialLoop|UI|Has|ContainsKey)\(')
files=sorted(p for d in ('Runtime','Core') for p in (ASSETS/d).rglob('*.cs'))
uses=[]      # (kind, key, file, line)
dynamic=[]
required=[]
for f in files:
    raw=f.read_text(encoding='utf-8',errors='replace');s=strip_comments(raw)
    rel=str(f.relative_to(ROOT))
    line_of=lambda pos:s.count('\n',0,pos)+1
    for m in CALL.finditer(s):
        kind=m.group(1);arg=first_arg(s,m.end()).strip()
        if kind=='ContainsKey' and f.name!='Soundscape.cs':continue        # other dictionaries share the name
        if re.match(r'(string|int|float|bool|var)\s',arg) or arg=='' :continue   # definition or empty
        lits=LIT.findall(arg)
        if not lits:
            if kind in ('Speak','BellwetherSpeak') and re.fullmatch(r'[A-Za-z_]\w*',arg):                                   # variable: take literals assigned to it in this file
                for am in re.finditer(r'\b'+re.escape(arg)+r'\s*=([^;=][^;]*);',s):
                    lits+=LIT.findall(am.group(1))
            if not lits:dynamic.append((kind,arg,rel,line_of(m.start())));continue
        for lit in lits:
            if len(lit)<2 or ' ' in lit or lit.startswith('HC_'):continue
            prefix=lit.endswith('_') and re.search(re.escape('"'+lit+'"')+r'\s*\+',arg) is not None
            if lit.endswith('_') and not prefix:continue
            uses.append(('prefix' if prefix else kind,lit,rel,line_of(m.start())))
    for m in re.finditer(r'\.Key\s*=\s*"([^"]+)"',s):uses.append(('Cue',m.group(1),rel,line_of(m.start())))
    for m in re.finditer(r'"(cc_[a-z0-9_]+)"',s):uses.append(('cc',m.group(1),rel,line_of(m.start())))
    if f.name=='Soundscape.cs':
        for m in re.finditer(r'foreach\(string id in new\[\]\{([^}]*)\}\)',s):
            for lit in LIT.findall(m.group(1)):required.append((lit,rel,line_of(m.start())))

# Keys the contracts promise even when no C# references them yet (AUDIO.md, FEAR_PLAN F4, wave G).
CONTRACT=['heartbeat_loop','wind_battlements_loop','door_rattle','book_drop','knife_stick','claw_steps_0','claw_steps_1','claw_steps_2',
          'claw_steps_3','bat_pass','bell_single',
          'castle_hide_breath_0','castle_hide_breath_1','castle_hide_clawstep','castle_hide_handle','castle_hide_whisper']
VARIANTS={'castle_hide_breath_':2}     # indexed sets that are not four clips long
# Owner feedback 13 (hc/fb-voice2): these castle lines were spoken 2D / as a PA; nothing may reference them any more, so their
# clips (voice_<id>.wav) are dead weight to delete. cc_hunt_hungry is gone for good (also from tools/voice-lines.json).
RETIRED=['cc_hunt_hungry','cc_night','cc_kit','cc_hint_signal','cc_hint_trace','cc_hint_voice','cc_contain','cc_ending','cc_dispatch']
retired_hits=[]
for kind,key,rel,line in uses:
    if key in RETIRED:retired_hits.append(f'{rel}:{line} references retired line {key}')
try:
    import json as _json
    for rid in _json.load(open(ROOT/'tools/voice-lines.json')):
        if rid=='cc_hunt_hungry':retired_hits.append('tools/voice-lines.json still has cc_hunt_hungry')
except Exception as e:retired_hits.append(f'voice-lines.json unreadable: {e}')
missing={}
def need(key,found,where,why):
    if not found:missing.setdefault(key,[]).append(f'{where}  ({why})')
checked=set()
for kind,key,rel,line in uses:
    where=f'{rel}:{line}';tag=(kind,key)
    if kind=='BellwetherSpeak':    # Game.BellwetherSpeak: Bellwether only, silent in the castle
        need('voice_'+key,('voice_'+key) in AUDIO,where,f'BellwetherSpeak("{key}") in Bellwether')
    elif kind=='Speak':
        k=speak_key(key)
        if k is None:continue      # dropped in the castle; Bellwether never calls Speak directly outside Game.BellwetherSpeak
        need('voice_'+k,('voice_'+k) in CASTLE,where,f'Speak("{key}") in the castle')
    elif kind=='cc':
        need('voice_'+key,('voice_'+key) in CASTLE,where,'cc_ id literal')
    elif kind=='prefix':
        for i in range(VARIANTS.get(key,4)):need(f'{key}{i}',(f'{key}{i}') in ANY,where,f'indexed clip "{key}"+n')
    elif kind in ('Cue','Loop','CastleLoop','SpatialLoop','UI','Has','ContainsKey'):
        if key=='step':
            for sfc in SURFACES:
                for i in range(4):need(f'step_{sfc}_{i}',f'step_{sfc}_{i}' in (CASTLE if sfc=='stone' else AUDIO),where,'Cue("step")'+(' (castle-only surface, CastleAudio)' if sfc=='stone' else ''))
        elif key=='voice':need('voice_evp','voice_evp' in AUDIO and 'voice_cc_bargain' in CASTLE,where,'Cue("voice")')
        elif key=='breath':need('breath_presence','breath_presence' in ANY,where,'Cue("breath")')
        else:need(key,key in ANY,where,f'{kind}("{key}")')
for key in CONTRACT:need(key,key in ANY,'check_audio_keys.py CONTRACT','promised by AUDIO.md / wave G')
for key,rel,line in required:
    need(key,key in ANY,f'{rel}:{line}','required-id array in Soundscape.Initialize')

if '-v' in sys.argv[1:]:
    seen=sorted({(k,key) for k,key,_,_ in uses})
    print(f'{len(seen)} distinct (kind,key) uses in {len(files)} files; disk: Audio {len(AUDIO)}, HiggsfieldAudio {len(HIGGS)}, CastleAudio {len(CASTLE)}')
    for k,key in seen:print(f'  {k:11s} {key}')
for kind,arg,rel,line in dynamic:print(f'note: dynamic {kind}({arg}) at {rel}:{line} (not checkable; literals feeding it are covered by the cc_ rule)')
if retired_hits:
    print('FAIL: retired castle voice lines are still referenced')
    for h in retired_hits:print('  '+h)
    sys.exit(1)
if missing:
    print(f'FAIL: {len(missing)} audio key(s) missing on disk')
    for k in sorted(missing):
        print('  MISSING',k)
        for w in missing[k][:4]:print('     used at',w)
    sys.exit(1)
print(f'OK: all audio keys resolve ({len({(k,key) for k,key,_,_ in uses})} distinct uses, {len(required)} required ids, {len(files)} files scanned)')
