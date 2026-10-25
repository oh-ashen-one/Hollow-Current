"""Keep authored Tripo prefabs authoritative over legacy procedural FBX names."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

def tripo_replacement(name, root=ROOT):
    source = root / 'Assets/HollowCurrent/Art/Tripo/Castle' / name
    manifest = source / 'import.json'
    if not manifest.exists():
        return None
    data = json.loads(manifest.read_text())
    if data.get('output') != 'Castle' or data.get('name') != name:
        raise ValueError('Invalid Castle replacement manifest: ' + str(manifest))
    prefab = root / 'Assets/HollowCurrent/Resources/Castle' / (name + '.prefab')
    required = [prefab, Path(str(prefab) + '.meta')]
    required += [source / part['file'] for part in data['primitives']]
    for mat in data['materials']:
        required.append(source / (mat['stem'] + '-color.png'))
        for flag, suffix in [('hasNormal', 'normal'), ('hasMetal', 'metal')]:
            if mat.get(flag): required.append(source / (mat['stem'] + '-' + suffix + '.png'))
    missing = [str(p) for p in required if not p.is_file()]
    if missing:
        raise FileNotFoundError('Incomplete Tripo replacement; restore/import it before rebuilding: ' + ', '.join(missing))
    return data

def check_replacements(root=ROOT):
    names = []
    for path in sorted((root / 'Assets/HollowCurrent/Art/Tripo/Castle').glob('*/import.json')):
        name = path.parent.name
        tripo_replacement(name, root)
        castle = root / 'Assets/HollowCurrent/Resources/Castle'
        for ext in ('.fbx', '.fbx.meta'):
            if (castle / (name + ext)).exists():
                raise ValueError('Duplicate legacy resource shadows Tripo prefab: ' + str(castle / (name + ext)))
        names.append(name)
    return names

if __name__ == '__main__':
    print('HC_TRIPO_REPLACEMENTS_PASS', len(check_replacements()))
