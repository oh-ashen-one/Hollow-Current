"""Remove pitched phonation from Fish breath masters, retaining their air envelope.

A noise vocoder transfers only slow per-band energy to independent noise. No
original waveform, vocal fundamental, vowel harmonics, or sigh pitch is mixed
back in. Deterministic seed makes regeneration reproducible.
"""
import numpy as np
from scipy.signal import butter, sosfilt

def unvoiced_breath(x, sr, loop=False):
    x=np.asarray(x,dtype=np.float64)
    rng=np.random.default_rng(20261025)
    # Six broad bands retain the breath colour without preserving vowel formants
    # at speech resolution. The 18 Hz envelope cannot carry vocal pitch.
    edges=[350,650,1100,1900,3200,5200,8500]
    smooth=butter(2,18,fs=sr,output='sos')
    out=np.zeros_like(x)
    for low,high in zip(edges,edges[1:]):
        band=butter(3,[low,high],btype='bandpass',fs=sr,output='sos')
        source=sosfilt(band,x)
        envelope=np.sqrt(np.maximum(sosfilt(smooth,source*source),0))
        carrier=sosfilt(band,rng.standard_normal(len(x)))
        carrier/=max(np.sqrt(np.mean(carrier*carrier)),1e-8)
        out+=carrier*envelope
    # Trim prolonged tails; short breaths with silence between them read as
    # trying to stay quiet. Retain the provider's timing within each breath.
    limit=6.5 if loop else 4.5
    out=out[:int(limit*sr)]
    peak=np.max(np.abs(out))
    if peak<1e-7:raise ValueError('Fish master contains no usable breath energy')
    out*=.65/peak
    fade=min(int(.035*sr),len(out)//4)
    out[:fade]*=np.linspace(0,1,fade);out[-fade:]*=np.linspace(1,0,fade)
    return out
