#!/usr/bin/env python3
"""Rebuild redistributable synthetic dialogue from original scripts using eSpeak NG.
Requires espeak-ng and ffmpeg on PATH. Does not use Apple System Voices.
"""
import json,shutil,subprocess,tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/HollowCurrent/Resources/Audio'
def main():
    for name in ('espeak-ng','ffmpeg'):
        if not shutil.which(name):raise SystemExit('Install '+name+' and put it on PATH first.')
    OUT.mkdir(parents=True,exist_ok=True)
    lines=json.loads((ROOT/'tools/voice-lines.json').read_text())
    with tempfile.TemporaryDirectory(prefix='hollow-voice-') as tmp:
        for name,spec in lines.items():
            dry=Path(tmp)/(name+'.wav')
            subprocess.run(['espeak-ng','-v',spec['voice'],'-s',str(spec['rate']),'-w',str(dry),spec['text']],check=True)
            fx='highpass=f=320,lowpass=f=3400,acompressor=threshold=0.1:ratio=3:attack=5:release=80,volume=1.3'
            if name in ('evp','whisper'):fx='aresample=22050,asetrate=22050*0.91,aresample=22050,highpass=f=190,lowpass=f=2600,aecho=0.75:0.45:83|173:0.20|0.10,volume=1.1'
            subprocess.run(['ffmpeg','-v','error','-y','-i',str(dry),'-map_metadata','-1','-ar','22050','-ac','1','-af',fx,'-c:a','pcm_s16le',str(OUT/('voice_'+name+'.wav'))],check=True)
    print('Generated',len(lines),'public voice recordings with eSpeak NG.')
if __name__=='__main__':main()
