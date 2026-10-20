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
# Bellwether (owner, 2026-10-19: "remove the little beeps you make when walking"): the shipped step_wood_* / step_tile_* carry the same tonal
# ding (check_audio_quality.py WARN), so the house gets noise-only sets too, written over Resources/Audio/step_{wood,tile,carpet}_* (their .meta
# files and guids stay). Domestic room, no hall tail; louder than the castle's careful walk (Bellwether plays steps at 0.22).
HOUSE=A.ROOT/'Assets/HollowCurrent/Resources/Audio';HOUSE_PEAK=-9.0
def wood(i):
    r=R(7500+i);n=int(.70*SR);buf=np.zeros(n);lift=[.03,.04,.035,.045][i]
    add(buf,heel(r,.12,[60,70,55,65][i],[300,340,280,320][i],[30,34,28,32][i],1.0),lift)          # hollow board thud, low band only
    add(buf,scuff(r,.10,[700,800,650,750][i],[2600,2400,2800,2500][i],[30,34,28,32][i],.012,.22),lift+.008)
    add(buf,cloth(r,.26,280,1900,10,.25),lift+.02)
    add(buf,mix(heel(r,.06,90,380,50,.30),scuff(r,.06,900,2400,40,.008,.10)),lift+[.13,.11,.15,.12][i])
    return A.room(buf,.35,.10,7600+i,lp_hz=3500)[:n]
def tile(i):
    r=R(7700+i);n=int(.70*SR);buf=np.zeros(n);lift=[.03,.035,.04,.03][i]
    add(buf,heel(r,.06,[120,140,110,130][i],[900,1000,850,950][i],[70,76,66,72][i],.8),lift)        # short hard heel, broad noise band
    add(buf,scuff(r,.08,[1800,1600,2000,1700][i],[6000,5600,6400,5800][i],[45,50,42,48][i],.006,.35),lift+.004)
    add(buf,cloth(r,.22,300,2200,11,.22),lift+.02)
    add(buf,mix(heel(r,.04,200,900,80,.25),scuff(r,.05,2000,5200,55,.005,.14)),lift+[.12,.10,.14,.11][i])
    return A.room(buf,.45,.12,7800+i,lp_hz=5000)[:n]
def carpet(i):
    r=R(7900+i);n=int(.60*SR);buf=np.zeros(n);lift=[.03,.04,.035,.03][i]
    add(buf,heel(r,.10,50,220,34,.8),lift)
    add(buf,cloth(r,.24,300,1600,12,.45),lift+.01)
    add(buf,scuff(r,.12,500,1800,26,.03,.18),lift+.02)
    return buf
def finish_house(name,x):
    x=np.asarray(x,dtype=np.float64);x=hp(x-np.mean(x),45,2);x*=10**((HOUSE_PEAK-A.peak_db(x))/20)
    idx=np.nonzero(np.abs(x)>10**(-50/20))[0];x=x[:min(len(x),idx[-1]+int(.08*SR))].copy();A.fade_out(x,.08)
    pcm=np.clip(np.round(x*32767),-32768,32767).astype('<i2')
    import wave
    with wave.open(str(HOUSE/(name+'.wav')),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(SR);w.writeframes(pcm.tobytes())
    print(f'{name}: {len(x)/SR:.2f}s peak {A.peak_db(x):.1f} dBFS (Bellwether, Resources/Audio)')
SETS={'stone':stone,'gravel':gravel}
HOUSE_SETS={'wood':wood,'tile':tile,'carpet':carpet}
def main():
    args=sys.argv[1:] or list(SETS)
    for s in args:
        if s in HOUSE_SETS:
            for i in range(4):finish_house(f'step_{s}_{i}',HOUSE_SETS[s](i))
            continue
        if s not in SETS:raise SystemExit('unknown set '+s+' (stone, gravel, wood, tile, carpet)')
        for i in range(4):finish_step(f'step_{s}_{i}',SETS[s](i))
if __name__=='__main__':main()
