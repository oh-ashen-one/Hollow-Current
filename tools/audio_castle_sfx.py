#!/usr/bin/env python3
"""Procedural Haunted Castle fear-wave SFX -> Resources/CastleAudio (free, deterministic, no paid generation).

usage: audio_castle_sfx.py [name ...]      (no names = everything)
       audio_castle_sfx.py --check         (re-measure every file this script owns and print the table)

Clips: heartbeat_loop, wind_battlements_loop, door_rattle, book_drop, knife_stick, claw_steps_0..3,
bat_pass, bell_single (derived from chapel_bell_toll.wav). Voices voice_cc_kit / voice_cc_ending come from
tools/generate_open_voice.py --castle (entries cc_kit, cc_ending in tools/voice-lines.json).

Method: numpy/scipy only. Damped modal synthesis (wood, stone, steel), filtered noise, filtered impulses and
synthetic-reverb convolution. Loops are built with circular (FFT) filtering and integer-cycle modulators so the
seam is continuous by construction (no crossfade needed; the check prints the seam step vs the typical step).
Finishing: DC removed, gain to -30 LUFS integrated (BS.1770 K-weighting, implemented here and cross-checked
against ffmpeg ebur128), capped so the peak never exceeds -8 dBFS; clips shorter than 0.4 s are measured with
silence padded to 1 s (same trick the existing short clips need). 48 kHz mono 16-bit PCM.
"""
import sys,wave,uuid
from pathlib import Path
import numpy as np
from scipy.signal import butter,sosfilt,fftconvolve,tf2sos

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/HollowCurrent/Resources/CastleAudio'
SR=48000
TARGET_LUFS=-30.0
PEAK_CEIL=-8.0

# ---------------------------------------------------------------- DSP helpers
def R(seed):return np.random.default_rng(seed)
def T(n):return np.arange(n)/SR
def bp(x,lo,hi,o=2):return sosfilt(butter(o,[lo,hi],'band',fs=SR,output='sos'),x)
def lp(x,f,o=2):return sosfilt(butter(o,f,'low',fs=SR,output='sos'),x)
def hp(x,f,o=2):return sosfilt(butter(o,f,'high',fs=SR,output='sos'),x)
def add(buf,ev,at):
    i=int(round(at*SR))
    if i<0:ev=ev[-i:];i=0
    if i>=len(buf):return
    e=ev[:max(0,len(buf)-i)];buf[i:i+len(e)]+=e
def fade_out(x,sec):
    n=min(len(x),int(sec*SR));x[-n:]*=0.5*(1+np.cos(np.linspace(0,np.pi,n)));return x
def fade_in(x,sec):
    n=min(len(x),int(sec*SR));x[:n]*=0.5*(1-np.cos(np.linspace(0,np.pi,n)));return x
def reverb_ir(rt60,seed,lp_hz=5000,pre=0.0,dur=None):
    """Synthetic room: exponentially decaying low-passed noise, sparse early reflections."""
    dur=dur or min(rt60*1.1,2.0);n=int(dur*SR);r=R(seed);t=T(n)
    ir=lp(r.standard_normal(n),lp_hz)*np.exp(-6.908*t/rt60)
    ir*=1-np.exp(-t*400)                       # soft onset so the direct sound is not smeared
    for d,a in ((0.007,.5),(0.013,.4),(0.021,.3),(0.034,.25)):
        k=int(d*SR)
        if k<n:ir[k]+=a*0.3*np.abs(ir).max()*10
    ir/=np.sqrt(np.sum(ir**2))+1e-12
    return np.concatenate([np.zeros(int(pre*SR)),ir])
def room(x,rt60,wet,seed,lp_hz=5000,tail=None):
    ir=reverb_ir(rt60,seed,lp_hz);y=fftconvolve(x,ir)
    out=np.zeros(len(y));out[:len(x)]+=x;out+=wet*y
    return out if tail is None else out[:int(tail*SR)]
def modal(t,modes):
    """modes: (freq,decay_per_s,amp[,phase])"""
    y=np.zeros_like(t)
    for m in modes:
        f,d,a=m[:3];p=m[3] if len(m)>3 else 0.0
        y+=a*np.sin(2*np.pi*f*t+p)*np.exp(-d*t)
    return y

# ---------------------------------------------------------------- loudness (BS.1770-4, 48 kHz)
_S1=tf2sos([1.53512485958697,-2.69169618940638,1.19839281085285],[1.0,-1.69065929318241,0.73248077421585])
_S2=tf2sos([1.0,-2.0,1.0],[1.0,-1.99004745483398,0.99007225036621])
def lufs(x):
    x=np.asarray(x,dtype=np.float64)
    if len(x)<SR:x=np.concatenate([x,np.zeros(SR-len(x))])
    y=sosfilt(_S2,sosfilt(_S1,x));blk=int(.4*SR);hop=int(.1*SR)
    ms=np.array([np.mean(y[i:i+blk]**2) for i in range(0,len(y)-blk+1,hop)])
    l=-0.691+10*np.log10(ms+1e-20);keep=ms[l>-70]
    if len(keep)==0:return -70.0
    thr=-0.691+10*np.log10(np.mean(keep))-10
    keep=ms[l>thr];return float(-0.691+10*np.log10(np.mean(keep)))
def peak_db(x):return float(20*np.log10(np.max(np.abs(x))+1e-12))

def write_wav(name,x):
    OUT.mkdir(parents=True,exist_ok=True)
    pcm=np.clip(np.round(x*32767),-32768,32767).astype('<i2')
    with wave.open(str(OUT/(name+'.wav')),'wb') as w:
        w.setnchannels(1);w.setsampwidth(2);w.setframerate(SR);w.writeframes(pcm.tobytes())
    ensure_meta(name)
def ensure_meta(name):
    meta=OUT/(name+'.wav.meta')
    if not meta.exists():   # same importer settings as every other CastleAudio clip, fresh guid
        ref=OUT/'wing_beats_loop.wav.meta'
        body=ref.read_text().split('\n')
        body[1]='guid: '+uuid.uuid4().hex
        meta.write_text('\n'.join(body))
def finish(name,x):
    x=np.asarray(x,dtype=np.float64);x=x-np.mean(x)
    g_l=TARGET_LUFS-lufs(x);g_p=PEAK_CEIL-peak_db(x)
    g=min(g_l,g_p);y=x*10**(g/20)
    write_wav(name,y)
    print(f'{name}: {len(y)/SR:.2f}s gain {g:+.1f} dB ({"peak-limited" if g_p<g_l else "loudness"}) -> {lufs(y):.1f} LUFS peak {peak_db(y):.1f} dBFS')

# ---------------------------------------------------------------- loops
def heartbeat():
    bpm=52;per=60/bpm;beats=6;n=int(round(per*beats*SR));pad=int(1.0*SR)
    r=R(11);buf=np.zeros(n+pad)
    def thump(f0,amp,decay,dur=.45):
        k=int(dur*SR);t=T(k)
        f=f0*(1+0.55*np.exp(-t*45));ph=2*np.pi*np.cumsum(f)/SR
        body=np.sin(ph)*np.exp(-t*decay)
        thud=bp(r.standard_normal(k),70,260)*np.exp(-t*38)*0.9          # chest slap
        low=lp(r.standard_normal(k),110,3)*np.exp(-t*20)*0.8           # wet low-passed body
        y=(body+thud+low)*amp;y*=1-np.exp(-t*500);return y
    for b in range(beats):
        t0=b*per+r.uniform(-.004,.004);a=r.uniform(.93,1.05)
        add(buf,thump(56,1.0*a,16),t0)
        add(buf,thump(68,.66*a,22,.38),t0+.285+r.uniform(-.006,.006))   # second thump softer, higher
    buf=lp(buf,420,2)
    buf=room(buf,.45,.18,12,2500,tail=(n+pad)/SR)
    y=buf[:n].copy();tail=buf[n:];y[:len(tail)]+=tail                    # wrap the tail onto the start: seamless loop
    return y
def wind():
    n=int(10.0*SR);r=R(23);dur=n/SR
    def shaped(fc,bw,seed):
        """periodic band-limited noise: gaussian mask in the FFT domain (circular, so it loops)"""
        X=np.fft.rfft(R(seed).standard_normal(n));f=np.fft.rfftfreq(n,1/SR)
        m=np.exp(-0.5*((f-fc)/bw)**2);y=np.fft.irfft(X*m,n);return y/(np.std(y)+1e-12)
    def lowshape(f_hi,seed,f_lo=30):
        X=np.fft.rfft(R(seed).standard_normal(n));f=np.fft.rfftfreq(n,1/SR)
        m=(1/(1+(f/f_hi)**4))*(1/(1+(f_lo/np.maximum(f,1e-3))**4));y=np.fft.irfft(X*m,n);return y/(np.std(y)+1e-12)
    t=np.arange(n)/SR
    def slow(seed,cycles=(1,2,3,5,7),depth=.55):
        rr=R(seed);s=sum(rr.uniform(.5,1)/c**.5*np.sin(2*np.pi*(c*t/dur)+rr.uniform(0,6.28)) for c in cycles)
        s/=np.std(s)+1e-9;return np.exp(depth*s)/np.exp(depth*depth/2)
    def gust(c,w,a):
        d=(t-c+dur/2)%dur-dur/2;return a*np.exp(-0.5*(d/w)**2)
    g=gust(2.7,.85,1.0)+gust(7.6,.55,.7)+gust(9.6,.3,.25)               # wrap-safe, periodic
    base=lowshape(260,24)*slow(1)*(.9+1.6*g)                              # body rumble
    whoosh=shaped(430,230,25)*slow(2,depth=.7)*(.35+1.4*g)
    hiss=shaped(3200,1400,26)*(.05+.45*g)*slow(3)
    howl=0
    for i,(fc,q,a) in enumerate(((286,16,1.0),(431,18,.8),(642,20,.55),(958,22,.35))):
        h=shaped(fc,fc/q,40+i)*slow(50+i,(1,2,3,4),.8)*a
        hi=shaped(fc*1.22,fc*1.22/q,60+i)*(.3*g)*a                          # gust pushes the tones up
        howl=howl+(h*(.6+.9*g)+hi*1.1)
    y=.45*base+.28*whoosh+.7*howl+hiss
    y=lp(np.concatenate([y,y,y]),6500,2)[n:2*n]                          # filter across the seam, keep the middle copy
    return y
# ---------------------------------------------------------------- one-shots
def wood_hit(r,f0=1.0,amp=1.0,body=1.0,dur=.16):
    k=int(dur*SR);t=T(k)
    base=[(310,95,1.0),(572,125,.8),(941,160,.6),(1533,210,.45),(2410,280,.25),(3350,360,.12)]
    modes=[(f*f0*r.uniform(.93,1.07),d*r.uniform(.85,1.2),a*r.uniform(.6,1.2)) for f,d,a in base]
    y=modal(t,modes)
    y+=bp(r.standard_normal(k),1400,7000)*np.exp(-t*850)*.9            # contact click
    y+=modal(t,[(130*f0,34,.9*body),(205*f0,52,.5*body)])               # carcass thump
    return y*amp
def door_rattle():
    n=int(2.5*SR);r=R(31);buf=np.zeros(n+SR//2)
    env=lambda s:np.clip(s/.22,0,1)**1.3*np.clip((2.45-s)/.75,0,1)**1.4  # swells in, tapers out
    s=0.04;k=0
    while s<2.45:
        e=env(s)*r.uniform(.55,1.0)
        add(buf,wood_hit(r,f0=r.uniform(.85,1.05),amp=.9*e,body=1.3),s)    # door slams into the frame
        m=r.integers(2,5);d=0.0
        for _ in range(m):                                                   # slats and latch chatter
            d+=r.uniform(.007,.028)
            add(buf,wood_hit(r,f0=r.uniform(1.15,1.7),amp=.5*e*r.uniform(.4,1),body=.3,dur=.1),s+d)
        # slat buzz while the door moves
        L=int(r.uniform(.07,.13)*SR);buzz=bp(r.standard_normal(L),900,3200)*np.hanning(L)*(r.standard_normal(L)>0.2)
        add(buf,buzz*.12*e,s+.01)
        s+=r.uniform(.13,.19)*(1+.25*(s/2.4))*(.8 if k%3==0 else 1.0);k+=1
    buf=room(buf,.38,.22,32,4500,tail=2.5)
    return fade_out(buf,.2)
def book_drop():
    n=int(.6*SR);r=R(41);buf=np.zeros(n+SR//2)
    def slam(amp):
        k=int(.35*SR);t=T(k)
        f=62*(1+.5*np.exp(-t*30));ph=2*np.pi*np.cumsum(f)/SR
        y=np.sin(ph)*np.exp(-t*26)*1.3                                    # cover hitting stone: body thud
        y+=bp(r.standard_normal(k),250,3800)*np.exp(-t*190)*1.1           # slap
        y+=lp(r.standard_normal(k),700)*np.exp(-t*60)*.8
        y+=modal(t,[(1090,55,.1),(1830,70,.07),(3070,90,.05)])*1.0         # stone ring, quiet
        return y*amp*(1-np.exp(-t*900))
    add(buf,slam(1.0),0.0);add(buf,slam(.22),.19)                          # settle bounce
    L=int(.09*SR);fl=bp(r.standard_normal(L),1800,7000)*np.hanning(L)*(0.5+0.5*np.sin(2*np.pi*70*T(L)))**2
    add(buf,fl*.22,.035)                                                    # pages
    buf=room(buf,.55,.3,42,3500,tail=.6)
    return fade_out(buf,.18)
def knife_stick():
    n=int(.5*SR);r=R(51);k=n;t=T(k);buf=np.zeros(n)
    pierce=hp(r.standard_normal(k),2600)*np.exp(-t*190)*np.clip(t/.004,0,1)  # steel entering
    buf+=pierce*.5
    th=modal(t,[(190,50,1.0),(360,70,.7),(620,95,.35)])+lp(r.standard_normal(k),1500)*np.exp(-t*140)*.9
    add(buf,th*1.2*(1-np.exp(-t*900)),.006)
    # the stuck blade: free end of a clamped beam, mode ratios 1 : 6.27 : 17.55, pitch rises a hair as the swing dies
    tw=np.zeros(k)
    for ratio,f1,d,a in ((1,330,5.5,.9),(6.27,330,13,.38),(17.55,330,28,.14)):
        f=f1*ratio*(1+.045*np.exp(-t*7));ph=2*np.pi*np.cumsum(f)/SR
        tw+=a*np.sin(ph)*np.exp(-d*t)*(1+.15*np.sin(2*np.pi*11*t))
    add(buf,tw*.55*(1-np.exp(-t*700)),.009)
    buf=room(buf,.2,.15,52,6000,tail=.5)
    return fade_out(buf,.14)
CLAWS=[  # toes, toe gap s, stone ring Hz, pad Hz, pad amp, scrape s, scrape lead(slip before contact), seed
    dict(toes=3,gap=.013,ring=1400,pad=120,pa=.55,scr=.045,lead=0.0,seed=61),
    dict(toes=4,gap=.008,ring=1950,pad=170,pa=.35,scr=.10,lead=0.0,seed=62),
    dict(toes=2,gap=.024,ring=1050,pad=88,pa=1.0,scr=.02,lead=0.0,seed=63),
    dict(toes=3,gap=.011,ring=2350,pad=140,pa=.45,scr=.07,lead=.06,seed=64)]
def claw_step(p):
    n=int(.3*SR);r=R(p['seed']);buf=np.zeros(n+SR//5)
    t0=p['lead']+.012
    if p['lead']>0:                                  # claw slips along the stone before it bites
        L=int(p['lead']*SR);sl=bp(r.standard_normal(L),2200,8500)*np.linspace(.1,1,L)**2;add(buf,sl*.5,.012)
    for i in range(p['toes']):
        s=t0+i*p['gap']+r.uniform(-.002,.002);k=int(.12*SR);t=T(k)
        click=hp(r.standard_normal(k),2800)*np.exp(-t*1700)*r.uniform(.75,1)
        ring=modal(t,[(p['ring']*r.uniform(.96,1.04),95,.35),(p['ring']*2.31,150,.2),(p['ring']*3.8,230,.1)])
        add(buf,(click+ring)*(1-.12*i),s)
    L=int(p['scr']*SR)
    if L>0:                                           # drag after the strike
        sc=bp(r.standard_normal(L),2500,9000)*np.hanning(L)*.28;add(buf,sc,t0+p['toes']*p['gap'])
    k=int(.2*SR);t=T(k)
    pad=(modal(t,[(p['pad'],55,1.0),(p['pad']*1.9,80,.4)])+lp(r.standard_normal(k),500)*np.exp(-t*90)*.6)*p['pa']*(1-np.exp(-t*600))
    add(buf,pad,t0+.004)
    buf=room(buf,.32,.18,p['seed']+10,5000,tail=.3)
    return fade_out(buf,.06)
def bat_pass():
    n=int(1.2*SR);r=R(71);buf=np.zeros(n);dark=np.zeros(n);bright=np.zeros(n)
    # flap times: ~13 Hz approaching, slowing as it recedes (doppler-ish)
    t=0.02;times=[]
    while t<1.15:
        times.append(t);f=14.5-3.8*(t/1.2)**1.3;t+=1/f*r.uniform(.92,1.08)
    for tf in times:
        L=int(r.uniform(.03,.045)*SR);u=np.linspace(0,1,L)
        shape=np.sin(np.pi*u**.6)**2                                       # fast snap, slow release
        w=r.standard_normal(L)
        snap=bp(w,650,3600)*shape
        crackle=np.sign(w)*np.abs(w)**3*0.15;crackle=bp(crackle,1500,6500)*shape  # leathery crinkle
        air=bp(r.standard_normal(L),120,420)*np.hanning(L)*.9
        add(bright,snap+crackle,tf);add(dark,air+lp(snap,1200)*.6,tf)
    c=.55;e=np.exp(-0.5*((T(n)-c)/.26)**2)
    near=np.clip(e*1.15,0,1)
    buf=dark*(.35+e)+bright*(e**1.5)*1.3
    # pitched squeaks: two short chirps (bat voice at the edge of hearing)
    for s0,f0,f1 in ((.30,5200,6400),(.78,6100,4700)):
        L=int(.05*SR);u=np.linspace(0,1,L);f=f0+(f1-f0)*u;ph=2*np.pi*np.cumsum(f)/SR
        add(buf,np.sin(ph)*np.hanning(L)*.05*e[int(s0*SR)],s0)
    buf=lp(buf,2200+5000*e.max(),2)
    buf=fade_in(buf,.03)
    buf=room(buf,.3,.12,72,4500,tail=1.2)
    return fade_out(buf,.1)
def bell_single():
    w=wave.open(str(OUT/'chapel_bell_toll.wav'));x=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').astype(np.float64)/32768
    h=int(.01*SR);env=np.array([np.sqrt(np.mean(x[i:i+h]**2)) for i in range(0,len(x)-h,h)])
    on=int(np.argmax(env>0.02))*h                                         # first real strike (lead-in swell is -45 dB)
    s=max(0,on-int(.012*SR));y=x[s:s+int(3.0*SR)].copy()
    y=fade_in(y,.004);y=fade_out(y,1.2)                                   # tail shortened to 3 s
    return y

CLIPS={'heartbeat_loop':heartbeat,'wind_battlements_loop':wind,'door_rattle':door_rattle,'book_drop':book_drop,
       'knife_stick':knife_stick,'bat_pass':bat_pass,'bell_single':bell_single}
for _i,_p in enumerate(CLAWS):CLIPS[f'claw_steps_{_i}']=(lambda p=_p:claw_step(p))
LOOPS={'heartbeat_loop','wind_battlements_loop'}
OWNED=list(CLIPS)+['voice_cc_kit','voice_cc_ending']

def read(name):
    w=wave.open(str(OUT/(name+'.wav')));fr=w.getframerate();ch=w.getnchannels()
    x=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').astype(np.float64)/32768
    return x,fr,ch
def check():
    print('| file | rate | ch | dur s | bytes | peak dBFS | LUFS | seam |');print('|---|---|---|---|---|---|---|---|')
    for name in OWNED:
        p=OUT/(name+'.wav')
        if not p.exists():print(f'| {name} | MISSING |');continue
        x,fr,ch=read(name);seam=''
        if name in LOOPS:
            d=np.abs(np.diff(x));seam=f'|x[0]-x[-1]| {abs(x[0]-x[-1])*32768:.0f} LSB vs p99 step {np.percentile(d,99)*32768:.0f} LSB'
        lu=lufs(x) if fr==48000 else lufs_any(x,fr)
        print(f'| {name} | {fr} | {ch} | {len(x)/fr:.2f} | {p.stat().st_size} | {peak_db(x):.1f} | {lu:.1f} | {seam} |')
def lufs_any(x,fr):
    from scipy.signal import resample_poly
    from math import gcd
    g=gcd(fr,SR);return lufs(resample_poly(x,SR//g,fr//g))
def main():
    args=sys.argv[1:]
    if '--check' in args:check();return
    names=args or list(CLIPS)
    for v in ('voice_cc_kit','voice_cc_ending'):
        if (OUT/(v+'.wav')).exists():ensure_meta(v)
    for nme in names:
        if nme not in CLIPS:raise SystemExit('unknown clip '+nme)
        finish(nme,CLIPS[nme]())
if __name__=='__main__':main()
