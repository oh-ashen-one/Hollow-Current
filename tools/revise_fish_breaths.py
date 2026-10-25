"""Bounded, cached six-clip breath correction; --generate explicitly permits API calls."""
import argparse,json,shutil
from pathlib import Path
import fish_audio_integrate as F

IDS={'player_breath','house_breath','count_breath','hide_breath_0','hide_breath_1','hide_exhale'}
REV=F.OUT/'breath-revision-2026-10-25'
class BreathClient(F.Client):
    def reserve(self,key,amount):
        if sum(x['ceiling_usd'] for x in self.state['reservations'])+amount>.30:
            raise RuntimeError('Breath revision $0.30 ceiling reached')
        super().reserve(key,amount)

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--generate',action='store_true');a=parser.parse_args()
    original_raw=F.RAW;F.RAW=REV/'raw';F.RAW.mkdir(parents=True,exist_ok=True)
    F.STATE=F.ROOT/'evidence/fish-breath-revision-budget.json'
    client=BreathClient(a.generate)
    items=[x for x in F.plan() if x['id'] in IDS]
    for item in items:
        src=client.tts(item);asr=client.transcript(item,src)
        if F.words(asr['text']):raise RuntimeError('Unexpected spoken words: '+item['id']+'; review saved transcript')
        print('READY',item['id'],flush=True)
    receipts={x['id']:x for x in json.loads((F.OUT/'integration.json').read_text())}
    plan={x['id']:x for x in json.loads((F.OUT/'plan.json').read_text())}
    for item in items:
        for suffix in ['.wav','.request.json','.asr.json']:
            shutil.copy2(F.RAW/(item['id']+suffix),original_raw/(item['id']+suffix))
        receipts[item['id']]=F.prepare(item,F.RAW/(item['id']+'.wav'))
        receipts[item['id']]['processing']='six-band unvoiced noise carrier; Fish temporal energy only'
        plan[item['id']]=item
    shutil.copy2(F.RES/'HiggsfieldAudio/breath_presence.wav',F.RES/'Audio/breath.wav')
    F.dump(F.OUT/'integration.json',list(receipts.values()));F.dump(F.OUT/'plan.json',list(plan.values()))
    end=client.balance();F.dump(REV/'usage.json',{'cap_usd':.30,'reserved_ceiling_usd':sum(x['ceiling_usd'] for x in client.state['reservations']),'observed_balance_delta':client.state['initial_balance']-end,'requests':client.state['reservations']})
    print('INSTALLED',len(items),'unvoiced breathing replacements',flush=True)
if __name__=='__main__':main()
