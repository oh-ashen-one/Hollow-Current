#!/usr/bin/env python3
"""Rebuild redistributable synthetic dialogue from original scripts using eSpeak NG.
Requires espeak-ng and ffmpeg on PATH. Does not use Apple System Voices.

usage: generate_open_voice.py [--castle] [entry ...]   (entries from voice-lines.json; none = all, or all cc_* with --castle)
"""
import json,shutil,subprocess,sys,tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/HollowCurrent/Resources/Audio'
CASTLE_OUT=ROOT/'Assets/HollowCurrent/Resources/CastleAudio'
# Count Claude: lower and slower than the house voices, dropped a further ~12% in pitch,
# then a long stone-hall echo chain so he sounds nothing like Bellwether's whisper/evp ghost.
CASTLE_FX=('aresample=22050,asetrate=22050*0.88,aresample=22050,highpass=f=110,lowpass=f=3000,'
           'acompressor=threshold=0.1:ratio=3:attack=5:release=90,apad=pad_dur=1.0,'
           'aecho=0.8:0.85:47|113|211|397|683|1120:0.40|0.32|0.26|0.20|0.14|0.09,volume=0.9')
def main():
    castle_only='--castle' in sys.argv[1:]
    only={a for a in sys.argv[1:] if not a.startswith('--')}
    for name in ('espeak-ng','ffmpeg'):
        if not shutil.which(name):raise SystemExit('Install '+name+' and put it on PATH first.')
    OUT.mkdir(parents=True,exist_ok=True);CASTLE_OUT.mkdir(parents=True,exist_ok=True)
    lines=json.loads((ROOT/'tools/voice-lines.json').read_text())
    with tempfile.TemporaryDirectory(prefix='hollow-voice-') as tmp:
        for name,spec in lines.items():
            castle=name.startswith('cc_')
            if castle_only and not castle:continue
            if only and name not in only:continue
            dry=Path(tmp)/(name+'.wav')
            cmd=['espeak-ng','-v',spec['voice'],'-s',str(spec['rate'])]
            if 'pitch' in spec:cmd+=['-p',str(spec['pitch'])]
            subprocess.run(cmd+['-w',str(dry),spec['text']],check=True)
            fx='highpass=f=320,lowpass=f=3400,acompressor=threshold=0.1:ratio=3:attack=5:release=80,volume=1.3'
            if castle:fx=CASTLE_FX
            elif name in ('evp','whisper'):fx='aresample=22050,asetrate=22050*0.91,aresample=22050,highpass=f=190,lowpass=f=2600,aecho=0.75:0.45:83|173:0.20|0.10,volume=1.1'
            subprocess.run(['ffmpeg','-v','error','-y','-i',str(dry),'-map_metadata','-1','-ar','22050','-ac','1','-af',fx,'-c:a','pcm_s16le',str((CASTLE_OUT if castle else OUT)/('voice_'+name+'.wav'))],check=True)
    unknown=only-set(lines)
    if unknown:raise SystemExit('unknown voice entries '+' '.join(sorted(unknown)))
    print('Generated',sum(1 for n in lines if (n.startswith('cc_') or not castle_only) and (not only or n in only)),'public voice recordings with eSpeak NG.')
if __name__=='__main__':main()
