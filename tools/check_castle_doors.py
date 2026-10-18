#!/usr/bin/env python3
"""Static check (no Unity): every castle door leaf in the prepared scene is visible when closed and can swing.

For each door hinge (a GameObject carrying the Door MonoBehaviour) in Castle.unity it reads
  * the static editor flags of every descendant: BatchingStatic (bit 4) on a moving leaf keeps it painted at the closed pose
    (the drawbridge lesson, see check_throwables_scene.py);
  * the leaf art (the "Castle / *Leaf" child): its FBX vertices, mapped the way Unity imports the old castle kit
    (Blender (x, y, z) -> Unity (-x, z, -y), docs/haunted-castle/handoffs/art-exterior2.md section 4, i.e. file (x, y, z) -> (-x, y, z)),
    then placed with the baked local transform, in world space;
and fails when
  * any hinge descendant is batching-static,
  * the closed leaf's bounds overlap a "Wall" box by more than 20 percent of the leaf volume (more than 50 percent = "inWall": the leaf is
    buried in the wall next to the opening, which is how the Great Hall doors were invisible while closed),
  * the leaf's centre lies on the -X side of its hinge (a leaf must extend +X from the hinge, the way the collision panel and Door.cs do),
  * a hinge has no leaf art,
  * (fc-fbx) the collision panel's thickness differs from the leaf's visible oak slab (DOOR_LEAF_SPECS t in tools/castle_architecture.py times
    the leaf art's Z scale) by more than 5 mm, or the slab is outside a real oak leaf (0.08-0.12 m great doors, 0.05-0.08 m the others).

usage: check_castle_doors.py [Assets/HollowCurrent/Scenes/Castle.unity] [--simulate-fix] [-v]
  --simulate-fix  apply CastleWorld.BuildDoor's runtime rules (turn a leaf whose meshes sit at -X by 180 degrees about the hinge; fc-fbx: scale the
                  leaf to CastleKitAxis.Leaves' slab and give the panel that thickness) before checking, to show what a re-bake with the fix
                  produces. Without it the file is judged as baked.
Exit 1 on any failure. The prepare step logs the same facts as HC_DOOR_LEAF lines.
"""
import math, re, struct, sys, zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
args = [a for a in sys.argv[1:] if not a.startswith('-')]
SIM = '--simulate-fix' in sys.argv
VERBOSE = '-v' in sys.argv
SCENE = Path(args[0]) if args else ROOT / 'Assets/HollowCurrent/Scenes/Castle.unity'
CASTLE_ART = ROOT / 'Assets/HollowCurrent/Resources/Castle'

# ---- minimal binary FBX vertex reader -----------------------------------------------------------
def fbx_vertices(path):
    data = path.read_bytes()
    ver = struct.unpack('<I', data[23:27])[0]
    found = []
    def node(pos):
        if ver >= 7500:
            end, nprops, plen, nlen = struct.unpack('<QQQB', data[pos:pos + 25]); pos += 25
        else:
            end, nprops, plen, nlen = struct.unpack('<IIIB', data[pos:pos + 13]); pos += 13
        if end == 0:
            return False, pos
        name = data[pos:pos + nlen].decode('latin1'); pos += nlen
        props = []
        for _ in range(nprops):
            t = chr(data[pos]); pos += 1
            if t in 'YCIFDL':
                fmt = {'Y': '<h', 'C': '<B', 'I': '<i', 'F': '<f', 'D': '<d', 'L': '<q'}[t]
                size = struct.calcsize(fmt); props.append(struct.unpack(fmt, data[pos:pos + size])[0]); pos += size
            elif t in 'fdilb':
                n, enc, clen = struct.unpack('<III', data[pos:pos + 12]); pos += 12
                raw = data[pos:pos + clen]; pos += clen
                if enc: raw = zlib.decompress(raw)
                props.append(struct.unpack('<%d%s' % (n, {'f': 'f', 'd': 'd', 'i': 'i', 'l': 'q', 'b': 'b'}[t]), raw))
            elif t in 'SR':
                n = struct.unpack('<I', data[pos:pos + 4])[0]; pos += 4; props.append(data[pos:pos + n]); pos += n
        if name == 'Vertices' and props:
            v = props[0]; found.extend((v[i], v[i + 1], v[i + 2]) for i in range(0, len(v), 3))
        while pos < end:
            ok, pos = node(pos)
            if not ok: break
        return True, end
    pos = 27
    while pos < len(data) - 30:
        ok, pos = node(pos)
        if not ok: break
    return found

# ---- scene parsing ------------------------------------------------------------------------------
src = SCENE.read_text()
docs = re.split(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n', src, flags=re.M)
objs = {docs[i + 1]: (int(docs[i]), docs[i + 2]) for i in range(1, len(docs), 3)}

door_guid = None
meta = ROOT / 'Assets/HollowCurrent/Runtime/Door.cs.meta'
m = re.search(r'guid: ([0-9a-f]{32})', meta.read_text())
door_guid = m.group(1)

def nums(b, key):
    mm = re.search(key + r': \{(.*?)\}', b)
    return [float(p.split(': ')[1]) for p in mm.group(1).split(', ')]

name, flags, comps, tr_of, go_of, children, parent, local = {}, {}, {}, {}, {}, {}, {}, {}
for fid, (cls, body) in objs.items():
    if cls == 1:
        name[fid] = re.search(r'm_Name: (.*)', body).group(1)
        flags[fid] = int(re.search(r'm_StaticEditorFlags: (\d+)', body).group(1))
        comps[fid] = [c for c in re.findall(r'component: \{fileID: (-?\d+)\}', body) if c in objs]
    elif cls == 4:
        g = re.search(r'm_GameObject: \{fileID: (-?\d+)\}', body).group(1)
        go_of[fid] = g; tr_of[g] = fid
        ch = re.search(r'm_Children:\n((?:  - \{fileID: -?\d+\}\n)*)', body)
        children[fid] = re.findall(r'fileID: (-?\d+)', ch.group(1)) if ch else []
        local[fid] = (nums(body, 'm_LocalPosition'), nums(body, 'm_LocalRotation'), nums(body, 'm_LocalScale'))
for t, cs in children.items():
    for c in cs: parent[c] = t

def qmat(q):
    x, y, z, w = q
    return [[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
            [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
            [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]]
def mm(a, b): return [[sum(a[i][k] * b[k][j] for k in range(3)) for j in range(3)] for i in range(3)]
def mv(a, v): return [sum(a[i][k] * v[k] for k in range(3)) for i in range(3)]
def tr(a): return [[a[j][i] for j in range(3)] for i in range(3)]
def world(t):
    """(rotation*scale matrix, position) of a transform id"""
    p, q, s = local[t]
    R = qmat(q); M = [[R[i][j] * s[j] for j in range(3)] for i in range(3)]
    if t in parent:
        PM, PP = world(parent[t])
        return mm(PM, M), [PP[i] + mv(PM, p)[i] for i in range(3)]
    return M, p

def descendants(t):
    out, st = [], list(children.get(t, []))
    while st:
        c = st.pop(); out.append(c); st += children.get(c, [])
    return out

def is_door(g):
    for c in comps[g]:
        cls, body = objs[c]
        if cls == 114 and ('guid: ' + door_guid) in body: return True
    return False

hinges = [g for g in name if g in tr_of and is_door(g)]
wall_boxes = []
for g, n in name.items():
    if n == 'Wall' and g in tr_of:
        M, P = world(tr_of[g])
        if abs(M[0][1]) + abs(M[0][2]) + abs(M[1][0]) + abs(M[1][2]) + abs(M[2][0]) + abs(M[2][1]) > 1e-3: continue
        sx, sy, sz = abs(M[0][0]), abs(M[1][1]), abs(M[2][2])
        wall_boxes.append(((P[0] - sx / 2, P[1] - sy / 2, P[2] - sz / 2), (P[0] + sx / 2, P[1] + sy / 2, P[2] + sz / 2)))

# fc-fbx: authored slab per leaf (tools/castle_architecture.py DOOR_LEAF_SPECS: (w, h, t, straps, ring)) and the fitted slab (CastleKitAxis.Leaves)
SPECS = {m.group(1): float(m.group(2)) for m in re.finditer(r'"(\w+Leaf)": \([\d.]+, [\d.]+, ([\d.]+),', (ROOT / 'tools/castle_architecture.py').read_text())}
FITTED = {m.group(1): float(m.group(2).rstrip('f')) for m in re.finditer(r'\{ "(\w+Leaf)", new LeafSpec\([\d.]+f, [\d.]+f, ([\d.]+f)\) \}',
          (ROOT / 'Assets/HollowCurrent/Runtime/Castle/CastleKitAxis.cs').read_text())}

verts_cache = {}
def art_vertices(art):
    if art not in verts_cache:
        f = CASTLE_ART / (art + '.fbx')
        verts_cache[art] = [(-v[0], v[1], v[2]) for v in fbx_vertices(f)]   # Unity import of the old castle kit: file x is mirrored
    return verts_cache[art]

def overlap_volume(lo, hi, box):
    d = [min(hi[i], box[1][i]) - max(lo[i], box[0][i]) for i in range(3)]
    return d[0] * d[1] * d[2] if all(x > 0 for x in d) else 0.0

failures, rows = [], []
for g in sorted(hinges, key=lambda x: name[x]):
    ht = tr_of[g]
    hM, hP = world(ht)
    bad_static = [name[go_of[d]] for d in descendants(ht) if flags[go_of[d]] & 4]
    if flags[g] & 4: bad_static.append(name[g])
    leaf = None
    for d in descendants(ht):
        gg = go_of[d]
        if name[gg].startswith('Castle / ') and name[gg].endswith('Leaf'): leaf = d; break   # the portrait leaf is a root with Canvas and leaf children
    if leaf is None:
        failures.append('%s: no leaf art under the hinge' % name[g]); continue
    art = name[go_of[leaf]][len('Castle / '):]
    p, q, s = local[leaf]
    LM, LP = world(leaf)
    verts = art_vertices(art)
    # centre of the leaf in the hinge frame, as CastleWorld.ArtLocalCentre sees it (mesh bounds centre)
    inv = tr(hM)   # hinge scale is 1
    def to_hinge(v):
        w = [LP[i] + mv(LM, v)[i] for i in range(3)]
        return mv(inv, [w[i] - hP[i] for i in range(3)])
    hv = [to_hinge(v) for v in verts]
    cx = (min(v[0] for v in hv) + max(v[0] for v in hv)) / 2
    turned = False
    if SIM and cx < 0:
        # BuildDoor: leaf turned 180 degrees about the hinge's Y: local x and z negate
        hv = [(-v[0], v[1], -v[2]) for v in hv]; cx = -cx; turned = True
    lo_l = [min(v[i] for v in hv) for i in range(3)]; hi_l = [max(v[i] for v in hv) for i in range(3)]
    wv = [[hP[i] + mv(hM, v)[i] for i in range(3)] for v in hv]
    lo = [min(v[i] for v in wv) for i in range(3)]; hi = [max(v[i] for v in wv) for i in range(3)]
    vol = max(1e-9, (hi[0] - lo[0]) * (hi[1] - lo[1]) * (hi[2] - lo[2]))
    inside = sum(overlap_volume(lo, hi, b) for b in wall_boxes)
    frac = min(1.0, inside / vol)
    in_wall = frac > .5
    problems = []
    if bad_static: problems.append('batching-static: ' + ', '.join(sorted(set(bad_static))[:3]))
    if frac > .2: problems.append('closed leaf %.0f%% inside wall boxes' % (frac * 100))
    if cx <= 0: problems.append('leaf extends to the -X side of its hinge (centre x %.2f)' % cx)
    # fc-fbx: collision panel thickness vs the visible oak slab
    panel = next((c for c in children.get(ht, []) if c != leaf and not name[go_of[c]].startswith('Castle / ')), None)
    slab = SPECS.get(art, 0) * abs(s[2]); pz = abs(local[panel][2][2]) if panel else 0
    if SIM and art in FITTED: slab = pz = FITTED[art]
    great = art == 'GreatDoorLeaf'
    if not panel: problems.append('no collision panel under the hinge')
    elif abs(pz - slab) > .005: problems.append('collision panel %.3f m thick, visible oak slab %.3f m' % (pz, slab))
    if slab and not ((.08 if great else .05) - 1e-4 <= slab <= (.12 if great else .08) + 1e-4): problems.append('oak slab %.3f m outside a real leaf' % slab)
    rows.append((name[g], art, cx, frac, in_wall, bool(bad_static), turned, lo, hi))
    if problems: failures.append('%s: %s' % (name[g], '; '.join(problems)))

for n, art, cx, frac, in_wall, st, turned, lo, hi in rows:
    if VERBOSE or frac > .2 or cx <= 0 or st:
        print('HC_DOOR_LEAF name=%s art=%s hingeX=%.2f inWall=%s wallOverlap=%.2f batchingStatic=%s%s bounds=(%.2f,%.2f,%.2f)-(%.2f,%.2f,%.2f)' %
              (n, art, cx, in_wall, frac, st, ' (simulated turn)' if turned else '', *lo, *hi))
print('door hinges', len(hinges), 'leaves checked', len(rows), 'wall boxes', len(wall_boxes), 'simulate_fix', SIM)
if len(hinges) < 20: failures.append('expected at least 20 door hinges, found %d' % len(hinges))
if failures:
    print('FAIL %d' % len(failures))
    for f in failures[:40]: print('  ' + f)
    sys.exit(1)
print('OK castle door leaves: none batching-static, none buried in a wall, all extend +X from the hinge, collider = oak slab')
