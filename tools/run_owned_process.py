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
    start=time.time();child=subprocess.Popen(cmd,start_new_session=True);stop=[False]
    def handle(sig,frame):stop[0]=True
    signal.signal(signal.SIGTERM,handle);signal.signal(signal.SIGINT,handle)
    result={'pid':child.pid,'command':cmd,'started_utc':time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime(start))}
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
