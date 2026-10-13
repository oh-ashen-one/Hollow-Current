# Player Sentiment Mining — What Actually Scares Players (Ghost-Hunting / Horror Games)

Research date: 2026-10-13. Slice 04 of the ghost-exploration horror game research pack.

## Methodology note

Reddit login backends and `www.reddit.com`/`old.reddit.com` (both direct and via Jina reader, which had the domain abuse-blocked) were unavailable from this machine. Reddit threads were read through the public Redlib mirror `red.artemislena.eu` (same thread content/IDs as reddit.com URLs cited). Steam reviews were pulled from the official public Steam appreviews JSON API (`store.steampowered.com/appreviews/739630` = Phasmophobia, `1929610` = Demonologist). The Exa MCP server was not configured on this machine, so web discovery used the WebSearch tool instead.

Sources mined:
- r/HorrorGaming — "What makes horror games TRULY terrifying to you?" (99 comments) — https://www.reddit.com/r/HorrorGaming/comments/17yncb9/what_makes_horror_games_truly_terrifying_to_you/
- r/PhasmophobiaGame — "Ngl the game needs to be scarier" (155 comments, Apr 2025) — https://www.reddit.com/r/PhasmophobiaGame/comments/1k4osb4/ngl_the_game_needs_to_be_scarier/
- r/PhasmophobiaGame — "How to make the game scary again?" (Mar 2026) — https://www.reddit.com/r/PhasmophobiaGame/comments/1rja3qd/how_to_make_the_game_scary_again/
- r/PhasmophobiaGame — "What was your scariest Phasmophobia moment?" — https://www.reddit.com/r/PhasmophobiaGame/comments/17tepo7/what_was_your_scariest_phasmophobia_moment/
- r/PhasmophobiaGame — "What is the scariest moment you remember experiencing in this game?" (Sep 2025) — https://www.reddit.com/r/PhasmophobiaGame/comments/1nndpvq/what_is_the_scariest_moment_you_remember/
- r/PhasmophobiaGame — "Easy to implement ideas to keep the game scary" (Oct 2020 — a launch-era design post-mortem on why fear died) — https://www.reddit.com/r/PhasmophobiaGame/comments/j6a90m/easy_to_implement_ideas_to_keep_the_game_scary/
- r/PhasmophobiaGame — "The oni is the scariest ghost in the game, do you agree?" — https://www.reddit.com/r/PhasmophobiaGame/comments/1d411xi/the_oni_is_the_scariest_ghost_in_the_game_do_you/
- r/PhasmophobiaGame — "some stuff im wishing for in coming updates" (Mar 2025) — https://www.reddit.com/r/PhasmophobiaGame/comments/1j2hu2e/some_stuff_im_wishing_for_in_coming_updates_long/
- r/PhasmophobiaGame — "Dear Developers." (Jun 2026, wishlist + counterarguments) — https://www.reddit.com/r/PhasmophobiaGame/comments/1ugbezw/dear_developers/
- r/PhasmophobiaGame — "Killed During Ghost Event" (false-hunt ambiguity) — https://www.reddit.com/r/PhasmophobiaGame/comments/1e7lqgn/killed_during_ghost_event/
- r/PhasmophobiaGame — Deogen reveal reaction — https://www.reddit.com/r/PhasmophobiaGame/comments/v9ljyn/didnt_know_what_to_expect_from_the_new_deogen/ via https://www.spieltimes.io/news/new-phasmophobia-ghost-deogen-gives-players-mini-heart-attack/
- Steam reviews: 100 top positive + 100 top negative all-time (Phasmophobia), 97 most helpful (Demonologist) — https://store.steampowered.com/appreviews/739630 and /1929610
- Rock Paper Shotgun, "Phasmophobia's beta branch is still the scariest way to play" (Feb 2021) — https://www.rockpapershotgun.com/phasmophobias-beta-branch-is-still-the-scariest-way-to-play
- TheXboxHub, "Is Phasmophobia the Scariest Game on Xbox?" — https://www.thexboxhub.com/is-phasmophobia-the-scariest-game-on-xbox/

---

## 1. The moments players describe as most terrifying

### 1.1 Emergent, unscripted-feeling encounters beat scripted scares
The strongest single theme in Phasmophobia "scariest moment" threads: the scares players remember years later are ones that felt *personal and accidental*, not authored.

- "Grafton. Breaker upstairs turns off so I go and turn it on. As I leave it turns off again so I turn around and flip it back on. It turns off AGAIN as I was leaving and as soon as I turn around towards the breaker I get the ghost event where it grabs you and gets all up in your face. I screamed like Kevin McAllister in Home Alone." — https://www.reddit.com/r/PhasmophobiaGame/comments/1nndpvq/what_is_the_scariest_moment_you_remember/
- "'Where IS this frigging ghost?' I said to [my daughter] as I walked into the bathroom. And on cue it appeared. RIGHT in front of me. It couldn't have been scripted better!" — same thread
- "For a game that doesn't technically have jump scares I get so many jump scares, often from the ghost appearing right in my face all the sudden. The brilliance is it's truly random so it puts you in a false sense of security even if you have 100s of hours in the game. Sometimes ghost is easy and you feel on top of the game, then a demon hunts you the moment you walk in." — same thread (u/Mourning20)
- "The horror it does have feels a lot more natural because it isn't pre-programmed. The jumpscares occur naturally as the ghost moves around and interacts with the map and the players." — u/brakenbonez, https://www.reddit.com/r/PhasmophobiaGame/comments/1ugbezw/dear_developers/

### 1.2 Being hunted / stalked / chased, especially with no defense
- r/HorrorGaming top answers: "Knowing you're being stalked" (u/Affectionate-Leek421); "Personally, it's the feeling of being 'hunted' without any chance to really defend yourself... then Mr X appeared. It's been months now and I still can't bring myself to keep going"; "Tension, being chased. Not knowing what's behind a door, being chased by an unkillable enemy." — https://www.reddit.com/r/HorrorGaming/comments/17yncb9/what_makes_horror_games_truly_terrifying_to_you/
- "It's the fear of the unknown. When you aren't outright getting attacked, but you hear stuff in the walls, whispers. Hard silences, seeing stuff down hallways. Never knowing what's real or not." — u/boy_yeetsworld, same thread

### 1.3 Sound design and darkness as primary fear carriers
- "For me, sound design... Just stop and listen. You'll hear the creaking of the ship, something in the vents, and an occasional whisper... Being in total darkness can be scary as well." (u/gamergabe85); "Slow burns. Having to squint to see if something in the distance. Little sounds that don't sound right. Hushed movement you hear but don't see." (u/glistening_cum_ropes); "Sound design is a massive factor." (u/SisterRayRomano) — HorrorGaming thread above
- Phasmophobia-specific: "first time hearing spirit box, basements, the parabolic mic" listed as scariest moments (u/InfiniteFrankie); "Random ghosty piano solos get me almost every time if I'm in the same room." (u/YikesTheRemix); a growling "WHERE ARE YOUUUU?" hunt vocal that a player never heard again ("making me wonder if I hallucinated it lol") — https://www.reddit.com/r/PhasmophobiaGame/comments/1nndpvq/what_is_the_scariest_moment_you_remember/
- Players explicitly ask for rare audio events — the rarity itself is what made the "WHERE ARE YOUUUU?" moment work (never heard it twice).

### 1.4 Social amplification: friends' deaths, prox chat, being alone
- "The scariest part is when all of a sudden your friend disappears and from there things get wild" — Steam review id 32-equivalent, positive (steam-reviews.txt line 38; https://store.steampowered.com/appreviews/739630)
- "I saw the figure of someone approaching me I said 'I've got the book'. Turns out the ghost took out my last friend and was pursuing me... I've never had chills like that from a game in my life." — u/Chronicalgx, scariest-moment thread (17tepo7)
- "my friend said the ghost hunted and killed him because he said 'this ghost is a p*ssy' and he died a few seconds after the hunt started... im still traumatized from that match." — u/Hamdan0xx, same thread
- Solo play is repeatedly cited as the fear multiplier: "just play solo. itll set you back on track"; "go play some solo, that's where it's actually scary"; "tbh it's not scary at all with friends, doing it alone still freaks me out at times" — https://www.reddit.com/r/PhasmophobiaGame/comments/1k4osb4/ngl_the_game_needs_to_be_scarier/ and /1rja3qd/
- VR multiplies it again: "Playing in VR (it's 10 times scarier on a biological level)... That bloody slithering worm with human head going down the stairs right in front of my face out of the darkness made me jump to the damned ceiling." — u/Shadenium, 17tepo7

### 1.5 Specific ghost behaviors that scare: rule-breakers
Players consistently name ghosts that violate expectations as the scariest, in "scariest ghost" threads (https://www.reddit.com/r/PhasmophobiaGame/comments/1d411xi/):
- Demon — "It can hunt as soon as you step inside for the first time, leaving you very vulnerable... Getting a hunt first few minutes catches me off guard every time." (early hunts break the "safe setup phase" contract)
- Deogen — "if it catches you off guard you are just dead. Would 100% remove Deo from the game given the choice"; launch reaction: "seeing it sprint straight for me with a broken back almost gave me a heart attack" (u/Crickettt_, via spieltimes.io). Constantly knowing where you are = relentless stalker feel.
- Myling — "They like to spawn behind people and you usually can't hear them until it's too late." (silence as threat)
- Phantom — "During hunts I am genuinely pooping my pants because I can't see them as well." (impaired visibility)
- Mimic — "This ghost was so active, so intuitive, and so aggressive that it felt like playing against a real person... It made the game scary again, and took my pride down a peg." (u/Father_Zeebis, 17tepo7) — unpredictability across rulesets restores fear for veterans.
- Pattern: ghosts are scary when they break a rule the player relies on (safe setup time, audible footsteps, visible model, consistent behavior). RPS confirms the dev-side version of this: "ghosts got better at tracking you down... they'd begin opening closet doors to find you" — a behavior change re-terrified a 50-hour player (https://www.rockpapershotgun.com/phasmophobias-beta-branch-is-still-the-scariest-way-to-play).

### 1.6 False alarms and ambiguity (events that mimic hunts)
- "the Ghost started an event, the event that mimics a hunt... Basically I always freak out because I think it's a hunt" — https://www.reddit.com/r/PhasmophobiaGame/comments/1e7lqgn/killed_during_ghost_event/
- "The first time I ever experienced the event where the light goes red. I thought it was some kind of hunt... when I looked back, it was just standing there translucent, mocking me. Never felt actual fear in this game until then." — u/orc_mode666, 17tepo7
- Crucifix burning *in the truck* (a place players believe is safe): "Never seen it happen before or since but it shook me for sure!" — u/caffinatednurse88, 17tepo7. Violating a "safe zone" is high-impact.

---

## 2. What kills the fear

### 2.1 Learned predictability of danger (the #1 killer)
The launch-era design post nails the mechanism: "The hunting phase, being the only real danger, is too predictable, easy to avoid... once you know that, unless the lights starts blinking, you are 100% safe. The ghost can open doors, throw plates and appear in front of you, it doesn't matter cause you know you are safe unless the lights are blinking. Suddenly you aren't scared anymore." — u/Nightmare2828 (Oct 2020), https://www.reddit.com/r/PhasmophobiaGame/comments/j6a90m/easy_to_implement_ideas_to_keep_the_game_scary/
- Top reply: "The game is scary and replayable because of how unpredictable it is. Once you realize that the one thing that can actually harm you is super predictable, it removes a large part of the scare factor."
- Same poster, later: "I literally had the ghost appear around the corner... and didn't break a sweat because lights weren't blinking.... how sad is that! If that had happen in my first couple games I would have shit myself."
- 2025 echo: "If you know that you can only get killed when the ghost is hunting it becomes predictable, boring, non-scary and like a chore. It needs to be a chance. Maybe you get killed in a hunt maybe not. Maybe it is a hunt, maybe not." — u/TanKer-Cosme, 1k4osb4

### 2.2 Desensitization through repetition (community consensus: unavoidable but manageable)
- "Fear is an emotion that comes largely from not knowing what's going to happen. When you already know how the game behaves, some of that is alleviated." — u/Jebward-SuckerofToes
- "Fear stems from the unknown most of the time. After hundreds of hunts how could the game possibly scare you more? You get desensitized." — u/alexkars123
- "games only scary when you arent confident in your knowledge." — u/KindaStupidTho4
- "It's the kind of game that you can enjoy in that capacity if you agree not to look things up. Fun can still be had after doing so, but it's never going to scare you again." — u/sturmh (on wikis/meta knowledge killing fear) — all https://www.reddit.com/r/PhasmophobiaGame/comments/1k4osb4/
- r/HorrorGaming agrees at genre level: "Being unpredictable. The beginning of horror games are often scarier because you don't know what to expect. Once you get comfortable that wears [off]" — top snippet of the 17yncb9 thread.
- Academic support: replay/fail states kill fear because "the player already knows what to expect and consequently much of the tension and atmosphere is lost" — https://www.jamiekennedyking.com/s/JamieKingDissertationPrint.pdf

### 2.3 The meta/checklist mindset replaces the horror mindset
- "if you play exclusively with the meta (no evidence runs, getting the ghost using the clues because you've memorised every ghost quirk) then you are no longer engaging with the scary content of the game... your brain is only engaging with the checklist of ghost clues you've created." — u/kerfuffle_dood, 1k4osb4
- "it won't [be scary] since we're already desensitized from the horror, and the game is now simply an endless puzzle game of guessing the ghost." — u/IPancakesI, 1rja3qd
- Steam negative: "The ghosts don't give many clues, so even if you're really trying, you mostly have to guess... When the ghosts attack, the animation is the same for all of them and is boring; they could have created a unique animation for each ghost to add some variety. The game is very repetitive and gets boring quickly." — steam-reviews.txt line 71 (https://store.steampowered.com/appreviews/739630)

### 2.4 Repetition of content and weak/janky presentation
- "go in with certain set up, hunt probably goes down in X amount of minutes, get your evidence and leave. Eventually every game becomes the same." — u/christhebeanboy, 1k4osb4
- "There's no unique kill animations it's always the hands coming out of your peripheral and grabbing you. Ghosts are whatever too." — u/Ogg360, 1k4osb4
- Steam: "the events are repetitive every year"; "you have a butcher, a skinny guy, 2 women and a child ghost. They should add more models" — steam-reviews.txt lines 101/296
- Bugs directly break horror: "My audio completely cuts out when I enter certain floors... The ghost AI is a joke. The game barely feels scary anymore" — Steam review "Phasmophobia Has Lost Its Soul", steam-reviews.txt line 83. Also the famous stuck-ghost loop: "the only thing scary about this game is the shop and equipment interface" (line 5).

### 2.5 Co-op comedy can defuse horror (double-edged)
Playing with friends converts fear to fun — repeatedly framed as a feature, not a bug ("It's not just about finding ghosts. It's about the chaos, the friendships, the unexpected jump scares" — steam-reviews.txt line 56; "Not only is it less boring and terrifying, you can have the best laugh... whilst the ghost is hunting" line 335). For a new game: co-op dilutes fear but massively boosts retention/word-of-mouth; solo mode is where horror credibility lives.

---

## 3. What players ask for (demand signals)

From 1k4osb4, 1ugbezw, 1j2hu2e, j6a90m (URLs above):

1. **Random peripheral horror that isn't mechanically meaningful**: "shadow figures, maybe seeing something in a mirror and then it's gone when you turn back, voices or banging or any of the typical paranormal things. Just stuff to make it not so systematic" (u/christhebeanboy); "shadow figures out of the corner of your eyes. Thinking you see something in a hallway when you shut the lights off. Or dare I say, heads peering around corners, ghosts on ceilings" (u/VenomClashOfClans); "mirrors that randomly showed the ghost behind you" (u/Safe_Long700).
2. **Remove the guaranteed-safety signal**: multiple hunt tells so "everything that was now just a scare jump attempt is now a potential indication of impending death"; apparitions should sometimes convert into chases "with a very high chance" (u/Nightmare2828, j6a90m).
3. **Varied hunt presentations and deaths**: "more variety in how ghosts appear to you, both as events and hunts" (u/Jimijamsthe1st); "unique kill animations" (u/Ogg360); "reaching out to you when they are close, stumbling when hunting, hair flying crazy, eyes... smiling at you or talking/screaming when hunting" (u/VenomClashOfClans).
4. **Sanity effects that distort perception**: below ~30% sanity "you should start hearing things, seeing things, NOT hearing things you should be... your tools should give strange readouts"; at 0% "you should genuinely be unsure of whats real. you should see evidence that isnt real" (u/floomfwastaken, 1j2hu2e — compared to Eternal Darkness sanity system; note Nintendo patent concern raised in comments).
5. **Rare one-off events**: the "WHERE ARE YOUUUU?" vocal's power came from never recurring (1nndpvq); "Hidden scares... will disappear if you don't see it" praised in Mortuary Assistant (u/KingaaCrimsonuu22, HorrorGaming).
6. **Ghost interacts with the world/player equipment more aggressively**: turning off players' cameras/EMF, throwing the ghost-writing book, escalating object throws as a hunt announcement (j6a90m).
7. **Spirit box / radio improvements**: choppy partial transmissions instead of clean static ("OH M..... T'S HUNTI.... GET TO... IDING SPO..."); talking to dead teammates via spirit box (u/yazisiz) — 1j2hu2e.
8. **Dead-player participation**: see/track the ghost while dead (1j2hu2e thread discussion) — keeps dead players engaged in co-op.
9. **New maps with strong identity**: Bleasdale rework "rekindled the scare factor... feels a lot more real"; "prison still scares the shit out of me just from the size and emptiness" (u/ManMyDogsAreBarking, 1k4osb4); requests for amusement park map.
10. **QoL that protects immersion**: faster walk/stamina ("walking at a snail's pace... is not a fun gameplay mechanic, its just annoying"), 1-4 item hotkeys instead of cycling (1j2hu2e). Frustration ≠ fear.
11. **Difficulty/custom settings as the player-side fix**: high event frequency, low grace period, low starting sanity ("Scares the shit outta me when I turn around and ghost is coming at me down a long hallway" — u/zzzsleepyzzz123). A new game should ship these dials natively.
12. Devs' own acknowledged roadmap item: the "Horror 2.0" update is planned specifically "to make thing scarier" — cited by multiple commenters (u/Commander_Skullblade, u/Nebula106, 1k4osb4) as proof even Kinetic agrees the fear decayed.

---

## 4. Comparator evidence (Demonologist Steam reviews — what "scarier than Phasmo" means)

- Positive: "This game is basically a better and scarier version of Phasmophobia... you can get scared as hell here, I don't exactly like jumpscares but in this game they are very good"; "the events that happen are so much more scary that i was ready for" — https://store.steampowered.com/appreviews/1929610
- Negative (instructive): "It brands itself as a Phasmophobia clone with actual scares, but the scares are canned animations that distract from the investigation and can lead you to believe a ghost is present when it's not. Completing an investigation means following a hyper-specific formula... It feels more like filling out a checklist than organically investigating a haunt." — same source
- Lesson: scripted/canned scares produce a bigger first-impression spike ("scarier!") but players punish them as artificial once recognized; emergent scares age better. Best design = systemic simulation that *produces* scare-shaped coincidences.

---

## 5. Design takeaways for the new POV ghost game

1. **Never give a 100% reliable danger tell.** If players can know "safe until X," everything else becomes noise. Vary hunt announcements; let harmless events occasionally escalate; make some apparitions approach.
2. **Build fear from simulation, not scripts**: randomness that creates unrepeatable personal stories ("it couldn't have been scripted better") is what players retell years later. Include rare (1-in-N-hundreds) audio/visual events that many players will never see twice.
3. **Peripheral, consequence-free phenomena** (shadow figures, mirror glimpses, corner-peeking heads, whispered names) carry dread between hunts without inflating lethality.
4. **Rule-breaking entities**: a roster where each ghost violates one assumed contract (hunts early, silent footsteps, invisible, teleports, mimics others) keeps veterans afraid. Behavior that changes over a match (aging, learning) re-creates the RPS beta effect.
5. **Violate safe zones occasionally** (truck/van events) — the single most-upvoted "shook me" stories involve safety failing.
6. **Sanity should distort perception** (false evidence, phantom sounds, unreliable teammates-on-radio), making low sanity scary by itself rather than just a hunt timer.
7. **Sound design is the cheapest fear multiplier** — creaks, vents, whispers, distant footsteps, name-whispering; invest here before visuals (HorrorGaming consensus).
8. **Solo mode is the horror showcase; co-op is the retention engine.** Expect and design for co-op comedy; keep optional hardcore dials (no evidence, low sanity start, short/no grace period, high event rate) as first-class settings, since players currently hack these together themselves.
9. **Fear decays no matter what** — plan live-ops (new ghost behaviors, map reworks, event variants) as the only durable antidote; players explicitly accept this cycle ("Horror 2.0 will only make the game scary again for a short period until you get used to the new scares" — u/Varrbarr).
10. **Avoid checklist-feel investigation and canned scare animations** (Demonologist negatives), identical kill animations, low ghost-model variety, and audio bugs — all repeatedly cited as fear-killers.
