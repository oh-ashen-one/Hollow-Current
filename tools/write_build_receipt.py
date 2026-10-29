#!/usr/bin/env python3
"""Record the source revision and assembly hash of a successfully built player."""
import hashlib,json,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1]
app=root/'Builds/macOS/Hollow Current.app'
assembly=app/'Contents/Resources/Data/Managed/Assembly-CSharp.dll'
if not assembly.is_file():raise SystemExit('Build the macOS player first.')
if subprocess.check_output(['git','status','--porcelain','--','Assets','Packages','ProjectSettings'],cwd=root,text=True).strip():
    raise SystemExit('Commit prepared game sources before writing the build receipt.')
revision=subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip()
receipt={'source_commit':revision,'assembly_sha256':hashlib.sha256(assembly.read_bytes()).hexdigest(),'app':'Hollow Current.app'}
(app.parent/'build-info.json').write_text(json.dumps(receipt,indent=2)+'\n')
print('Recorded build from',revision)
