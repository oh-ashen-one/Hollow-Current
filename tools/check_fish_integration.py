#!/usr/bin/env python3
"""Offline provenance, coverage and content checks; never contacts a provider."""
import hashlib,json
from pathlib import Path
import soundfile as sf
from fish_audio_integrate import ROOT,OUT,RES,words

def main():
    lines=json.loads((ROOT/'tools/voice-lines.json').read_text())
    plan=json.loads((OUT/'plan.json').read_text())
    receipts=json.loads((OUT/'integration.json').read_text())
    by_id={x['id']:x for x in receipts};checks=0
    def require(ok,message):
        nonlocal checks
        checks+=1
        if not ok:raise AssertionError(message)
    require(len(by_id)==len(plan)==36,'Expected all 28 dialogue lines and 8 vocal-effect targets')
    require({'voice_'+k for k in lines}<={x['id'] for x in plan},'Scripted dialogue coverage')
    guids=set()
    for item in plan:
        receipt=by_id[item['id']];target=RES/item['target'];raw=OUT/'raw'/(item['id']+'.wav')
        request=json.loads((OUT/'raw'/(item['id']+'.request.json')).read_text())
        require(request['model']=='drama-3',item['id']+' model')
        require(hashlib.sha256(raw.read_bytes()).hexdigest()==receipt['raw_sha256'],item['id']+' raw master')
        require(hashlib.sha256(target.read_bytes()).hexdigest()==receipt['sha256'],item['id']+' installed hash')
        transcript=json.loads((OUT/'raw'/(item['id']+'.asr.json')).read_text())
        require(transcript['audio_sha256']==receipt['raw_sha256'],item['id']+' ASR provenance')
        actual=words(transcript['text']);expected=words(item['text'])
        phonation=item['id']=='stagger' and actual==['ah'] and '[screaming]' in transcript['text']
        require(actual==expected or phonation,item['id']+' unexpected spoken text')
        info=sf.info(target)
        require(info.samplerate==48000 and info.channels==1 and info.subtype=='PCM_16',item['id']+' format')
        if item['id'] in ('house_breath','count_breath'):
            require(info.duration/.90<3.6,item['id']+' overlaps the 3.6-second cadence at low pitch')
        meta=Path(str(target)+'.meta').read_text();guid=next(l.split(': ',1)[1] for l in meta.splitlines() if l.startswith('guid: '))
        require(guid not in guids,item['id']+' duplicate GUID');guids.add(guid)
    require((RES/'Audio/breath.wav').read_bytes()==(RES/'HiggsfieldAudio/breath_presence.wav').read_bytes(),'legacy breath is Fish too')
    require(json.loads((OUT/'content-review.json').read_text())==[],'Unresolved content review')
    usage=json.loads((OUT/'usage.json').read_text())
    require(usage['reserved_ceiling_usd']<=5,'API budget cap')
    result={'passed':True,'checks':checks,'scripted_lines':len(lines),'vocal_effect_targets':8,'legacy_aliases':1,'engine_launched':False,'listening_review':False}
    (ROOT/'evidence').mkdir(exist_ok=True)
    (ROOT/'evidence/fish-integration-check.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(result))

if __name__=='__main__':main()
