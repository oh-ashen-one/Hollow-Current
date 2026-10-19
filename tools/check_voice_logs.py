#!/usr/bin/env python3
"""Log lint for the Count's-voice rule (fd-voice, 2026-10-18; owner: "Nothing should be talked about as if it's a speaker throughout the entire house").

usage: check_voice_logs.py [--require-evp] [--require-summary] <player.log | dir> ...

Fails (exit 1) when, in any given player log:
  * an HC_COUNT_VOICE line has from != head, or lacks from=;
  * an HC_EVP line has verbal=True with from != head, or a trumpet/crypt HC_EVP has verbal != False;
  * an HC_LIE phantom_listen / HC_WATCHED ear_whisper line is not verbal=False, or names a voice_cc clip;
  * an HC_RARE_LINE is not from=head;
  * HC_VOICE_SUMMARY reports from_other != 0.
--require-evp: at least one HC_EVP from=trumpet verbal=False must exist (the escape recipe).  --require-summary: at least one HC_VOICE_SUMMARY.
Directories are searched recursively for player.log / Player.log / *.log. The static half of the rule is tools/check_castle_scares.py.
"""
import re, sys
from pathlib import Path

args = [a for a in sys.argv[1:] if not a.startswith("--")]
require_evp = "--require-evp" in sys.argv
require_summary = "--require-summary" in sys.argv
if not args:
    sys.exit(__doc__)
logs = []
for a in args:
    p = Path(a)
    logs += sorted(p.rglob("*.log")) if p.is_dir() else [p]
fails, evp_trumpet, summaries, head_lines = [], 0, 0, 0
for log in logs:
    for n, line in enumerate(log.read_text(errors="replace").splitlines(), 1):
        where = "%s:%d" % (log, n)
        def kv(k):
            m = re.search(r"\b%s=(\S+)" % k, line)
            return m.group(1) if m else None
        if "HC_COUNT_VOICE" in line:
            head_lines += 1
            if kv("from") != "head":
                fails.append("%s HC_COUNT_VOICE from=%s (must be head): %s" % (where, kv("from"), line.strip()))
        elif "HC_EVP" in line:
            src, verbal = kv("from"), kv("verbal")
            if src == "trumpet" and verbal == "False":
                evp_trumpet += 1
            if verbal == "True" and src != "head":
                fails.append("%s HC_EVP verbal=True from=%s: %s" % (where, src, line.strip()))
            if src in ("trumpet", "crypt", "lie") and verbal != "False":
                fails.append("%s HC_EVP from=%s must be verbal=False: %s" % (where, src, line.strip()))
        elif "HC_LIE kind=phantom_listen" in line or "HC_WATCHED" in line and "ear_whisper" in line:
            if kv("verbal") != "False" or "voice_cc" in line:
                fails.append("%s non-head cue must be verbal=False and name no voice clip: %s" % (where, line.strip()))
        elif "HC_RARE_LINE" in line and kv("from") != "head":
            fails.append("%s HC_RARE_LINE not from=head: %s" % (where, line.strip()))
        elif "HC_VOICE_SUMMARY" in line:
            summaries += 1
            if kv("from_other") != "0":
                fails.append("%s HC_VOICE_SUMMARY from_other=%s (must be 0): %s" % (where, kv("from_other"), line.strip()))
if require_evp and evp_trumpet == 0:
    fails.append("no HC_EVP from=trumpet verbal=False in the given logs (escape run: record at the ear-trumpet spot)")
if require_summary and summaries == 0:
    fails.append("no HC_VOICE_SUMMARY in the given logs")
print("voice log lint: %d log(s), %d HC_COUNT_VOICE (all from=head), %d trumpet EVP texture(s), %d summary line(s)" % (len(logs), head_lines, evp_trumpet, summaries))
if fails:
    for f in fails:
        print("FAIL " + f)
    sys.exit(1)
print("PASS the Count's voice came only from his head")
