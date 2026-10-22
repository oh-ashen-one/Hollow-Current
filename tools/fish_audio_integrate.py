#!/usr/bin/env python3
"""Generate, content-check and install offline Drama 3 vocals. No engine launch.

Paid requests require --generate and are reserved against a persistent $5 ledger
BEFORE submission. Uncertain completions are not automatically retried. Raw
masters, reference hashes and prompts remain in docs/audio/fish-integration.
"""
import argparse, hashlib, json, re, shutil, subprocess, time, uuid
from pathlib import Path
import msgpack, numpy as np, requests, soundfile as sf
from scipy.signal import resample_poly
import fish_audio_audition as A

ROOT=A.ROOT
RES=ROOT/'Assets/HollowCurrent/Resources'
AUD=ROOT/'docs/audio/fish-audition-2026-10-22'
OUT=ROOT/'docs/audio/fish-integration'
RAW=OUT/'raw'
STATE=ROOT/'evidence/fish-budget.json'
SR=48000


def dump(p,v):
    p.parent.mkdir(parents=True,exist_ok=True)
    p.write_text(json.dumps(v,indent=2)+'\n')


def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def words(s):
    values=re.findall(r"[a-z0-9]+",re.sub(r'<[^>]*>|\[[^\]]*\]','',s).lower())
    # ASR orthography only: these are the same spoken words, not missing text.
    aliases={'bellweather':'bellwether','11th':'eleventh','13th':'thirteenth','15th':'fifteenth'}
    return [aliases.get(v,v) for v in values]


class Client:
    def __init__(self,allowed):
        self.allowed=allowed; self.s=requests.Session()
        self.s.headers['Authorization']='Bearer '+(Path.home()/'.claude/secrets/fish-audio.key').read_text().strip()
        self.state=json.loads(STATE.read_text()) if STATE.exists() else {'cap_usd':5,'reservations':[]}
        if 'initial_balance' not in self.state:
            self.state['initial_balance']=self.balance();dump(STATE,self.state)
    def balance(self):
        r=self.s.get(A.API+'/wallet/self/api-credit',timeout=20);r.raise_for_status();return float(r.json()['credit'])
    def reserve(self,key,amount):
        if not self.allowed:raise RuntimeError('Paid call requires --generate')
        if any(x['id']==key for x in self.state['reservations']):raise RuntimeError('Uncertain or failed prior request; inspect '+key)
        if sum(x['ceiling_usd'] for x in self.state['reservations'])+amount>5:raise RuntimeError('$5 cap reached')
        self.state['reservations'].append({'id':key,'ceiling_usd':amount,'status':'pending','utc':time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime())});dump(STATE,self.state)
    def finish(self,key,status):
        next(x for x in self.state['reservations'] if x['id']==key)['status']=status;dump(STATE,self.state)
    def tts(self,item):
        dest=RAW/(item['id']+'.wav');meta=RAW/(item['id']+'.request.json')
        ref=AUD/'references'/(item['voice']+'_reference.wav')
        spec={'model':'drama-3','text':item['prompt'],'reference_sha256':sha(ref),'reference_text':A.VOICES[item['voice']]['reference_text'],'format':'wav','sample_rate':44100,'temperature':.7,'latency':'normal'}
        if dest.exists():
            if meta.exists():
                saved=json.loads(meta.read_text());saved.pop('reused_audition',None)
                if saved!=spec:raise RuntimeError('Cached request changed: '+item['id'])
            return dest
        reuse=item.get('audition')
        if reuse:
            shutil.copy2(AUD/'raw'/(reuse+'_drama3.wav'),dest);dump(meta,{**spec,'reused_audition':reuse})
            return dest
        key='tts:'+item['id'];self.reserve(key,max(.01,len(item['prompt'].encode())*50/1e6))
        body={'text':item['prompt'],'references':[{'audio':ref.read_bytes(),'text':spec['reference_text']}],'format':'wav','sample_rate':44100,'latency':'normal','temperature':.7}
        r=self.s.post(A.API+'/v1/tts',headers={'model':'drama-3','Content-Type':'application/msgpack'},data=msgpack.packb(body,use_bin_type=True),timeout=(20,180))
        if r.status_code!=200:
            self.finish(key,'HTTP '+str(r.status_code));raise RuntimeError('TTS HTTP '+str(r.status_code)+' '+item['id'])
        dest.write_bytes(r.content);dump(meta,spec);self.finish(key,'complete');return dest
    def transcript(self,item,path):
        dest=RAW/(item['id']+'.asr.json')
        if dest.exists():return json.loads(dest.read_text())
        key='asr:'+item['id'];duration=sf.info(path).duration;self.reserve(key,max(.01,np.ceil(duration)*.36/3600))
        with path.open('rb') as f:r=self.s.post(A.API+'/v1/asr',headers={'model':'transcribe-1-pro'},files={'audio':(path.name,f,'audio/wav')},data={'ignore_timestamps':'true','language':'en'},timeout=(20,120))
        if r.status_code!=200:self.finish(key,'HTTP '+str(r.status_code));raise RuntimeError('ASR HTTP '+str(r.status_code))
        text=r.json().get('text','');data={'text':text,'audio_sha256':sha(path),'model':'transcribe-1-pro'};dump(dest,data);self.finish(key,'complete');return data


def plan():
    lines=json.loads((ROOT/'tools/voice-lines.json').read_text());takes={t['id']:t for t in A.TAKES}
    reused={'cc_listen':'count_stalking','cc_hunt_found':'count_found','cc_hunt_run_b':'count_hunt','warning':'dispatch_warning','whisper':'house_whisper','evp':'house_evp'}
    items=[]
    for name,spec in lines.items():
        castle=name.startswith('cc_');role='count' if castle else 'house' if name in ('evp','whisper') else 'dispatch'
        direction=('intimate low controlled menace, deliberate clear speech, no theatrical announcer voice' if castle else 'human protective British field operator, grounded natural speech')
        if '_hunt_' in name:direction='quiet predatory delight, close and threatening, clear words, restrained rough breath'
        if '_ledger_' in name:direction='a very old man recalling a terrible secret, low and precise, clearly intelligible unhurried speech'
        if name=='cc_bargain':direction='a low intimate confession, exhausted regret beneath menace, speak every word clearly'
        if name=='ending':direction='a protective human operator finally releasing her held breath, fragile genuine relief'
        if name in ('contain','return'):direction='quiet urgent reassurance, steady clear words'
        prompt='['+direction+'] '+spec['text'];reuse=reused.get(name)
        if reuse:prompt=takes[reuse]['text']
        items.append(dict(id='voice_'+name,voice=role,text=spec['text'],prompt=prompt,fx='stone' if castle else 'evp' if role=='house' else 'radio',target=('CastleAudio' if castle else 'Audio')+'/voice_'+name+'.wav',lufs=-28 if role=='house' else -24.2,audition=reuse))
    effects=[
        ('player_breath','player','[gasp] [panting] [panting] [sigh]','HiggsfieldAudio/breath_hidden.wav','dry',-30,'player_hidden_breath',True),
        ('house_breath','house','[inhale] [shaky exhale] [sigh]','HiggsfieldAudio/breath_presence.wav','house',-30,None,False),
        ('count_breath','count','[inhale] [exhale] [inhale] [exhale] [sigh]','CastleAudio/breath_presence.wav','stone',-30,'count_door_breath',False),
        ('hide_breath_0','count','[inhale] [exhale] [inhale] [exhale] [sigh]','CastleAudio/castle_hide_breath_0.wav','stone',-28,'count_door_breath',False),
        ('hide_breath_1','count','[gasp] [low groan] [exhale] [sigh]','CastleAudio/castle_hide_breath_1.wav','stone',-28,None,False),
        ('hide_exhale','count','[exhale] [sigh]','CastleAudio/castle_hide_whisper.wav','dry',-28,None,False),
        ('stagger','count','[scream] [groan]','CastleAudio/stagger_screech.wav','stone',-28,None,False),
        ('death','count','[screaming] [groaning] [exhale]','CastleAudio/death_wail_sunrise.wav','stone',-29,None,False),
    ]
    for name,voice,prompt,target,fx,lufs,reuse,loop in effects:
        items.append(dict(id=name,voice=voice,prompt=prompt,text='',fx=fx,lufs=lufs,target=target,audition=reuse,loop=loop))
    return items


def prepare(item,src):
    staging=ROOT/'evidence/fish-mix';staging.mkdir(parents=True,exist_ok=True)
    tmp=staging/(item['id']+'.wav');A.mono48(src,tmp);x,_=sf.read(tmp)
    if not .2<len(x)/SR<45 or np.max(np.abs(x))<.001:raise RuntimeError('Invalid audio '+item['id'])
    death_tail=None
    if item['id']=='death':
        original=OUT/'original-death-music.wav'
        if not original.exists():shutil.copy2(RES/item['target'],original)
        y,sr=sf.read(original);y=y.mean(axis=1) if y.ndim>1 else y
        if sr!=SR:y=resample_poly(y,SR,sr)
        # Preserve the documented non-vocal sunrise swell after the dying wail.
        x=x[:int(4.3*SR)];x[-int(.3*SR):]*=np.linspace(1,0,int(.3*SR))
        death_tail=y[int(4.3*SR):].copy()
    x=A.mix(x,item['fx'])
    idx=np.flatnonzero(np.abs(x)>10**(-55/20))
    if len(idx):x=x[max(0,idx[0]-int(.025*SR)):min(len(x),idx[-1]+int(.08*SR))]
    if item.get('loop'):
        # Crossfade the tail into the beginning, then rotate the seam into its smooth middle.
        n=min(int(.3*SR),len(x)//8);f=np.linspace(0,1,n)
        x=np.r_[x[-n:]*(1-f)+x[:n]*f,x[n:-n]]
        x=np.roll(x,-n//2)
    sf.write(tmp,x,SR,subtype='PCM_16')
    dest=RES/item['target'];dest.parent.mkdir(parents=True,exist_ok=True)
    A.normalize(tmp,dest,item['lufs'])
    # Cut only silence below -50 dBFS after normalization, preserving natural decays.
    x,_=sf.read(dest);idx=np.flatnonzero(np.abs(x)>10**(-50/20))
    if not item.get('loop') and len(idx):
        x=x[:min(len(x),idx[-1]+int(.075*SR))];n=min(int(.02*SR),len(x)//4);x[-n:]*=np.linspace(1,0,n)
    if item.get('loop'):
        # At most one PCM sample correction ensures a click-free exact wrap.
        x[-1]=x[0]
    if death_tail is not None:
        # The original musical tail is not sent to Fish or put through the new
        # vocal EQ/reverb/normalizer. Its sample values remain unchanged after
        # the short join. The documented music transition is at about 4 s.
        cut=int(4.3*SR);x=x[:cut];x=np.pad(x,(0,max(0,cut-len(x))))
        n=int(.15*SR);x[-n:]*=np.linspace(1,0,n);death_tail[:n]*=np.linspace(0,1,n)
        x=np.r_[x,death_tail]
    sf.write(dest,x,SR,subtype='PCM_16')
    meta=Path(str(dest)+'.meta')
    if not meta.exists():
        template=(RES/'HiggsfieldAudio/breath_presence.wav.meta').read_text()
        meta.write_text(re.sub(r'guid: [0-9a-f]+','guid: '+uuid.uuid5(uuid.NAMESPACE_URL,'hollow-current/fish/'+item['target']).hex,template))
    return {'id':item['id'],'target':item['target'],'sha256':sha(dest),'raw_sha256':sha(src),'seconds':round(len(x)/SR,3),'loop':item.get('loop',False)}


def main():
    p=argparse.ArgumentParser();p.add_argument('--generate',action='store_true');a=p.parse_args()
    RAW.mkdir(parents=True,exist_ok=True);client=Client(a.generate);items=plan();dump(OUT/'plan.json',items)
    results=[];failed=[]
    for item in items:
        src=client.tts(item);asr=client.transcript(item,src);actual=words(asr['text']);expected=words(item['text'])
        pain_phonation=item['id']=='stagger' and actual==['ah'] and '[screaming]' in asr['text']
        if actual!=expected and not pain_phonation:
            failed.append({'id':item['id'],'expected':item['text'],'actual':asr['text']});print('CONTENT REVIEW REQUIRED',item['id'],repr(asr['text']),flush=True);continue
        result=prepare(item,src);results.append(result);dump(OUT/'integration.json',results)
        print('INSTALLED',item['target'],result['seconds'],'s',flush=True)
    # Preserve the unused legacy breath ID, too; runtime Cue redirects it to breath_presence.
    if any(x['id']=='house_breath' for x in results):shutil.copy2(RES/'HiggsfieldAudio/breath_presence.wav',RES/'Audio/breath.wav')
    dump(OUT/'content-review.json',failed)
    end=client.balance();client.state['latest_balance']=end;dump(STATE,client.state)
    dump(OUT/'usage.json',{'cap_usd':5,'reserved_ceiling_usd':round(sum(x['ceiling_usd'] for x in client.state['reservations']),6),'observed_balance_delta':round(client.state['initial_balance']-end,6),'requests':client.state['reservations'],'note':'Balance may post late or include other account activity; ceiling uses list TTS price and rounded ASR duration.'})
    print('Installed',len(results),'of',len(items),'items; content-review',len(failed))
    if failed:raise SystemExit(2)


if __name__=='__main__':main()
