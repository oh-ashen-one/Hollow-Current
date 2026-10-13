#!/usr/bin/env python3
"""Post-process a raw Higgsfield Seed Audio download into a Resources/CastleAudio clip.
usage: audio_castle_post.py <raw.wav> <name> [--loop] [--lufs -30] [--xfade 0.3] [--peak dB] [--keep-tail]
Steps: mono downmix, 48 kHz, trim leading/trailing silence (< -50 dB), optional seamless-loop
crossfade, gain to integrated LUFS target (matches Resources/Audio, about -30 LUFS), -1.5 dBFS ceiling.
Requires ffmpeg. Deterministic. Prints duration, LUFS and peak of the result."""
import argparse,re,subprocess,tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/HollowCurrent/Resources/CastleAudio'
def run(*a):return subprocess.run(a,check=True,capture_output=True,text=True)
def dur(p):return float(run('ffprobe','-v','error','-show_entries','format=duration','-of','csv=p=0',str(p)).stdout)
def lufs(p):
    r=subprocess.run(['ffmpeg','-nostats','-i',str(p),'-af','ebur128=peak=true','-f','null','-'],capture_output=True,text=True).stderr
    s=r[r.rfind('Summary'):]
    return float(re.search(r'I:\s+(-?[\d.]+) LUFS',s).group(1)),float(re.search(r'Peak:\s+(-?[\d.inf]+) dBFS',s).group(1))
def main():
    ap=argparse.ArgumentParser();ap.add_argument('raw');ap.add_argument('name');ap.add_argument('--loop',action='store_true')
    ap.add_argument('--lufs',type=float,default=-30.0);ap.add_argument('--xfade',type=float,default=0.3)
    ap.add_argument('--peak',type=float,default=None,help='normalise to this peak dBFS instead of LUFS (clips under 0.4 s)')
    ap.add_argument('--keep-tail',action='store_true',help='do not trim trailing silence (reverb tails)')
    a=ap.parse_args();OUT.mkdir(parents=True,exist_ok=True)
    with tempfile.TemporaryDirectory() as t:
        t=Path(t);trim=t/'trim.wav'
        edge='silenceremove=start_periods=1:start_threshold=-50dB:start_silence=0.02'
        if not a.keep_tail:edge+=',areverse,silenceremove=start_periods=1:start_threshold=-50dB:start_silence=0.05,areverse'
        run('ffmpeg','-v','error','-y','-i',a.raw,'-ac','1','-ar','48000','-af',edge,str(trim))
        src=trim
        if a.loop:
            L=dur(trim);x=a.xfade;loop=t/'loop.wav'
            fc=(f'[0]atrim=0:{x},asetpts=PTS-STARTPTS[h];[0]atrim={L-x},asetpts=PTS-STARTPTS[tl];'
                f'[0]atrim={x}:{L-x},asetpts=PTS-STARTPTS[m];[tl][h]acrossfade=d={x}:c1=tri:c2=tri[x];[x][m]concat=n=2:v=0:a=1')
            run('ffmpeg','-v','error','-y','-i',str(trim),'-filter_complex',fc,'-ar','48000',str(loop));src=loop
        i,pk=lufs(src)
        gain=(a.peak-pk) if a.peak is not None else a.lufs-i
        run('ffmpeg','-v','error','-y','-i',str(src),'-af',f'volume={gain}dB,alimiter=limit=0.84:level=false','-c:a','pcm_s16le','-ar','48000','-ac','1',str(OUT/(a.name+'.wav')))
    o=OUT/(a.name+'.wav');i,p=lufs(o)
    print(f'{o.name}: {dur(o):.2f}s {i:.1f} LUFS peak {p} dBFS {o.stat().st_size} bytes')
if __name__=='__main__':main()
