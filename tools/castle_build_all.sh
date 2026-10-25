#!/bin/sh
# Rebuild procedural Castle art, preserving authoritative Tripo prefabs.
# Run through the shared GPU slot coordinator (Blender processes run sequentially).
set -e
cd "$(dirname "$0")/.."
BLENDER="${BLENDER:-/Applications/Blender.app/Contents/MacOS/Blender}"
python3 tools/castle_replacements.py
python3 tools/castle_textures.py
python3 tools/castle_sky.py 2048
"$BLENDER" -b -P tools/castle_architecture.py
"$BLENDER" -b -P tools/castle_props.py
python3 tools/castle_r12_textures.py
"$BLENDER" -b -P tools/castle_props_r12.py
"$BLENDER" -b -P tools/castle_furniture.py
"$BLENDER" -b -P tools/castle_exterior.py
python3 tools/castle_hall_textures.py
"$BLENDER" -b -P tools/castle_hall_kit.py
"$BLENDER" -b -P tools/castle_verify.py
python3 tools/castle_replacements.py
