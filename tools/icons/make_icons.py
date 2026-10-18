"""Writes <Mod>/icon.svg for every mod. tools/render_icons.ps1 calls this, then renders the PNGs."""
import os

from frame import icon

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
PARCHMENT = "#efe2c4"

# Dashed outline = "not made yet": the theme of the two tracker icons.
BUILD = f"""
  <g fill="none" stroke="{PARCHMENT}" stroke-width="7" stroke-linecap="round" stroke-linejoin="round"
     stroke-dasharray="16 11" opacity="0.9">
    <path d="M36 126 L110 58 L184 126"/>
    <path d="M54 112 L54 210 L166 210 L166 112"/>
  </g>
  <g fill="none" stroke="{PARCHMENT}" stroke-width="5" stroke-linecap="round" stroke-dasharray="9 8" opacity="0.6">
    <path d="M54 160 L166 160"/>
    <path d="M110 112 L110 210"/>
  </g>
  <g filter="url(#shadow)" transform="translate(170 136) rotate(-38)">
    <rect x="-8" y="-6" width="16" height="86" rx="5" fill="url(#wood)" stroke="#2a1a0c" stroke-width="2.5"/>
    <rect x="-9" y="50" width="18" height="7" rx="2" fill="#3b2614"/>
    <rect x="-9" y="64" width="18" height="7" rx="2" fill="#3b2614"/>
    <rect x="-44" y="-46" width="88" height="42" rx="7" fill="url(#stone)" stroke="#2b2722" stroke-width="3"/>
    <rect x="-14" y="-50" width="28" height="50" rx="3" fill="url(#iron)" stroke="#2b2722" stroke-width="3"/>
    <path d="M-38 -38 L-24 -38" stroke="#c9c2b4" stroke-width="3" stroke-linecap="round" opacity="0.7"/>
  </g>
"""

CRAFT = f"""
  <g fill="{PARCHMENT}" fill-opacity="0.07" stroke="{PARCHMENT}" stroke-width="6" stroke-linecap="round"
     stroke-linejoin="round" opacity="0.9">
    <path d="M128 24 L145 44 L145 110 L111 110 L111 44 Z" pathLength="100" stroke-dasharray="6.5 4.5"/>
    <path d="M86 112 L170 112" pathLength="100" stroke-dasharray="18 10"/>
    <path d="M128 120 L128 132" fill="none"/>
    <circle cx="128" cy="140" r="7" fill="none"/>
  </g>
  <circle cx="128" cy="160" r="70" fill="url(#glow)"/>
  <g filter="url(#shadow)">
    <path d="M36 152 Q66 154 82 146 L214 146 L214 170 L186 170 Q170 176 166 194 L184 216 L76 216 L94 194
             Q90 176 74 170 L66 170 Q46 166 36 152 Z" fill="url(#iron)" stroke="#23262a" stroke-width="3.5"
          stroke-linejoin="round"/>
    <path d="M84 150 L210 150" stroke="#e4e6e8" stroke-width="3" stroke-linecap="round" opacity="0.8"/>
  </g>
  <g fill="#ffd27a">
    <path d="M64 108 l4 10 l10 4 l-10 4 l-4 10 l-4 -10 l-10 -4 l10 -4 Z"/>
    <path d="M196 120 l3 7 l7 3 l-7 3 l-3 7 l-3 -7 l-7 -3 l7 -3 Z"/>
    <path d="M178 96 l2 5 l5 2 l-5 2 l-2 5 l-2 -5 l-5 -2 l5 -2 Z"/>
  </g>
"""

TROPHY = f"""
  <g filter="url(#shadow)">
    <path d="M70 92 Q128 78 186 92 L186 168 Q186 208 128 230 Q70 208 70 168 Z"
          fill="url(#wood)" stroke="url(#gold)" stroke-width="5" stroke-linejoin="round"/>
  </g>
  <g fill="none" stroke="url(#gold)" stroke-width="8" stroke-linecap="round" stroke-linejoin="round">
    <path d="M112 116 Q94 92 70 80 Q52 72 42 52"/>
    <path d="M90 94 Q78 76 84 50"/>
    <path d="M70 80 Q50 82 32 76"/>
    <path d="M144 116 Q162 92 186 80 Q204 72 214 52"/>
    <path d="M166 94 Q178 76 172 50"/>
    <path d="M186 80 Q206 82 224 76"/>
  </g>
  <g>
    <ellipse cx="100" cy="124" rx="16" ry="8" transform="rotate(-25 100 124)" fill="#b39a6c" stroke="#3a2a18" stroke-width="2.5"/>
    <ellipse cx="156" cy="124" rx="16" ry="8" transform="rotate(25 156 124)" fill="#b39a6c" stroke="#3a2a18" stroke-width="2.5"/>
    <path d="M128 214 Q113 212 110 196 L103 148 Q102 128 116 120 L140 120 Q154 128 153 148 L146 196 Q143 212 128 214 Z"
          fill="#c9b48a" stroke="#3a2a18" stroke-width="3" stroke-linejoin="round"/>
    <path d="M128 132 L128 190" stroke="#a8916a" stroke-width="4" stroke-linecap="round"/>
    <ellipse cx="115" cy="152" rx="5" ry="6.5" fill="#1c140c"/>
    <ellipse cx="141" cy="152" rx="5" ry="6.5" fill="#1c140c"/>
    <ellipse cx="128" cy="204" rx="10" ry="7" fill="#2a1d12"/>
  </g>
  <g filter="url(#shadow)">
    <circle cx="198" cy="198" r="30" fill="#ff9e1a" stroke="#1a1410" stroke-width="4"/>
    <path d="M198 178 L214 198 L204 198 L204 216 L192 216 L192 198 L182 198 Z" fill="#1a1410"/>
  </g>
"""

UNLOCK = f"""
  <g filter="url(#shadow)">
    <path d="M30 140 Q128 46 226 140 Q128 234 30 140 Z" fill="#1a1612" stroke="{PARCHMENT}" stroke-width="10"
          stroke-linejoin="round"/>
    <circle cx="128" cy="140" r="38" fill="{PARCHMENT}"/>
    <circle cx="128" cy="140" r="17" fill="#1a1612"/>
    <circle cx="141" cy="127" r="8" fill="#1a1612"/>
  </g>
  <circle cx="194" cy="70" r="56" fill="url(#glow)"/>
  <g filter="url(#shadow)">
    <circle cx="194" cy="70" r="34" fill="#ff9e1a" stroke="#1a1410" stroke-width="4"/>
    <rect x="187" y="46" width="14" height="32" rx="6" fill="#1a1410"/>
    <circle cx="194" cy="90" r="7.5" fill="#1a1410"/>
  </g>
"""

# Two essences (Thunderblood and Frostfire) and a "x2" badge: one share per player.
ESSENCE = f"""
  <defs>
    <radialGradient id="thunder" cx="38%" cy="34%" r="70%">
      <stop offset="0" stop-color="#ffe7a8"/>
      <stop offset="0.35" stop-color="#e2453a"/>
      <stop offset="1" stop-color="#4a0d14"/>
    </radialGradient>
    <radialGradient id="frost" cx="38%" cy="34%" r="70%">
      <stop offset="0" stop-color="#e8fbff"/>
      <stop offset="0.4" stop-color="#4fb6e8"/>
      <stop offset="1" stop-color="#0d2a4a"/>
    </radialGradient>
  </defs>
  <circle cx="92" cy="150" r="80" fill="url(#glow)"/>
  <g filter="url(#shadow)">
    <circle cx="164" cy="112" r="44" fill="url(#frost)" stroke="#0b1622" stroke-width="4"/>
    <path d="M150 96 Q164 84 178 96" fill="none" stroke="#ffffff" stroke-width="4" stroke-linecap="round" opacity="0.6"/>
  </g>
  <g filter="url(#shadow)">
    <circle cx="100" cy="156" r="62" fill="url(#thunder)" stroke="#1a0a0c" stroke-width="4"/>
    <path d="M110 112 L84 160 L104 160 L92 200 L124 146 L104 146 L116 112 Z" fill="#ffe7a8" stroke="#5a1418"
          stroke-width="3" stroke-linejoin="round"/>
  </g>
  <g filter="url(#shadow)">
    <circle cx="196" cy="196" r="30" fill="#ff9e1a" stroke="#1a1410" stroke-width="4"/>
    <text x="196" y="207" text-anchor="middle" font-family="Georgia, serif" font-weight="bold" font-size="30"
          fill="#1a1410">&#215;2</text>
  </g>
"""

# A see-through spirit with an arrow passing straight through it: players walk through summons.
SUMMON = f"""
  <defs>
    <linearGradient id="spirit" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#d8fbff" stop-opacity="0.85"/>
      <stop offset="0.55" stop-color="#5fd0e0" stop-opacity="0.6"/>
      <stop offset="1" stop-color="#1f6f8a" stop-opacity="0.25"/>
    </linearGradient>
    <radialGradient id="spiritglow" cx="50%" cy="50%" r="50%">
      <stop offset="0" stop-color="#7fe6f2" stop-opacity="0.45"/>
      <stop offset="1" stop-color="#7fe6f2" stop-opacity="0"/>
    </radialGradient>
  </defs>
  <circle cx="128" cy="134" r="96" fill="url(#spiritglow)"/>
  <g filter="url(#shadow)" fill="none" stroke="{PARCHMENT}" stroke-width="9" stroke-linecap="round"
     stroke-linejoin="round">
    <path d="M30 152 L206 152"/>
    <path d="M188 130 L216 152 L188 174"/>
  </g>
  <path d="M74 214 L74 118 Q74 54 128 54 Q182 54 182 118 L182 214 L164 196 L146 214 L128 196 L110 214 L92 196 Z"
        fill="url(#spirit)" stroke="#d8fbff" stroke-opacity="0.8" stroke-width="3.5" stroke-linejoin="round"/>
  <ellipse cx="108" cy="114" rx="10" ry="14" fill="#0e2a33"/>
  <ellipse cx="148" cy="114" rx="10" ry="14" fill="#0e2a33"/>
  <ellipse cx="111" cy="108" rx="3.5" ry="4.5" fill="#d8fbff"/>
  <ellipse cx="151" cy="108" rx="3.5" ry="4.5" fill="#d8fbff"/>
"""

# A horned boss skull inside a circular "start over" arrow: reset the fight.
BOSS = f"""
  <circle cx="128" cy="136" r="80" fill="url(#glow)"/>
  <g filter="url(#shadow)" fill="none" stroke="{PARCHMENT}" stroke-width="10" stroke-linecap="round"
     stroke-linejoin="round">
    <path d="M196 92 A82 82 0 1 0 210 148"/>
    <path d="M168 86 L198 90 L202 60"/>
  </g>
  <g filter="url(#shadow)">
    <path d="M102 120 Q84 104 80 78 Q96 94 112 104 Z" fill="#c9b48a" stroke="#3a2a18" stroke-width="3" stroke-linejoin="round"/>
    <path d="M154 120 Q172 104 176 78 Q160 94 144 104 Z" fill="#c9b48a" stroke="#3a2a18" stroke-width="3" stroke-linejoin="round"/>
    <path d="M128 192 Q110 190 108 176 L104 166 Q90 150 96 128 Q104 104 128 102 Q152 104 160 128 Q166 150 152 166
             L148 176 Q146 190 128 192 Z" fill="#e3d6b8" stroke="#3a2a18" stroke-width="3.5" stroke-linejoin="round"/>
    <ellipse cx="115" cy="142" rx="10" ry="11" fill="#1c140c"/>
    <ellipse cx="141" cy="142" rx="10" ry="11" fill="#1c140c"/>
    <path d="M128 156 L122 168 L134 168 Z" fill="#1c140c"/>
    <path d="M114 178 L114 188 M122 180 L122 190 M134 180 L134 190 M142 178 L142 188" stroke="#3a2a18" stroke-width="3"
          stroke-linecap="round"/>
  </g>
"""

# A medal with a half-revealed "?": achievements that no longer hide what they need.
PEEK = f"""
  <g filter="url(#shadow)">
    <path d="M96 30 L128 96 L104 108 L70 40 Z" fill="#7a2a22" stroke="#2a1410" stroke-width="3" stroke-linejoin="round"/>
    <path d="M160 30 L128 96 L152 108 L186 40 Z" fill="#9a3a2c" stroke="#2a1410" stroke-width="3" stroke-linejoin="round"/>
  </g>
  <circle cx="128" cy="156" r="86" fill="url(#glow)"/>
  <g filter="url(#shadow)">
    <circle cx="128" cy="156" r="64" fill="url(#gold)" stroke="#5a3a10" stroke-width="5"/>
    <circle cx="128" cy="156" r="50" fill="none" stroke="#fff1c4" stroke-width="3" opacity="0.6"/>
  </g>
  <text x="128" y="186" text-anchor="middle" font-family="Georgia, serif" font-weight="bold" font-size="84"
        fill="#2a1a0c">?</text>
  <g fill="#ffd27a">
    <path d="M52 96 l4 10 l10 4 l-10 4 l-4 10 l-4 -10 l-10 -4 l10 -4 Z"/>
    <path d="M206 110 l3 7 l7 3 l-7 3 l-3 7 l-3 -7 l-7 -3 l7 -3 Z"/>
  </g>
"""

# A steaming bowl and the orange "not eaten yet" diamond.
FOOD = f"""
  <g fill="none" stroke="{PARCHMENT}" stroke-width="6" stroke-linecap="round" opacity="0.75">
    <path d="M100 96 Q88 80 100 64 Q112 48 100 32"/>
    <path d="M128 96 Q116 80 128 64 Q140 48 128 32"/>
    <path d="M156 96 Q144 80 156 64 Q168 48 156 32"/>
  </g>
  <circle cx="128" cy="150" r="80" fill="url(#glow)"/>
  <g filter="url(#shadow)">
    <path d="M40 120 L216 120 Q212 200 128 214 Q44 200 40 120 Z" fill="url(#wood)" stroke="#2a1a0c" stroke-width="4"
          stroke-linejoin="round"/>
    <ellipse cx="128" cy="120" rx="88" ry="14" fill="#7a3a1a" stroke="#2a1a0c" stroke-width="4"/>
    <ellipse cx="110" cy="117" rx="20" ry="5" fill="#c2662e"/>
  </g>
  <g filter="url(#shadow)">
    <rect x="178" y="166" width="40" height="40" rx="4" transform="rotate(45 198 186)" fill="#ff9e1a"
          stroke="#1a1410" stroke-width="4"/>
  </g>
"""

# A fish on a hook.
FISH = f"""
  <circle cx="120" cy="150" r="84" fill="url(#glow)"/>
  <g fill="none" stroke="{PARCHMENT}" stroke-width="6" stroke-linecap="round" opacity="0.85">
    <path d="M176 28 L176 96 Q176 122 152 122 Q132 122 132 104"/>
  </g>
  <g filter="url(#shadow)">
    <path d="M36 158 Q92 96 168 150 L214 118 L206 158 L214 198 L168 166 Q92 220 36 158 Z" fill="#6aa7c8"
          stroke="#13283a" stroke-width="4" stroke-linejoin="round"/>
    <path d="M60 158 Q100 136 150 152" fill="none" stroke="#d8eef8" stroke-width="4" stroke-linecap="round" opacity="0.6"/>
    <circle cx="70" cy="152" r="6" fill="#13283a"/>
  </g>
"""

# A paw print and claw marks: creatures to slay.
BEAST = f"""
  <circle cx="128" cy="140" r="86" fill="url(#glow)"/>
  <g fill="none" stroke="{PARCHMENT}" stroke-width="8" stroke-linecap="round" opacity="0.8">
    <path d="M150 40 L206 120"/>
    <path d="M128 44 L190 136"/>
    <path d="M106 50 L170 148"/>
  </g>
  <g filter="url(#shadow)" fill="url(#iron)" stroke="#23262a" stroke-width="3.5">
    <ellipse cx="92" cy="172" rx="40" ry="34"/>
    <ellipse cx="50" cy="126" rx="14" ry="18"/>
    <ellipse cx="78" cy="102" rx="14" ry="19"/>
    <ellipse cx="110" cy="102" rx="14" ry="19"/>
    <ellipse cx="136" cy="128" rx="14" ry="18"/>
  </g>
"""

# A seed sprouting under a ring of the world.
SEED = f"""
  <circle cx="128" cy="140" r="88" fill="url(#glow)"/>
  <circle cx="128" cy="140" r="76" fill="none" stroke="{PARCHMENT}" stroke-width="5" stroke-dasharray="14 10"
          opacity="0.6"/>
  <g filter="url(#shadow)">
    <path d="M128 150 Q128 104 128 82" stroke="#5f9a3a" stroke-width="8" fill="none" stroke-linecap="round"/>
    <path d="M128 106 Q96 70 70 84 Q92 116 128 106 Z" fill="#7dbb4a" stroke="#203a12" stroke-width="3"/>
    <path d="M128 92 Q160 56 188 70 Q164 102 128 92 Z" fill="#8fcb58" stroke="#203a12" stroke-width="3"/>
    <ellipse cx="128" cy="176" rx="36" ry="30" fill="url(#wood)" stroke="#2a1a0c" stroke-width="4"/>
    <path d="M110 166 Q128 154 146 166" stroke="#c99a5e" stroke-width="3" fill="none" opacity="0.7"/>
  </g>
"""

# A compass rose with its four points.
COMPASS = f"""
  <circle cx="128" cy="140" r="90" fill="url(#glow)"/>
  <g filter="url(#shadow)">
    <circle cx="128" cy="140" r="78" fill="#1a1612" stroke="url(#gold)" stroke-width="6"/>
    <path d="M128 70 L142 140 L128 210 L114 140 Z" fill="url(#gold)" stroke="#5a3a10" stroke-width="3"
          stroke-linejoin="round"/>
    <path d="M58 140 L128 126 L198 140 L128 154 Z" fill="{PARCHMENT}" stroke="#5a3a10" stroke-width="3"
          stroke-linejoin="round"/>
    <path d="M128 70 L142 140 L114 140 Z" fill="#ff9e1a"/>
    <circle cx="128" cy="140" r="8" fill="#1a1612" stroke="url(#gold)" stroke-width="3"/>
  </g>
"""

# A chair by the fire.
COMFORT = f"""
  <circle cx="170" cy="170" r="70" fill="url(#glow)"/>
  <g filter="url(#shadow)">
    <path d="M48 52 L48 214 M48 140 L118 140 L118 214 M48 52 L92 52 L92 140" fill="none" stroke="url(#wood)"
          stroke-width="16" stroke-linecap="round" stroke-linejoin="round"/>
    <rect x="40" y="128" width="90" height="16" rx="5" fill="url(#wood)" stroke="#2a1a0c" stroke-width="3"/>
  </g>
  <g filter="url(#shadow)">
    <path d="M146 214 L206 214" stroke="#3b2614" stroke-width="10" stroke-linecap="round"/>
    <path d="M176 206 Q140 176 166 140 Q170 162 182 166 Q178 140 194 120 Q196 150 210 170 Q214 196 176 206 Z"
          fill="#ff9e1a" stroke="#7a2a0a" stroke-width="3" stroke-linejoin="round"/>
    <path d="M178 202 Q162 186 176 168 Q182 184 192 188 Q194 198 178 202 Z" fill="#ffe7a8"/>
  </g>
"""

# A stack of coins and a "new" star.
COINS = f"""
  <circle cx="120" cy="160" r="84" fill="url(#glow)"/>
  <g filter="url(#shadow)" stroke="#5a3a10" stroke-width="3.5">
    <ellipse cx="110" cy="200" rx="58" ry="16" fill="url(#gold)"/>
    <rect x="52" y="168" width="116" height="32" fill="url(#gold)" stroke="none"/>
    <ellipse cx="110" cy="168" rx="58" ry="16" fill="url(#gold)"/>
    <rect x="52" y="136" width="116" height="32" fill="url(#gold)" stroke="none"/>
    <ellipse cx="110" cy="136" rx="58" ry="16" fill="url(#gold)"/>
    <rect x="52" y="104" width="116" height="32" fill="url(#gold)" stroke="none"/>
    <ellipse cx="110" cy="104" rx="58" ry="16" fill="#f6d27a"/>
    <path d="M52 104 L52 200 M168 104 L168 200" fill="none"/>
  </g>
  <g filter="url(#shadow)">
    <path d="M196 40 l10 24 l26 2 l-20 17 l6 25 l-22 -14 l-22 14 l6 -25 l-20 -17 l26 -2 Z" fill="#ff9e1a"
          stroke="#1a1410" stroke-width="4" stroke-linejoin="round"/>
  </g>
"""

# A trophy cup with a check: achievements that count again.
CUP = f"""
  <circle cx="128" cy="130" r="88" fill="url(#glow)"/>
  <g filter="url(#shadow)">
    <path d="M70 52 L186 52 L180 110 Q172 156 128 162 Q84 156 76 110 Z" fill="url(#gold)" stroke="#5a3a10"
          stroke-width="4" stroke-linejoin="round"/>
    <path d="M74 66 Q36 66 42 98 Q48 124 82 128" fill="none" stroke="url(#gold)" stroke-width="10"/>
    <path d="M182 66 Q220 66 214 98 Q208 124 174 128" fill="none" stroke="url(#gold)" stroke-width="10"/>
    <rect x="116" y="160" width="24" height="30" fill="url(#gold)" stroke="#5a3a10" stroke-width="3"/>
    <rect x="84" y="188" width="88" height="22" rx="4" fill="url(#wood)" stroke="#2a1a0c" stroke-width="3"/>
    <path d="M100 100 L120 122 L158 80" fill="none" stroke="#1a1410" stroke-width="12" stroke-linecap="round"
          stroke-linejoin="round"/>
  </g>
"""

# Metal bars, one still a dashed outline: materials you haven't found yet.
BARS = f"""
  <circle cx="128" cy="150" r="88" fill="url(#glow)"/>
  <g filter="url(#shadow)" stroke-linejoin="round">
    <path d="M40 196 L64 160 L132 160 L108 196 Z" fill="#c07a3a" stroke="#3a2010" stroke-width="4"/>
    <path d="M108 196 L132 160 L132 176 L108 212 Z" fill="#8a5226" stroke="#3a2010" stroke-width="4"/>
    <path d="M40 196 L108 196 L108 212 L40 212 Z" fill="#a8652e" stroke="#3a2010" stroke-width="4"/>
    <path d="M70 150 L94 114 L162 114 L138 150 Z" fill="url(#iron)" stroke="#23262a" stroke-width="4"/>
    <path d="M138 150 L162 114 L162 130 L138 166 Z" fill="#4a4e53" stroke="#23262a" stroke-width="4"/>
    <path d="M70 150 L138 150 L138 166 L70 166 Z" fill="#7d8288" stroke="#23262a" stroke-width="4"/>
  </g>
  <g fill="none" stroke="{PARCHMENT}" stroke-width="5" stroke-dasharray="10 8" stroke-linejoin="round" opacity="0.85">
    <path d="M130 100 L154 64 L222 64 L198 100 Z"/>
    <path d="M198 100 L222 64 L222 80 L198 116 L130 116 L130 100"/>
  </g>
"""

ICONS = {"MaterialTracker": BARS, "BestiaryTracker": BEAST, "ComfortHelper": COMFORT, "ExploreHelper": COMPASS, "FishTracker": FISH, "FoodTracker": FOOD, "MerchantTracker": COINS, "ModdedAchievements": CUP, "WorldSeed": SEED, "AchievementPeek": PEEK, "BossReset": BOSS, "BuildTracker": BUILD, "CraftTracker": CRAFT, "EssenceForAll": ESSENCE, "SummonTweaks": SUMMON, "TrophyAssister": TROPHY,
         "UnlockAwareness": UNLOCK}

for mod, art in ICONS.items():
    with open(os.path.join(ROOT, mod, "icon.svg"), "w", encoding="utf-8", newline="\n") as f:
        f.write(icon(art))
    print(f"wrote {mod}/icon.svg")
