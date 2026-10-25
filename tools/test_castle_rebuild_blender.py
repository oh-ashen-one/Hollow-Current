"""Blender regression: real legacy builders must not recreate Tripo resource names.
Run with Blender -b -P tools/test_castle_rebuild_blender.py through the shared slot.
"""
import hashlib, importlib, json, sys, tempfile
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
import castle_common as cc
import castle_lib as lib
from castle_replacements import check_replacements

names = set(check_replacements())
protected = list((cc.ROOT/'Assets/HollowCurrent/Art/Tripo/Castle').rglob('*'))
protected += [p for name in names for p in cc.CASTLE.glob(name+'.prefab*')]
def digest():
    return {str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in protected if p.is_file()}
before = digest(); tested = []
lib.reset_scene()
for module in ['castle_architecture','castle_props','castle_props_r12','castle_furniture','castle_hall_kit']:
    mod = importlib.import_module(module)
    for name, make, budget in mod.BUILDERS:
        if name not in names: continue
        builder = make()
        if module == 'castle_hall_kit': mod.turn_to_unity(builder)
        info = lib.export_asset(builder, budget)
        assert info['replacement'] == 'Tripo', name
        if module == 'castle_hall_kit': mod.write_fbx_meta(name)
        tested.append(module+':'+name)
assert tested, 'No legacy generator paths exercised'
# Some shipped prefabs have no remaining legacy builder. Guard every import too.
from types import SimpleNamespace
for name in names:
    assert lib.export_asset(SimpleNamespace(name=name))['replacement'] == 'Tripo'

assert digest() == before, 'Rebuild modified authored Tripo assets'
assert set(check_replacements()) == names
# Verify a retained procedural asset still exports; isolate its FBX and metadata.
original = cc.CASTLE
with tempfile.TemporaryDirectory(dir=cc.ROOT/'evidence') as folder:
    cc.CASTLE = Path(folder)
    mod = importlib.import_module('castle_props')
    name, make, budget = next(x for x in mod.BUILDERS if x[0] == 'Orrery')
    info = lib.export_asset(make(), budget)
    assert (cc.CASTLE/'Orrery.fbx').stat().st_size > 0
cc.CASTLE = original
result = {'passed':True,'replacement_names':len(names),'legacy_builder_paths':tested,'protected_files':len(before),'retained_procedural_export':'Orrery'}
(cc.ROOT/'evidence/castle-rebuild-regression.json').write_text(json.dumps(result,indent=2)+'\n')
print('HC_REBUILD_REGRESSION_PASS', len(tested), 'legacy paths;', len(before), 'protected files unchanged')
