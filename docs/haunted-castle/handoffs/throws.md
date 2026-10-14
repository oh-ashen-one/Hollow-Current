# Handoff: castle prop throws + `voice_cc_contain` (branch `hc/throws`)

2026-10-14. Static work only: the owner was live-streaming, so nothing was built, run, prepared or baked. Evidence is code
reading, the committed prepared scene `Assets/HollowCurrent/Scenes/Castle.unity` (34a0de9 bake) and `.playtest-fear.log`
(fear run on build 34a0de9). `tools/check_source.py` PASS (487 rules; runtime compiled against the player and editor
assemblies; editor compiled).

## 1. Prop throws never fire: root cause

Fear run: `HC_THROWABLES director=38`, then `phase=throw thrown=False prop=none`, `phase=poltergeist flung=0`,
summary `HC_THROW=0 HC_THROW_poltergeist=0`. No `HC_THROWABLE_BOUNDS_INVALID` line, so every candidate was rejected before
the bounds rule, silently. Two independent blockers:

1. **Sight line ends inside the table's collision proxy (what rejected them in this run).** `CastleWorld.GreatHall` builds
   `LongTable collision` from the art's nominal size (`CastleLayout` LongTable 6.6 x 0.95 x 1.4, length scale 0.5), so the
   box spans y 0 to 0.95 over x -0.7..0.7, z 22.05..25.35. The 12 hall throwables stand on the real top at y 0.775 (plates at x
   +-0.5, books, goblets, candlesticks at z 23.7 +- 1.5). Their visual centres (0.79 to 0.92) are all inside that box, so
   `Physics.Linecast(eye, centre)` in both `ScareDirector.ThrowNearbyProp` and `CastleScares.Poltergeist` hits the box face
   and the candidate is skipped. The driver stood 2.2 m from a hall throwable for both calls. The dining table has the same
   shape (proxy 0.9 vs top 0.792: goblets and books inside, candlesticks clear); the reading table is 1 cm over its props.
2. **Throwables are static-batched in the prepared scene (would stop them next).** `NativeBuild.PersistLayoutAssets` marks
   every castle renderer `BatchingStatic|OccluderStatic|OccludeeStatic` unless it is under a Door, an Interactable,
   "Porcelain doll" or "Authored / Mug". Throwables were not exempt: `python3 tools/check_throwables_scene.py` reports all 38
   with mesh parts at `m_StaticEditorFlags: 22` (exit 1). A static-batched part does not move with its transform, and its
   MeshFilter reports the combined batch mesh, so even a candidate that passed the sight test would fail the bounds rule
   or fly invisibly. (Bellwether's mug is exempt by name, which is why the Bellwether throw works.)

Ruled out: no Rigidbody or collider on any throwable in the scene (so the `GetComponent<Rigidbody>()` "already thrown" gate
is open); the `Game.Map==MapId.Castle` gate is satisfied (`director=38`); `Castle` (CastleDread) exists (HC_SCARE_CASTLE
fires); the distance (6 m) and view-angle (65 degrees) gates pass at the driver's 2.2 m stand.

## Fix (commit e4f1571)

- `Editor/NativeBuild.cs`: renderers under a `Throwable` count as moving (no static flags). **Only lands after
  PrepareCastle** (re-bake); Bellwether has no Throwable, so its flags are unchanged.
- `Runtime/Castle/Throwable.cs`: `SightBlocked(from,to,out blocker)` raycasts the line and ignores a BoxCollider that
  encloses the target (the support's own proxy); `StaticBatched`; `LogSkip` / `LogSkipSummary`.
- `Runtime/ScareDirector.cs` `ThrowNearbyProp`: castle uses `SightBlocked` and refuses batched props; Bellwether keeps the
  original `Linecast` path and logs nothing new.
- `Runtime/Castle/CastleScares.cs` `Poltergeist`: same sight test and batch guard.
- New log lines (castle only): `HC_THROW_SKIP reason=far|view_angle|sight_line|static_batched|no_renderer|no_mesh|bounds|launch_refused
  source=director|poltergeist prop=... at=... d=... angle=... [detail=<blocker name@hit/length>]` for candidates within 9 m
  (thrown ones only counted), then `HC_THROW_SKIP summary source=... candidates=N <reason>=<count>...` when nothing was thrown.
- `PlaytestDriver` throw probe also prints `sight=<SightBlocked result> batched=<bool>` next to the plain `line=`.
- `tools/check_throwables_scene.py`: no-Unity check of the prepared scene's Throwable static flags (exit 0 = none batched, 38 roots).

## What a windowed run must prove (not claimed here)

1. `NativeBuild.PrepareCastle` (re-bake), then `python3 tools/check_throwables_scene.py` exits 0.
2. Build, fear run: `HC_PLAY_THROW_PROBE ... line=LongTable collision@... sight=clear batched=False`, then `HC_THROW prop=Throwable / ...`,
   `phase=throw thrown=True`, `phase=poltergeist flung=N>0` with `HC_SCARE_CASTLE poltergeist N`. Note the summary tag
   `HC_THROW_poltergeist` only counts the director's apparition path; the driver calls `Poltergeist()` directly, so its
   proof is `flung=` and `HC_SCARE_CASTLE poltergeist`.
3. Eyes on it: the prop visibly leaves the table, tumbles, knocks (ScareImpact), and does not push the investigator.
   If anything is still refused, the `HC_THROW_SKIP` lines name the reason.
4. Bellwether receipt unchanged (mug throw in NativeVerification).

## 2. `voice_cc_contain` (commit b6c21b2): produced

Referenced by `Soundscape.Speak("contain")` (castle maps `contain` to `voice_cc_contain`) from `Game` on entering
`Phase.Containment` (the bell-rope binding) and from `RadioHint` once diagnosed. Made exactly as F4 made `voice_cc_kit` /
`voice_cc_ending`: eSpeak NG 1.52.0 + ffmpeg `CASTLE_FX` via `python3 tools/generate_open_voice.py --castle cc_contain`
(new entry filter so the other Count voices are not rewritten; the pipeline reproduces the committed `voice_cc_kit.wav` byte
for byte). Line: "Ring it, then. Better hands than yours have put me to bed." It is written for the binding moment, which is
when the code plays it, not for a room trap. 22050 Hz mono, 9.06 s, peak -12.4 dBFS, -29.4 LUFS. The meta comes from
`audio_castle_sfx.ensure_meta`, with the same importer as `voice_cc_kit`. Rows added to `AUDIO.md` and `ASSET_LOG.md` (cost 0); the
existing `ASSET_LICENSES.md` line for `CastleAudio/voice_cc_*.wav` covers it. A windowed run should show no
`HC_MISSING_ART speech voice_cc_contain` and someone should listen to the line once.
