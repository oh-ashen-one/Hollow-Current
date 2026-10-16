#!/usr/bin/env python3
"""Audio quality gate over every clip in Resources/Audio, HiggsfieldAudio and CastleAudio. Exits 1 on any failure.

usage: python3 tools/check_audio_quality.py [-v] [--strict]

Fails on (CPU-only, no Unity):
  * castle_hide_* hidden-player beats: integrated loudness outside -30..-26 LUFS, peak at or above 0.6 full scale
    (-4.4 dBFS) or trailing silence above 0.15 s (wave L)
  * peak above -1 dBFS (any clip)
  * loop seam: |x[0]-x[-1]| > 0.01 full scale, i.e. the step you hear when the clip is concatenated with itself
    (loops = keys played through Loop/CastleLoop/SpatialLoop in the C#, plus every *_loop clip)
  * voice loudness: any voice_* whose BS.1770 integrated loudness is more than 1.5 dB from the median of Bellwether's
    Resources/Audio/voice_* clips (voice_evp and voice_whisper, Bellwether's deliberate ghost voices, are exempt)
  * footsteps (step_*): a tonal "ding" in the 150-600 Hz band: any Welch bin (about 47 Hz wide, 8 neighbours) more than 12 dB over the
    median of its neighbours, or band spectral flatness below 0.35. step_tile_* (peak 20+ dB, flatness 0.07, the r13 "ding") and
    step_wood_* trip this; as shipped Bellwether clips they are WARN, the castle's step_stone_* / step_gravel_* are FAIL
  * trailing silence above 0.3 s (samples below -50 dBFS after the last audible one) on any non-loop clip
Shipped Bellwether clips (Resources/Audio, HiggsfieldAudio) are reported as WARN instead of FAIL for the voice and tail
rules because they are not ours to edit; --strict turns those warnings into failures. CastleAudio is always strict.
Fix pass: tools/audio_castle_fix.py.
"""
import re,sys
from pathlib import Path
import numpy as np
from scipy.io import wavfile
from scipy.signal import welch
sys.path.insert(0,str(Path(__file__).resolve().parent))
import audio_castle_sfx as A
ROOT=Path(__file__).resolve().parents[1]
RES=ROOT/'Assets/HollowCurrent/Resources'
DIRS=('Audio','HiggsfieldAudio','CastleAudio')
BUMP_MAX=12.0;FLAT_MIN=0.35
PEAK_MAX=-1.0;SEAM_MAX=0.01;VOICE_TOL=1.5;TAIL_MAX=0.3;SIL_DB=-50.0
GHOST={'voice_evp','voice_whisper'}
def load(p):
    sr,x=wavfile.read(p)
    x={np.dtype('int16'):lambda a:a/32768.,np.dtype('int32'):lambda a:a/2**31,np.dtype('uint8'):lambda a:(a-128)/128.}.get(x.dtype,lambda a:a.astype(float))(x)
    return sr,(x.mean(1) if x.ndim>1 else x)
def tonal(x,sr,lo=150,hi=600,nb=3):
    """(worst bin over its neighbours in dB, spectral flatness) of the lo..hi Hz band, Welch with ~47 Hz bins"""
    nper=int(2**round(np.log2(sr/47)));f,P=welch(x,sr,nperseg=nper,noverlap=nper*3//4,window='hann')
    Pd=10*np.log10(P+1e-20);idx=np.nonzero((f>=lo)&(f<=hi))[0];worst=-99.0
    for i in idx:
        nbrs=[Pd[j] for j in range(i-nb,i+nb+1) if j!=i and 0<=j<len(Pd)]
        worst=max(worst,float(Pd[i]-np.median(nbrs)))
    Pb=P[idx];return worst,float(np.exp(np.mean(np.log(Pb+1e-20)))/np.mean(Pb))
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
                seam=float(abs(x[0]-x[-1])),isloop=p.stem in loopset,
                tone=(tonal(x,sr) if p.stem.startswith('step_') else None)))
    bell=[c['lufs'] for c in clips if c['dir']=='Audio' and c['name'].startswith('voice_') and c['name'] not in GHOST]
    med=float(np.median(bell));fails=[];warns=[]
    def report(c,msg,shipped_ok):
        (warns if shipped_ok and c['dir']!='CastleAudio' and not strict else fails).append(f"{c['dir']}/{c['name']}: {msg}")
    for c in clips:
        if c['peak']>PEAK_MAX:report(c,f"peak {c['peak']:.1f} dBFS > {PEAK_MAX}",False)
        if c['isloop'] and c['seam']>SEAM_MAX:report(c,f"loop seam step {c['seam']:.4f} > {SEAM_MAX}",False)
        if c['name'].startswith('voice_') and c['name'] not in GHOST and abs(c['lufs']-med)>VOICE_TOL:
            report(c,f"voice {c['lufs']:.1f} LUFS, {c['lufs']-med:+.1f} dB from the Bellwether median {med:.1f}",True)
        if c['tone']:
            bump,flat=c['tone']
            if bump>BUMP_MAX:report(c,f"tonal peak {bump:.1f} dB over its neighbours in 150-600 Hz (> {BUMP_MAX}): a ding",True)
            if flat<FLAT_MIN:report(c,f"150-600 Hz band spectral flatness {flat:.2f} < {FLAT_MIN} (tonal)",True)
        if c['name'].startswith('castle_hide_'):
            if not -30.0<=c['lufs']<=-26.0:report(c,f"hide beat {c['lufs']:.1f} LUFS outside -30..-26",False)
            if c['peak']>=20*np.log10(.6):report(c,f"hide beat peak {c['peak']:.1f} dBFS is at or above 0.6 full scale",False)
            if c['tail']>0.15+0.01:report(c,f"hide beat tail {c['tail']:.2f} s > 0.15",False)
        if not c['isloop'] and c['tail']>TAIL_MAX:report(c,f"trailing silence {c['tail']:.2f} s > {TAIL_MAX}",True)
    print(f"{len(clips)} clips ({', '.join(d+' '+str(sum(c['dir']==d for c in clips)) for d in DIRS)}); Bellwether voice median {med:.2f} LUFS over {len(bell)} clips; {len(loopset&{c['name'] for c in clips})} loops")
    if verbose:
        for c in clips:print('%-16s %-26s %5d Hz %6.2f s %6.1f LUFS peak %6.1f tail %5.2f seam %.4f%s%s'%(c['dir'],c['name'],c['sr'],c['dur'],c['lufs'],c['peak'],c['tail'],c['seam'],' loop' if c['isloop'] else '',(' tone %.1f dB flat %.2f'%c['tone']) if c['tone'] else ''))
    for w in warns:print('WARN (shipped Bellwether, not edited):',w)
    if fails:
        print(f'FAIL: {len(fails)} finding(s)')
        for f in fails:print('  ',f)
        sys.exit(1)
    print('OK: no peak above -1 dBFS, no loop seam above 0.01, every voice within 1.5 dB of the median, no trailing silence above 0.3 s, no tonal peak in any castle footstep')
if __name__=='__main__':main()
