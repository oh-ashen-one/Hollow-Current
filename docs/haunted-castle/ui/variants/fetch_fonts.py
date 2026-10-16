"""Optional acquisition tool; normal rendering uses the committed fonts offline."""
from pathlib import Path
import hashlib, json, urllib.request

ROOT = Path(__file__).resolve().parent
FAMILIES = {
    'courierprime': ['CourierPrime-Regular.ttf', 'CourierPrime-Bold.ttf'],
    'imfellenglish': ['IMFeENrm28P.ttf'],
    'barlowcondensed': ['BarlowCondensed-Regular.ttf', 'BarlowCondensed-SemiBold.ttf'],
    'sharetechmono': ['ShareTechMono-Regular.ttf'],
    'unifrakturmaguntia': ['UnifrakturMaguntia-Book.ttf'],
}
VARIANTS = {
    '01-casefile': ['courierprime'],
    '02-occult-folio': ['imfellenglish'],
    '03-last-light': ['barlowcondensed'],
    '04-dead-channel': ['sharetechmono', 'courierprime'],
    '05-two-moons': ['unifrakturmaguntia', 'imfellenglish'],
}

def main():
    lock = ROOT / 'FONT_SOURCES.json'
    rev = json.loads(lock.read_text())['revision'] if lock.exists() else json.load(
        urllib.request.urlopen('https://api.github.com/repos/google/fonts/commits/main'))['sha']
    records = []
    for slug, families in VARIANTS.items():
        for family in families:
            dest = ROOT / slug / 'fonts' / family
            dest.mkdir(parents=True, exist_ok=True)
            for name in FAMILIES[family] + ['OFL.txt']:
                url = f'https://raw.githubusercontent.com/google/fonts/{rev}/ofl/{family}/{name}'
                data = urllib.request.urlopen(url).read()
                if name == 'OFL.txt':
                    assert b'SIL OPEN FONT LICENSE' in data.upper(), family
                (dest / name).write_bytes(data)
                records.append({'path': str((dest / name).relative_to(ROOT)), 'url': url,
                                'sha256': hashlib.sha256(data).hexdigest(), 'license': 'OFL-1.1'})
    lock.write_text(json.dumps({'revision': rev, 'files': records}, indent=2) + '\n')
    print(f'Verified OFL licences and saved {len(records)} pinned font/licence files.')

if __name__ == '__main__':
    main()
