"""Shared frame for the mod icons: every icon is <Mod>/icon.svg built from this frame plus its own artwork.

Run tools/render_icons.ps1 to regenerate the SVGs and render them to 256x256 icon.png files.
"""

DEFS = """
  <defs>
    <radialGradient id="bg" cx="50%" cy="42%" r="72%">
      <stop offset="0" stop-color="#3a3128"/>
      <stop offset="0.65" stop-color="#211c17"/>
      <stop offset="1" stop-color="#110e0b"/>
    </radialGradient>
    <linearGradient id="gold" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#f6d27a"/>
      <stop offset="0.5" stop-color="#d9a441"/>
      <stop offset="1" stop-color="#9c6b22"/>
    </linearGradient>
    <linearGradient id="wood" x1="0" y1="0" x2="1" y2="0">
      <stop offset="0" stop-color="#5a3a1f"/>
      <stop offset="0.45" stop-color="#8a5c32"/>
      <stop offset="1" stop-color="#4a2f18"/>
    </linearGradient>
    <linearGradient id="iron" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#b9bcc0"/>
      <stop offset="0.5" stop-color="#7d8288"/>
      <stop offset="1" stop-color="#4a4e53"/>
    </linearGradient>
    <linearGradient id="stone" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="#9a9488"/>
      <stop offset="1" stop-color="#5b564e"/>
    </linearGradient>
    <radialGradient id="glow" cx="50%" cy="50%" r="50%">
      <stop offset="0" stop-color="#ffb347" stop-opacity="0.55"/>
      <stop offset="1" stop-color="#ffb347" stop-opacity="0"/>
    </radialGradient>
    <filter id="shadow" x="-20%" y="-20%" width="140%" height="140%">
      <feDropShadow dx="0" dy="4" stdDeviation="4" flood-color="#000" flood-opacity="0.65"/>
    </filter>
  </defs>
"""

# Background, gold border and small knots in the corners.
FRAME = """
  <rect width="256" height="256" fill="url(#bg)"/>
  <rect x="9" y="9" width="238" height="238" rx="20" fill="none" stroke="url(#gold)" stroke-width="5"/>
  <rect x="17" y="17" width="222" height="222" rx="14" fill="none" stroke="#d9a441" stroke-opacity="0.35" stroke-width="1.5"/>
  <g fill="url(#gold)">
    <path d="M9 40 L40 9 L48 9 L9 48 Z"/>
    <path d="M247 40 L216 9 L208 9 L247 48 Z"/>
    <path d="M9 216 L40 247 L48 247 L9 208 Z"/>
    <path d="M247 216 L216 247 L208 247 L247 208 Z"/>
  </g>
"""


def icon(art: str) -> str:
    return ('<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256" viewBox="0 0 256 256">'
            + DEFS + FRAME + art + "</svg>\n")
