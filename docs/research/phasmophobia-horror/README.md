# What Makes Ghost-Hunting Horror Games Scary — Research Pack

Research date: 2026-10-13. Goal: distill what makes Phasmophobia-style POV ghost-exploration
games terrifying, and turn it into an actionable integration list for our own game.

**How to read this pack:**

- This file = the master list + integration guide (start here).
- `notes/01-phasmophobia.md` — deep autopsy of Phasmophobia itself (~30 sources).
- `notes/02-comparable-games.md` — Demonologist, Devour, Forewarned, Ghost Watchers, Obsideo,
  Pacify, Ghost Exorcism Inc, Lethal Company, Content Warning.
- `notes/03-open-source.md` — open-source repos worth studying, with real mechanics numbers.
- `notes/04-player-sentiment.md` — what players say scares them / bores them (Reddit + Steam).
- `notes/05-design-theory.md` — horror design theory (pacing, audio, sanity, the uncanny).

Every claim in the notes files has a source URL. The one-line formula that fell out of all
five research slices:

> **Fear = anticipation of danger you can't fully predict, in a place that feels familiar,
> while a job forces you to stay — and the scare itself is just the punctuation.**

---

## Part 1 — The Master Fear List (what makes these games scary)

Ranked roughly by how consistently each factor showed up across reviews, player threads,
design essays, and the games' own success.

### 1. Anticipation beats the scare itself
Critics and players agree the apparition/kill "isn't particularly scary" — every step *toward*
it is. Phasmophobia's genius is described as "restraint and subtlety": overwhelming darkness,
no music, ambient hum broken by bangs and whispers. Crucially, **the investigation loop is the
anticipation machine** — gathering evidence forces repeated trips back into the ghost's room
while danger escalates. (PC Gamer, Laptop Mag — notes/01)

### 2. Never let the player know they're safe
The #1 fear-killer found in player research: a reliable danger tell. "Unless the lights are
blinking, you are 100% safe... Suddenly you aren't scared anymore." Hunts gated behind a known
signal turn every other event into noise. The scariest ghosts are **rule-breakers** — Demon
(hunts during the "safe" setup phase), Myling (silent), Phantom (invisible), Deogen (always
knows where you are), Mimic (imitates other ghosts' rules). Fear = violated contracts.
(notes/04)

### 3. Emergent scares > scripted scares
The moments players retell years later felt personal and accidental: "It couldn't have been
scripted better." Demonologist's scripted-feeling scare events spike harder on first impression
but players punish them as "canned" once recognized (~10h shelf life per reviews). Emergent
scares from a simulation age indefinitely. (notes/02, 04)

### 4. Sanity as a visible doom clock
A fear resource that drains in darkness and on ghost events, gates the lethal phase, and is
only partially understood by players ("part of what makes it such an effective horror game is
its unexplained mechanics" — GameRevolution). Design lineage: Eternal Darkness (sanity warps
what you perceive), Amnesia (every fear-mitigation strategy costs sanity — hiding needs
darkness, darkness drains sanity), Phasmophobia (sanity = hunt timer). Players *ask* for low
sanity to make the game lie: false evidence, phantom sounds, unreliable tools. (notes/01, 04, 05)

### 5. Two-tier scare structure with loud telegraphs — then ambiguity
Frequent non-lethal ghost events (whispers, thrown objects, apparitions — these cost sanity)
build toward rare lethal hunts. Phasmo hunts telegraph with locked doors, strobing flashlights,
radio static, heartbeat, frozen breath — seconds of pure dread before the chase. Player demand:
**multiple, varied hunt tells** and events that *sometimes* escalate into real danger, so no
single signal means safety. (notes/01, 04)

### 6. Voice is the killer feature
The most distinctive system in the genre: the ghost hears your real microphone. Saying its name
angers it; talking during a hunt gets you found; the spirit box answers questions aloud.
Proximity chat with realistic drop-off + fragile walkie-talkies means "you can't connect with
your friends without also connecting with danger." Lethal Company proved it again: enemies that
path toward actual mic noise, and a friend's voice cutting off mid-scream *is* the scare.
Playing on Discord side-channel "makes things too easy and far less terrifying." (notes/01, 02, 05)

### 7. Sound design is the primary fear channel
Player consensus: creaks, whispers, footsteps, things in the walls. Audio restraint — long
near-silent stretches make every tiny sound leap out ("silence becomes the soundtrack"). Rare
one-off audio events are disproportionately memorable (a player heard a growled
"WHERE ARE YOUUUU?" once, ever, and still wonders if they hallucinated it). Binaural/positional
voices *behind and above* the headphone-wearing player (Hellblade technique). (notes/04, 05)

### 8. Give the player a job
Why doesn't the player just leave? Obligation. "A locked door says you have no choice. An
objective says you do have a choice — you just can't progress until you make the bad one."
Mundane routine teaches what "normal" looks like, so one changed detail terrifies. Divided
attention (more monitoring channels than you can watch) creates information deficit. Phasmo's
own lead dev: it was designed **puzzle-first, horror-second** — the horror was "the perfect fit."
(runradiant video essay, PC Gamer interview — notes/05)

### 9. Tension is a sawtooth, not a constant
Build-up → release → relief → build again. Too much build-up = boredom; too many scares =
numbing/conditioning. Practical budget (Ntokos 10-level fear spectrum): idle at levels 2–5
(unexplained traces, audio cues, objects moving), spike to 8–9 only for hunts, then guarantee a
relief lull. Comedy/jank between peaks is a *fear multiplier*, not a diluter — laughter resets
the baseline so the next scare lands harder, and generates retellable stories (Lethal Company's
explicit design thesis: "funny and scary at the same time"). (notes/01, 02, 05)

### 10. Familiar places, one wrong detail
Phasmophobia's suburban houses "look exactly like your home where you're playing." Liminal
spaces = familiar places stripped of people and purpose. "Cozy first" (Frictional): make the
player comfortable, then corrupt it one detail at a time — repetition that reframes. Players
beg for peripheral, consequence-free phenomena: shadow figures, mirror glimpses, heads peering
around corners. (notes/04, 05)

### 11. Real stakes, opt-in danger, and the exit sprint
Death costs real resources (your gear, half your pay, team sanity) but dead players become
mute spectators in a ghost realm watching friends get hunted — spectator-horror. Cursed
possessions (Ouija, summoning circle) trade sanity for information: "a loaded gun players point
at themselves." The scariest phase in Forewarned is *after* success — the breathless extraction
sprint when the map turns hostile. Devour ties monster speed/aggression directly to objective
progress. (notes/01, 02)

### 12. No safe zones, and teammates get taken in front of you
High-impact violations: the ghost leaves the house (Ghost Watchers), chases you to the van and
tries the door (Ghost Exorcism Inc), burns a crucifix *in the truck* (Phasmo). Watching the
monster grab and drag a screaming friend away is Devour's signature moment — watching a friend
get taken beats being taken. Forewarned's mimic appears as a *different teammate to each
player* — pure social paranoia. (notes/02, 04)

---

## Part 2 — Integration Guide (how to bake this into our game)

Concrete systems, in suggested build priority. Numbers from the reverse-engineered
Phasmophobia mechanics docs where available.

### A. Core loop architecture
1. **Ghost AI = state machine + decaying "activity" stat + sanity gate.** States:
   idle → wander → appear → hunt. Activity starts at 0, +10–25 when a player speaks, decays
   continuously; interactions roll against it. Hunt is a dice roll (every 2–6s) gated by
   average team sanity. This one shape produced the entire genre. (Spec: github.com/azsry/phasmophobia_mechanics — notes/03)
2. **Sanity system:** drains in darkness (rate scales with map size/difficulty, halved solo),
   costs on ghost events (-10 to -20%), -15% to everyone on a teammate death, restored
   partially by pills that cost an inventory slot. Add display noise (±2%) so players never
   trust the exact number. Below ~30%, start lying: false tool readings, phantom radio traffic,
   sounds that aren't there. Recovery must always be harder than loss.
3. **Two-tier events:** a large pool of non-lethal haunt events (whispers, object throws,
   brief apparitions, lights dying) that drain sanity; rare hunts with 2–5s multi-sensory
   telegraph (lights strobe, doors lock, heartbeat, radio static). Then break the contract:
   some events occasionally escalate, some hunts have no tell, some tells are false alarms.

### B. The ghost roster — every ghost breaks one rule
Data-driven power table, one signature rule-violation each: hunts at any sanity; silent
footsteps; invisible during hunts; always knows your location but slow; mimics other ghosts'
evidence; empowered by electronics; won't hunt a group. Never let one player strategy stay
safe across the whole roster. Add behavior that *changes within a match* (learns patrol
routes, returns to where it last saw you).

### C. Voice & audio
1. Mic always live. Volume-threshold → detection radius during hunts; recognized phrases
   (ghost's name, provocations, "let's leave") push the activity stat with a ~20s cooldown.
2. Proximity voice with sharp realistic drop-off; walkie-talkies as a fragile global channel
   that scrambles during hunts; per-room acoustic filters (echoing basement, dead carpeted
   bedroom). Demonologist/Forewarned/Lethal Company all prove this is the most praised
   immersion feature of the era.
3. No music. Sparse ambient bed, long silence. Ghost heard before seen. A library of rare
   one-off vocal events (1-in-hundreds odds) — rarity is the point.

### D. Map & lighting
Familiar domestic spaces (houses, school, prison) with a cozy baseline corrupted one detail
at a time. Darkness drains sanity; room lights protect but the fusebox is finite and the ghost
can kill it. Flashlight narrows vision to a tunnel (never make areas so dark navigation
breaks). Peripheral consequence-free phenomena: corner-of-eye shadows, mirror glimpses,
something peering then gone.

### E. Stakes & economy
Death costs the gear you brought + a cut of pay; dead players become spectators who can see
the ghost but can't warn the living (or give them a tiny, unreliable channel — spirit box
through the veil is a top player request). Cursed objects per map: sanity-for-information
gambles that can trigger hunts at any sanity if misused. Optional objectives = push-your-luck
greed dial. Endgame flip: after identification, an exorcism/escape ritual while the map is at
max hostility — "identify and leave" alone makes the endgame an anticlimax (the most common
"better than Phasmo" feature across every competitor).

### F. Co-op fear engineering
Friends dampen fear — design counterweights: objectives that force splitting up, the
van/monitor role (asymmetric fear: watch your friends' sanity drain on cameras), comms that
fail under ghost activity, per-player-only scare events, a mimic that looks like a teammate.
Keep solo mode as the horror showcase and ship hardcore dials as first-class settings
(0 evidence, low start sanity, no grace period, high event rate) — players currently hack
these together themselves.

### G. Longevity plan (fear decays — accept it and design for it)
Desensitization is unavoidable; even Phasmo's devs have a "Horror 2.0" plan because veterans
stopped being scared. The durable antidotes: rotating ghost behaviors, new maps with strong
identity, rare event variants, and behavior that resists memorization. Do NOT copy Phasmo's
actual answer (equipment tiers/grind bloat) — it's widely disliked. (notes/01, 04)

---

## Part 3 — Anti-patterns (what kills the fear)

1. **A single reliable danger tell** — the #1 killer. If "lights blinking = danger" is
   guaranteed, everything else becomes noise.
2. **Scripted/canned scare events** — big first-impression spike, then players call them
   artificial. Generate scares from simulation.
3. **Checklist-feel investigation** — when the brain engages only with "the checklist of ghost
   clues," horror mindset dies. Evidence should be partial, ambiguous, sometimes misleading.
4. **Identical kill animations / low ghost-model variety** — repeatedly cited in negative
   Steam reviews.
5. **Frustration disguised as difficulty** — snail-speed sprint, grind walls, inventory churn.
   "Not a fun gameplay mechanic, it's just annoying." Frustration ≠ fear.
6. **Audio bugs** — a broken soundscape directly breaks the horror. Audio is the fear channel;
   treat audio bugs as severity-1.
7. **Precision challenges in an imprecise game** — RNG-heavy challenge modes frustrate instead
   of frighten.
8. **Over-explaining mechanics** — under-documentation is part of the horror. Players
   deliberately avoid wikis to preserve it.

## Part 4 — What players explicitly ask for (free feature list)

From r/PhasmophobiaGame threads and Steam reviews (full citations in notes/04):

- Shadow figures, mirror glimpses, corner-peeking heads, ghosts on ceilings
- Multiple/varied hunt tells; events that sometimes convert into chases
- Sanity that distorts perception (false evidence, phantom radio, missing sounds)
- Ghost interfering with your equipment (kills your camera, throws the book)
- Choppy partial radio transmissions ("OH M— T'S HUNTI— GET TO—")
- Talking to dead teammates via spirit box
- Rare one-off events most players never see twice
- Safe-zone violations (truck/van events)
- New maps with strong identity (prison cited as scary "just from the size and emptiness")
- First-class hardcore difficulty dials

## Part 5 — Open-source repos worth studying

Full detail in notes/03. Top picks:

| Repo | Stars | Why |
|---|---|---|
| azsry/phasmophobia_mechanics | 161 | Reverse-engineered spec of Phasmo's actual mechanics with real numbers — the single best architecture reference |
| FrictionalGames/AmnesiaTheDarkDescent | 3,678 | Full C++ source; `LuxInsanityHandler.cpp` = sanity as a scheduled psychological-event system (GPL — learn, don't copy) |
| Regalis11/scpcb | 539 | SCP: Containment Breach; the blink meter — monster moves only when unobserved (CC BY-SA) |
| Oplexitie/Cupcakes-Framework | 88 | MIT Godot FNAF framework; FNAF's 0–20 difficulty + movement-opportunity roll is the simplest proven stalker AI |
| ThiDiamondDev/horror-fps-template | 44 | Godot: drag-doors, inventory, night-vision camcorder |
| tybayn/phasmo-cheat-sheet | 190 | Structured ghost/evidence data tables |

Gap worth noting: no high-quality open-source Phasmo clone exists, and no good open-source
sound-reactive-monster implementation — our mic-reactive ghost AI would be genuinely novel
open-source territory.

---

## Handoff notes for the build session

- Start from Part 2, sections A–C: ghost state machine + sanity + two-tier events + mic/audio.
  That cluster is 80% of the fear. Everything else is amplification.
- The quickest win over the entire genre: **never ship a guaranteed safety signal** (Part 3 #1)
  and **make low sanity lie to the player** (Part 2 A.2). Both are cheap, both are top player
  demands, and neither exists well in any current game.
- Read notes/03 before writing ghost AI — the state machine + activity-stat + sanity-gate
  shape with real tunable numbers is already documented there.
