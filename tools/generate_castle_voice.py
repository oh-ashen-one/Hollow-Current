#!/usr/bin/env python3
"""Render the Count's lines (every cc_* entry in tools/voice-lines.json) with Kokoro-82M, then a dark stone-hall FX chain.

Replaces the eSpeak NG castle voice (owner feedback r13 item 11: "really bad ... something much scarier and much more
human-sounding"). Bellwether's eSpeak lines are untouched (tools/generate_open_voice.py still owns them).

usage (needs the Kokoro venv, Python 3.12, CPU only; see docs/haunted-castle/handoffs/fb-audio.md):
  python tools/generate_castle_voice.py                       render every cc_* line into Resources/CastleAudio
  python tools/generate_castle_voice.py cc_listen cc_stay     render only those
  python tools/generate_castle_voice.py --audition [cc_id]    render one line (default cc_hunt_hungry) with each candidate voice into
                                                              docs/haunted-castle/audition/voice_<voice>_<id>.wav; writes nothing else
  options: --voice bm_george  --speed 0.84  --target-lufs -24.2  --device cpu

Model: hexgrad/Kokoro-82M (Apache-2.0; output redistributable). Phonemes come from misaki + espeak-ng (GPL-3 tool, used as a
program only; no eSpeak audio is in the result) and spaCy en_core_web_sm (MIT) for text analysis. No Apple voices, no paid TTS.

Chain (all deterministic given the model weights): Kokoro 24 kHz -> sentence pauses -> 48 kHz -> slow/pitch drop 5 % (resample, so
the voice also gets 5 % slower) -> optional breath/rasp layer (voice-gated band noise, -27 dB) -> HPF 70 Hz, low shelf +4 dB at
220 Hz, presence dip, LPF 9 kHz -> gentle compression (ratio 2.2) -> stone hall: early reflections plus a 2.2 s noise-IR tail
(pre-delay 28 ms, dark) mixed at about -11 dB -> trim lead/tail (tail ends where the clip falls under -50 dBFS, fade 120 ms, at
most 0.15 s of silence kept) -> loudness to the Bellwether voice median so check_audio_quality.py stays green (-24.2 LUFS)
-> peak cap -3 dBFS. 48 kHz mono PCM16.
"""
import json,re,subprocess,sys,tempfile,uuid,wave
from pathlib import Path
import numpy as np
from scipy.signal import butter,sosfilt,fftconvolve,lfilter,resample_poly
sys.path.insert(0,str(Path(__file__).resolve().parent))
import audio_castle_sfx as A
ROOT=Path(__file__).resolve().parents[1]
CASTLE_OUT=ROOT/'Assets/HollowCurrent/Resources/CastleAudio'
AUDITION=ROOT/'docs/haunted-castle/audition'
SR=A.SR;KSR=24000
DEFAULT_VOICE='bm_george';DEFAULT_SPEED=0.84;DEFAULT_LUFS=-24.2
CANDIDATES=[('bm_george',.84),('bm_lewis',.84),('am_michael',.84),('am_adam',.84),('bm_fable',.84)]
PITCH_DROP=0.95           # 5 % (brief: at most 6 %)
PEAK_CAP=-3.0
def lang_for(voice):return 'b' if voice.startswith('b') else 'a'
_PIPES={}
def pipe(voice,device):
    from kokoro import KPipeline
    k=lang_for(voice)
    if k not in _PIPES:_PIPES[k]=KPipeline(lang_code=k,repo_id='hexgrad/Kokoro-82M',device=device)
    return _PIPES[k]
def sentences(text):
    return [s.strip() for s in re.split(r'(?<=[.!?])\s+',text.strip()) if s.strip()]
def kokoro(text,voice,speed,device):
    """24 kHz float dry voice, a short human-length beat between sentences (longer after a full stop than a comma)."""
    p=pipe(voice,device);parts=[];rng=np.random.default_rng(5)
    for s in sentences(text):
        segs=[a.numpy() for _,_,a in p(s,voice=voice,speed=speed)]
        parts.append(np.concatenate(segs))
        parts.append(np.zeros(int(KSR*(0.32+0.12*rng.random()))))
    return np.concatenate(parts[:-1])
def f0_median(x,sr=KSR):
    """crude autocorrelation pitch over voiced frames (for the audition notes only)"""
    n=int(.04*sr);vals=[]
    for i in range(0,len(x)-2*n,n):
        f=x[i:i+2*n]
        if np.sqrt(np.mean(f**2))<.03:continue
        f=f-f.mean();ac=np.correlate(f,f,'full')[len(f)-1:];lo,hi=int(sr/300),int(sr/60)
        k=lo+int(np.argmax(ac[lo:hi]))
        if ac[k]>.35*ac[0]:vals.append(sr/k)
    return float(np.median(vals)) if vals else 0.0
def bp(x,lo,hi,o=2):return sosfilt(butter(o,[lo,hi],'band',fs=SR,output='sos'),x)
def ffmpeg_filter(x,af):
    with tempfile.TemporaryDirectory(prefix='hc-voice-') as d:
        a=Path(d)/'in.wav';b=Path(d)/'out.wav'
        A_wav(a,x);subprocess.run(['ffmpeg','-v','error','-y','-i',str(a),'-af',af,'-ar',str(SR),'-ac','1','-c:a','pcm_s16le',str(b)],check=True)
        import soundfile as sf
        y,_=sf.read(str(b));return y
def A_wav(path,x):
    pcm=np.clip(np.round(np.asarray(x)*32767),-32768,32767).astype('<i2')
    with wave.open(str(path),'wb') as w:
        w.setnchannels(1);w.setsampwidth(2);w.setframerate(SR);w.writeframes(pcm.tobytes())
def hall(x):
    """Stone great hall: five early reflections then a long dark tail (noise IR, exponential decay)."""
    r=np.random.default_rng(31);n=int(2.2*SR*1.05);t=np.arange(n)/SR
    ir=sosfilt(butter(2,2600,'low',fs=SR,output='sos'),r.standard_normal(n))*np.exp(-6.908*t/2.2)
    ir*=1-np.exp(-t*60)                                   # slow onset: no smearing of consonants
    ir=np.concatenate([np.zeros(int(.028*SR)),ir]);ir/=np.sqrt(np.sum(ir**2))
    early=np.zeros(int(.12*SR))
    for d,a in ((.011,.50),(.023,.40),(.037,.32),(.053,.26),(.079,.20)):early[int(d*SR)]=a
    wet=fftconvolve(x,ir);out=np.zeros(len(wet));out[:len(x)]+=x
    e=fftconvolve(x,early);out[:len(e)]+=e*0.55
    out+=wet*10**(-11.0/20)                               # tail level relative to dry (about -11 dB)
    return out
def process(dry24,target_lufs):
    x=resample_poly(dry24,SR//KSR,1)                       # 24 -> 48 kHz
    x=resample_poly(x,int(round(PITCH_DROP*1000)),1000)    # play slower by 5 %: pitch and tempo down together
    # breath / rasp: band noise gated by the voice envelope so it only lives under speech
    env=lfilter([1-.9995],[1,-.9995],np.abs(x));env=env/(env.max()+1e-9)
    rng=np.random.default_rng(13)
    breath=bp(rng.standard_normal(len(x)),1800,6500)*np.sqrt(np.clip(env,0,1))
    breath*=np.sqrt(np.mean(x**2))/(np.sqrt(np.mean(breath**2))+1e-9)*10**(-27/20)
    x=x+breath
    x=x/(np.abs(x).max()+1e-9)*.85
    x=ffmpeg_filter(x,'highpass=f=70,lowshelf=g=4:f=220:t=q:w=0.7,equalizer=f=2800:t=q:w=1.0:g=-2,lowpass=f=9000,'
                      'acompressor=threshold=0.09:ratio=2.2:attack=12:release=160:makeup=2')
    x=hall(np.concatenate([np.zeros(int(.05*SR)),x]))
    # trim: lead to 50 ms, tail where the clip falls below -50 dBFS (keep 0.15 s), 120 ms fade
    x=x/(np.abs(x).max()+1e-9)
    def trim(y):
        idx=np.nonzero(np.abs(y)>10**(-50/20))[0]
        end=min(len(y),idx[-1]+int(.12*SR)) if len(idx) else len(y)
        y=y[:end].copy();A.fade_out(y,.12)
        first=np.nonzero(np.abs(y)>10**(-50/20))[0]
        return y[max(0,first[0]-int(.04*SR)):] if len(first) else y
    y=x*10**((target_lufs-A.lufs(x))/20);y=trim(y)           # first pass fixes the -50 dBFS tail point
    y=y*10**((target_lufs-A.lufs(y))/20)                      # exact loudness on the trimmed clip
    pk=A.peak_db(y)
    if pk>PEAK_CAP:y*=10**((PEAK_CAP-pk)/20)
    return y
def write(path,y,meta=True):
    A_wav(path,y)
    if meta:
        m=Path(str(path)+'.meta')
        if not m.exists():
            ref=CASTLE_OUT/'voice_cc_bargain.wav.meta';body=ref.read_text().split('\n');body[1]='guid: '+uuid.uuid4().hex;m.write_text('\n'.join(body))
def main():
    args=sys.argv[1:];opts={};ids=[];i=0
    while i<len(args):
        a=args[i]
        if a in('--voice','--speed','--target-lufs','--device'):opts[a]=args[i+1];i+=2
        elif a=='--audition':opts[a]=True;i+=1
        else:ids.append(a);i+=1
    device=opts.get('--device','cpu')
    lines=json.loads((ROOT/'tools/voice-lines.json').read_text())
    cc={k:v for k,v in lines.items() if k.startswith('cc_')}
    lufs=float(opts.get('--target-lufs',DEFAULT_LUFS))
    if '--audition' in opts:
        lid=ids[0] if ids else 'cc_hunt_hungry';AUDITION.mkdir(parents=True,exist_ok=True)
        for v,sp in CANDIDATES:
            dry=kokoro(cc[lid]['text'],v,float(opts.get('--speed',sp)),device)
            y=process(dry,lufs);write(AUDITION/f'voice_{v}_{lid}.wav',y,meta=False)
            print(f'{v}: dry f0 {f0_median(dry):.0f} Hz, {len(y)/SR:.2f} s, {A.lufs(y):.1f} LUFS, peak {A.peak_db(y):.1f} dBFS')
        return
    voice=opts.get('--voice',DEFAULT_VOICE);speed=float(opts.get('--speed',DEFAULT_SPEED))
    unknown=[i for i in ids if i not in cc]
    if unknown:raise SystemExit('unknown entries '+' '.join(unknown))
    for lid in (ids or cc):
        dry=kokoro(cc[lid]['text'],voice,speed,device);y=process(dry,lufs)
        write(CASTLE_OUT/('voice_'+lid+'.wav'),y)
        print(f'voice_{lid}.wav: {len(y)/SR:.2f} s, {A.lufs(y):.1f} LUFS, peak {A.peak_db(y):.1f} dBFS')
if __name__=='__main__':main()
