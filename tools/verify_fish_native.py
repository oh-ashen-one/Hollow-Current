#!/usr/bin/env python3
"""Run the two-map audio verification suite sequentially through live GPU slots.

Each run releases its slot before queuing the next. Never auto-retries a failed
run, alters a foreign reservation, or changes player settings intentionally.
"""
import argparse,json,subprocess,sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
SLOT=Path.home()/'.cache/gpu-slot/bin/gpu_slot.sh'
APP=ROOT/'Builds/macOS/Hollow Current.app/Contents/MacOS/Hollow Current'
E=ROOT/'evidence'

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--suffix',default='');args=parser.parse_args()
    results=[]
    runs=[('bellwether2',900,['-hc-verify']),('castle-escape',600,['escape']),('castle-fear',900,['fear']),('castle-kill',600,['kill'])]
    for name,timeout,flags in runs:
        label=name+args.suffix
        for state in [Path.home()/'.cache/gpu-slot',Path.home()/'sm2-n1/_scratch/gpu']:
            for p in state.iterdir():
                if p.is_file() and any(t in p.name.upper() for t in ('PAUSED','FAULT','STOP')):
                    raise RuntimeError('Shared safety marker present: '+str(p))
        out=E/('fish-'+label);log=E/('fish-'+label+'-player.log')
        if out.exists():raise RuntimeError('Evidence already exists; inspect before rerunning '+name)
        opts=flags if name=='bellwether2' else ['-hc-playtest',str(out),*flags]
        if name=='bellwether2':opts+=['-hc-evidence',str(out)]
        # A per-process network denial verifies that the shipped voices and
        # gameplay need no provider connection. It changes no system settings.
        launch=['/usr/bin/sandbox-exec','-p','(version 1) (allow default) (deny network*)',str(APP)]
        cmd=[str(SLOT),'capture','--label','hc-fish-'+label,'--timeout','1800','--json',str(E/('fish-'+label+'-slot.json')),'--',sys.executable,str(ROOT/'tools/run_owned_process.py'),'--timeout',str(timeout),'--receipt',str(E/('fish-'+label+'-process.json')),'--',*launch,'-screen-width','1920','-screen-height','1080','-screen-fullscreen','0',*opts,'-hc-audio-proof',str(out/'audio'),'-logFile',str(log)]
        print('QUEUED',name,flush=True)
        rc=subprocess.call(cmd,cwd=ROOT)
        text=log.read_text(errors='replace') if log.exists() else ''
        if name=='bellwether2':passed='HC_NATIVE_ROUTE_PASS' in text
        else:passed=any('HC_PLAY_RESULT mode='+flags[0] in l and 'blockers=0' in l and 'won=True' in l for l in text.splitlines())
        result={'run':name,'evidence_folder':str(out.relative_to(ROOT)),'network_denied':True,'exit_code':rc,'route_pass':passed,'log':str(log.relative_to(ROOT))}
        results.append(result);(E/'fish-native-suite.json').write_text(json.dumps(results,indent=2)+'\n')
        print('RESULT',json.dumps(result),flush=True)
        if rc or not passed:raise SystemExit(2)

if __name__=='__main__':main()
