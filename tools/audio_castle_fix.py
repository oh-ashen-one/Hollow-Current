#!/usr/bin/env python3
"""Wave K repair pass over Resources/CastleAudio (free, CPU-only, idempotent): run it after
tools/generate_open_voice.py --castle and tools/audio_castle_sfx.py, then run tools/check_audio_quality.py.

usage: audio_castle_fix.py [--dry-run]

1. Loops: any loop clip (a key played through Loop/CastleLoop/SpatialLoop, or named *_loop) whose wrap step
   |x[0]-x[-1]| exceeds SEAM_FIX (0.005 full scale) gets its tail crossfaded into its head (0.3 s, equal power).
   The clip gets 0.3 s shorter and its first/last samples become neighbours of the original signal, so the
   concatenated clip has no step. Clips already under the threshold are left alone (idempotent).
2. Voices (voice_cc_*): trailing silence (below -50 dBFS) is cut to 0.15 s with a 60 ms fade, then gain is set so
   integrated loudness equals the median of Bellwether's Resources/Audio/voice_* clips (BS.1770, same meter as
   audio_castle_sfx.py), peak capped at -3 dBFS. voice_evp / voice_whisper are Bellwether's deliberately quiet ghost
   voices and are left out of that median.
3. Other non-loop castle clips with more than 0.25 s of trailing silence get the same trim (bell_single).
Shipped Bellwether files (Resources/Audio, HiggsfieldAudio) are never written.
"""
import sys,re
from pathlib import Path
import numpy as np
from scipy.io import wavfile
sys.path.insert(0,str(Path(__file__).resolve().parent))
import audio_castle_sfx as A
ROOT=Path(__file__).resolve().parents[1]
RES=ROOT/'Assets/HollowCurrent/Resources'
CASTLE=RES/'CastleAudio'
SEAM_FIX=0.005;XFADE=0.3;SIL_DB=-50.0;KEEP_TAIL=0.15;FADE=0.06;PEAK_CAP_DB=-3.0
GHOST={'voice_evp','voice_whisper'}
def load(p):
    sr,x=wavfile.read(p);assert x.dtype==np.int16,p
    return sr,x.astype(np.float64)/32768
def save(p,sr,x):wavfile.write(p,sr,np.clip(np.round(x*32767),-32768,32767).astype(np.int16))
def lufs(x,sr):return A.lufs(x) if sr==A.SR else A.lufs_any(x,sr)
def loop_names():
    names=set()
    for p in (ROOT/'Assets/HollowCurrent').rglob('*.cs'):
        if '/Editor/' in str(p) or '/Tests/' in str(p):continue
        names|=set(re.findall(r'\b(?:Loop|CastleLoop|SpatialLoop)\("([a-z0-9_]+)"',p.read_text(errors='replace')))
    names|={f.stem for f in CASTLE.glob('*_loop.wav')}
    return names
def tail_len(x,sr):
    idx=np.nonzero(np.abs(x)>10**(SIL_DB/20))[0]
    return (len(x)-1-idx[-1])/sr if len(idx) else len(x)/sr
def trim_tail(x,sr):
    idx=np.nonzero(np.abs(x)>10**(SIL_DB/20))[0]
    if not len(idx):return x
    y=x[:min(len(x),idx[-1]+1+int(KEEP_TAIL*sr))].copy();n=int(FADE*sr)
    y[-n:]*=0.5*(1+np.cos(np.linspace(0,np.pi,n)));return y
def xfade_loop(x,sr):
    n=int(XFADE*sr);L=len(x);u=np.linspace(0,1,n)
    mix=x[L-n:]*np.cos(u*np.pi/2)+x[:n]*np.sin(u*np.pi/2)          # tail fades out while the head fades in
    return np.concatenate([mix,x[n:L-n]])
def main():
    dry='--dry-run' in sys.argv[1:];loops=loop_names()
    bell=[]
    for p in sorted((RES/'Audio').glob('voice_*.wav')):
        if p.stem in GHOST:continue
        sr,x=load(p);bell.append(lufs(x,sr))
    target=float(np.median(bell));print(f'Bellwether voice median {target:.2f} LUFS over {len(bell)} clips')
    for p in sorted(CASTLE.glob('*.wav')):
        sr,x=load(p);name=p.stem;note=[]
        if name in loops:
            seam=abs(x[0]-x[-1])
            if seam>SEAM_FIX:
                x=xfade_loop(x,sr);note.append(f'loop seam {seam:.4f} -> {abs(x[0]-x[-1]):.4f}, {len(x)/sr:.2f} s')
        elif name.startswith('voice_cc_'):
            t=tail_len(x,sr)
            if t>KEEP_TAIL+0.01:x=trim_tail(x,sr);note.append(f'tail {t:.2f} -> {tail_len(x,sr):.2f} s')
            g=target-lufs(x,sr)
            pk=20*np.log10(np.abs(x).max()+1e-12)
            if pk+g>PEAK_CAP_DB:g=PEAK_CAP_DB-pk;note.append('peak-capped')
            if abs(g)>0.05:x=x*10**(g/20);note.append(f'gain {g:+.2f} dB')
        else:
            t=tail_len(x,sr)
            if t>0.25:x=trim_tail(x,sr);note.append(f'tail {t:.2f} -> {tail_len(x,sr):.2f} s')
        if note:
            print(f'{name}: '+'; '.join(note))
            if not dry:save(p,sr,x)
if __name__=='__main__':main()
