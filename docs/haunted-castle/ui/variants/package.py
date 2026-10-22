"""Regenerate the style/asset indexes and labelled comparison sheets offline."""
from pathlib import Path
import json, shutil
from PIL import Image, ImageDraw, ImageFont
from render_core import ROOT, CONFIGS, SCREENS

DESCRIPTIONS = {
 '01-casefile': [
  'An investigator sorts the impossible into an ordinary evidence folder.',
  'A punched sheet, binder rings and a manila tab establish an object, not a web panel.',
  'Courier Prime makes field notes, recovered transcripts and case conclusions feel related.',
  'Oxide stamps and the negative selection strip mark decisions without ornate buttons.',
  'The HUD borrows a small evidence slip; the final screen becomes a disposition stamp.'],
 '02-occult-folio': [
  'The castle presents its own book of witnesses, printed in iron-gall ink.',
  'Heavy black rules and a narrow diamond border surround a quiet bone-coloured reading field.',
  'A ruled reliquary woodcut identifies the ledger; red wax physically closes the page.',
  'IM Fell English keeps body, menu and inscription typography in the same material world.',
  'The HUD sheds the page and keeps only candle-coloured rules and small verbal prompts.'],
 '03-last-light': [
  'The disappearance is expressed through empty space and an unoccupied doorway.',
  'Tall condensed headings create hierarchy without parchment, buttons or ornamental containers.',
  'Warm white type, a single faded red accent and hairline rules sit on almost black.',
  'Reading, journal and pause screens share a quiet left axis with a deliberately vacant right edge.',
  'A future 180 ms opacity fade would be sufficient; these PNGs show the stable readable state.'],
 '04-dead-channel': [
  'The interface is the investigator’s field monitor and its recovered tape.',
  'Timecode, focus lock, tape remaining and signal bars form an instrument vocabulary.',
  'The ledger is a bright camera document beside the monitor controls, not green text on a menu card.',
  'Share Tech Mono handles equipment labels; Courier Prime handles transcribed evidence.',
  'Scanlines stay outside the text backings, and signal loss resolves to a flat trace.'],
 '05-two-moons': [
  'A black illuminated manuscript belongs to the castle beneath its two moons.',
  'UnifrakturMaguntia is reserved for short display phrases; IM Fell English carries readable prose.',
  'Gold tracery repeats the narrow lancets, reliquary and paired moons of the setting.',
  'The ink-dark ground, restrained rubric and inset folio numbering replace generic RPG panels.',
  'The closing cards read as memorial inscriptions; the gameplay HUD retains only the quiet type and rules.'],
}
RANKING = [
 ('03-last-light', 'Best overall: the clearest departure from the disliked document card, with the lowest visual load during horror play.'),
 ('04-dead-channel', 'Strongest investigative identity: the instrument framing connects evidence, HUD and failure state across both maps.'),
 ('05-two-moons', 'Strongest castle identity: memorable display type and lunar stonework, with a less natural fit for suburban Bellwether.'),
 ('01-casefile', 'Most practical evidence workflow: excellent transcript clarity, though its bright paper needs a restrained on-screen footprint.'),
 ('02-occult-folio', 'Most diegetic castle document: cohesive ink and wax materials, but closest to the existing vellum direction.'),
]
RECIPE = {
 1:'Flat olive folder with a raised manila tab; a 512 px paper texture keeps grain in the outer 48 px; binder rings and punched holes occupy the left margin. Oxide red is for retrieval and disposition only.',
 2:'A bone sheet sits inside a black book edge. Three nested rules and repeated diamonds are printed ornaments. The reliquary is parallel linework. Wax is a seeded irregular polygon with a rim highlight and impressed seal.',
 3:'A near-black rectangle, a 3 px red left rule and two faint empty doorway contours. No paper or panel texture is necessary. Large headings and generous spacing do the work.',
 4:'A dark green CRT bezel has four focus brackets. Horizontal 1 px scanlines are baked into the decorative area. The filmed document uses a pale green-white backing and dark type; the body is never distressed.',
 5:'Black ground with narrow paired lancets, thin gold boundary rules, small seal roundels and two moons. The ledger woodcut stays in the left margin. Every text run has a clean ink-dark field, interrupting tracery where necessary.',
}
MOTIFS = {
 1:'Binder rings, evidence numbers, manila tabs, recovery stamp, carbon-copy selection, hand-drawn pulse.',
 2:'Reliquary woodcut, iron-gall border, diamond register, wax seal, manuscript titling, candle rules.',
 3:'Empty doorway, solitary red rule, oversized condensed title, quiet folio number, negative space.',
 4:'Focus corners, timecode, PLAY/REC, signal bars, tape counter, scanlines, zero-carrier trace.',
 5:'Twin moons, pointed lancets, gold roundels, reliquary, blackletter chapter title, memorial inscription.',
}

def main():
 sources=json.loads((ROOT/'FONT_SOURCES.json').read_text())
 font=ROOT/'03-last-light/fonts/barlowcondensed/BarlowCondensed-SemiBold.ttf'
 def f(size): return ImageFont.truetype(str(font),size)
 rank={slug:i+1 for i,(slug,_) in enumerate(RANKING)}
 for slug,c in CONFIGS.items():
  p=ROOT/slug
  (p/'render.py').write_text('"""Render all eight screens and textures; keep the variants folder together."""\nfrom pathlib import Path\nimport sys\nsys.path.insert(0, str(Path(__file__).resolve().parent.parent))\nfrom render_core import render\nif __name__ == "__main__":\n    render('+repr(slug)+')\n')
  audit=json.loads((p/'layout_audit.json').read_text())
  palettes='\n'.join(f'| {name} | `{c[name]}` |' for name in ['bg','paper','ink','accent','muted','dark','light'])
  types=[]
  for role in ['display','body','mono']:
   sizes=sorted({v['size'] for v in audit if v['role']==role})
   types.append(f'| {role} | `{c[role]}` | '+', '.join(str(x) for x in sizes)+' px |')
  text=f'# {c["name"]}\n\n'+'\n'.join(DESCRIPTIONS[slug])+f'''\n\n## Palette

| Role | sRGB hex |
| --- | --- |
{palettes}

## Type at 1920 × 1080

| Style | Bundled font | Sizes used |
| --- | --- | --- |
'''+ '\n'.join(types)+f'''

Tracking is 0 px for all body text. Last light uses 2–4 px on small capitals; Two moons uses 2 px on its document kicker; headings in the shared journal/pause templates use 2 px kicker tracking. Casefile and Dead channel keep most labels untracked. The title tagline and map-choice label use 1 px. Body copy wraps by measured font width and a 70-character ceiling, with 1.25–1.36 line spacing. The full pixel rectangles, colours and sizes are in `layout_audit.json`.

Minimum measured text contrast: **{min(x['contrast'] for x in audit):.2f}:1**. Body copy is 30 px or larger (document prose is 36–46 px across the set); smaller 25–29 px text is supplementary instrumentation or metadata. Every text run has an opaque, quiet backing, so no stain, scanline or ornament crosses letters. Body text stays readable against a near-black scene.

## Frame and material recipe

{RECIPE[c['kind']]}

Motifs: {MOTIFS[c['kind']]}

## Unity IMGUI translation

- Pure code: flat backing rectangles, selection strips, text, slider tracks/knobs, status meters, settings values and focus borders. One dynamic Font per GUIStyle; use the bundled regular font for prose, the display font for titles and the mono style for labels. No fallback to system fonts.
- Textures: `surface.png` (512², quiet centre), `frame.png` (512² RGBA, nine-slice border 48 px), and `icon_atlas.png` (512² RGBA). The atlas contains seal (0,0,170,170), reliquary (185,10,130,170), doorway (345,10,130,170), and pulse (0,210,512,130). Keep corner motifs fixed-size when nine-slicing.
- `photo_placeholder.png` is 720 × 540, a deliberately illustrated 4:3 placeholder. `castle_plate.png` and `bellwether_plate.png` are 1920 × 1080 procedural review plates. They are not screenshots of the shipped game, and do not imply engine integration.
- Scale a 1920 × 1080 reference canvas uniformly and letterbox as needed. Preserve at least 72 px safe margins for HUD information. The hunt state is represented together with ordinary objective/prompt information to stress-test hierarchy.
- Use GUI.DrawTexture for paper, wax/ornament crops and plates; GUI.Label for actual strings; GUI.DrawTexture with a 1 px solid texture for rules. Prebake any grain/scanline detail. No UI shader, real-time blur, masking system or post-process is required.
- The supplied frame and atlas are reusable samples; the complete material/layout recipe is `../render_core.py`. Export the exact screen-specific ornament layer from that code when implementing a selected direction. No C# or scene has been changed in this proposal.

## Regenerate

From this directory: `python3 render.py`. From the variants root: `python3 render_core.py {slug}`. The adjacent shared renderer is required; all fonts and source textures regenerate offline. Dependencies are pinned in `../requirements.txt`. The renderer generates all eight named PNGs, the six texture files and the measurement log. Run `python3 ../package.py` to refresh comparison sheets after a change.

## Screens

`document_castle.png`, `document_bellwether.png`, `journal.png`, `hud.png`, `title.png`, `settings.png`, `ending_closed.png`, `ending_lost.png` — all 1920 × 1080. `REVIEW_SHEET.png` is the labelled overview. “At dawn, the bell kept its silence.” and “The house has learned another name.” are the two original one-line ending epitaphs.
'''
  (p/'README.md').write_text(text)
  fs=[x for x in sources['files'] if x['path'].startswith(slug+'/')]
  assets=f'# Asset provenance — {c["name"]}\n\nAll fonts are unmodified SIL Open Font License 1.1 binaries from the official Google Fonts repository, pinned to `{sources["revision"]}`. Each family has its original `OFL.txt` beside the TTF. No Apple/system fonts, stock imagery, copied game assets or generated-provider media are included.\n\n| File | Source | Licence |\n| --- | --- | --- |\n'
  for item in fs:
   rel=item['path'].split('/',1)[1]
   assets+=f'| `{rel}` | [Pinned Google Fonts source]({item["url"]}) | OFL-1.1 |\n'
  codeurl='https://github.com/oh-ashen-one/Hollow-Current/blob/codex/ui-variants/docs/haunted-castle/ui/variants/render_core.py'
  for name,func in [('surface.png','Study.textures'),('frame.png','Study.textures'),('icon_atlas.png','Study.textures / seal / woodcut / arch / waveform'),('photo_placeholder.png','photo'),('castle_plate.png','scene'),('bellwether_plate.png','scene')]:
   assets+=f'| `textures/{name}` | [Original procedural source]({codeurl}), `{func}` | CC0-1.0; see `../ARTWORK_LICENSE.md` |\n'
  assets+='\nAll eight screen PNGs and the review sheet are original procedural compositions; their original visual elements are CC0-1.0. The fonts retain OFL-1.1; the game strings supplied by the owner remain project content. The script and documentation use the repository MIT licence. `../FONT_SOURCES.json` records SHA-256 hashes for every downloaded font/licence file. All output pixels come from bundled fonts or seeded geometry/noise. Scene plates and photo placeholders are explicitly illustrative.\n'
  (p/'ASSETS.md').write_text(assets)
  sheet=Image.new('RGB',(1920,2400),'#141719'); d=ImageDraw.Draw(sheet)
  for i,screen in enumerate(SCREENS):
   xx=(i%2)*960; yy=(i//2)*600
   d.text((xx+20,yy+12),screen.replace('_',' ').upper(),font=f(30),fill='#eae5d8')
   sheet.paste(Image.open(p/f'{screen}.png').resize((960,540),Image.Resampling.LANCZOS),(xx,yy+60))
  sheet.save(p/'REVIEW_SHEET.png')
 # Side-by-side comparison, reading scale 58% of originals.
 sheet=Image.new('RGB',(2400,3700),'#101416'); d=ImageDraw.Draw(sheet)
 d.text((70,40),'HOLLOW CURRENT / FIVE UI DIRECTIONS',font=f(72),fill='#eee9dd')
 d.text((72,128),'CASTLE DOCUMENT  +  IN-PLAY HUD     /     1920 × 1080 originals',font=f(32),fill='#bcc5c4')
 for i,(slug,c) in enumerate(CONFIGS.items()):
  yy=210+i*690
  d.text((70,yy),f'{i+1:02}  {c["name"].upper()}',font=f(40),fill='#eee9dd')
  d.text((2330,yy+5),f'RECOMMENDATION #{rank[slug]}',font=f(30),fill='#d1ad80',anchor='ra')
  for j,screen in enumerate(['document_castle','hud']):
   sheet.paste(Image.open(ROOT/slug/f'{screen}.png').resize((1120,630),Image.Resampling.LANCZOS),(70+j*1140,yy+50))
 sheet.save(ROOT/'CONTACT_SHEET.png')
 ranking='\n'.join(f'{i+1}. **[{CONFIGS[slug]["name"]}]({slug}/README.md)** — {reason}' for i,(slug,reason) in enumerate(RANKING))
 (ROOT/'README.md').write_text(f'''# Hollow Current — five UI directions

Five complete static proposals, 40 original 1920 × 1080 screens, reproducible offline. Prepared against source commit `be4c828022ed7df99d87ba701c75246392140fcf` on `codex/ui-variants`. No C#, scenes, audio, existing handoff or wall-art assets were edited. The previous r13 vellum screen was inspected as variant 0; these are five separate proposals, not an implemented engine UI.

![Document and HUD comparison](CONTACT_SHEET.png)

## Recommended order

{ranking}

Ranking is a design judgment about this brief, not a user-test result. The owner can select any direction before Unity implementation. Last light is the best starting point for both maps; Dead channel is the strongest alternative if an equipment-led identity is preferred.

## Reproduce and verify

1. Install Python 3.11+ with the versions in `requirements.txt` (use a virtual environment if needed).
2. `python3 render_core.py` regenerates all 40 PNGs, all textures and layout audit logs. Each variation also has its own `render.py` entry point. Rendering is offline and seeded.
3. `python3 package.py` regenerates the style sheets, asset manifests, five review sheets and this comparison.
4. `python3 verify.py` checks dimensions, exact required copy, readable body sizes, contrast, glyph coverage, text intersections, font hashes, licence files and bounded textures; it renders a second time and compares SHA-256 hashes of every generated PNG and audit log.

`fetch_fonts.py` is an optional acquisition tool, not a rendering dependency. It uses the immutable revision in `FONT_SOURCES.json`. All required fonts and their original OFL texts are committed.

## Contents and review limits

Each direction contains the eight requested PNGs, a reproducible entry point, a detailed style sheet, `ASSETS.md`, OFL fonts, six procedural textures, a measurement log and a labelled overview. `CONTACT_SHEET.png` compares each castle document with its in-play HUD; the original full-size screens are the authority for readability.

The 4:3 family photograph is an illustrated placeholder. Scene plates are dark procedural stand-ins, not actual game frames. These are static design proposals: no interaction, Unity font import, performance or dynamic-state test is claimed. All art was drawn procedurally to meet the explicit reproducibility brief; no paid image generation was used. Review and iteration were performed in this Codex session; no separate model critic or unsupported model was claimed.

`QA_REPORT.md` records the final mechanical and visual inspection evidence. `HANDOFF.md` records this task's scoped branch and implementation boundary. Original procedural artwork is CC0-1.0 (`ARTWORK_LICENSE.md`); source scripts/docs use the repository MIT licence; font licences remain OFL-1.1.
''')
 print('Style sheets, asset manifests and six comparison sheets regenerated.')

if __name__=='__main__': main()
