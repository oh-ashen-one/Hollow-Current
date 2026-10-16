"""Render all eight screens and textures; keep the variants folder together."""
from pathlib import Path
import sys
sys.path.insert(0, str(Path(__file__).resolve().parent.parent))
from render_core import render
if __name__ == "__main__":
    render('03-last-light')
