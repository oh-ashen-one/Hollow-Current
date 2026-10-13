# Phasmophobia Design Autopsy — What Makes It Scary and Why It Works

Research notes for a POV ghost-exploration horror game inspired by Phasmophobia. Sources are cited inline with URLs. Local copies of key fetched pages are in this directory (`pcgamer-bestghost.txt`, `pcgamer-jank.txt`, `pcgamer-oneyear.txt`, `pcgamer-smarter.txt`, `gamerev-sanity.txt`, `laptopmag.txt`, `gamespot-tips.txt`, `fextra-hunt.txt`, `fextra-death.txt`, `fextra-sanity.txt`, `kinetic-cursed.txt`, `steam-solo.txt`, `liveservice.txt`).

---

## 1. Context: what Phasmophobia is and how big it got

- Co-op (1–4 player) first-person psychological horror by Kinetic Games, launched into Steam Early Access Sept 2020. Players are paranormal investigators who enter haunted locations, gather 3 types of evidence with tools (EMF reader, UV light, thermometer, spirit box, video cameras…), identify the ghost type, and leave. There is no "busting" — you identify, not fight. [PC Gamer — best ghost game ever made](https://www.pcgamer.com/phasmophobia-is-the-best-ghost-game-ever-made/)
- Made by a single developer ("Dknighter," 25-year-old UK indie, no formal dev training). Expected max ~500 concurrent players on launch day; it peaked over 110k concurrent, sold 2M+ copies in EA within weeks, topped Steam sales charts for 3 weeks, and was top-5 most-viewed on Twitch. Success forced him to expand the team and extend Early Access. [IGN](https://www.ign.com/articles/phasmophobia-dev-reconsidering-early-access-plans-after-huge-success) / [Eurogamer](https://www.eurogamer.net/phasmophobia-will-likely-stay-in-early-access-for-longer-thanks-to-its-phenomenal-success) / [Eurogamer — solo dev hires help](https://www.eurogamer.net/phasmophobias-solo-dev-hires-help-as-new-update-drops) / [wnhub](https://wnhub.io/news/other/item-17714)
- Perfect timing: pandemic + Halloween 2020; streamers (Jacksepticeye, xQc, Markiplier) drove it. A "scary experience you could share online with friends" when people were locked down. [Twinfinite](https://twinfinite.net/features/video-games-that-became-popular-thanks-to-a-streamer/) / [IGN SEA](https://sea.ign.com/news/164714/phasmophobia-is-a-new-steam-and-twitch-sensation)
- Long tail: ~840k Steam reviews, 94/100 player score ("Very Positive"/Overwhelmingly Positive territory) as of 2026. [Steambase](https://steambase.io/games/phasmophobia/reviews)

## 2. The core scare engine: anticipation, not the kill

- **"Anticipation is Phasmophobia's greatest source of tension. An apparition getting you isn't particularly scary, yet for some reason, every step up to that point is spine-chilling."** The game uses overwhelming darkness (even lit rooms look "dingy, decrepit"), a complete lack of music, and a constant atmospheric hum interrupted by occasional bangs and whispers. [Laptop Mag — 8 scariest games](https://www.laptopmag.com/features/the-8-scariest-games-ive-played-throughout-my-video-game-career)
- **Restraint and subtlety as design pillars.** "Phasmophobia's genius is that this is a game of restraint and subtlety… It knows that no ghost is ever going to be as scary as the one in your head, and shows real restraint in how it uses small events and subtle clues to build an atmosphere." Ghosts mostly "freak you out" rather than kill you. [PC Gamer — best ghost game](https://www.pcgamer.com/phasmophobia-is-the-best-ghost-game-ever-made/)
- **Escalating pressure curve within a match.** "The game makes your initial hunts relatively gentle, then the ghosts move up a notch. They start to notice you faster. They'll start to mess with you. Maybe you temporarily lose track of the team, then hear an unmistakeable breath passing close by one ear. Maybe you even see a silhouette for a second." [PC Gamer — best ghost game](https://www.pcgamer.com/phasmophobia-is-the-best-ghost-game-ever-made/)
- **The investigation itself is the anticipation machine.** Detecting the ghost is always "only a matter of time" — the threat escalates *because you are making progress*. Gathering evidence requires repeated trips into the house, so the game forces you to re-enter the scary place again and again as it gets more dangerous. [PC Gamer — best ghost game](https://www.pcgamer.com/phasmophobia-is-the-best-ghost-game-ever-made/)
- Design lesson: fear comes from *approaching* the scare under worsening odds, not from the scare itself. The ghost event/kill is the punctuation; the sentence is long.

## 3. Sanity: the invisible doom clock

- Sanity is effectively a health/fear meter (100→0). It drains in darkness inside the investigation area (rates scale with map size and difficulty; ~100→50% in ~5 min on Professional medium map), from ghost events touching you (-10%, Oni -20%, Banshee singing -15%), ghost abilities, cursed possessions, and -15% to everyone when a teammate dies. Restored by sanity pills (limited, occupy inventory slots). Lit rooms (ceiling lights, not flashlights) halt drain. [Fextralife — Sanity](https://phasmophobia.wiki.fextralife.com/Sanity) / [GameRevolution](https://www.gamerevolution.com/guides/661898-phasmophobia-how-does-sanity-work-what-does-0-mean)
- **Why it's scary:** sanity directly gates hunts — most ghosts can only hunt once team-average sanity ≤ 50% (some exceptions: Demon can hunt at any sanity; Yokai triggers off voice; Mare hunts earlier in dark). So the player can *feel* safety eroding: every flicker, event, and death visibly shortens the fuse. [Fextralife — Sanity](https://phasmophobia.wiki.fextralife.com/Sanity) / [sidekick sanity guide](https://sidekick.modax.ai/guides/phasmophobia/sanity-management-guide)
- **Deliberately under-explained mechanics.** "Part of what makes Phasmophobia such an effective horror game is its unexplained mechanics, including the Sanity system." Not knowing exactly what hurts you keeps players superstitious and anxious. [GameRevolution](https://www.gamerevolution.com/guides/661898-phasmophobia-how-does-sanity-work-what-does-0-mean)
- Sanity pills vs equipment inventory tradeoff: taking pills means giving up a tool slot — safety is a resource decision. [GameRevolution](https://www.gamerevolution.com/guides/661898-phasmophobia-how-does-sanity-work-what-does-0-mean)

## 4. Ghost events vs hunts: two-tier scare structure

- **Ghost events** are the non-lethal scares: apparitions (shadow/translucent forms), singing, whispers, breath by your ear, lights flicking off, doors slamming, objects thrown, car alarms, the "airball" mist that hisses and drains sanity. They cost sanity (pushing you toward hunts) but don't kill. [Steam discussion — sanity events](https://steamcommunity.com/app/739630/discussions/0/3380536561722196971/) / [fandom Ghost page snippet](https://phasmophobia.fandom.com/wiki/Ghost)
- **Hunts** are the lethal phase, with a hard telegraphed onset: exit doors lock, flashlights/electronics within 10m of the ghost flicker (15m for Raiju), a grace period (2–5s by difficulty) where the ghost doesn't move, then it chases at 1.7 m/s accelerating to ~2.8 m/s with line of sight. The ghost is visible only ~31% of the time during a hunt (flashing in and out). It listens: hears player speech within 9m and active electronics within 7.5m. [Fextralife — Hunts](https://phasmophobia.wiki.fextralife.com/Hunts)
- During hunts, radio/walkie signal gets scrambled and flashlights strobe — "the flickering of a player's flashlight and the static coming over the radio makes it impossible to hear other players besides the ones beside you. And even then, it's strongly encouraged that they don't speak or the monster will know where to travel." [Laptop Mag](https://www.laptopmag.com/features/the-8-scariest-games-ive-played-throughout-my-video-game-career)
- **Why hunts land:** the warning signs (lights out, doors locking, heartbeat, frozen breath, radio static) give seconds of pure dread *before* danger arrives; then survival depends on silence and stillness, which is itself unbearable. "I cannot tell you what it's like to hide silently for minutes, then hear someone say 'I think it's gone.'" [PC Gamer — best ghost game](https://www.pcgamer.com/phasmophobia-is-the-best-ghost-game-ever-made/)
- Hunt probabilities are dice rolls (1-in-2 check every 2–6s, then 1-in-5 success, 1-in-3 below 25% sanity), so players can never feel fully safe — unpredictability is structural. [Fextralife — Hunts](https://phasmophobia.wiki.fextralife.com/Hunts)

## 5. Hiding mechanics

- During a hunt you survive by breaking line of sight and hiding in closets/lockers or behind furniture, staying silent (voice AND electronics betray you). Willow Street House deliberately shipped with no lockers, forcing hide-behind-furniture play. [GameLuster](https://gameluster.com/phasmophobia-update-adds-two-new-ghost-types-and-more/) / [Fextralife — Hunts](https://phasmophobia.wiki.fextralife.com/Hunts)
- Hiding is undermined for variety: the Deogen always knows where you are (opens lockers/closets) but is slow up close — subverting the one strategy players rely on. [Pro Game Guides](https://progameguides.com/phasmophobia/how-to-escape-the-deogen-in-phasmophobia/)
- Higher difficulties block some hiding spots with clutter, so routes must be planned. [KeenGamer maps guide](https://www.keengamer.com/articles/guides/phasmophobia-all-maps-guide-breaker-locations-best-looping-spots-and-many-more/)

## 6. Voice recognition & audio design: the signature systems

- **In-game voice chat is load-bearing.** Proximity chat (only heard same room / muffled adjacent) vs walkie-talkie global chat. Playing on Discord "makes things a bit too easy and far less terrifying." [GameSpot tips](https://www.gamespot.com/articles/phasmophobia-guide-general-ghost-hunting-tips/1100-6483649/)
- **The ghost hears you.** Ghosts respond to speech (provocation, names, swearing), and the Spirit Box lets you ask direct questions aloud and get answers. "What it seems to do is pick up on the moods of your team. It responds when people are scared and say hasty things like 'let's just get out of here.' I've seen people try to deliberately provoke it and get nothing, then two minutes later when they're isolated for a split-second something shoots from the darkness." [PC Gamer — best ghost game](https://www.pcgamer.com/phasmophobia-is-the-best-ghost-game-ever-made/)
- Voice recognition produces emergent comedy/terror stories: insulting the ghost gets players killed; rapping scares ghosts off; spirit box fussiness (ghosts refusing non-questions) added in updates. [PC Gamer — one year in](https://www.pcgamer.com/a-year-into-early-access-and-phasmophobia-is-still-ugly-janky-and-dulli-love-it/) / [PC Gamer — ghosts smarter](https://www.pcgamer.com/phasmophobia-updates-will-make-ghosts-smarter-and-less-predictable/)
- **Audio restraint:** no music; ambient hum, occasional bangs/whispers; heartbeat + visible breath freezing as hunt warnings; radio static as both fiction and information loss. [Laptop Mag](https://www.laptopmag.com/features/the-8-scariest-games-ive-played-throughout-my-video-game-career) / [PC Gamer — best ghost game](https://www.pcgamer.com/phasmophobia-is-the-best-ghost-game-ever-made/)
- Walkie-talkie unreliability under high ghost activity is a designed information-cut: "if you aren't near your crew, you'll have no idea what could be happening to them." [GameSpot](https://www.gamespot.com/articles/phasmophobia-guide-general-ghost-hunting-tips/1100-6483649/)

## 7. Co-op dynamics: shared fear, separation, and the van

- Fear and laughter blend: "Few co-op games capture fear and laughter as perfectly as Phasmophobia… tension builds as you gather evidence, call out ghost names, and watch your sanity slowly tick down. But it's the unpredictable scares – doors slamming, lights flickering, or a sudden apparition – that keep you on edge." [EarlyGame best co-op list](https://earlygame.com/guides/gaming/the-20-best-rated-cooperative-games-on-steam-right-now)
- **Separation is the scare multiplier.** Big maps (school, asylum) force splitting up, regroup points, and van-guided navigation over the walkie. The van/truck is a safe command HQ with monitors — one player can stay behind as intel/support (the "van camper" role), which creates asymmetric fear: the van player watches sanity drain and cameras while others scream. [GameSpot](https://www.gamespot.com/articles/phasmophobia-guide-general-ghost-hunting-tips/1100-6483649/) / [TapTap Lethal Company review comparing formats](https://www.taptap.io/post/6584569)
- **Friends dampen fear — solo multiplies it.** Steam thread: "Suddenly the game that has felt no more than a haunted house attraction with friends becomes ACTUALLY menacing and terrifying… Any ghost event will cause me actual fear." [Steam — Why is it so scary Solo?](https://steamcommunity.com/app/739630/discussions/0/5169559267821620772/) (Note: mechanically, solo sanity drain is actually halved — the terror is purely psychological. [Fextralife — Sanity](https://phasmophobia.wiki.fextralife.com/Sanity))
- **Jank as pressure valve.** The horror is interleaved with slapstick (ragdoll corpses flopping onto toilets, clunky animations, recycled cheap assets), which lowers blood pressure between peaks and "turns something gloomy into side-splitting comedy" — the whiplash makes the real scares hit harder. [PC Gamer — jank](https://www.pcgamer.com/i-can-only-face-the-horrors-of-phasmophobia-because-of-its-slapstick-jankiness/) / [PC Gamer — one year in](https://www.pcgamer.com/a-year-into-early-access-and-phasmophobia-is-still-ugly-janky-and-dulli-love-it/)
- Co-op also enables betrayal-comedy: locking friends in rooms with the ghost, flicking light switches at pivotal moments, secretly lighting the summoning circle. "Since the game is hardly scary when playing in multiplayer…" [GAMINGbible](https://www.gamingbible.com/news/platform/playstation/playstation-multiplayer-horror-game-111159-20241120)
- The liminal, cheap-asset look accidentally works: "the low quality animations and assets… create this disorientating, liminal vibe. I think Purgatory probably looks a lot like Phasmophobia." [PC Gamer — one year in](https://www.pcgamer.com/a-year-into-early-access-and-phasmophobia-is-still-ugly-janky-and-dulli-love-it/)

## 8. Cursed possessions: opt-in risk/reward horror

- Added in the v0.5.0 Cursed Possessions update (Dec 2021): one of 6–7 items (Ouija Board, Music Box, Voodoo Doll, Haunted Mirror, Summoning Circle, Tarot Cards, later Monkey Paw) spawns per contract. Official framing: "Cursed Possessions trade sanity for information, but be warned; using these items can cause an additional negative effect, so think carefully if you want to anger the ghost for the rest of the investigation." [Kinetic Games dev blog](https://kineticgames.co.uk/news/phasmophobia-nightmare-rhxys)
- Misuse triggers **cursed hunts** that can start at ANY sanity level and make all subsequent hunts longer; crucifixes won't stop them. Ouija board breaks if you don't say "Goodbye." [Pro Game Guides](https://progameguides.com/phasmophobia/what-happens-if-you-dont-say-goodbye-to-the-ouija-board-in-phasmophobia/) / [Fextralife — Hunts](https://phasmophobia.wiki.fextralife.com/Hunts)
- Design lesson: gives players a loaded gun to point at themselves — agency over danger ("high-risk, high-reward move that can either fast-track your investigation or get you killed in seconds"). [Lemon8 Ouija](https://www.lemon8-app.com/@cardz_gaming/7458711853766672939)

## 9. Death consequences: real stakes without run-ending harshness

- Death costs: you lose all equipment you brought (must re-buy; insurance refunds a fraction — ~half loadout cost on Amateur, quarter on higher difficulties), half your pay, and the difficulty multiplier. Teammates each lose 15% sanity. Your dropped gear can be recovered by the living. [Fextralife — Death](https://phasmophobia.wiki.fextralife.com/Death) / [Attack of the Fanboy](https://attackofthefanboy.com/guides/phasmophobia-how-to-make-money-and-how-does-insurance-work/)
- Dead players enter a blue-lit ghost realm: they can see the ghost and observe, but the living can't hear them (dead can hear each other), they can't use walkie/spirit box, and their journal marks are hidden. Being dead is spectator-horror — you watch your friends get hunted, unable to warn them. [Fextralife — Death](https://phasmophobia.wiki.fextralife.com/Death) / [PC Gamer — best ghost game](https://www.pcgamer.com/phasmophobia-is-the-best-ghost-game-ever-made/)
- On lower difficulties a kill ends the hunt ("the ghost got what it wanted"); on Nightmare/Insanity a kill extends the hunt — punishing. [Fextralife — Hunts](https://phasmophobia.wiki.fextralife.com/Hunts)
- Rare revive mechanics via cursed items (High Priestess tarot, Monkey Paw wish — 50% the wisher dies instead) keep death tense but not always permanent. [Fextralife — Death](https://phasmophobia.wiki.fextralife.com/Death)

## 10. Ghost variety as replayability and paranoia

- 12 ghosts at launch, 24+ now; each has distinct evidence combos, hunt thresholds, speeds, tells, and hidden behaviors (Yokai is voice-triggered; Hantu empowered by cold; Mare hunts earlier in darkness; Deogen can't be hidden from; Raiju empowered by electronics). "There are a lot of them, with a lot of different patterns, so when you hop out of your van you're immediately in experiment mode." [PC Gamer — one year in](https://www.pcgamer.com/a-year-into-early-access-and-phasmophobia-is-still-ugly-janky-and-dulli-love-it/) / [RPS — Yokai & Hantu](https://www.rockpapershotgun.com/phasmophobia-adds-two-new-ghosties-that-i-dont-want-to-meet)
- Dev philosophy: keep taking players out of their comfort zone — ghosts wander rooms more, prefer returning to where they last spotted a player (making "safe" rooms unsafe), refuse lazy spirit-box chatter, anti-cheese changes. "More varied ghosts leads to less predictability, which in turn makes for scarier encounters." [PC Gamer — ghosts smarter](https://www.pcgamer.com/phasmophobia-updates-will-make-ghosts-smarter-and-less-predictable/) / [Screen Rant](https://screenrant.com/phasmophobia-ghosts-scary-smart-new-update/)
- Mystery preservation matters: some players deliberately avoid guides because "the ambiguity… has made Phasmophobia such a trip." Under-documentation is part of the horror. [PC Gamer — one year in](https://www.pcgamer.com/a-year-into-early-access-and-phasmophobia-is-still-ugly-janky-and-dulli-love-it/)

## 11. Criticisms / failure modes (what to avoid in your own design)

- **Fear decays with mastery.** Veterans report the game stops being scary; scares become "rare… maybe one in 20 games." Long-term engagement shifted to grinding, tiers, challenges. [The Problems with Live Service Horror (video essay, Aug 2025)](https://www.youtube.com/watch?v=EQdxGtzSXw0)
- **Live-service bloat:** equipment tier system (tier 1/2/3 for every item), prestige wipes, bloated shop/loadout UI — criticized as "artificially inflating depth… adding arbitrary hurdles." [Same video essay](https://www.youtube.com/watch?v=EQdxGtzSXw0)
- **Precision challenges expose jank:** the Gold Apocalypse Sunny Meadows challenge (slow walk, hunts every ~15–20s on the biggest map) is "a feat entirely of RNG, requiring very little skill, but ultimate patience" — demanding precision from an imprecise game frustrates rather than frightens. [Same video essay](https://www.youtube.com/watch?v=EQdxGtzSXw0)
- No narrative: match-based loop "stretched out to infinity," story absent by design. Fine for co-op horror but noted as a ceiling. [Same video essay](https://www.youtube.com/watch?v=EQdxGtzSXw0)
- Multiplayer with chatty friends can fully defuse the horror ("hardly scary when playing in multiplayer") — solo mode and comms-friction mechanics (proximity chat, radio scrambling) are the counterweights. [GAMINGbible](https://www.gamingbible.com/news/platform/playstation/playstation-multiplayer-horror-game-111159-20241120) / [Steam solo thread](https://steamcommunity.com/app/739630/discussions/0/5169559267821620772/)
- Streamer-era hazards: hackers used third-party tools to jumpscare streamers, showing how much of the game's appeal lives in streamable scare-reactions. [GameSpot](https://www.gamespot.com/articles/phasmophobia-hackers-are-terrorizing-streamers-with-jump-scares/1100-6483510/)

## 12. Distilled design lessons for a Phasmo-inspired game

1. **Make progress itself escalate danger** — the investigation loop (returning to the ghost's room with more gear) IS the anticipation builder.
2. **Two-tier scares:** frequent non-lethal events (which erode a visible safety resource) + rare lethal hunts with loud multi-sensory telegraphs (lights, locked doors, heartbeat, radio static) seconds before danger.
3. **A visible-but-mysterious doom meter (sanity)** that players influence but never fully understand breeds superstition and tension.
4. **Voice as a system:** the monster hearing real speech, plus proximity chat + fragile radio, turns communication into gameplay and information loss into horror.
5. **Silence as survival mechanic** during hunts — hiding works only if you also shut up and kill your electronics.
6. **Death should cost real resources** (gear, pay, team sanity) but leave the dead a haunting spectator role.
7. **Opt-in danger objects (cursed possessions)** let players gamble sanity for information — self-directed scare escalation and great streamer moments.
8. **Ghost variety with distinct rules/tells** keeps paranoia alive across hundreds of matches; never let one strategy stay safe.
9. **Interleave comedy/jank** so fear has contrast; shared fear + laughter is the co-op hook.
10. **Darkness + silence > jump scares:** no music, oppressive ambient sound, sparse apparitions. "No ghost is ever as scary as the one in your head."
11. Plan for **fear decay**: mastery kills horror; rotating maps/ghost behaviors/events and optional hard modifiers are the long-term answer (Phasmo's own answer — grind/tiers — is widely disliked; do better).

## Source list

- https://www.pcgamer.com/phasmophobia-is-the-best-ghost-game-ever-made/
- https://www.pcgamer.com/a-year-into-early-access-and-phasmophobia-is-still-ugly-janky-and-dulli-love-it/
- https://www.pcgamer.com/i-can-only-face-the-horrors-of-phasmophobia-because-of-its-slapstick-jankiness/
- https://www.pcgamer.com/phasmophobia-updates-will-make-ghosts-smarter-and-less-predictable/
- https://www.ign.com/articles/phasmophobia-dev-reconsidering-early-access-plans-after-huge-success
- https://www.eurogamer.net/phasmophobia-will-likely-stay-in-early-access-for-longer-thanks-to-its-phenomenal-success
- https://www.eurogamer.net/phasmophobias-solo-dev-hires-help-as-new-update-drops
- https://sea.ign.com/news/164714/phasmophobia-is-a-new-steam-and-twitch-sensation
- https://www.laptopmag.com/features/the-8-scariest-games-ive-played-throughout-my-video-game-career
- https://www.gamerevolution.com/guides/661898-phasmophobia-how-does-sanity-work-what-does-0-mean
- https://www.gamespot.com/articles/phasmophobia-guide-general-ghost-hunting-tips/1100-6483649/
- https://www.gamespot.com/articles/phasmophobia-hackers-are-terrorizing-streamers-with-jump-scares/1100-6483510/
- https://phasmophobia.wiki.fextralife.com/Hunts
- https://phasmophobia.wiki.fextralife.com/Death
- https://phasmophobia.wiki.fextralife.com/Sanity
- https://kineticgames.co.uk/news/phasmophobia-nightmare-rhxys
- https://progameguides.com/phasmophobia/what-happens-if-you-dont-say-goodbye-to-the-ouija-board-in-phasmophobia/
- https://progameguides.com/phasmophobia/how-to-escape-the-deogen-in-phasmophobia/
- https://steamcommunity.com/app/739630/discussions/0/5169559267821620772/
- https://steamcommunity.com/app/739630/discussions/0/3380536561722196971/
- https://steambase.io/games/phasmophobia/reviews
- https://www.youtube.com/watch?v=EQdxGtzSXw0 ("The Problems with Live Service Horror", Aug 2025 — transcript saved locally)
- https://www.youtube.com/watch?v=rKukBU4LFZg (student level-design analysis, found not transcribed)
- https://www.gamingbible.com/news/platform/playstation/playstation-multiplayer-horror-game-111159-20241120
- https://twinfinite.net/features/video-games-that-became-popular-thanks-to-a-streamer/
- https://earlygame.com/guides/gaming/the-20-best-rated-cooperative-games-on-steam-right-now
- https://www.rockpapershotgun.com/phasmophobia-adds-two-new-ghosties-that-i-dont-want-to-meet
- https://screenrant.com/phasmophobia-ghosts-scary-smart-new-update/
- https://gameluster.com/phasmophobia-update-adds-two-new-ghost-types-and-more/
- https://attackofthefanboy.com/guides/phasmophobia-how-to-make-money-and-how-does-insurance-work/
- https://www.taptap.io/post/6584569
- https://www.diva-portal.org/smash/get/diva2:1904515/FULLTEXT01.pdf (academic auto-ethnography: "Social Interaction in Horror Games — Phasmophobia"; download timed out, cited from search snippet)
