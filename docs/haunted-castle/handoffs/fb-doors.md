# fb-doors: castle door leaves (owner feedback items 3, 4, 5)

Branch `hc/fb-doors`, worktree `.wt/doors`. Model: Sonnet 5.5. Date 2026-10-15. No Unity launch (machine load average 83, LOOP rule 3: builders do not launch Unity), so nothing here is seen in an engine yet: the integrator must rebake and capture (recipe below).

## Diagnosis (item 3: Great Hall doors invisible until E)

Cause: **the door leaf art lands on the wrong side of its hinge, inside the wall.** Every older `Resources/Castle/*.fbx` is exported with Blender `axis_forward=-Z, axis_up=Y`, which Unity imports as Blender (x, y, z) -> Unity (-x, z, -y) (documented in `handoffs/art-exterior2.md` section 4: "every other Resources/Castle FBX is exported with the old assumption"). `GreatDoorLeaf` etc. are authored to extend +X from the hinge (file vertices x 0..1.5), so in Unity they extend to -X from the hinge. `CastleWorld.BuildDoor` placed them with identity rotation, while the collision panel (a cube at +X) and `Door.cs`'s swing probe both assume +X. Result:

- Closed: the leaf sits 1.5 m to the side of the opening, buried in the wall next to the doorway (Hall great doors left leaf: x -3.0..-1.5 at z 14.2..14.44, wall slab x -6..-1.5). The doorway shows only the invisible panel collider: "completely invisible, then an invisible wall". The leaf only becomes visible when it swings out of the wall after E.
- Open: the visible leaf swings on the mirrored side of the (correct) collision panel and probe: "the door opens into the wall", and the leaf and panel disagree.

It was not batching-static (ruled out): every `Door` hinge descendant in the baked `Castle.unity` has `m_StaticEditorFlags: 0` (PersistLayoutAssets zeroes them for anything under a `Door`), the art is not a missing-art placeholder, the renderer is enabled, no `Wall` box fills the opening (verified from the baked scene).

Evidence (static, from the baked scene `Scenes/Castle.unity` at e80fa28 and the FBX vertices): `python3 tools/check_castle_doors.py` FAILS on all 23 hinges (leaf centre x -0.75 / -0.45 in the hinge frame, 37-76 % of the leaf volume inside "Wall" boxes for 18 of them; the other 5 sit beside the wall, still on the wrong side), and `--simulate-fix` (applies the BuildDoor rule) PASSES with 0 % overlap.

## What changed

| File | Change | Item |
|---|---|---|
| `Runtime/Castle/CastleWorld.cs` `BuildDoor` | after `CastleArt.Place` of the leaf, measure its meshes' centre in the hinge frame (`ArtLocalCentre`); if it is on the -X side, turn the leaf 180 deg about the hinge (a re-exported leaf that already extends +X is left alone). Logs `HC_DOOR_LEAF_AXIS name= art= centre= turned= now=` for every door. Applies to all kinds (Standard `DoorLeaf`, Great `GreatDoorLeaf` x2, Service `ServiceDoorLeaf`, `PortraitDoorLeaf`). The portrait canvas now faces the portrait hall (it faced the closet before). | 3, 4 |
| `Runtime/Castle/CastleWorld.cs` `LogDoorLeaves(root, stage)` | new public static: one line per door `HC_DOOR_LEAF name= stage= bounds= inWall= wallOverlap= static= batched=` (closed leaf renderer bounds vs the "Wall" box colliders; inWall = more than half inside; static = any leaf renderer `isStatic`) plus `HC_DOOR_LEAF_SUMMARY ... problems=`. | 3 |
| `Runtime/World.cs` (piece 0 file, 1 line) | calls it after the hinge-to-wall-face placement loop in `BuildCastle` (stage "hinges placed", the final pose). | 3 |
| `Editor/NativeBuild.cs` (piece 0 file, 1 line) | calls it in `Prepare` after `PersistLayoutAssets` (stage "persisted", final static flags). | 3 |
| `Runtime/Door.cs` | `ChooseSwing`: open away from the player whenever that side has >= 85 deg clear, else the widest clear arc; the `transform.position.x<0` gate is gone. Works for `Toggle()` and when something else sets `Open` (the hunter): `Update` detects the change and chooses the same way. `HC_DOOR_STATE` log kept (now with `playerSide=`). | 4 |
| `Runtime/Door.cs` | never push the player: while a leaf moves, for `SettleSeconds` (0.5 s) after, and while the CharacterController still overlaps a leaf collider, `Physics.IgnoreCollision(leaf collider, player CharacterController)` is asserted every frame (re-asserted because `Investigator.Teleport` toggles the controller). Collision returns once the player stands clear, so a closed door still stops you and an open leaf is solid. `Door.IgnoringPlayer` exposes the state. The Count / navigation do not use these colliders as a body; `Open`, `OpenAngle`, `HasClearOpenPose`, `MeasureSwing` and the validators are unchanged. `Investigator.Teleport` is not involved in any door path (grep: only hiding, checkpoint restore, fall reset and scripted drivers call it). | 5 |
| `Runtime/Interactable.cs`, `Runtime/Interface.cs` (piece 0 file, 1 expression) | `Interactable.Prompt` = "Open <label>" / "Close <label>" for doors, the label otherwise; the HUD interaction line now uses `Focus.Prompt` (reads `[E] Open Hall great doors`). `Label` is unchanged because `PlaytestDriver`/`CaptureMode` look interactables up by it. The focus ray is 2.7 m (`Investigator.Update`), the panel collider is 0.09 m thick and 1.5 m wide per leaf, so the prompt shows from 2.7 m, well before the bump (>= 1.5 m required). | 3 |
| `Runtime/Castle/CaptureMode.cs` | new `-hc-capture-art doors` set: `hall_doors_closed` (eye 0,1.64,11 -> 0,2.2,14, courtyard side, 3 m back), `hall_doors_closed_inside` (eye z 17, hall side), `hall_doors_open`, `hall_doors_open_inside` (both Hall great doors opened first, 3 s settle, closed after). Logs `doors <view> <door> open= openAngle= leafRot= ignoringPlayer=`. | verify |
| `tools/check_castle_doors.py` | new static check, see below. | 3 |

## Static check

`python3 tools/check_castle_doors.py [scene] [--simulate-fix] [-v]` reads the baked castle scene and the FBX vertices (Unity import mapping above) and fails when any door-hinge descendant is batching-static (flag bit 4), when a closed leaf is more than 20 % inside "Wall" boxes, when a leaf's centre is on the -X side of its hinge, or when a hinge has no leaf. **It fails on the current committed scene (e80fa28) until the castle is re-prepared with this branch**; `--simulate-fix` shows the expected result. After the integrator's re-prepare it must print `OK castle door leaves ...`; add it to the castle post-bake checks next to `check_throwables_scene.py`.

Other checks on this branch: `check_source.py` PASS (1132 assertions, the one known CS0649 family of warnings), `check_castle_scares.py` PASS, `check_audio_keys.py` OK, `check_throwables_scene.py` OK (unchanged; it still reports the drawbridge parts not batched).

## Integrator: what to run

1. Re-prepare and re-bake Castle (leaf rotation is baked into the scene; no re-export of the FBX is needed). In the prepare log expect 23 `HC_DOOR_LEAF_AXIS ... turned=True now=(+x)` lines, and `HC_DOOR_LEAF_SUMMARY stage=hinges placed doors=23 problems=0` and `stage=persisted ... problems=0`; every `HC_DOOR_LEAF` line `inWall=False wallOverlap=0.00 static=False batched=False`. `HC_CASTLE_DOOR` / `HC_DOOR_CLEARANCE` validator lines must be unchanged (the panel and probes did not move).
2. `python3 tools/check_castle_doors.py` must print OK on the re-baked `Scenes/Castle.unity`; `python3 tools/check_throwables_scene.py` still exit 0.
3. Build (no extra bake for code-only changes) and capture through the slot, windowed, background (`open -g`), one instance:
   `~/.cache/gpu-slot/bin/gpu_slot.sh capture --label fb-doors -- open -g -W -n -a "/Users/midir/hollow-current-haunted-castle/Builds/macOS/Hollow Current.app" --args -hc-capture <outdir> -hc-capture-art doors -hc-capture-size 1920x1080 -logFile <outdir>/doors.log` (the pattern of the r13 critic brief; the capture mode is scripted, it never reads the mouse or keyboard; windowed, background). Read `doors.log` for the `doors <view> ...` lines.
   Expected frames `art_doors_hall_doors_closed.png`: two oak leaves with iron straps and rings fill the 3 x 4.5 m opening from 3 m back (the previous build shows the empty opening and the wall); `_open`: both leaves swung 85 deg or more away from the camera, parked beside the opening on the far side, the leaf visible on the same side as its collision (`doors ... leafRot=` log); `_inside` pair is the same from the hall.
4. Live feel (Hari or a scripted walk): walk up to the Hall great doors from the courtyard: leaves visible from the gatehouse onward, `[E] Open Hall great doors` appears at about 2.7 m, E opens them away from you, you are never pushed even when standing in the arc, and when you stand in a closing door's path it stops pushing you (it passes through; `ignoringPlayer=True` until you step clear).

## Risks and notes

- Visual leaf thickness is 0.24 m (great) / 0.19 m (standard), the collision panel 0.09 m: a leaf parked flat against a wall shows a few cm of art through the wall face. The old (mirrored) state hid this; if a critic notices, thin the leaf art in `tools/castle_architecture.py` (`DOOR_LEAF_SPECS`) rather than the panel (the panel feeds the swing probe).
- The runtime rule means a future FBX re-export that fixes the axis (leaf extending +X in Unity) needs no code change: the leaf is then left unturned (`turned=False` in the log).
- Other pre-axis-fix kit pieces still land turned 180 deg about Y (front = -Z). Symmetric pieces are unaffected; asymmetric ones (portrait frames, windows with a front face) were not audited here.
- `HC_DOOR_LEAF` uses the "Wall" box colliders only; the 0.125 m veneer tiles (`Castle / Wall_2m`) are not counted, so a leaf touching only veneer would read `inWall=False`.
- The hunter (`Presence`) opens doors by setting `Open=true`; the swing is now chosen relative to the player's side in that case too (previously the widest arc). The Count is not blocked by the leaf colliders being ignored (the ignore pair is leaf vs the player's CharacterController only).
- `Interface.cs`, `World.cs`, `NativeBuild.cs` are piece-0 files: each edit is one line, listed above, so the integrator can resolve conflicts by re-applying them (`use+p.Focus.Prompt+`, `CastleWorld.LogDoorLeaves(Root,"hinges placed")`, `CastleWorld.LogDoorLeaves(authored.Root,"persisted")`).
