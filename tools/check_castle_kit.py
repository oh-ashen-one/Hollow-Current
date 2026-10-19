#!/usr/bin/env python3
"""Static check (no Unity): the old castle kit's FBX axis (fc-fbx).

The pre-axis-fix kit (tools/castle_lib.py export without castle_gothic.turn_to_unity) imports turned 180 degrees about Y: Blender (x, y, z)
lands on Unity (-x, z, -y), so an asset's authored front (+Y) faces -Z at yaw 0 while CastleLayout's contract is "front = +Z at yaw 0"
(docs/haunted-castle/handoffs/art-exterior2.md section 4, fb-doors.md, fc-fbx.md). Runtime/Castle/CastleKitAxis.cs holds the per-asset
policy table (parsed here, one row per line) and writes HC_KIT_AXIS lines at prepare time.

usage:
  check_castle_kit.py <prepare-or-validate.log>          parse HC_KIT_AXIS / HC_KIT_AXIS_SUMMARY (fc-light2's HC_CANDLES: floating must be 0; fc-art2's HC_TELESCOPE: aim=window clear=True); fail on problems > 0, on any ok=False line,
                                                         on a missing summary, or when a Turn piece is not turned
  check_castle_kit.py [Assets/.../Castle.unity] [--simulate-fix] [-v]
                                                         audit a baked scene the same way CastleKitAxis.Log does (walls = the "Wall" boxes,
                                                         piece bounds = the imported FBX bounds from the table). --simulate-fix turns every
                                                         Policy.Turn piece the way CastleArt.Place now does, to show what a re-prepare gives.
Exit 1 on any problem.
"""
import math, re, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TABLE = ROOT / 'Assets/HollowCurrent/Runtime/Castle/CastleKitAxis.cs'
args = [a for a in sys.argv[1:] if not a.startswith('-')]
SIM = '--simulate-fix' in sys.argv
VERBOSE = '-v' in sys.argv
MAX_BACK_GAP, MIN_FRONT_CLEAR, MAX_WALL_OVERLAP = .6, .3, .05
MUST_SEE = ('Throne', 'Bookshelf', 'HighBackChair', 'Fireplace', 'CoffinNiche', 'TapestryPanel', 'Portrait', 'GreatDoorLeaf')

def table():
    src = TABLE.read_text()
    rows = {}
    for m in re.finditer(r'\{ "(\w+)", new Entry\(Policy\.(\w+), Check\.(\w+), (-?1), ([^)]*?), (true|false), "([^"]*)"\) \}', src):
        nums = [float(v.strip().rstrip('f')) for v in m.group(5).split(',')]
        rows[m.group(1)] = dict(policy=m.group(2), check=m.group(3), front=int(m.group(4)), b=nums, aggregate=m.group(6) == 'true', why=m.group(7))
    for k in ('MaxBackGap', 'MinFrontClear', 'MaxWallOverlap'):
        mm = re.search(k + r' = (-?[\d.]+)f', src)
        if not mm: raise SystemExit('CastleKitAxis.cs: constant %s not found' % k)
    if len(rows) < 60: raise SystemExit('CastleKitAxis.cs: parsed only %d kit rows' % len(rows))
    return rows

KIT = table()

# ---- log mode ------------------------------------------------------------------------------------------------------------------------
def check_log(path):
    text = Path(path).read_text(errors='replace')
    lines = [l for l in text.splitlines() if 'HC_KIT_AXIS ' in l]
    summaries = re.findall(r'HC_KIT_AXIS_SUMMARY instances=(\d+) aggregated=(\d+) turned=(\d+) wallBacked=(\d+) problems=(\d+)', text)
    failures = []
    if not summaries: failures.append('no HC_KIT_AXIS_SUMMARY line (log from a build without fc-fbx, or the castle was not prepared)')
    bad, seen, unturned = [], {}, []
    for l in lines:
        kv = dict(re.findall(r'(\w+)=(\S+)', l[l.index('HC_KIT_AXIS '):]))
        art = kv.get('art', '?'); seen[art] = seen.get(art, 0) + 1
        if kv.get('ok') != 'True': bad.append(l.strip())
        if kv.get('policy') == 'Turn' and kv.get('turned') != 'True': unturned.append(l.strip())
        if VERBOSE: print(l.strip()[l.index('HC_KIT_AXIS '):])
    for s in summaries:
        inst, agg, turned, wb, problems = map(int, s)
        print('HC_KIT_AXIS_SUMMARY instances=%d aggregated=%d turned=%d wallBacked=%d problems=%d' % (inst, agg, turned, wb, problems))
        if problems: failures.append('summary problems=%d' % problems)
    if bad: failures.append('%d HC_KIT_AXIS lines with ok!=True' % len(bad))
    if unturned: failures.append('%d Policy.Turn pieces not turned (CastleArt.Place did not wrap them)' % len(unturned))
    if summaries:
        missing = [a for a in MUST_SEE if a not in seen]
        if missing: failures.append('no per-instance HC_KIT_AXIS line for ' + ', '.join(missing) + ' (per-instance lines are written in the editor / -hc-verify)')
    # fc-light2: every chapel candle taper stands in a wall sconce (CastleWorld.LogCandles). Logs from before fc-light2 have no line.
    candles = re.findall(r'HC_CANDLES placed=(\d+) sconced=(\d+) snapped=(\d+) removed=(\d+) floating=(\d+)', text)
    if not candles and '--allow-no-candles' not in sys.argv: failures.append('no HC_CANDLES line (log from before fc-light2; pass --allow-no-candles for old logs)')
    for c in candles:
        placed, sconced, snapped, removed, floating = map(int, c)
        print('HC_CANDLES placed=%d sconced=%d snapped=%d removed=%d floating=%d' % (placed, sconced, snapped, removed, floating))
        if floating: failures.append('HC_CANDLES floating=%d (a candle taper with nothing holding it)' % floating)
        if sconced + snapped != placed: failures.append('HC_CANDLES sconced+snapped=%d != placed=%d' % (sconced + snapped, placed))
    # fc-art2: the observatory telescope looks out of a window with its footprint clear (CastleWorld.LogTelescope). Logs from before fc-art2 have no line.
    scopes = re.findall(r'HC_TELESCOPE aim=(\w+) clear=(\w+)', text)
    if not scopes and '--allow-no-art2' not in sys.argv: failures.append('no HC_TELESCOPE line (log from before fc-art2; pass --allow-no-art2 for old logs)')
    for aim, clear in scopes:
        print('HC_TELESCOPE aim=%s clear=%s' % (aim, clear))
        if aim != 'window' or clear != 'True': failures.append('HC_TELESCOPE aim=%s clear=%s (the tube must look out of a window with its footprint clear)' % (aim, clear))
    print('HC_KIT_AXIS lines', len(lines), 'assets', len(seen))
    for l in (bad + unturned)[:40]: print('  ' + l[l.index('HC_KIT_AXIS '):])
    return failures

# ---- scene mode ----------------------------------------------------------------------------------------------------------------------
def check_scene(scene):
    src = scene.read_text()
    docs = re.split(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n', src, flags=re.M)
    objs = {docs[i + 1]: (int(docs[i]), docs[i + 2]) for i in range(1, len(docs), 3)}
    def nums(b, key):
        mm = re.search(key + r': \{(.*?)\}', b); return [float(p.split(': ')[1]) for p in mm.group(1).split(', ')]
    name, tr_of, go_of, children, parent, local = {}, {}, {}, {}, {}, {}
    for fid, (cls, body) in objs.items():
        if cls == 1: name[fid] = re.search(r'm_Name: (.*)', body).group(1)
        elif cls == 4:
            g = re.search(r'm_GameObject: \{fileID: (-?\d+)\}', body).group(1); go_of[fid] = g; tr_of[g] = fid
            ch = re.search(r'm_Children:\n((?:  - \{fileID: -?\d+\}\n)*)', body)
            children[fid] = re.findall(r'fileID: (-?\d+)', ch.group(1)) if ch else []
            local[fid] = (nums(body, 'm_LocalPosition'), nums(body, 'm_LocalRotation'), nums(body, 'm_LocalScale'))
    for t, cs in children.items():
        for c in cs: parent[c] = t
    def qmat(q):
        x, y, z, w = q
        return [[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)], [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
                [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]]
    def mm_(a, b): return [[sum(a[i][k] * b[k][j] for k in range(3)) for j in range(3)] for i in range(3)]
    def mv(a, v): return [sum(a[i][k] * v[k] for k in range(3)) for i in range(3)]
    def tr(a): return [[a[j][i] for j in range(3)] for i in range(3)]
    def localm(t):
        p, q, s = local[t]; R = qmat(q); return [[R[i][j] * s[j] for j in range(3)] for i in range(3)], p
    def world(t):
        M, p = localm(t)
        if t in parent:
            PM, PP = world(parent[t]); return mm_(PM, M), [PP[i] + mv(PM, p)[i] for i in range(3)]
        return M, p
    R180 = [[-1, 0, 0], [0, 1, 0], [0, 0, -1]]
    walls = []
    for g, n in name.items():
        if n == 'Wall' and g in tr_of:
            M, P = world(tr_of[g])
            c = [[P[i] + mv(M, [sx * .5, sy * .5, sz * .5])[i] for i in range(3)] for sx in (-1, 1) for sy in (-1, 1) for sz in (-1, 1)]
            walls.append(([min(v[i] for v in c) for i in range(3)], [max(v[i] for v in c) for i in range(3)]))
    def ray(o, d, maxd=30.):
        best = maxd
        for lo, hi in walls:
            t0, t1, hit = 0., best, True
            for i in range(3):
                if abs(d[i]) < 1e-9:
                    if o[i] < lo[i] or o[i] > hi[i]: hit = False; break
                    continue
                a, b = (lo[i] - o[i]) / d[i], (hi[i] - o[i]) / d[i]
                if a > b: a, b = b, a
                t0, t1 = max(t0, a), min(t1, b)
                if t0 > t1: hit = False; break
            if hit: best = min(best, t0)
        return best
    def overlap(lo, hi):
        vol = max(1e-9, (hi[0] - lo[0]) * (hi[1] - lo[1]) * (hi[2] - lo[2])); s = 0
        for b in walls:
            d = [min(hi[i], b[1][i]) - max(lo[i], b[0][i]) for i in range(3)]
            if all(x > 0 for x in d): s += d[0] * d[1] * d[2]
        return min(1., s / vol)
    def flat(v):
        l = math.hypot(v[0], v[2]); return [v[0] / l, 0., v[2] / l] if l > 1e-9 else [0., 0., 1.]
    def V(v): return '(%.2f,%.2f,%.2f)' % tuple(v)
    castle_root = next((tr_of[g] for g, n in name.items() if n.startswith('Haunted Castle / ') and g in tr_of and tr_of[g] not in parent), None)
    rows, aggregated, index = [], {}, {}
    n_inst = turned_n = wb_n = problems = agg_n = 0
    for g, n in sorted(name.items(), key=lambda kv: kv[1]):
        if not n.startswith('Castle / ') or g not in tr_of: continue
        art = n[len('Castle / '):]
        e = KIT.get(art)
        if not e: continue
        t = tr_of[g]
        if e['aggregate']: aggregated[art] = aggregated.get(art, 0) + 1; agg_n += 1; continue
        index[art] = index.get(art, 0) + 1
        M, P = world(t)
        model = next((c for c in children.get(t, []) if name.get(go_of[c]) == 'FBX / ' + art), None)
        is_turned = False
        if model is not None:
            q = local[model][1]; is_turned = abs(abs(q[1]) - 1) < 1e-3
        simulated = SIM and not is_turned and e['policy'] == 'Turn'
        MM = mm_(M, R180) if (is_turned or simulated) else M
        x0, x1, y0, y1, z0, z1 = e['b']
        corners = [[P[i] + mv(MM, [x, y, z])[i] for i in range(3)] for x in (x0, x1) for y in (y0, y1) for z in (z0, z1)]
        lo = [min(c[i] for c in corners) for i in range(3)]; hi = [max(c[i] for c in corners) for i in range(3)]
        ok, extra, expected, front = True, '', '', None
        if e['policy'] == 'Hinged':
            h = parent.get(t)
            LM, LP = localm(t)
            c = [LP[i] + mv(LM, [(x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2])[i] for i in range(3)]
            ok = h is not None and c[0] > 0; expected = 'leaf_extends_+X_from_hinge'; extra = ' hingeCentre=' + V(c)
        elif e['policy'] == 'Adaptive':
            cen = [(x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2]
            if art == 'Drawbridge':
                LM, LP = localm(t); c = [LP[i] + mv(LM, cen)[i] for i in range(3)]
                ok = c[2] < 0; expected = 'deck_extends_-Z_from_hinge'; extra = ' hingeCentre=' + V(c)
            else:
                wc = [P[i] + mv(M, cen)[i] for i in range(3)]
                if castle_root is not None:
                    RM, RP = world(castle_root); wc = mv(tr(RM), [wc[i] - RP[i] for i in range(3)])
                ok = wc[2] < -.45; expected = 'chains_over_the_moat'; extra = ' centre=' + V(wc)
        else:
            front = flat(mv(MM, [0, 0, e['front']])); layout = flat(mv(M, [0, 0, 1]))
            faces = front[0] * layout[0] + front[2] * layout[2] > .7
            if e['policy'] == 'Turn':
                ok = faces; expected = 'front_to_room_back_to_wall' if e['check'] == 'WallBacked' else 'front_along_layout_yaw'
            else:
                expected = 'front_to_room_back_to_wall_as_imported' if e['check'] == 'WallBacked' else 'as_imported'
            if e['check'] == 'WallBacked':
                fext = max((c[0] - P[0]) * front[0] + (c[2] - P[2]) * front[2] for c in corners)
                bext = max(-((c[0] - P[0]) * front[0] + (c[2] - P[2]) * front[2]) for c in corners)
                back = ahead = 1e9
                for k in (.15, .5, .85):
                    o = [P[0], lo[1] + (hi[1] - lo[1]) * k, P[2]]
                    back = min(back, ray(o, [-front[0], 0, -front[2]])); ahead = min(ahead, ray(o, front))
                gap, clear, ov = back - bext, ahead - fext, overlap(lo, hi)
                ok = ok and gap <= MAX_BACK_GAP and clear >= MIN_FRONT_CLEAR and ov <= MAX_WALL_OVERLAP
                extra = ' backGap=%.2f frontClear=%.2f wallOverlap=%.2f' % (gap, clear, ov); wb_n += 1
            extra = ' facesLayout=%s' % faces + extra
        n_inst += 1; turned_n += is_turned or simulated; problems += not ok
        line = 'HC_KIT_AXIS name=%s#%d art=%s policy=%s check=%s turned=%s%s pos=%s%s bounds=%s/%s%s expected=%s ok=%s' % (
            art, index[art], art, e['policy'], e['check'], is_turned or simulated, ' (simulated)' if simulated else '', V(P),
            '' if front is None else ' front=' + V(front), V([(lo[i] + hi[i]) / 2 for i in range(3)]), V([hi[i] - lo[i] for i in range(3)]), extra, expected, ok)
        if VERBOSE or not ok: print(line)
    if VERBOSE:
        for art, c in sorted(aggregated.items()):
            print('HC_KIT_AXIS name=%s art=%s policy=%s instances=%d expected=%s ok=True' % (art, art, KIT[art]['policy'], c, 'symmetric_under_180' if KIT[art]['policy'] == 'Symmetric' else 'as_imported'))
    print('HC_KIT_AXIS_SUMMARY instances=%d aggregated=%d turned=%d wallBacked=%d problems=%d (scene %s, walls %d, simulate_fix %s)' % (
        n_inst, agg_n, turned_n, wb_n, problems, scene.name, len(walls), SIM))
    return ['%d pieces fail the axis check' % problems] if problems else []

if args and not args[0].endswith('.unity'):
    failures = check_log(args[0])
else:
    failures = check_scene(Path(args[0]) if args else ROOT / 'Assets/HollowCurrent/Scenes/Castle.unity')
if failures:
    print('FAIL ' + '; '.join(failures))
    sys.exit(1)
print('OK castle kit axis: every old-kit piece faces the way its placement intends (turned where the import was mirrored)')
