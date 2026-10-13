#!/usr/bin/env python3
"""Read-only validation of built scene script payloads against this game's schema.
Run with `uv run --with UnityPy python tools/audit_player_scene.py`.
Checks actual serialized byte lengths; does not launch an engine or renderer.
"""
from pathlib import Path
import json,struct
import UnityPy
ROOT=Path(__file__).resolve().parents[1]
DATA=ROOT/'Builds/macOS/Hollow Current.app/Contents/Resources/Data'
class Reader:
    def __init__(self,raw):self.raw=raw;self.offset=0
    def skip(self,n):
        self.offset+=n
        if self.offset>len(self.raw):raise ValueError('payload ends before current field layout')
    def integer(self):
        value=struct.unpack_from('<i',self.raw,self.offset)[0];self.skip(4);return value
    def align(self):self.offset=(self.offset+3)&~3
    def string(self):
        n=self.integer()
        if n<0 or n>len(self.raw):raise ValueError('invalid serialized string length')
        value=self.raw[self.offset:self.offset+n].decode('utf8');self.skip(n);self.align();return value
    def array(self,size):
        n=self.integer()
        if n<0 or n>100000:raise ValueError('invalid array length')
        self.skip(n*size);return n

def check_scene():
    environment=UnityPy.load(str(DATA/'level0'),str(DATA/'globalgamemanagers.assets'))
    checks=[];errors=[]
    for obj in environment.objects:
        if obj.assets_file.name!='level0':continue
        if obj.type.name!='MonoBehaviour':
            try:obj.read_typetree()
            except Exception as ex:errors.append({'id':obj.path_id,'type':obj.type.name,'error':str(ex)})
            continue
        base=obj.read(check_read=False);name=base.m_Script.read().m_ClassName;reader=Reader(obj.get_raw_data());label=''
        try:
            reader.skip(12);reader.skip(1);reader.align();reader.skip(12);reader.string()
            if name=='WorldLabel':pass
            elif name=='PracticalFixture':reader.skip(12)
            elif name=='Door':reader.skip(16);reader.skip(1);reader.align()
            elif name=='Interactable':
                reader.integer();label=reader.string();reader.string();reader.skip(24);reader.skip(12);reader.skip(12)
            elif name=='WorldLayout':
                reader.skip(60)
                for _ in range(4):reader.array(12)
                reader.array(12)
                for _ in range(reader.integer()):reader.string()
                reader.array(12)
            else:raise ValueError('Unreviewed script schema: '+name)
            if reader.offset!=len(reader.raw):raise ValueError('payload has unexpected trailing fields')
            checks.append({'id':obj.path_id,'class':name,'label':label,'bytes':len(reader.raw),'matched':True})
        except Exception as ex:errors.append({'id':obj.path_id,'class':name,'label':label,'bytes':len(reader.raw),'parsed':reader.offset,'error':str(ex)})
    result={'passed':not errors,'script_payloads':checks,'errors':errors,'scope':'built level0 native objects and current project MonoBehaviour byte layouts; not a rendered test'}
    (ROOT/'evidence/player-scene-payload-audit.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({'passed':result['passed'],'script_payloads':len(checks),'errors':errors},indent=2))
    return result['passed']
if __name__=='__main__':raise SystemExit(0 if check_scene() else 1)
