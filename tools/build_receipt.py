"""Fingerprint game inputs, including freshly prepared scenes not yet committed."""
import hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]

def source_digest(root=ROOT):
    h=hashlib.sha256()
    for folder in ['Assets','Packages','ProjectSettings']:
        for p in sorted((root/folder).rglob('*')):
            if not p.is_file() or p.name=='.DS_Store':continue
            h.update(p.relative_to(root).as_posix().encode()+b'\0')
            with p.open('rb') as stream:
                for chunk in iter(lambda:stream.read(1024*1024),b''):h.update(chunk)
    return h.hexdigest()
