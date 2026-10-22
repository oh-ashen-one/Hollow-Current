#!/usr/bin/env python3
"""Check native route results and decoded vocal metadata against installed WAVs.

Listener samples prove mixed output, not a listening verdict or audibility of
every observed source (looping sources may be playing at zero mixer volume).
"""
import json,re
from pathlib import Path
import soundfile as sf
ROOT=Path(__file__).resolve().parents[1]
E=ROOT/'evidence';RES=ROOT/'Assets/HollowCurrent/Resources'

def main():
    runs=json.loads((E/'fish-native-suite.json').read_text());results=[]
    assert len(runs)==4,'Four terminal run results required'
    for run in runs:
        name=run['run'];text=(ROOT/run['log']).read_text(errors='replace')
        folder=ROOT/run.get('evidence_folder','evidence/fish-'+name)
        output=json.loads((folder/'audio/audio-output.json').read_text())
        clips=[]
        for line in text.splitlines():
            m=re.search(r'HC_AUDIO_PLAY clip=(\S+) samples=(\d+) hz=(\d+) channels=(\d+)',line)
            if not m:continue
            clip,samples,hz,ch=m.groups();folders=['Audio','HiggsfieldAudio']+([] if name=='bellwether2' else ['CastleAudio'])
            files=[RES/f/(clip+'.wav') for f in folders if (RES/f/(clip+'.wav')).exists()]
            assert files,'Missing source for played clip '+clip
            info=sf.info(files[-1]);match=info.frames==int(samples) and info.samplerate==int(hz) and info.channels==int(ch)
            assert match,'Built audio metadata differs from current resource: '+clip
            clips.append(clip)
        assert run['exit_code']==0 and run['route_pass'],name+' route failed'
        assert output['nonzero']>5000 and output['clippedSamples']==0,name+' output failed'
        assert clips,name+' no vocal observations'
        result={'run':name,'route_pass':True,'decoded_voice_metadata_matches':True,'observed_vocal_clips':sorted(set(clips)),'listener_output':output,'scope':'scripted native playback; no human listening acceptance'}
        if name=='bellwether2':
            native=json.loads((folder/'native-report.json').read_text());result['native_assertions_pass']=native['passed'];result['performance_pass']=native['performance_pass'];result['performance_reason']=native['performance_reason']
            assert native['passed'] and native['breathing_frames']>50 and native['speech_takes']>0
        else:
            result['castle_voice_summary']=[l for l in text.splitlines() if l.startswith('HC_VOICE_SUMMARY')]
            assert all('from_other=0' in l for l in result['castle_voice_summary'])
        results.append(result)
    out={'passed':True,'runs':results,'notes':['Source isPlaying observations include muted loops; Bellwether native route separately checks breathing and ducking activity.','GPU capture slots are shared, not exclusive performance measurements.','Dialogue taste and in-world listening remain the owner acceptance step.']}
    (E/'fish-native-audio-validation.json').write_text(json.dumps(out,indent=2)+'\n')
    print(json.dumps(out,indent=2))

if __name__=='__main__':main()
