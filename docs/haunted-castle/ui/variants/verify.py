"""Evidence-based validation of rendered deliverables and offline regeneration."""
import hashlib, json, subprocess, sys
from pathlib import Path
from PIL import Image
from fontTools.ttLib import TTFont
from render_core import ROOT, CONFIGS, SCREENS, CASTLE, PHOTO, OBJECTIVE, WARNING, DOCS

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def norm(s): return ' '.join(s.split())

def main():
    errors=[]; checks=0; measured=[]
    def check(ok,why):
        nonlocal checks
        checks+=1
        if not ok: errors.append(why)
    sources=json.loads((ROOT/'FONT_SOURCES.json').read_text())
    for record in sources['files']:
        p=ROOT/record['path']
        check(p.exists() and sha(p)==record['sha256'],'Font/source checksum: '+record['path'])
        if p.name=='OFL.txt': check('SIL OPEN FONT LICENSE' in p.read_text().upper(),'Licence: '+record['path'])
    for slug,c in CONFIGS.items():
        p=ROOT/slug; audit=json.loads((p/'layout_audit.json').read_text())
        fonts={role:TTFont(p/'fonts'/c[role]).getBestCmap() for role in ['display','body','mono']}
        for name in ['README.md','ASSETS.md','render.py']:
            check((p/name).is_file(),f'{slug}: missing {name}')
        for screen in SCREENS:
            path=p/f'{screen}.png'
            check(path.is_file(),f'{slug}: missing {screen}')
            with Image.open(path) as im: check(im.size==(1920,1080),f'{slug}/{screen}: dimensions')
            runs=[x for x in audit if x['screen']==screen]
            joined=norm(' '.join(x['text'] for x in runs))
            required={
              'document_castle':['BLOOD LEDGER','The steward',norm(CASTLE),'ESC'],
              'document_bellwether':['Family photograph',norm(PHOTO),'ESC'],
              'journal':DOCS+['Anchor','Pattern','Closure','Close the case','DREAD','ESC'],
              'hud':[norm(OBJECTIVE),'Open Great door [E]','The door is held.',norm(WARNING),'STAMINA','LANTERN'],
              'title':['HOLLOW CURRENT','14 Bellwether Lane','Haunted Castle','Continue','New case','Settings','Quit'],
              'settings':['Look sensitivity','Volume','Windowed','Invert Y','Continue','New case','Settings','Quit','ESC'],
              'ending_closed':['CASE CLOSED','At dawn, the bell kept its silence.'],
              'ending_lost':['SIGNAL LOST','The house has learned another name.'],
            }[screen]
            for text in required: check(text in joined,f'{slug}/{screen}: missing exact copy {text!r}')
            for i,u in enumerate(runs):
                x,y,r,b=u['box']
                check(0<=x<r<=1920 and 0<=y<b<=1080,f'{slug}/{screen}: out-of-canvas {u["text"]}')
                check(u['contrast']>=4.5,f'{slug}/{screen}: contrast {u["text"]}')
                if u['role']=='body': check(u['size']>=30,f'{slug}/{screen}: undersized body')
                missing={ch for ch in u['text'] if ord(ch) not in fonts[u['role']]}
                check(not missing,f'{slug}/{screen}: missing glyph {missing} in {c[u["role"]]}')
                if screen.startswith('document') and u['role']=='body':
                    check(len(u['text'])<=70,f'{slug}/{screen}: line longer than 70 characters')
                for v in runs[i+1:]:
                    X,Y,R,B=v['box']
                    check(not (min(r,R)>max(x,X) and min(b,B)>max(y,Y)),f'{slug}/{screen}: text collision {u["text"]!r} / {v["text"]!r}')
        for texture in (p/'textures').glob('*.png'):
            with Image.open(texture) as im:
                check(max(im.size)<=2048,f'{texture}: texture above 2048')
                if texture.stem=='photo_placeholder': check(im.width*3==im.height*4,f'{slug}: photo not 4:3')
        measured.append({'slug':slug,'screens':8,'text_runs':len(audit),'minimum_contrast':min(x['contrast'] for x in audit)})
    # Render the entry points, not only the shared API, to verify the delivery commands.
    paths=sorted(list(ROOT.glob('*/*.png'))+list(ROOT.glob('*/textures/*.png'))+list(ROOT.glob('*/layout_audit.json'))+[ROOT/'CONTACT_SHEET.png'])
    before={str(p.relative_to(ROOT)):sha(p) for p in paths}
    for slug in CONFIGS:
        subprocess.run([sys.executable,str(ROOT/slug/'render.py')],check=True,cwd=ROOT/slug)
    subprocess.run([sys.executable,str(ROOT/'package.py')],check=True,cwd=ROOT)
    after={str(p.relative_to(ROOT)):sha(p) for p in paths}
    for name in before: check(before[name]==after[name],'Nondeterministic regeneration: '+name)
    result={'passed':not errors,'checks':checks,'measured':measured,'reproduced_files':len(paths),'errors':errors}
    (ROOT/'verification.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(result,indent=2))
    if errors: raise SystemExit(1)

if __name__=='__main__': main()
