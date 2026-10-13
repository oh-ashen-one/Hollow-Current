#!/usr/bin/env python3
"""Validate actual converted furniture and field-kit payloads without Unity."""
import hashlib,json,math,struct
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
ledger=json.loads((ROOT/'Art/Tripo/ledger.json').read_text());reports=[]
assert ledger['observed_spend']<=ledger['authorized_credit_budget']
for item in ledger['assets']:
 name=item.get('unity_name') or {'dining-table':'DiningTable','dining-chair':'DiningChair','child-bed':'ChildBed','parents-bed':'ParentsBed'}.get(item['name'],item['name'].title())
 folder=ROOT/'Assets/HollowCurrent/Art/Tripo'/name
 manifest=json.loads((folder/'import.json').read_text())
 assert hashlib.sha256((ROOT/manifest['source']).read_bytes()).hexdigest()==manifest['sha256']
 for part in manifest['primitives']:
  data=(folder/part['file']).read_bytes();magic,count,indices=struct.unpack_from('<III',data)
  assert magic==0x48434d31 and count==part['vertices'] and indices==part['triangles']*3
  assert len(data)==12+count*32+indices*4
  values=struct.unpack_from('<'+str(count*8)+'f',data,12);assert all(math.isfinite(v) for v in values)
  faces=struct.unpack_from('<'+str(indices)+'I',data,12+count*32);assert max(faces)<count
  assert 100<indices//3<=35000
  assert max(values[1::8])-min(values[1::8])>.01
 for material in manifest['materials']:
  for suffix in ['color','normal','metal']:
   with Image.open(folder/(material['stem']+'-'+suffix+'.png')) as im:
    assert 512<=im.width<=2048 and 512<=im.height<=2048
    if suffix=='metal':assert im.mode=='RGBA' # smoothness resides in alpha
 reports.append({'asset':name,'triangles':sum(p['triangles'] for p in manifest['primitives']),'verified_sha256':manifest['sha256']})
expected={'Sofa','DiningTable','DiningChair','Wardrobe','ChildBed','ParentsBed','Washer','Refrigerator','Sideboard','Doll','Bookcase','Furnace','Bathtub','InductionReader','Flashlight','UVLamp','EVPRecorder','FieldRadio'}
assert {r['asset'] for r in reports}==expected and len(reports)==len(expected)
(ROOT/'evidence/tripo-assets.json').write_text(json.dumps({'passed':True,'engine_launched':False,'rendered':False,'assets':reports,'credit_spend':ledger['observed_spend']},indent=2)+'\n')
print(f'PASS {len(reports)} real GLB hashes, binary mesh bounds/indices, finite vertex data, and PBR texture payloads; no engine/render launched.')
