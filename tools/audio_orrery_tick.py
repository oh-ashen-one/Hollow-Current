#!/usr/bin/env python3
"""Deterministic synthetic brass orrery tick -> Resources/CastleAudio/orrery_tick.wav (no paid generation).
Two damped metallic partials plus a short noise click, 48 kHz mono, normalised by audio_castle_post.py."""
import math,random,struct,subprocess,sys,wave,tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def main():
    sr=48000;n=int(sr*.28);rnd=random.Random(7);out=[]
    for i in range(n):
        t=i/sr
        click=(rnd.random()*2-1)*math.exp(-t*700)*.9
        ring=sum(a*math.sin(2*math.pi*f*t)*math.exp(-t*d) for f,a,d in((1480,.5,38),(2630,.3,55),(4410,.18,90),(860,.25,26)))
        out.append(max(-1,min(1,click+ring)))
    with tempfile.TemporaryDirectory() as tmp:
        raw=Path(tmp)/'tick.wav'
        with wave.open(str(raw),'wb') as w:
            w.setnchannels(1);w.setsampwidth(2);w.setframerate(sr);w.writeframes(b''.join(struct.pack('<h',int(s*32000)) for s in out))
        subprocess.run([sys.executable,'-B',str(ROOT/'tools/audio_castle_post.py'),str(raw),'orrery_tick','--keep-tail','--peak','-14'],check=True)
if __name__=='__main__':main()
