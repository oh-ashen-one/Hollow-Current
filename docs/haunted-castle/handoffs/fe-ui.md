# fe-ui: disabled castle button, settings card and lost card on DocLayout (wave FE, HANDOFF open item 7)

Branch `hc/fe-ui` (from haunted-castle 408f406). Renderer/layout only: **no scene, prefab or gameplay change, no bake**; the integrator needs a normal player build. Bellwether code paths are untouched (every change is behind `CastlePanel` / `castleCards`).

## What changed
| File | Change |
|---|---|
| `Runtime/Interface.cs` `Action()` | Disabled button on the castle card (`!GUI.enabled && castleCards && !plain`, i.e. FILE RECONSTRUCTION) is no longer the dimmed dark art (grey on vellum). It is an outlined "slip": 7 % umber wash, 1 px border, label and key cap in `castleDisabled` = (60,32,24) solid. Same button size, label and key cap position. Bellwether and all enabled buttons are unchanged. |
| `Runtime/Interface.cs` `EndingCastle(bool won)` | The castle "Signal lost" card now uses the castle nine-slice card and `DocLayout.Ending(true)` (screen `ending_lost`), the same text column as the won / kill card. Buttons: RETRY CHECKPOINT (280 wide) beside NEW INVESTIGATION (to the seal corner limit) at y 684, EXIT TO DESKTOP under them; with no checkpoint NEW INVESTIGATION sits alone at the old column (`new-solo`). `Ending()` calls it for any castle ending (`CastlePanel`), Bellwether keeps `DarkCard` and its old positions. |
| `Runtime/Interface.cs` `Settings()` / `CastleSettings()` | On the castle the card is `CastleCard(DocLayout.Settings().Frame)`; content x moves 558 -> 572 (clear of the top-left filigree), y positions are unchanged, rows whose bottom reaches the seal corner (page 0 toggle at y 703) are `LowW` = 825 wide so ON/OFF stays left of the seal. `castleCards` is true while the card is drawn, so text uses the near-black castle ink. `CastleSettings(sx, sw, sp)` takes the column and width (Bellwether passes 558 / 924: same numbers as before). Logs `HC_DOC_LAYOUT screen=settings overlaps=N ...` once. |
| `Core/DocLayout.cs` | `Ending(bool lost = false)` (adds `Retry`, `NewSolo`); new `Settings()` / `SettingsSpec` declaring every control of all three settings pages (one verification covers them). |
| `Tests/RulesTests.cs` | `DocLayoutTests`: lost + settings layouts verify with 0 violations; buttons side by side; the old settings positions (first row under filigree, last toggle under seal) are flagged. |
| `tools/DocLayoutDump.cs`, `tools/docui_journal_mock.py` | The mock now also renders the lost card (with checkpoint) and the settings card (page 2 = longest text; pages 0 and 1 too), asserts C# overlaps = 0, measured text inside declared rect / safe rect / clear of ornaments, text contrast, and (new) disabled slip contrast >= 4.5:1; exits non-zero on any failure. |

## Why
HANDOFF open item 7: the disabled FILE RECONSTRUCTION label (grey (.47,.43,.38) on a grey-tinted dark art over vellum) was unreadable in the FD frame `captures/fd/escape/16_journal.png`; settings and lost cards still used the stretched card with ornaments landing under text (r13 critic section 4 left them out of fd-journal).

## Decisions
- Lost card becomes the **paper** card (not a nine-slice of the dark Warning art): the dark art is a 3:1 strip with corner frames and a feather that distort when stretched to 850 x 685, and DocLayout's ornament constants describe `PanelCard.png`. Text colour on it is the existing castle ink. If the owner wants it dark, a second nine-slice spec is needed (documented as a risk).
- Disabled treatment is umber on vellum, not a wax stamp: smallest change, keeps the label and key cap, still reads as "unavailable" (no fill, thin rule).
- The microphone threshold row on settings page 2 is drawn with `GUI.enabled=false` while the mic is off (IMGUI halves its alpha). It is a disabled control (contrast-exempt) and was not changed; mention if the owner calls it faint.

## Verified (no engine launched)
- `INPUT_SYSTEM_REFERENCE=/Users/midir/hollow-current-haunted-castle/Library/ScriptAssemblies/Unity.InputSystem.dll python3 tools/check_source.py` PASS (rules incl. the new `DocLayoutTests`; player + editor compile; only the known CS0649/CS0162 warnings).
- `python3 tools/docui_journal_mock.py` prints `HC_DOC_LAYOUT screen=journal|ending|ending_lost|settings overlaps=0` and `overlaps=0` total; disabled slip contrast on the darkest 5 % of the paper under the button: label 5.2:1, key cap 5.2:1 (the FD frame was about 1.3:1; target 4.5:1); enabled text on the cards stays >= 6.8:1 (darkest 5 %), median 7.4:1 or better.
- Mocks read by eye (1920 x 1080): `docs/haunted-castle/rounds/r13/docui_mock_journal_fd.png` (disabled slip readable), `docui_mock_lost_fe.png`, `docui_mock_settings_fe.png` (page 2), `docui_mock_settings_p0_fe.png`, `docui_mock_settings_p1_fe.png`. Every label's first letter is visible (THE COUNT'S NIGHT, CONTROLS, Look, Long night, Microphone, LAST TRANSMISSION, Signal, RETRY, NEW, EXIT, FILE RECONSTRUCTION), the ornaments sit in the corners clear of text, and no seam shows in the nine-slice.
- Mock limits as in fd-journal: PIL not the engine (1 to 2 px baseline/kerning), slider thumbs and the dialogue preview are approximations, the page 1 kicker uses "LEDGER 015" for `Book.CardStyle`.

## Integrator recipe (normal player build of this branch, no bake)
1. Death run (lost card), castle: `~/.cache/gpu-slot/bin/gpu_slot.sh capture --label fe-ui -- "<Hollow Current.app>/Contents/MacOS/Hollow Current" -hc-playtest <outdir> death -screen-width 1920 -screen-height 1080 -screen-fullscreen 0` (the existing death route; the terminal frame after `08_dead` shows the lost card; take a frame after the dive). Windowed, `open -g`, no mouse grab.
2. Settings: `-hc-verify -hc-ui-only` on the castle build writes `ui-pause-settings.png` / `ui-title-settings.png` (castle when `Game.Map` is the castle); cycle the three pages with the DIALOGUE DISPLAY / CASTLE NIGHT button, or capture page 0 only.
3. Escape run journal frame (`16_journal`-equivalent): FILE RECONSTRUCTION must now read as dark umber on vellum.
4. Logs: `HC_DOC_LAYOUT screen=settings overlaps=0`, `HC_DOC_LAYOUT screen=ending_lost overlaps=0`, still `screen=journal` / `screen=ending` = 0, no `HC_DOC_LAYOUT violation`.
5. Bellwether `-hc-verify -hc-ui-only` must still PASS (its settings, journal and ending are the old code paths; byte-identical positions).

## Risks
- Unverified in the engine (as fd-journal): nine-slice cells for two more cards; `Graphics.DrawTexture` seams are the thing to look for (the whole-pixel rounding fix from fd-integ is shared).
- Title-screen settings with the castle selected but the Bellwether scene loaded still draw the old card (`CastlePanel` keys on `Game.Map`, like the journal), so only paused castle play and a loaded castle title use the new card.
- The slip colour goes through `Panel(...)` (`.linear`) like the other UI rules, so it renders as the sRGB value; if the in-engine label looks lighter than the mock, check the colour space, not the layout.
- A Codex UI direction can replace this: the rule to keep is `DocLayout.Verify`.
