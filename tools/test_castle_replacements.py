"""Regression: rebuilds fail closed on broken imports and duplicate Resources keys."""
import json
from pathlib import Path
import tempfile
import unittest
from castle_replacements import tripo_replacement, check_replacements

class ReplacementTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.source = self.root / 'Assets/HollowCurrent/Art/Tripo/Castle/Pew'
        self.castle = self.root / 'Assets/HollowCurrent/Resources/Castle'
        self.source.mkdir(parents=True); self.castle.mkdir(parents=True)
        (self.source / 'import.json').write_text(json.dumps({'name':'Pew','output':'Castle','primitives':[{'file':'mesh.bytes','triangles':1}], 'materials':[{'stem':'surface','hasNormal':True}]}))
        for path in [self.source/'mesh.bytes', self.source/'surface-color.png', self.source/'surface-normal.png', self.castle/'Pew.prefab', self.castle/'Pew.prefab.meta']:
            path.write_bytes(b'fixture')
    def test_replacement_and_procedural_name(self):
        self.assertEqual(check_replacements(self.root), ['Pew'])
        self.assertIsNone(tripo_replacement('Orrery', self.root))
    def test_missing_import_payload_does_not_fall_back(self):
        (self.source/'mesh.bytes').unlink()
        with self.assertRaises(FileNotFoundError): tripo_replacement('Pew', self.root)
    def test_missing_prefab_does_not_fall_back(self):
        (self.castle/'Pew.prefab').unlink()
        with self.assertRaises(FileNotFoundError): tripo_replacement('Pew', self.root)
    def test_duplicate_fbx_is_rejected_without_deleting_it(self):
        duplicate = self.castle/'Pew.fbx'; duplicate.write_bytes(b'preserve')
        with self.assertRaises(ValueError): check_replacements(self.root)
        self.assertEqual(duplicate.read_bytes(), b'preserve')
    def test_orphaned_importer_meta_is_rejected(self):
        (self.castle/'Pew.fbx.meta').write_bytes(b'meta')
        with self.assertRaises(ValueError): check_replacements(self.root)

if __name__ == '__main__': unittest.main()
