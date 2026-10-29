#!/bin/bash
# Build only: prepare/bake both maps, compile the player and package it. No playtest.
set -euo pipefail
cd "$(dirname "$0")/.."
UNITY="${UNITY:-/Applications/Unity/Hub/Editor/6000.3.20f1/Unity.app/Contents/MacOS/Unity}"
[ -x "$UNITY" ] || { echo 'Set UNITY to the pinned Unity 6000.3.20f1 executable.' >&2; exit 1; }
mkdir -p evidence Builds/macOS
run_stage() {
  local stage="$1" method="$2"
  shift 2
  local command=(python3 tools/run_owned_process.py --timeout 1800 --receipt "$PWD/evidence/build-$stage-process.json" -- "$UNITY" -batchmode -quit -projectPath "$PWD" -executeMethod "$method" -logFile "$PWD/evidence/build-$stage.log" "$@")
  if [ -x "$HOME/.cache/gpu-slot/bin/gpu_slot.sh" ]; then
    "$HOME/.cache/gpu-slot/bin/gpu_slot.sh" capture --label "hollow-build-$stage" --timeout 1800 --json "$PWD/evidence/build-$stage-slot.json" -- "${command[@]}"
  else
    "${command[@]}"
  fi
}
run_stage castle HollowCurrent.Editor.HouseLighting.PrepareAndBakeCastle
run_stage house HollowCurrent.Editor.HouseLighting.PrepareAndBake
run_stage player HollowCurrent.Editor.NativeBuild.BuildPreparedMacAll -nographics
python3 tools/write_build_receipt.py
ditto -c -k --sequesterRsrc --keepParent 'Builds/macOS/Hollow Current.app' Builds/macOS/Hollow-Current-macOS.zip
unzip -tq Builds/macOS/Hollow-Current-macOS.zip
shasum -a 256 Builds/macOS/Hollow-Current-macOS.zip
printf '\nBuild complete. Run ./tools/play.sh\n'
