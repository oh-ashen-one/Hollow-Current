#!/bin/zsh
# usage: gen.sh <name> <aspect> ; reads <name>.prompt.txt, writes <name>.png (full-res) + <name>.job.json
set -e; R=$(cd "$(dirname "$0")/refs" && pwd); n=$1; ar=$2
higgsfield generate create gpt_image_2_5 --prompt "$(cat "$R/$n.prompt.txt")" --aspect_ratio "$ar" --quality high --resolution 2k --wait --wait-timeout 8m --json > "$R/$n.job.json" 2>"$R/$n.err"
url=$(grep -oE 'https://[^"]+\.png' "$R/$n.job.json" | head -1); [ -n "$url" ] || { echo "no png url"; cat "$R/$n.err"; exit 1; }
curl -sL -o "$R/$n.png" "$url"; curl -sL -o "$R/$n.preview.webp" "${url%.png}_min.webp"; echo "$n -> $(file "$R/$n.png" | grep -oE '[0-9]+ ?x ?[0-9]+' | head -1) $(stat -f%z "$R/$n.png")B"
