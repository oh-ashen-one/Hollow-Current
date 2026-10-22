#!/usr/bin/env python3
"""Bound a task-owned command; terminate its group and wait 60s before force kill.

Run inside the shared GPU-slot wrapper. This is not a slot or safety bypass.
"""
import argparse,json,os,signal,subprocess,time
from pathlib import Path

def main():
    p=argparse.ArgumentParser();p.add_argument('--timeout',type=float,required=True);p.add_argument('--receipt',type=Path,required=True);p.add_argument('command',nargs=argparse.REMAINDER);a=p.parse_args()
    cmd=a.command[1:] if a.command and a.command[0]=='--' else a.command
    if not cmd:p.error('command required')
    renderer_names={'unity','unrealeditor','godot','robloxstudio','blender','hollow current'}
    renders=any(Path(x).name.lower() in renderer_names for x in cmd) and '-nographics' not in cmd and '--headless' not in cmd
    live=[]
    if renders:
        # Check after the queued slot is granted, not just before a long wait.
        for row in subprocess.check_output(['ps','-axo','pid=,stat=,comm='],text=True).splitlines():
            parts=row.strip().split(None,2)
            if len(parts)!=3 or Path(parts[2]).name.lower() not in renderer_names:continue
            pid,state,exe=parts
            argv=subprocess.run(['ps','-p',pid,'-o','args='],text=True,capture_output=True).stdout
            if '-nographics' in argv or '--headless' in argv:continue
            live.append({'pid':int(pid),'state':state,'executable':exe})
        if len(live)>=2 or any('E' in x['state'] or 'Z' in x['state'] or Path(x['executable']).name.lower()=='hollow current' for x in live):
            a.receipt.parent.mkdir(parents=True,exist_ok=True)
            a.receipt.write_text(json.dumps({'refused':'renderer cap, exiting engine, or existing Hollow Current instance','existing_renderers':live},indent=2)+'\n')
            print('Renderer preflight refused; no child launched.',flush=True);raise SystemExit(75)
    start=time.time();child=subprocess.Popen(cmd,start_new_session=True);stop=[False]
    def handle(sig,frame):stop[0]=True
    signal.signal(signal.SIGTERM,handle);signal.signal(signal.SIGINT,handle)
    result={'pid':child.pid,'command':cmd,'existing_renderers':live,'started_utc':time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime(start))}
    a.receipt.parent.mkdir(parents=True,exist_ok=True);a.receipt.write_text(json.dumps(result,indent=2)+'\n')
    while child.poll() is None and not stop[0] and time.time()-start<a.timeout:time.sleep(.5)
    if child.poll() is None:
        result['terminated']=True;os.killpg(child.pid,signal.SIGTERM)
        try:child.wait(timeout=60)
        except subprocess.TimeoutExpired:
            result['force_killed_after_60s']=True;os.killpg(child.pid,signal.SIGKILL);child.wait()
    result.update(exit_code=child.returncode,elapsed_seconds=round(time.time()-start,2))
    a.receipt.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result),flush=True)
    raise SystemExit(124 if result.get('terminated') else child.returncode)

if __name__=='__main__':main()
