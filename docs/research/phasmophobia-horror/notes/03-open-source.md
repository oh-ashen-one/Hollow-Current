# Open-Source Horror Games on GitHub — Research Notes

Research date: 2026-10-13. Star counts are as of this date. Slice goal: find open-source horror games / horror-mechanic implementations worth studying for a POV ghost-exploration game inspired by Phasmophobia.

---

## Tier 1 — Must-study for a Phasmophobia-style game

### 1. azsry/phasmophobia_mechanics — 161★
- URL: https://github.com/azsry/phasmophobia_mechanics (default branch `stable`; latest writeup at `versions/stale/README.md`)
- Not a game — a reverse-engineered documentation of Phasmophobia's actual internal mechanics, derived from analyzing the shipped C#/Unity code (decompiled). This is the single most valuable "architecture spec" for a ghost-hunting clone. Key systems documented with real numbers:
  - **Ghost phases (state machine)**: `in truck → setup → idle → wander → ghost appearing → hunting`. Idle timer 2–6s; on expiry a 1-in-2 roll attempts a hunt gated by average team sanity + hunting multiplier (sanity 25–50 → 1-in-6 hunt chance; ≤25 → 1-in-4).
  - **Ghost activity stat**: starts at 0, +10–25 on recognized player phrase, +20–30 if smudge stick used within 6m, decays by `deltaTime/2` per frame. A random roll `0..randomAbilityValue` (100/115/130 by difficulty, × ghost-type multipliers, +15% solo) vs. activity level gates interactions/appearances.
  - **Sanity**: drains per frame `(dt * mapDropRate * difficultyRate)` — small map 0.12, medium 0.08, large 0.05; difficulty multiplier 1.0/1.5/2.0; halved solo. No drain in light or outside. Cannot drop below 50 during setup. Truck display adds `Random.Range(-2,3)` noise. Sanity pills +40%. Visible ghost/hunting within 10m drains extra 0.2/s (Phantom 0.4).
  - **Hunting phase**: 25/35/50s by difficulty. Targets nearest visible player via **raycasts to player head position** (crouching behind objects breaks LOS — one ray while chasing, two rays 0.167m/0.01m offset otherwise). Hunt start locks all doors (10–20s), flickers fusebox + all flashlights. Crucifix within 3m (5m Banshee) cancels. After a kill: teleport to favourite room, no hunt for 25s. Phantoms visible every 1–2s, others 0.3–1s (strobe effect).
  - **Ghost-type powers** as data-driven specials: Banshee stalks one target (navigates near them, hunts if LOS during approach); Jinn needs fusebox on, 2× navmesh speed beyond 4m, ability drains 25% sanity; Wraith teleports within 3m of a random player, no salt footsteps; Poltergeist throws ALL props at once; Phantom walks to a random player; Revenant 1.5× slower unchasing, 2× faster chasing; Shade won't hunt with 2+ players in room; Mare hunt chance −10 with lights on / +10 dark; Demon +15 hunt multiplier.
  - **Voice/mic reactivity**: ghosts listen continuously, not just on push-to-talk. Saying the ghost's name (first name suffices) angers it with a 20s reaction cooldown; generic reactions +10–25 activity, 1-in-200 chance to kill fusebox, 1-in-8 to interact with a prop or door. Shy ghosts won't respond with multiple players in the room. Spirit box: only within 3m, same floor, lights off in room, 10s response cooldown (older versions).
  - **Fear-denial-of-information details**: footsteps at 5% volume outside hunt/appearing phases; ghost can appear as shadow-only to alive players (dead players see full model); sanity <50 makes window-apparition events possible every 10–30s.
  - **Evidence rules**: thermometer has ±2°C noise; EMF L2 (interaction), L3 (throw), L4 (appearance), L5 = 1-in-4 upgrade if ghost has EMF5 trait; fingerprints only on one side of touched doors; ghost orbs only in favourite room.
- Why study it: gives exact tunable numbers and the overall control-flow shape (state machine + activity stat + sanity gate + per-type power table) rather than guesses.

### 2. FrictionalGames/AmnesiaTheDarkDescent — 3,678★ (GPL v3)
- URL: https://github.com/FrictionalGames/AmnesiaTheDarkDescent
- Full C++ source of the most influential first-person horror game of its era (HPL2 engine). Directly readable canonical implementations:
  - **Sanity system**: `amnesia/src/game/LuxInsanityHandler.cpp` / `.h` — a data-driven *insanity event* system: `iLuxInstanityEvent` base class, each event XML-loaded with a `MaxSanity` threshold; time between events scales with current sanity. Events include bugs/vision distortion etc. Sanity restored by `cLuxItemType_Sanity` items (`LuxItemType.cpp`) and standing near lamps (`LuxProp_Lamp.cpp` computes sanity gain scaled by current sanity: less sanity → bigger regain from light).
  - Related: `LuxPlayer` sanity getters, inventory sanity UI (`LuxInventory.h`).
- Sister repos: AmnesiaAMachineForPigs (1,459★, https://github.com/FrictionalGames/AmnesiaAMachineForPigs), PenumbraOverture (565★, HPL1 engine 779★, https://github.com/FrictionalGames/HPL1Engine).
- Why study it: sanity as a *scheduled psychological-event scheduler*, not just a number — the UX of gradual unreliability. Also physics-based door/prop interaction (grab-and-drag) that became horror-genre standard.

### 3. Regalis11/scpcb (SCP – Containment Breach) — 539★ (CC BY-SA 3.0)
- URL: https://github.com/Regalis11/scpcb
- Complete Blitz3D source of SCP:CB. Key mechanics findable in code:
  - **Blink meter**: `BlinkMeterIMG` loaded in `Menu.bb`, drawn in HUD (`Update.bb`); the blink bar forces periodic blinks, and SCP-173 only moves while unobserved/blinking — the classic "don't look away, but you must blink" tension loop.
  - Procedural facility map generation (room-grid based); the README itself jokes "the source code is perhaps more horrifying than the game itself" (spaghetti Blitz3D — study the *mechanics*, not the architecture).
- Ports worth more for modern architecture:
  - Jabka666/scpcb-ue-my — 86★, Ultimate Edition (actively maintained fork, Blitz3D NG): https://github.com/Jabka666/scpcb-ue-my
  - SCP-Godot/SCPCB — 12★, Godot 4 reimagining with blinking mechanic on the roadmap (project discontinued, points to SCP:CB Reborn): https://github.com/SCP-Godot/SCPCB
  - RadioArtz/scp-project-unreal — 20★, UE recreation: https://github.com/RadioArtz/scp-project-unreal
- Why study it: blink/observation mechanic is directly adaptable to ghosts that only move/act when unobserved (Weeping-Angel-style hunts).

---

## Tier 2 — Engine templates & frameworks (good starting architecture)

### 4. CrookedSmileStudios/ultimate_immersive_fps_horror — 57★ (Godot)
- URL: https://github.com/CrookedSmileStudios/ultimate_immersive_fps_horror
- Amnesia-style immersive FPS controller in Godot 4, built as a YouTube tutorial series (playlist linked in README). **Deprecated** in favor of https://github.com/CrookedSmileStudios/godot-immersive-first-person-toolkit-dev — use the successor.

### 5. ThiDiamondDev/horror-fps-template — 44★ (Godot 4.3)
- URL: https://github.com/ThiDiamondDev/horror-fps-template
- Inventory + interaction system aimed at horror: collectible items, crafting recipes, mouse-drag doors/drawers with locks, and a **camcorder with night vision and rechargeable batteries** — directly relevant to POV ghost-hunting equipment design. Inheritable scene structure (`CollectibleItem.tscn` + `on_collect()` override pattern).

### 6. Oplexitie/Cupcakes-Framework — 88★ (Godot 4, MIT)
- URL: https://github.com/Oplexitie/Cupcakes-Framework
- FNAF framework: office with mouse-proximity scrolling, equirectangular perspective shader, expandable camera system, and **character AI faithful to FNAF: per-character difficulty 0–20 (Custom Night), movement-opportunity rolls, custom states per camera pose**. FNAF's AI (every ~5s, roll 1–20 ≤ AI level → advance) is the simplest proven "stalker AI" pacing model — trivially portable to a ghost's approach stages. (No jumpscares included, deliberately.)
- Related: MotoLegacy/OpenFNaF — 72★, MIT, C re-implementation of FNAF 1: https://github.com/MotoLegacy/OpenFNaF

### 7. MsVElsina/HEReloaded — 44★ (Unreal)
- URL: https://github.com/MsVElsina/HEReloaded
- Modified version of the free "Horror Engine" UE asset (blueprint framework for horror: interaction, inventory, flashlight, scare triggers). SouhaibBenchaker/ALS_HorrorEngine (3★) merges it with Advanced Locomotion System.

### 8. TyBaynesSNHU/PhasmophobiaClone — 0★ (Unreal Engine 5, C++)
- URL: https://github.com/TyBaynesSNHU/PhasmophobiaClone
- "A clone of common Phasmophobia gameplay elements to learn from." Source tree shows it's built on Epic's official UE5 `Variant_Horror` template (`Source/PhasmophobiaClone/Variant_Horror/HorrorCharacter|GameMode|PlayerController|UI`) — i.e., Epic's own horror-feature variant sample is the base. Small/new repo (created Oct 2025) but shows the current blessed UE path for horror.

---

## Tier 3 — Adjacent references

- **andrewRCr/SurvivalHorrorProject** — 41★, UE5 survival horror capstone: nine environmental puzzles, 3rd-person combat with per-limb hit reactions, map + inventory systems. https://github.com/andrewRCr/SurvivalHorrorProject
- **pablostanley/vackrooms** — 21★, endless first-person backrooms presented as a *damaged VHS recording* (Next.js, Three.js, Rapier, WebGPU). The found-footage/camcorder post-processing is exactly the POV aesthetic slice. https://github.com/pablostanley/vackrooms
- **dabardibid/Budget-Backrooms** — 33★, UE4 singleplayer backrooms, liminal-space procedural wanderability. https://github.com/dabardibid/Budget-Backrooms
- **tybayn/phasmo-cheat-sheet** — 190★, webapp guide to Phasmophobia — useful as a structured data source (ghost types, evidence tables, timers) even though it's not a game. https://github.com/tybayn/phasmo-cheat-sheet
- **fiskybusiness/phasmophobia** — 27★, modded Assembly-CSharp.dll + config for Phasmophobia (multiplayer room sizes, multiple simultaneous ghosts) — evidence of how the game's lobby/ghost-count logic behaves when pushed. https://github.com/fiskybusiness/phasmophobia
- **cnnrde/phasmoeditor** — 29★, save-file editor — documents Phasmo save format (inventory/progression). https://github.com/cnnrde/phasmoeditor
- **ObsidianBlk/ThePact** — 1★, top-down ghost-hunting game (Godot Wild Jam #52) — small, readable, complete game loop. https://github.com/ObsidianBlk/ThePact
- **SteveSmith16384/AlienIsolationRemake** — 7★, early Godot sci-fi horror FPS. https://github.com/SteveSmith16384/AlienIsolationRemake
- **TheJulianAlva/project-rxj** — 2★, retro 3D survival-horror engine from scratch (Python/OpenGL): tank controls, **fixed cameras**, modular state-driven architecture — for classic RE-style camera work. https://github.com/TheJulianAlva/project-rxj
- **AshfordFamily/recvx-decomp** — 273★, completed decompilation of Resident Evil Code: Veronica (PS2) — canonical survival-horror codebase if fixed-camera/inventory design is ever wanted. https://github.com/AshfordFamily/recvx-decomp
- FNAF web/js remakes for quick AI reading: souwell/Five-Nights-at-Freddys-Web (168★), Slynchy/FNAF-JS (48★), irv77/hd_fnaf (127★).
- Note: searches for "sound reactive enemy", "microphone enemy AI", "sanity meter" turned up no significant dedicated repos — sound-reactive monsters (Lethal Company Eyeless Dog style) are not well covered by open source; the closest open references are Phasmophobia's voice-recognition reactions documented in azsry/phasmophobia_mechanics. Lethal Company clones found are tiny student projects (erikSaandy/Unearthly_Catastrophe in s&box, 1★).

---

## Cross-cutting architecture lessons for the ghost-hunting game

1. **Ghost = finite state machine + a decaying "activity" stat + sanity gate.** Idle/wander/favourite-room/appear/hunt states; per-frame decay keeps the ghost calm unless provoked; player voice and tools (smudge) push the activity stat; hunt probability is a dice roll gated by average sanity. All numbers available in azsry/phasmophobia_mechanics.
2. **Sanity is the master pacing dial, and should schedule events, not just tick down.** Amnesia's `LuxInsanityHandler` shows the pattern: a pool of psychological events each with a max-sanity threshold and sanity-scaled frequency. Phasmo adds: no drain in light/outside, drain noise on the displayed value, low sanity enables apparition events.
3. **Hunt = information flicker.** Strobe visibility (visible 0.3–1s intervals), flashlight/fusebox flicker, locked doors, footsteps at full volume vs. 5% when passive. Fear comes from degraded perception, not from the monster model.
4. **LOS raycasts to the head make hiding legible.** Crouch-behind-furniture works because the ghost raycasts at head height; two narrow offset rays when searching. Cheap and creates fair stealth.
5. **Mic reactivity is a differentiator with cheap implementations.** Phasmo listens continuously; name-saying has a cooldown and raises activity; shy ghosts check player count in the room. Even simple volume-threshold triggers (sound-reactive ghost) capture much of this.
6. **Per-ghost-type powers as a data table.** Banshee single-target fixation, Jinn speed/fusebox coupling, Wraith teleport, Poltergeist mass-throw, Phantom wander-to-player — one ability hook per type turns one ghost AI into a roster.
7. **Blink/observation mechanic (SCP-CB) is a proven second axis** for hunts: monster moves only when unobserved; a forced blink meter guarantees vulnerability windows.
8. **FNAF AI is the minimal stalker loop**: difficulty 0–20 per character, fixed movement-opportunity interval, roll ≤ level advances the monster one stage toward the player; jumpscare on arrival. Readable, tunable per-difficulty, and easy to layer under the state machine in (1).
9. **Interaction feel matters as much as AI**: mouse-drag doors/drawers (ThiDiamondDev template, Amnesia lineage), camcorder/night-vision as both tool and aesthetic (vackrooms' VHS pipeline for POV look).

## Gaps / caveats
- No high-quality open-source Phasmophobia *clone* with real traction exists; clones found are near-zero-star student projects. The closest thing to a spec is the mechanics documentation repo (Tier 1 #1).
- Phasmophobia itself is closed source; azsry's repo is reverse-engineered documentation, and its `stale` folder is explicitly out of date vs. current patches (game mechanics have changed in later versions — treat numbers as 2021-era).
- Amnesia/Penumbra sources are GPL v3 — read for learning, don't copy code into a proprietary project. SCP:CB is CC BY-SA 3.0 (share-alike applies). Cupcakes-Framework and OpenFNaF are MIT.
