# Horror Design Theory for a POV Ghost-Exploration Game (Phasmophobia-inspired)

Research compiled 2026-10-13. Every finding cites its source inline. Raw source dumps and
YouTube transcripts are kept alongside this file in `/tmp/ghost-research/` and
`/tmp/ghost-research/subs/`.

---

## 1. The master principle: tension is a cycle, not a constant

**Build-up → release → relief → build-up again. Both halves are mandatory.**

- Horror works as a cycle of tension and release: "It's about anticipation... those stretches
  where you know something terrible's about to happen but you don't know when. The build-up to
  the scare is as important as the scare itself." P.T. is the canonical example — its scares
  are unremarkable; the hallway walk makes you build the fear yourself. After the payoff, the
  player is *less* tense, so you must let tension rebuild before the next peak.
  (Extra Credits, "Shiver with Anticipation" — https://www.youtube.com/watch?v=OyiAR2BXtKU)
- Too much build-up with no release → boredom (A Machine for Pigs). Too much release →
  conditioning/numbing (Dead Space-style constant assault). Hitchcock: the scariest thing is
  the unknown; let imagination run away with the player.
  (Game Wisdom — https://game-wisdom.com/critical/tension-horror-game-design)
- Long lulls are a feature: in early Silent Hill there are often 10–15 seconds between enemy
  encounters, plus stretches of "simply walking around with nothing happening" — in horror this
  is what binds the experience together, because "the best horror lets you do the work for it."
  (Extra Credits, same video)
- A community codified checklist says the same: never hold 100% tension; use a sawtooth model
  (buildup → peak/scare → dedicated relief period) to prevent "numbing."
  (GD-Agentic-Skills horror SKILL.md — https://github.com/thedivergentai/gd-agentic-skills/blob/main/skills/godot-genre-horror/SKILL.md)
- Sustained dread across a whole game comes from *patterns*, not scenes: recurring symbols,
  repetition that re-frames earlier details, and incomplete answers where each discovery
  expands the mystery instead of resolving it. Pressure then persists even between sessions.
  (The Meridian Archives, "Why Your Horror Game Has No Tension" — https://www.youtube.com/watch?v=guNqdQypF2w)
- Ntokos (2018) "Level of Fear Spectrum" — a 10-level ladder useful as a pacing budget:
  1 Calmness (safe room) → 2 Mild Anxiety (unexplained traces) → 3 Vigilance (blood/evidence)
  → 4 Restlessness (audio cues, minor objects moving) → 5 Tense (heard but unseen footsteps,
  objects falling) → 6 Distress (witnessed violence) → 7 Fright (weak jumpscares, hiding)
  → 8 Dread (enemy close, loud jumpscares) → 9 Terror (chase) → 10 Panic (no control).
  A ghost game should spend most of its time oscillating between levels 2–5, spiking to 8–9
  during hunts. (Quoted in: Social Interaction in Horror Games thesis, Table 1 —
  https://www.diva-portal.org/smash/get/diva2:1904515/FULLTEXT01.pdf)

**Application:** treat each investigation as a sawtooth — setup phase (1–3), evidence gathering
with escalating ambient events (3–5), ghost events (6–7), hunt (9), post-hunt lull, repeat
with higher amplitude. Sanity is the ratchet that raises each successive peak.

## 2. The unknown and limited information: players scare themselves

- Frictional's core method is "mental modeling": build an unsettling space and let the
  player's imagination generate the monster. Thomas Grip on Slender: The Arrival: "It was all
  just me scaring myself — no mechanics!" The risk: mental modeling breaks down fast if nothing
  happens; it needs feeding with vague, partial stimuli. (Game Developer, "Four ways to design
  for horror" — https://www.gamedeveloper.com/design/four-ways-to-design-for-horror-from-i-amnesia-i-dev-frictional-games)
- Grip's horror-wrapping vs horror-simulation distinction: Resident Evil wraps standard combat
  in horror theming; Clock Tower / Amnesia / Outlast / Alien: Isolation *simulate* horror-film
  behavior (hide, peek, run, listen) so the player reenacts horror scenes through gameplay.
  Vague systems are essential: "if you just make things vague enough, and allow the player to
  play based on that vagueness, you can make engaging games of pure horror simulation" (Slender).
  In Alien: Isolation the alien's apparent randomness makes the mental model vague and "more
  prone to paranoid imagination." (Frictional Games blog —
  https://frictionalgames.com/2014-10-thoughts-on-alien-isolation-and-horror-simulation/)
- Limit perception to amplify guessing: Silent Hill's fog, Fatal Frame's invisible ghosts,
  Slender's darkness, P.T.'s right angles and closed doors. "If you've ever had that moment of
  tension right before you turn a corner or open a door in a horror game, this is why."
  (Extra Credits, op. cit.)
- Red herrings keep the brain from ever feeling safe: creaking doors, rustling leaves, shadows,
  mirrors, mannequins — false threats prevent the player from predicting the real one.
  (Extra Credits, op. cit.)
- Phasmophobia deliberately leaves mechanics unexplained (what sanity does, what items do) —
  reviewers call this a core part of its effectiveness. (GameRevolution —
  https://www.gamerevolution.com/guides/661898-phasmophobia-how-does-sanity-work-what-does-0-mean)
- Kinetic Games actively patched out predictable/cheesable ghost behavior because "more varied
  ghosts leads to less predictability, which in turn makes for scarier encounters."
  (ScreenRant — https://screenrant.com/phasmophobia-ghosts-scary-smart-new-update/)

**Application:** the ghost should be *mostly* inferred, rarely seen. Evidence tools give partial,
ambiguous, sometimes misleading readings. Randomized event timing (not scripted) prevents the
predictability that kills fear — see jump-scare neuroscience below.

## 3. Vulnerability: no weapons, no power fantasy

- Frictional deliberately removed weapons from Penumbra: Black Plague onward because it
  "changed the player's perception of the game's world"; Amnesia's achievement was recreating
  Silent Hill 2's closet cutscene as *gameplay*. Mechanics that increase player power destroy
  horror: weapon upgrades "might make the game more 'fun'... it will destroy the horror element."
  (Frictional blog op. cit.; Game Developer "Four ways" op. cit.)
- Design horror emotion-first, not mechanics-first: "Is this game scary?" drives every
  decision; the gameplay mechanic "is almost sort of a side thing." (Game Developer, op. cit.)
- Resident Evil 7's commercial success validated the return to "immersion, atmosphere and
  vulnerability" after the action-heavy era; modern horror's resonant themes are "isolation,
  paranoia, uncertainty, loss of control." (CoffeeCup Studios, "The Secret Reason Horror Games
  Are Finally Scary Again" — https://www.youtube.com/watch?v=G-IAc6uVqYU)
- A ghost hunter carries *measurement* tools, not weapons — this matches Phasmophobia's
  "ghost-detecting equipment but no ghost-busting gear" formula. Phasmophobia also proves that
  in a group "you're not really safer... They can't protect you." (pikmonwolf, "How
  Phasmophobia Mastered Multiplayer Horror" — https://www.youtube.com/watch?v=wxf-FW9jgKg)

## 4. Jump scares: why most fail, and how the good ones work

- A jump scare is a *release*, worthless without a build-up, and a one-time trick by design.
  Games built on nothing but jump scares (FNAF-style loops, Layers of Fear) fail because (a)
  there is no tension build between scares, (b) players learn the trigger conditions ("jump
  scares are built as conditionals... once you know the conditions, you know when to expect
  it"), and (c) conditioning makes each subsequent scare weaker. (Game Developer, "A Lack of
  Fright" — https://www.gamedeveloper.com/design/a-lack-of-fright-examining-jump-scare-horror-game-design)
- The best jump scares *change the rules going forward*: Resident Evil's dog-through-window
  worked on two levels — surprise, plus it taught players "enemies can come from outside the
  environment," so players generate their own tension in every later corridor ("the audience
  becomes their own worst enemy"). Fatal Frame 3's passive ghost in the safe area similarly
  broke an established rule. (same source)
- Neuroscience framing (PAX Aus 2016 talk):
  - Player-dependent scares run on operant conditioning — disrupt a learned action→reward
    pattern (do A in context X, get R... then once, get a scare). "Creating a false sense of
    security" is the mechanic.
  - Player-independent scares run on classical conditioning and depend on unpredictability;
    media-literate players have been conditioned to read suspense cues, so the fix is
    *randomness*: "the potential for video games to randomly generate content makes
    player-independent jump scares fundamentally less predictable than those of movies."
  - Cognitive overload amplifies scares: FNAF multitasking keeps the prefrontal cortex busy so
    it can't suppress the amygdala; the player is scared, worried a scare may come, and anxious
    about task performance simultaneously.
  - Learned helplessness is powerful but dangerous — players can quit, unlike trapped animals,
    so helplessness needs an anchor of hope.
  (With A Terrible Fate — https://withaterriblefate.com/2017/01/09/from-pax-aus-the-psychology-and-neuroscience-of-jump-scares/)
- Phasmophobia's hunts follow exactly this: deadly only during hunts, but hunts are "incredibly
  unpredictable in terms of when they occur, meaning you can never feel completely free."
  Phasmo also shows death can be low-key (hands closing in at screen edge) and still land —
  "scary without overly relying on jumpscares." (pikmonwolf, op. cit.; Niche Gamer review —
  https://nichegamer.com/reviews/phasmophobia-early-access-review/)

**Application:** make ghost manifestations random-timer-driven within sanity-weighted windows;
vary event *types* so no conditioning to one cue; let every major scare alter a rule the player
relied on (the "safe" truck breached, a closet fails, equipment lies).

## 5. Audio design: the primary fear channel

- Sound is information *and* threat. Alien: Isolation forces constant listening (survivors,
  androids, the alien, save-station beeps); your own noise is dangerous, which makes players
  paranoid about what they hear and what they make. "You get a mind model primed for picking up
  and seriously considering even the slightest sound." (Frictional blog, op. cit.)
- Dead Space: no traditional score — "we were going for textures and mood"; music is "sometimes
  very subtle, but can at a moment's notice move seamlessly into a blaring dissonant cacophony."
  Most source audio was self-recorded, not library stock. (Designing Sound, Don Veca interview
  recap — https://designingsound.org/2009/06/22/all-about-the-sound-of-dead-space/)
- Silence is a weapon: in liminal-space design "silence becomes the soundtrack"; stepping from
  noise into near-silence makes every tiny sound leap out. Sparse-but-deliberate beds
  (fluorescent hum, distant drips, HVAC) make emptiness audible. Dead Space (Remake) "employs
  silence strategically, making every sound a trigger." (Ties That Bind Gaming —
  https://tiesthatbindgaming.com/knotwork/video-games/liminal-spaces-in-video-games/;
  fearing.org — https://www.fearing.org/horror-games/the-rise-of-horror-games-in-2025/549)
- Binaural audio: Hellblade records voices performed around a Neumann KU 100 dummy head, so
  hallucinated voices sit *around* the player (accurate to how auditory hallucinations are
  actually experienced — "not inside your head"). Headphones are required; non-verbal vocal
  noises ("any possible noise you can make with your mouth") are used to "make them feel
  uncomfortable... when we hear a voice that is not supposed to be there." Audio serves story:
  "story is king." Caveat: binaural is expensive and "set in stone" — commit to a vision before
  recording. (Game Developer on GDC 2025 talk by Ninja Theory's Daniele Galante —
  https://www.gamedeveloper.com/audio/how-ninja-theory-created-hellblade-ii-s-unsettling-soundscape)
- Phasmophobia's masterstroke is *voice as mechanic*: real voice chat is the thing that keeps
  players tied to reality, and the game hijacks it — saying the ghost's name angers it, talking
  during a hunt gets you found and killed, dead players go silent. "You can't connect with your
  friends without also connecting with danger." (pikmonwolf, op. cit.)

**Application:** design audio as the detection system (ghost heard before seen — footsteps,
whispers, humming), keep long near-silent stretches, use positional/binaural ghost vocalizations
behind and above the player, and (if multiplayer or mic-enabled) let the player's own voice be
a liability.

## 6. Darkness and lighting

- Darkness limits agency: "limiting light sources... builds tension and nurtures fear" (Habel &
  Kooyman's "Agency Mechanics"), with "pitch-black play" as the extreme form. (PhD thesis,
  Univ. Innsbruck — https://ulb-dok.uibk.ac.at/ulbtirolhs/download/pdf/10338077)
- But darkness must not break navigation: "areas assisted with flashlights should never be so
  dark that the player cannot see" (breaks immersion); the flashlight's *reduced cone of
  vision* is itself the fear tool — it narrows attention to a tunnel while the periphery stays
  hostile. (DiVA thesis on Last of Us 2 lighting —
  https://www.diva-portal.org/smash/get/diva2:1767122/FULLTEXT01.pdf)
- Phasmophobia mechanizes light vs dark: sanity drains in darkness and is protected by room
  lights; low sanity then increases ghost activity — a feedback loop where your fear resource
  literally decays in the dark and restores in the light. (GameRevolution, op. cit.)
- Lighting style for uncanny spaces: harsh artificial light (flickering fluorescents, single
  bare bulbs) creates sharp shadows and uncomfortable brightness — liminal dread doesn't require
  pitch black. (Ties That Bind Gaming, op. cit.)

## 7. The uncanny, liminal space, and "home" as horror set

- Liminal spaces = familiar places stripped of people and purpose (empty school hallway, mall,
  office at 3am). "They take places we recognize and remove the meaning from them... familiar
  without being safe." Designers exploit them because "the environment itself becomes the story"
  and imagination fills the gap. Distorted geometry (a hallway extending too far, repeating
  identical doors) quietly breaks familiarity. (Ties That Bind Gaming, op. cit.; Steph Walsh —
  https://stephwalshauthor.com/why-are-liminal-spaces-so-unsettling/)
- Phasmophobia exploits this directly: suburban houses "look exactly like your home where
  you're playing. Seeing a decaying ghost walking among furniture you also have at home makes
  everything seem much more real." (pikmonwolf, op. cit.)
- Grip's "cozy first" technique: make the player comfortable, then "you just add an extra layer
  and it goes from being cozy to being a horror environment." (Den of Geek —
  https://www.denofgeek.com/games/silent-hill-resident-evil-scary-horror-games/)
- The night-shift variant: workplaces are designed to be occupied; experiencing them empty and
  dark is uncanny by *time displacement* alone — "a supermarket after closing, a school at
  midnight." (runradiant, below)

## 8. Structure: give the player a job (the Phasmophobia loop, explained)

runradiant's "Why Horror Games Keep Giving You a Job"
(https://www.youtube.com/watch?v=TZU_qBqujxQ) is the single most applicable analysis for a
ghost-hunting game:

- **Obligation replaces imprisonment.** Horror's core problem: why doesn't the player just
  leave? A job/contract gives a believable reason to stay — "the exit can be completely
  unlocked... It imprisons you through obligation." It also bridges the player/character gap:
  the character enters the basement because the job requires it, not because they're stupid.
- **Routine → rule violation.** Mundane repetition (take order, make drink, repeat) teaches
  what normal looks like; then the game "changes one tiny variable and lets your familiarity do
  the rest." "People are extremely sensitive to broken patterns." A ghost in an unfamiliar room
  is a scare; a ghost in a room you've walked through nine times is horror.
- **Normality and horror co-occupy space.** The game never announces "normal is over" — table 4
  still ordered a latte while something stalks you outside.
- **Divided attention creates information deficit.** Give more monitoring channels than the
  player can watch (FNAF cameras/doors/power); "every decision creates an information deficit
  somewhere else." Investigation tasks that require concentration while the environment quietly
  changes (Mortuary Assistant) force "do I keep working or investigate what I saw?"
- **Agency forces the bad decision.** "A locked door says you have no choice. An objective says
  you do have a choice — you just can't progress until you make the bad one." The player
  *participates* in their own endangerment, which worsens anticipation.
- **Push-your-luck economics.** Lethal Company's quota turns greed into the horror driver:
  leave now with a mediocre haul, or re-enter the building where things already tried to kill
  you. Optional objectives/bonus evidence in a ghost game work the same way.

Phasmophobia's own origin confirms this structure: Daniel Knight set out to make "a co-op
puzzle game where you actually had to stand next to your friends and figure the puzzle out
together... the horror is kind of like the secondary part — it just happened to be the perfect
fit." The investigation checklist IS the job; evidence ambiguity drives team arguments that get
interrupted by the ghost. (PC Gamer interview —
https://www.pcgamer.com/games/horror/i-dont-actually-play-horror-games-phasmophobias-lead-developer-had-no-intention-of-making-a-horror-game-but-still-kicked-off-a-whole-new-genre/)

## 9. Sanity and psychological mechanics

- Phasmophobia sanity is "essentially a health bar" and a countdown clock: drains in darkness
  and on ghost events, restores via pills/lit areas; low sanity raises hunt frequency, so the
  game forces escalating danger — "you simply can't take one step at a time. You have to put
  yourself at risk." (GameRevolution, op. cit.; pikmonwolf, op. cit.)
- Three ingredients of a memorable sanity mechanic: (1) **visibility of consequence** — the
  player must see/experience the cost, not read a number; (2) **asymmetry of recovery** —
  losing sanity must be easier than regaining it; (3) **narrative integration** — sanity should
  change what the player *perceives or believes*, not just combat stats.
  (Maximilien design guide — https://maximilien-game.com/horror-games-with-a-sanity-mechanic/)
- Lineage worth stealing from:
  - Eternal Darkness (2002): fourth-wall-breaking sanity effects aimed at the *player* (fake
    save corruption, volume drops, bleeding walls) — "the fourth wall is the only one the
    player thought was safe." (Maximilien, op. cit.; CBR —
    https://www.cbr.com/greatest-horror-games-sold-poorly/)
  - Amnesia (2010): sanity makes the player complicit — hiding requires darkness, darkness
    drains sanity, light is finite, looking at the monster costs sanity but not looking means
    not knowing where it is. "Every fear-mitigation strategy carries a cost in the other
    dimension." (Maximilien, op. cit.)
  - Darkest Dungeon: mental collapse as contagion — one panicking member drags the team down
    (directly applicable to co-op ghost hunts).
- Grip's "uncertain outcome" proposal: replace fear of losing progress with "what if I affect
  the world in a negative way?" — choices embedded in the world (interact or not, how you behave
  near a creature) that later change how scary sections play out. (Frictional blog, op. cit.)

## 10. Environmental storytelling

- Liminal/horror levels tell story through fragments: a half-erased message, an abandoned toy,
  scrawled symbols — "our imaginations will fill the gaps," and fragmented lore raises more
  questions than it answers (matches the incomplete-answers principle in §1). (Ties That Bind
  Gaming, op. cit.)
- Grip's 4-Layers approach gives the discipline: gameplay must fit the world/mood (no
  ludonarrative double-think), interactions must be understandable, and — critically for horror
  pacing — *repetition is the enemy*: "noticing patterns in a game system is not far away from
  wanting to optimize them," which pulls players out of narrative/horror mindset. Vary ghost
  behavior so players can't optimize the fear away. (Frictional blog, "4-Layers" —
  https://frictionalgames.blogspot.com/2014/04/4-layers-narrative-design-approach.html)
- Practical caveat from Alien: Isolation: players under constant threat cannot absorb dense
  lore — keep environmental storytelling to glanceable fragments during danger, richer material
  in safe zones (the truck). (Frictional blog, op. cit.)

## 11. Social fear dynamics (if co-op)

- Academic autoethnography on Phasmophobia: player interaction can *lower* fear (humor is a
  documented fear-management tool), while isolation raises it — solo play is scarier.
  Implication for design: separate players, force solo objectives, or make communication itself
  dangerous (voice mechanics) to claw fear back in multiplayer.
  (DiVA thesis — https://www.diva-portal.org/smash/get/diva2:1904515/FULLTEXT01.pdf;
  pikmonwolf, op. cit.)
- Co-op horror's commercial superpower is shareability: "fear is fun to watch... reactions
  dominate modern internet culture" (Lethal Company, R.E.P.O. spread via clips). Design for
  clip-able emergent moments. (CoffeeCup Studios, op. cit.)

## 12. Condensed checklist for the POV ghost game

1. Oscillate Ntokos levels 2–5 during investigation; spike 8–9 in hunts; guarantee relief after.
2. Ghost inferred, not shown; evidence partial/ambiguous; event timing randomized.
3. No weapons; tools measure/protect imperfectly; group ≠ safety.
4. Jump scares only as rare rule-breaking releases; every big scare rewrites a "safe" assumption.
5. Audio-first detection; long silence; binaural whispers behind/above; player voice is dangerous.
6. Darkness drains composure; flashlight narrows vision but never fully blinds; harsh flickering
   practicals over pitch black.
7. Familiar domestic maps + one wrong detail at a time; cozy baseline before corruption.
8. The contract/checklist is the "job": obligation to stay, routine to learn, quota to tempt greed.
9. Sanity = visible, asymmetric, perception-warping countdown; let low sanity make the game lie.
10. Story in fragments; never pause the threat for lore.
11. For co-op: split the team, endanger voice chat, and build moments worth clipping.

## Source files archived locally

- `alien-isolation-frictional.txt`, `four-ways-frictional.txt`, `4layers.txt` — Frictional/Grip
- `jumpscares.txt`, `tension.txt` — Game Wisdom / Game Developer
- `jumpscare-neuro.txt` — With A Terrible Fate (PAX Aus neuroscience)
- `phasmo-dev-interview.txt` — PC Gamer × Daniel Knight
- `phasmo-thesis.pdf` / `phasmo-thesis-full.txt` — DiVA social-fear thesis (incl. Ntokos table)
- `phasmo-sanity.txt` — GameRevolution sanity guide
- `sanity-mechanics.txt` — Maximilien sanity-mechanic design guide
- `liminal-spaces.txt` — Ties That Bind Gaming
- `hellblade-audio.txt` — Game Developer, GDC 2025 Ninja Theory binaural talk
- `deadspace-audio.txt` — Designing Sound Dead Space roundup
- `horror-2025-trends.txt` — fearing.org
- `subs/` — YouTube transcripts: Extra Credits (OyiAR2BXtKU), pikmonwolf (wxf-FW9jgKg),
  runradiant (TZU_qBqujxQ), Meridian Archives (guNqdQypF2w), CoffeeCup (G-IAc6uVqYU),
  TheDingDog (L0Xgngfrwhc, mostly gameplay — low value)
