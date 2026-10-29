#!/bin/sh
# Refuse stale source-checkout builds; never silently open another copy by name.
set -eu
cd "$(dirname "$0")/.."
python3 - <<'PY'
import hashlib,json,subprocess
from pathlib import Path
receipt=Path('Builds/macOS/build-info.json')
if not receipt.exists():raise SystemExit('No verified build receipt. Build this checkout or download a release from https://github.com/oh-ashen-one/Hollow-Current/releases')
r=json.loads(receipt.read_text());dll=Path('Builds/macOS/Hollow Current.app/Contents/Resources/Data/Managed/Assembly-CSharp.dll')
if not dll.exists() or hashlib.sha256(dll.read_bytes()).hexdigest()!=r['assembly_sha256']:raise SystemExit('Build assembly differs from its receipt. Rebuild before playing.')
if subprocess.call(['git','diff','--quiet',r['source_commit'],'--','Assets','Packages','ProjectSettings'])!=0:raise SystemExit('This app predates the checked-out game sources. Build again; the old app has not been launched.')
PY
app="$PWD/Builds/macOS/Hollow Current.app"
if [ -x "$HOME/.cache/gpu-slot/bin/gpu_slot.sh" ]; then
  GPU_SLOT_CAPTURE_MAX_HOLD=0 exec "$HOME/.cache/gpu-slot/bin/gpu_slot.sh" capture --label hollow-current-play -- /usr/bin/open -W -n -a "$app" --args -screen-fullscreen 0
fi
exec /usr/bin/open -a "$app" --args -screen-fullscreen 0
