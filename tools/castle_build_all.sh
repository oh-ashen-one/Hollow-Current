#!/bin/sh
# Regenerate every Haunted Castle art asset headlessly (no Unity, no GPU engine) and verify the result.
set -e
cd "$(dirname "$0")/.."
BLENDER="${BLENDER:-/Applications/Blender.app/Contents/MacOS/Blender}"
python3 tools/castle_textures.py
python3 tools/castle_sky.py 2048
"$BLENDER" -b -P tools/castle_architecture.py
"$BLENDER" -b -P tools/castle_props.py
python3 tools/castle_r12_textures.py
"$BLENDER" -b -P tools/castle_props_r12.py
"$BLENDER" -b -P tools/castle_furniture.py
"$BLENDER" -b -P tools/castle_verify.py
