#!/usr/bin/env python3
"""Bounded Fish auditions only; never writes Resources or embeds credentials.

Requires requests, msgpack, numpy, scipy, soundfile and ffmpeg. Use --generate
to authorize paid calls. Existing responses are reused; uncertain calls leave
a .pending marker and require manual inspection before retrying.
"""
import argparse
import base64
import hashlib
import json
import os
from pathlib import Path
import subprocess

import msgpack
import numpy as np
import requests
import soundfile as sf
from scipy.signal import fftconvolve, butter, sosfilt

ROOT = Path(__file__).resolve().parents[1]
API = "https://api.fish.audio"
SR = 48000
VOICES = {
    "count": {
        "instruction": "An original male horror character, an elderly English aristocrat with a resonant low baritone. Precise cultured British consonants, intimate chest resonance, dry age-worn texture, a hint of breath. He sounds dangerously real, intelligent and quietly amused, never a cartoon vampire or trailer announcer. Slow deliberate speech with clear words. Dry solo voice, no music, no echo.",
        "reference_text": "I can hear your heart. It is very loud, in my house. Stay a little longer. We have all the time in the world.",
    },
    "dispatch": {
        "instruction": "An original British woman in her forties, a real emergency field operator. Warm low alto, steady grounded diction, slightly tired and rough from long night shifts. A protective human presence with contained fear beneath professional control. Natural conversational delivery, not a presenter or synthetic assistant. Dry solo voice with no radio effect, no music.",
        "reference_text": "Bellwether Field Office. I am still here. Listen carefully. Find cover, turn your light off, and stay quiet until I tell you.",
    },
    "house": {
        "instruction": "An original elderly female ghost with a delicate low voice and a faded English accent. Intimate, grief-stricken and unsettlingly calm, with audible fragile breath and slightly uneven pauses. Sounds like a real person speaking inches from a microphone. Understandable words, restrained pain, no theatrical witch cackle. Dry voice, no echo, no music.",
        "reference_text": "Her chair. Do not put it away. Keep the house warm. I waited downstairs for so long. You left me there.",
    },
    "player": {
        "instruction": "An original young adult woman, a frightened but determined investigator. Natural middle register, close microphone, unpolished human voice with breath catching between words. Tense and vulnerable, never seductive or theatrical. Dry solo voice without music or effects.",
        "reference_text": "Stay quiet. Just breathe slowly. Do not let him hear you. Wait until the footsteps stop. I can get out of here.",
    },
}
TAKES = [
    dict(id="count_stalking", voice="count", old="CastleAudio/voice_cc_listen.wav", fx="stone",
         text="[very quiet and intimate, a patient predator savoring the discovery, slow cultured British speech] I can hear your heart. [short pause] It is very loud, <whisper>in my house.</whisper>"),
    dict(id="count_found", voice="count", old="CastleAudio/voice_cc_hunt_found.wav", fx="stone",
         text="[a low close whisper, delighted recognition, a tiny sinister smile, absolutely no shouting] There you are."),
    dict(id="count_hunt", voice="count", old="CastleAudio/voice_cc_hunt_run_b.wav", fx="stone",
         text="[breathless predatory excitement, the civilized mask cracking into a harsh commanding voice] Yes. <forceful>Run.</forceful> [a low breathy chuckle, then threateningly quiet] I like it when they run."),
    dict(id="dispatch_warning", voice="dispatch", old="Audio/voice_warning.wav", fx="radio",
         text="[an emergency operator suddenly realizing the danger, urgently trying to keep her voice steady] That is not interference. [short pause] Find cover. Light off. <soft>Stay quiet.</soft>"),
    dict(id="house_whisper", voice="house", old="Audio/voice_whisper.wav", fx="house",
         text="[a frail woman's barely audible whisper, accusing through restrained grief, almost touching the listener's ear] You left her downstairs."),
    dict(id="house_evp", voice="house", old="Audio/voice_evp.wav", fx="evp",
         text="[an old woman quietly pleading, speaking through shallow breath, each thought difficult to release] Her chair. [short pause] Do not put it away. <whisper>Keep the house warm.</whisper>"),
    dict(id="player_hidden_breath", voice="player", old="CastleAudio/breath_hidden.wav", fx="dry",
         text="[gasp] [panting] [panting] [sigh]"),
    dict(id="count_door_breath", voice="count", old="CastleAudio/castle_hide_breath_0.wav", fx="stone",
         text="[inhale] [exhale] [inhale] [exhale] [sigh]"),
]


def write_json(path, data):
    path.write_text(json.dumps(data, indent=2) + "\n")


def request(session, path, body, model, dest, binary=False):
    payload = msgpack.packb(body, use_bin_type=True) if binary else json.dumps(body).encode()
    spec = {"model": model, "endpoint": path, "payload_sha256": hashlib.sha256(payload).hexdigest()}
    metadata = dest.with_suffix(dest.suffix + '.request.json')
    if dest.exists():
        if metadata.exists() and json.loads(metadata.read_text()) != spec:
            raise RuntimeError(f"Prompt/reference changed: use a new output directory for {dest.name}")
        if not metadata.exists():
            write_json(metadata, spec)
        return dest.read_bytes() if binary else json.loads(dest.read_text())
    pending = dest.with_suffix(dest.suffix + ".pending")
    if pending.exists():
        raise RuntimeError(f"Uncertain prior request: inspect {pending.name} before retrying")
    pending.write_text(model + "\n")
    response = session.post(API + path, data=payload, headers={
        "model": model,
        "Content-Type": "application/msgpack" if binary else "application/json",
    }, timeout=(20, 180))
    if response.status_code != 200:
        # Never dump request headers, credentials, or a provider error body.
        try:
            message = str(response.json().get('message', ''))[:300]
        except ValueError:
            message = ''
        message = message.replace(session.headers['Authorization'].removeprefix('Bearer '), '[REDACTED]')
        if 400 <= response.status_code < 500:
            pending.unlink()  # Explicit rejection, not an uncertain completion.
        raise RuntimeError(f"{path}: HTTP {response.status_code}: {message}; no automatic retry")
    if binary:
        dest.write_bytes(response.content)
        data = response.content
    else:
        data = response.json()
        write_json(dest, data)
    write_json(metadata, spec)
    pending.unlink()
    return data


def credit(session):
    r = session.get(API + "/wallet/self/api-credit", timeout=20)
    if r.status_code != 200:
        raise RuntimeError(f"Credit check: HTTP {r.status_code}")
    return float(r.json()["credit"])


def mono48(source, target):
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", str(source),
                    "-ar", str(SR), "-ac", "1", "-c:a", "pcm_s16le", str(target)], check=True)


def mix(x, kind):
    x = sosfilt(butter(2, 65, "highpass", fs=SR, output="sos"), x)
    if kind in ("radio", "evp"):
        x = sosfilt(butter(2, [300 if kind == "radio" else 200, 3500], "bandpass", fs=SR, output="sos"), x)
    if kind in ("stone", "house", "evp"):
        # Keep the real performance in front. No artificial pitch lowering.
        seconds = 1.25 if kind == "stone" else .42
        t = np.arange(int(seconds * SR)) / SR
        ir = np.random.default_rng(42).normal(size=len(t)) * np.exp(-6.9*t/seconds)
        ir = sosfilt(butter(2, 2200, fs=SR, output="sos"), ir)
        ir /= max(np.sqrt(np.sum(ir**2)), 1e-12)
        ir = np.r_[np.zeros(int(.025*SR)), ir]
        wet = fftconvolve(x, ir)
        out = np.zeros(len(wet)); out[:len(x)] = x
        out += wet * (.13 if kind == "stone" else .065)
        x = out
    n = min(int(.012*SR), len(x)//2)
    x[:n] *= np.linspace(0, 1, n)
    x[-n:] *= np.linspace(1, 0, n)
    return x


def normalize(source, target, target_lufs=-24.2):
    # Two-pass measured normalization: old and new audition takes are level matched.
    pre = "acompressor=threshold=0.04:ratio=3:attack=4:release=100:makeup=1,"
    af = pre + f"loudnorm=I={target_lufs}:TP=-3:LRA=12:print_format=json"
    p = subprocess.run(["ffmpeg", "-hide_banner", "-i", str(source), "-af", af, "-f", "null", "-"],
                       text=True, capture_output=True, check=True)
    m, _ = json.JSONDecoder().raw_decode(p.stderr[p.stderr.rfind("{"):])
    if not np.isfinite(float(m['input_i'])):
        raise RuntimeError("Silent audition: " + source.name)
    af = (pre + f"loudnorm=I={target_lufs}:TP=-3:LRA=12:measured_I={m['input_i']}:"
          f"measured_TP={m['input_tp']}:measured_LRA={m['input_lra']}:"
          f"measured_thresh={m['input_thresh']}:offset={m['target_offset']}:linear=true")
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", str(source), "-af", af,
                    "-ar", str(SR), "-ac", "1", "-c:a", "pcm_s16le", str(target)], check=True)
    return m


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--key-file", type=Path, default=Path.home()/".claude/secrets/fish-audio.key")
    parser.add_argument("--generate", action="store_true")
    args = parser.parse_args()
    out = args.output.resolve(); out.mkdir(parents=True, exist_ok=True)
    raw = out/"source"; raw.mkdir(exist_ok=True)
    write_json(out/"plan.json", {"voices": VOICES, "takes": TAKES, "tts_model": "drama-3",
                                "scope": "Both maps: voices and vocal sounds only; auditions, not integrated"})
    # Conservative list-price estimate, capped before any paid request.
    estimate = len(VOICES)*.01 + sum(len(t['text'].encode()) for t in TAKES)*50/1_000_000
    if estimate > 1:
        raise RuntimeError("Audition exceeds the $1 request budget")
    session = requests.Session()
    session.headers['Authorization'] = 'Bearer ' + args.key_file.read_text().strip()
    balance_before = credit(session)
    budget_file = out/'budget.json'
    prior_budget = json.loads(budget_file.read_text()) if budget_file.exists() else {}
    first_balance = prior_budget.get('first_observed_balance', balance_before)
    if balance_before < estimate:
        raise RuntimeError("Insufficient credit for bounded audition")
    write_json(budget_file, {"list_price_estimate_usd": estimate, "balance_before": balance_before,
                            "first_observed_balance": first_balance})
    for role, spec in VOICES.items():
        cache = raw/(role+"_design.json")
        if not cache.exists() and not args.generate:
            raise RuntimeError("New paid requests require --generate")
        result = request(session, "/v1/voice-design", {**spec, "language": "en", "n": 1,
                         "seed": 73, "speed": .94 if role == "count" else 1.0}, "voice-design-1", cache)
        c = result['candidates'][0]
        (raw/(role+"_reference.wav")).write_bytes(base64.b64decode(c['audio_base64']))
        print('Voice reference ready:', role, flush=True)
    receipt = []
    for take in TAKES:
        name = take['id']; source = raw/(name+"_drama3.wav")
        if not source.exists() and not args.generate:
            raise RuntimeError("New paid requests require --generate")
        request(session, "/v1/tts", {"text": take['text'], "references": [{
            "audio": (raw/(take['voice']+"_reference.wav")).read_bytes(),
            "text": VOICES[take['voice']]['reference_text']}], "format": "wav",
            "sample_rate": 44100, "latency": "normal", "temperature": .7}, "drama-3", source, binary=True)
        converted = raw/(name+"_mono.wav"); mono48(source, converted)
        x, sr = sf.read(converted)
        if not .3 < len(x)/sr < 55 or np.max(np.abs(x)) < .001:
            raise RuntimeError('Unexpected/silent generated audio: '+name)
        mixed = raw/(name+"_mixed.wav"); sf.write(mixed, mix(x, take['fx']), SR, subtype="PCM_16")
        target = out/(name+"_new.wav")
        normalize(mixed, target, -28 if 'breath' in name else -24.2)
        old = ROOT/'Assets/HollowCurrent/Resources'/take['old']
        if not old.exists() and old.name == 'breath_hidden.wav':
            old = ROOT/'Assets/HollowCurrent/Resources/HiggsfieldAudio/breath_hidden.wav'
        current = out/(name+"_current.wav")
        normalize(old, current, -28 if 'breath' in name else -24.2)
        a, _ = sf.read(current); b, _ = sf.read(target)
        # Explicit old / 1.2 second gap / new comparison; no synthetic spoken labels.
        sf.write(out/(name+"_before_after.wav"), np.r_[a, np.zeros(int(1.2*SR)), b], SR, subtype="PCM_16")
        receipt.append({"id": name, "model": "drama-3", "replacement_for": take['old'],
                        "new_seconds": round(len(b)/SR, 3), "peak_dbfs": round(20*np.log10(max(np.abs(b))+1e-12), 2),
                        "sha256": hashlib.sha256(target.read_bytes()).hexdigest(),
                        "comparison_order": "current, 1.2 seconds silence, proposed",
                        "runtime_verified": False})
        write_json(out/"receipt.json", receipt)
        print('Audition ready:', name, receipt[-1]['new_seconds'], 'seconds', flush=True)
    balance_after = credit(session)
    write_json(out/"budget.json", {"list_price_estimate_usd": estimate, "balance_before": balance_before,
        "first_observed_balance": first_balance,
        "balance_after": balance_after, "observed_balance_change": round(balance_before-balance_after, 6),
        "note": "Account balance delta may include concurrent usage. No game asset was replaced."})
    print('Completed', len(receipt), 'auditions. Account balance delta:', round(balance_before-balance_after, 6), flush=True)


if __name__ == "__main__":
    main()
