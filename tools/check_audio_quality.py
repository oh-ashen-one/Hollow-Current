#!/usr/bin/env python3
"""Audio quality gate over every clip in Resources/Audio, HiggsfieldAudio and CastleAudio. Exits 1 on any failure.

usage: python3 tools/check_audio_quality.py [-v] [--strict]

Fails on (CPU-only, no Unity):
  * peak above -1 dBFS (any clip)
  * loop seam: |x[0]-x[-1]| > 0.01 full scale, i.e. the step you hear when the clip is concatenated with itself
    (loops = keys played through Loop/CastleLoop/SpatialLoop in the C#, plus every *_loop clip)
  * voice loudness: any voice_* whose BS.1770 integrated loudness is more than 1.5 dB from the median of Bellwether's
    Resources/Audio/voice_* clips (voice_evp and voice_whisper, Bellwether's deliberate ghost voices, are exempt)
  * trailing silence above 0.3 s (samples below -50 dBFS after the last audible one) on any non-loop clip
Shipped Bellwether clips (Resources/Audio, HiggsfieldAudio) are reported as WARN instead of FAIL for the voice and tail
rules because they are not ours to edit; --strict turns those warnings into failures. CastleAudio is always strict.
Fix pass: tools/audio_castle_fix.py.
"""
import re,sys
from pathlib import Path
import numpy as np
from scipy.io import wavfile
sys.path.insert(0,str(Path(__file__).resolve().parent))
import audio_castle_sfx as A
ROOT=Path(__file__).resolve().parents[1]
RES=ROOT/'Assets/HollowCurrent/Resources'
DIRS=('Audio','HiggsfieldAudio','CastleAudio')
PEAK_MAX=-1.0;SEAM_MAX=0.01;VOICE_TOL=1.5;TAIL_MAX=0.3;SIL_DB=-50.0
GHOST={'voice_evp','voice_whisper'}
def load(p):
    sr,x=wavfile.read(p)
    x={np.dtype('int16'):lambda a:a/32768.,np.dtype('int32'):lambda a:a/2**31,np.dtype('uint8'):lambda a:(a-128)/128.}.get(x.dtype,lambda a:a.astype(float))(x)
    return sr,(x.mean(1) if x.ndim>1 else x)
def loops():
    names=set()
    for p in (ROOT/'Assets/HollowCurrent').rglob('*.cs'):
        if '/Editor/' in str(p) or '/Tests/' in str(p):continue
        names|=set(re.findall(r'\b(?:Loop|CastleLoop|SpatialLoop)\("([a-z0-9_]+)"',p.read_text(errors='replace')))
    for d in DIRS:names|={f.stem for f in (RES/d).glob('*_loop.wav')}
    return names
def main():
    verbose='-v' in sys.argv[1:];strict='--strict' in sys.argv[1:]
    loopset=loops();clips=[]
    for d in DIRS:
        for p in sorted((RES/d).glob('*.wav')):
            sr,x=load(p);n=len(x);idx=np.nonzero(np.abs(x)>10**(SIL_DB/20))[0]
            clips.append(dict(dir=d,name=p.stem,sr=sr,dur=n/sr,peak=20*np.log10(np.abs(x).max()+1e-12),
                lufs=(A.lufs(x) if sr==A.SR else A.lufs_any(x,sr)),tail=((n-1-idx[-1])/sr if len(idx) else n/sr),
                seam=float(abs(x[0]-x[-1])),isloop=p.stem in loopset))
    bell=[c['lufs'] for c in clips if c['dir']=='Audio' and c['name'].startswith('voice_') and c['name'] not in GHOST]
    med=float(np.median(bell));fails=[];warns=[]
    def report(c,msg,shipped_ok):
        (warns if shipped_ok and c['dir']!='CastleAudio' and not strict else fails).append(f"{c['dir']}/{c['name']}: {msg}")
    for c in clips:
        if c['peak']>PEAK_MAX:report(c,f"peak {c['peak']:.1f} dBFS > {PEAK_MAX}",False)
        if c['isloop'] and c['seam']>SEAM_MAX:report(c,f"loop seam step {c['seam']:.4f} > {SEAM_MAX}",False)
        if c['name'].startswith('voice_') and c['name'] not in GHOST and abs(c['lufs']-med)>VOICE_TOL:
            report(c,f"voice {c['lufs']:.1f} LUFS, {c['lufs']-med:+.1f} dB from the Bellwether median {med:.1f}",True)
        if not c['isloop'] and c['tail']>TAIL_MAX:report(c,f"trailing silence {c['tail']:.2f} s > {TAIL_MAX}",True)
    print(f"{len(clips)} clips ({', '.join(d+' '+str(sum(c['dir']==d for c in clips)) for d in DIRS)}); Bellwether voice median {med:.2f} LUFS over {len(bell)} clips; {len(loopset&{c['name'] for c in clips})} loops")
    if verbose:
        for c in clips:print('%-16s %-26s %5d Hz %6.2f s %6.1f LUFS peak %6.1f tail %5.2f seam %.4f%s'%(c['dir'],c['name'],c['sr'],c['dur'],c['lufs'],c['peak'],c['tail'],c['seam'],' loop' if c['isloop'] else ''))
    for w in warns:print('WARN (shipped Bellwether, not edited):',w)
    if fails:
        print(f'FAIL: {len(fails)} finding(s)')
        for f in fails:print('  ',f)
        sys.exit(1)
    print('OK: no peak above -1 dBFS, no loop seam above 0.01, every voice within 1.5 dB of the median, no trailing silence above 0.3 s')
if __name__=='__main__':main()
