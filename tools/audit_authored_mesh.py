#!/usr/bin/env python3
"""Audit original HCM buffers without launching an engine or rendering.
Checks manifest counts, index bounds, finite data and face/vertex normal agreement.
"""
import argparse
import json
import struct
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--prefix',default='')
p.add_argument('--output',default='evidence/authored-mesh-audit.json')
a=p.parse_args()
folder=ROOT/'Assets/HollowCurrent/Art/Authored'
kit=json.loads((folder/'kit.json').read_text());checks=[]
for asset in kit['assets']:
    if not asset['name'].startswith(a.prefix):continue
    vertices=[];triangles=0;opposed=0
    for part in asset['parts']:
        raw=(folder/part['file']).read_bytes();magic,count,index_count=struct.unpack_from('<Iii',raw)
        assert magic==0x48434d31 and count==part['vertices'] and index_count==part['triangles']*3,part['file']
        assert len(raw)==12+count*32+index_count*4,part['file']
        data=np.frombuffer(raw,dtype='<f4',offset=12,count=count*8).reshape(count,8)
        indices=np.frombuffer(raw,dtype='<i4',offset=12+count*32,count=index_count).reshape(-1,3)
        assert np.isfinite(data).all() and indices.min()>=0 and indices.max()<count,part['file']
        normals=np.cross(data[indices[:,1],:3]-data[indices[:,0],:3],data[indices[:,2],:3]-data[indices[:,0],:3])
        opposed+=int(((normals*data[indices,3:6].mean(1)).sum(1)<-1e-8).sum())
        triangles+=len(indices);vertices.append(data[:,:3])
    data=np.concatenate(vertices)
    checks.append({'name':asset['name'],'triangles':triangles,'minimum':data.min(0).tolist(),'maximum':data.max(0).tolist(),'opposed_normals':opposed})
assert checks,'No assets selected'
result={'passed':all(x['opposed_normals']==0 for x in checks),'scope':'CPU original mesh buffers; no native rendering, collision or visual acceptance','assets':checks}
(ROOT/a.output).write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({'passed':result['passed'],'assets':len(checks),'triangles':sum(x['triangles'] for x in checks),'output':a.output}))
raise SystemExit(0 if result['passed'] else 1)
