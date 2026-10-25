"""Check the shipped breath revision for voiced pitch, loops and prompt regressions."""
import json
import numpy as np
import soundfile as sf
from scipy.signal import correlate
from fish_audio_integrate import ROOT,OUT,RES

ids={'player_breath','house_breath','count_breath','hide_breath_0','hide_breath_1','hide_exhale'}
rows=[]
for item in json.loads((OUT/'plan.json').read_text()):
    if item['id'] not in ids:continue
    assert item.get('unvoiced_breath'),item['id']+' must use the unvoiced breath processor'
    assert not any(word in item['prompt'].lower() for word in ('groan','moan','sigh')),item['id']+' regressed prompt'
    x,sr=sf.read(RES/item['target']);scores=[]
    for start in range(0,len(x)-sr//10,sr//20):
        frame=x[start:start+sr//10]
        c=correlate(frame,frame,mode='full',method='fft')[len(frame)-1:]
        if c[0]>1e-5:scores.append(float(max(c[int(.0025*sr):int(.0125*sr)])/c[0]))
    assert scores,item['id']+' has no audible breath frames'
    periodic=max(scores)
    assert periodic<.30,(item['id'],periodic)
    assert sr==48000 and max(abs(x))<.71
    if item.get('loop'):assert x[-1]==x[0]
    rows.append({'id':item['id'],'seconds':len(x)/sr,'peak':float(max(abs(x))),'voiced_pitch_correlation_max':periodic,'loop':item.get('loop',False)})
assert {r['id'] for r in rows}==ids
result={'passed':True,'clips':rows,'note':'Checks rejection of periodic vocal tone, not subjective listening acceptance.'}
(ROOT/'evidence/revised-breath-check.json').write_text(json.dumps(result,indent=2)+'\n')
print('PASS six unvoiced breath clips; no moan/groan/sigh directions, pitched frames, clipping or loop seam')
