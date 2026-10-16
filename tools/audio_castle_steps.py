#!/usr/bin/env python3
"""Castle footsteps -> Resources/CastleAudio (free, deterministic, numpy/scipy only).

usage: audio_castle_steps.py [stone|gravel ...]     (no arguments = both sets; this script owns only step_stone_* and
                                                   step_gravel_*, so running it bare is safe, unlike audio_castle_sfx.py)

Why: the shared step_tile_* clips have a strong tonal peak at about 335-390 Hz (20 dB over its neighbours); that tone is the
"ding" the owner heard on every step (r13 feedback 1). The castle now maps indoor positions to "stone" (World.StepSurface) and
layers these clips over the shared ones (Soundscape loads CastleAudio over Audio by file name).

step_stone_0..3: a careful walk in cloth and soft leather on flagstone. Each clip is built only from filtered NOISE events so
there is no tonal component: a soft heel thud (low-passed noise), a leather sole scuff (band-passed noise, slow attack),
a cloth swish (amplitude-modulated noise, the trouser shuffle, kept under the scuff), a small toe settle, plus a quiet
short noise-reverb tail for the hall. Timing, bands and gains differ per clip so four steps never repeat.
step_gravel_0..3: the same walk on loose grit outdoors (the shared gravel clips peak at -2.6 dBFS, far too hot).
Finishing: DC removed, peak -14 dBFS, 48 kHz mono PCM16. Checked by tools/check_audio_quality.py (tonality rule).
"""
import sys
from pathlib import Path
import numpy as np
sys.path.insert(0,str(Path(__file__).resolve().parent))
import audio_castle_sfx as A
SR=A.SR;PEAK=-14.0
R=A.R;bp=A.bp;lp=A.lp;hp=A.hp;add=A.add

def env(n,attack,decay,shape=1.0):
    t=np.arange(n)/SR
    a=np.minimum(1,t/max(attack,1e-4))
    a=0.5*(1-np.cos(np.pi*a))
    return a*np.exp(-t*decay)**shape
def noise(r,n):return r.standard_normal(n)
def slow_mod(r,n,hz):
    """smooth positive random modulator (cloth rustle)"""
    m=lp(noise(r,n),hz,2);m=m/(np.abs(m).max()+1e-9);return 0.35+0.65*np.abs(m)
def heel(r,dur=.09,lo=70,hi=520,decay=48,gain=1.0):
    n=int(dur*SR);x=bp(noise(r,n),lo,hi,2)*env(n,.004,decay);return gain*x
def scuff(r,dur=.16,lo=1200,hi=4200,decay=22,attack=.018,gain=.5):
    n=int(dur*SR);x=bp(noise(r,n),lo,hi,2)*env(n,attack,decay);return gain*x
def cloth(r,dur=.30,lo=260,hi=2300,decay=9,gain=.4):
    n=int(dur*SR);x=bp(noise(r,n),lo,hi,2)*slow_mod(r,n,14)*env(n,.03,decay);return gain*x
def mix(*evs):
    out=np.zeros(max(len(e) for e in evs))
    for e in evs:out[:len(e)]+=e
    return out
def hall(x,seed,wet=.16):
    return A.room(x,.9,wet,seed,lp_hz=2800)
def finish_step(name,x):
    x=np.asarray(x,dtype=np.float64);x=x-np.mean(x)
    x=hp(x,60,2)
    x*=10**((PEAK-A.peak_db(x))/20)
    idx=np.nonzero(np.abs(x)>10**(-50/20))[0]          # end the clip 0.10 s after it falls under -50 dBFS (tail rule: <= 0.3 s)
    x=x[:min(len(x),idx[-1]+int(.10*SR))].copy();A.fade_out(x,.10)
    A.write_wav(name,x)
    print(f'{name}: {len(x)/SR:.2f}s peak {A.peak_db(x):.1f} dBFS {A.lufs(x):.1f} LUFS')
def stone(i):
    r=R(7100+i);n=int(.80*SR);buf=np.zeros(n)
    # careful walk: the sole slides a touch before the heel lands; every clip shifts the balance
    pre=[.000,.012,.000,.020][i]
    lift=[.045,.035,.055,.040][i]
    add(buf,scuff(r,.07,900+80*i,3000,30,.02,.18*[1,.8,1.1,.9][i]),pre)
    add(buf,heel(r,.10,[90,110,80,100][i],[480,560,430,520][i],[46,52,42,50][i],1.0),pre+lift)
    add(buf,scuff(r,.17,[1300,1100,1500,1250][i],[4300,3800,4600,4000][i],[22,26,20,24][i],.016,.50),pre+lift+.012)
    add(buf,cloth(r,.32,[260,300,240,280][i],[2300,2000,2500,2200][i],[9,11,8,10][i],[.42,.36,.46,.38][i]),pre+lift+.02)
    add(buf,mix(heel(r,.07,160,620,60,.30),scuff(r,.10,1500,3600,34,.01,.16)),pre+lift+[.15,.12,.17,.14][i])   # toe settles
    buf=hall(buf,7200+i,[.18,.15,.20,.16][i])
    return buf[:n]
def gravel(i):
    r=R(7300+i);n=int(.80*SR);buf=np.zeros(n)
    add(buf,heel(r,.09,80,600,50,.7),.045)
    g=int(.20*SR);grit=np.zeros(g)
    for _ in range(46+6*i):                          # grains: tiny filtered noise ticks, dense then sparse
        at=int(abs(r.normal(0,.05))*SR)
        if at>=g-40:continue
        k=int(r.uniform(.002,.006)*SR);tick=bp(noise(r,k),1800,7000,2)*np.exp(-np.arange(k)/(k*.3))
        grit[at:at+k]+=r.uniform(.2,1)*tick[:max(0,g-at)][:k]
    add(buf,grit*.55,.05)
    add(buf,cloth(r,.26,300,2000,10,.30),.07)
    add(buf,mix(heel(r,.06,150,700,60,.25),bp(noise(r,int(.08*SR)),1500,5000,2)*env(int(.08*SR),.008,40)*.12),.20+.02*i)
    buf=hall(buf,7400+i,.10)
    return buf[:n]
SETS={'stone':stone,'gravel':gravel}
def main():
    args=sys.argv[1:] or list(SETS)
    for s in args:
        if s not in SETS:raise SystemExit('unknown set '+s+' (stone, gravel)')
        for i in range(4):finish_step(f'step_{s}_{i}',SETS[s](i))
if __name__=='__main__':main()
