#!/usr/bin/env python3
"""Record the current game inputs and assembly of a successfully built player."""
import hashlib,json,subprocess
from pathlib import Path
from build_receipt import ROOT,source_digest
app=ROOT/'Builds/macOS/Hollow Current.app'
assembly=app/'Contents/Resources/Data/Managed/Assembly-CSharp.dll'
if not assembly.is_file():raise SystemExit('Build the macOS player first.')
revision=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip()
receipt={'source_commit':revision,'source_digest':source_digest(),'assembly_sha256':hashlib.sha256(assembly.read_bytes()).hexdigest(),'app':'Hollow Current.app'}
(app.parent/'build-info.json').write_text(json.dumps(receipt,indent=2)+'\n')
print('Recorded built inputs from',revision)
