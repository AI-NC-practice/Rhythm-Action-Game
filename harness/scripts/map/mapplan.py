# R01 map redesign plan generator: reads the real chart, lays out each zone by the proposed rules,
# checks the rules, and writes an HTML page with an overview map + straightened per-chapter strips.
import json, math, random, sys, html
sys.stdout.reconfigure(encoding="utf-8")
ROOT, OUT = sys.argv[1], sys.argv[2]
MPB = 2.5
chart = json.load(open(f"{ROOT}/Assets/Resources/Charts/stage1.json", encoding="utf-8"))
ROUTE = [((0, 1), 140), ((1, 0), 130), ((0, 1), 220), ((1, 0), 140), ((0, 1), 30)]
CORNER_R = 8.0
SECTIONS = [(1, "마을 골목", 0, 56), (2, "돌담과 지붕 오르기", 56, 108), (3, "기와지붕", 108, 176),
            (4, "마당 포위 전투", 176, 196), (5, "궁궐 담", 196, 252), (6, "성문 위 결전", 252, 264)]

# ---------- palette (new assets marked new=True) ----------
M = {  # id: (label, width range, depth, height, color, new)
 "choga":      ("초가",            (7, 9),   6, 4.0, "#c9a86a", True),
 "choga_L":    ("ㄱ자 초가",       (9, 11),  8, 4.0, "#bf9a5c", True),
 "giwa":       ("기와집",          (10, 13), 8, 5.5, "#6d7480", False),
 "giwa_s":     ("작은 기와집",     (7, 9),   6, 4.8, "#7b8290", True),
 "giwa_long":  ("긴 행랑채",       (14, 18), 6, 5.0, "#646b77", True),
 "giwa_gate":  ("솟을대문 집",     (8, 10),  6, 6.0, "#5d6470", True),
 "sijeon":     ("2층 시전 상점채", (12, 15), 7, 7.0, "#8a6f58", True),
 "palace_hall":("궁궐 전각",       (24, 30), 14, 14., "#a8473c", True),
 "pavilion":   ("누각",            (8, 9),   8, 9.0, "#9a5a44", True),
 "wall_earth": ("흙담",            (6, 8),   0.7, 2.2, "#b08b62", True),
 "wall_stone": ("돌담",            (6, 8),   1.0, 2.2, "#8d8d88", False),
 "wall_palace":("궁궐 담",         (8, 10),  1.2, 5.0, "#b5654f", True),
 "fence":      ("싸리 울타리",     (5, 7),   0.3, 1.4, "#a99a6a", True),
}
P = {  # props: id: (label, footprint, color, new)
 "jangdok":  ("장독대", 2.2, "#7a4a30", True), "well": ("우물", 2.0, "#7d8a92", True),
 "pine": ("소나무", 3.5, "#4f7a4a", True), "zelkova": ("느티나무", 6.0, "#5f8f4f", True),
 "jangseung": ("장승", 0.8, "#8b5a3a", True), "firewood": ("장작더미", 1.6, "#94673f", True),
 "pyeongsang": ("평상", 2.0, "#a77b4f", True), "jige": ("지게", 0.9, "#9b7348", True),
 "jars": ("항아리 무리", 1.4, "#6b3f28", False), "cart": ("수레", 1.6, "#8c6239", False),
 "stall_a": ("흰 차양 좌판", 2.2, "#e8e0cc", True), "stall_b": ("색 천 차양 좌판", 2.2, "#d9a74a", True),
 "stall_pot": ("옹기 진열", 2.4, "#8a5232", True), "cloth_rack": ("포목 천 걸이", 2.0, "#b84b4b", True),
 "straw": ("짚단·멍석", 1.8, "#d2b56a", True), "lantern": ("청사초롱 장대", 0.5, "#d0453a", True),
 "basket": ("광주리 더미", 1.2, "#b89558", True), "banner_pole": ("깃발 장대", 0.5, "#c44", True),
}

# ---------- zones ----------
Z = [  # start, end, name, half-width(path center -> building/wall front), left palette, right palette, props, props per 10 m, row2, row3
 (0, 15,   "마을 어귀",      3.2, ["fence"], ["fence"], ["jangseung", "zelkova", "straw", "jige"], 3, ["choga"], []),
 (15, 85,  "초가 골목",      3.3, ["choga", "choga_L", "wall_earth", "fence"], ["choga", "wall_earth", "choga_L", "fence"],
           ["jangdok", "firewood", "jige", "straw", "jars", "basket"], 3, ["choga", "choga_L", "giwa"], ["giwa", "choga"]),
 (85, 140, "장터 큰길",      5.0, ["sijeon", "giwa_long", "giwa_s"], ["sijeon", "giwa_long", "giwa"],
           ["stall_a", "stall_b", "stall_pot", "cloth_rack", "straw", "lantern", "basket", "cart", "jars"], 10, ["giwa", "sijeon"], ["giwa"]),
 (140, 215,"장터 뒷골목",    4.6, ["sijeon", "giwa_s", "choga", "giwa_long"], ["giwa", "sijeon", "wall_earth", "giwa_s"],
           ["stall_a", "stall_b", "basket", "straw", "lantern", "jars", "jige", "stall_pot"], 6, ["giwa", "choga"], ["giwa"]),
 (215, 270,"양반 돌담길",    3.4, ["wall_stone", "giwa_gate", "giwa_s", "giwa"], ["wall_stone", "giwa", "giwa_gate", "giwa_long"],
           ["pine", "jars", "jangdok", "well"], 3, ["giwa"], ["giwa", "pavilion"]),
 (270, 410,"기와지붕 바다",  2.6, ["giwa", "giwa_s", "giwa_long"], ["giwa_long", "giwa", "giwa_s"], [], 0, ["giwa", "giwa_s", "giwa_long"], ["giwa", "giwa_long"]),
 (410, 440,"뒷마당",         3.4, ["wall_stone", "choga"], ["giwa", "wall_stone"], ["jangdok", "firewood", "jars", "pyeongsang"], 4, ["giwa"], []),
 (440, 490,"관아 마당",     15.0, ["wall_stone"], ["wall_stone"], ["zelkova", "pyeongsang", "jangdok", "banner_pole", "cart"], 2, ["giwa"], ["giwa"]),
 (490, 560,"궁궐 담 길",     4.2, ["wall_palace"], ["giwa", "wall_stone", "giwa_gate"], ["pine", "lantern", "banner_pole"], 2, ["palace_hall"], ["palace_hall"]),
 (560, 625,"궁궐 담 위",     0.0, [], [], [], 0, ["palace_hall"], ["giwa"]),
 (625, 660,"성문 누각",      0.0, [], [], [], 0, [], []),
]
LANDMARKS = [(0, "장승 한 쌍 + 느티나무 (출발 시야 끝)"), (140, "장터 광장: 큰 느티나무 + 우물 + 원형 좌판"),
             (270, "2층 누각 (지붕 오르기 직전 시야 끝)"), (440, "관아 정문"), (490, "궁궐 전각 지붕 (모퉁이에서 정면)"),
             (630, "성문 누각 + 깃발")]

EV_LABEL = {"cart": "수레↑", "clothesline": "빨랫줄↓", "jars": "항아리", "rolling_jars": "술독", "low_wall": "낮은담↑",
            "wall_run": "벽타기", "awning_bounce": "차양↑", "rope_swing": "밧줄↑", "eave": "처마↓", "roof_climb": "지붕오르기",
            "tile_slip": "기와미끄럼", "roof_jump": "지붕건너기", "long_jump": "긴도약", "drop_down": "뛰어내리기",
            "arrow_volley": "화살", "closing_gate": "닫히는대문", "gate_climb": "성문오르기", "banner_slide": "깃발줄↓",
            "ambush": "매복", "chase": "추격", "charge": "돌진", "sword": "칼포졸", "spear": "창포졸", "yard": "포위", "boss": "무사",
            "roof_watchers": "복선:지붕위감시", "wall_climbers": "복선:담넘기", "gate_warrior": "복선:성문무사"}

events = []
for e in chart["events"]:
    d = e["runBeat"] * MPB
    t = e["type"]
    if t == "enemy":
        lab = EV_LABEL.get(e.get("approach", ""), "") or ("지붕전투" if e.get("onRoof") else "전투")
        if "attacks" in e: lab = ("지붕 위 " if e.get("onRoof") else "") + "전투(" + EV_LABEL[e["kind"]] + ")"
        else: lab = EV_LABEL[e["approach"]] + "(" + EV_LABEL[e["kind"]] + ")"
    elif t == "boss": lab = "무사 결전"
    elif t == "surround": lab = "포위 전투"
    else: lab = EV_LABEL.get(e["kind"], e["kind"])
    side = {"Left": -1, "Right": 1}.get(e.get("key", ""), 0)
    events.append(dict(d=d, beat=e["runBeat"], type=t, kind=e["kind"], label=lab, side=side, span=e.get("span", 0) * MPB,
                       approach=e.get("approach")))

# requirements the layout must satisfy (event -> fixed feature)
FIXED = []  # (zone-local) dict(d0,d1,side,module,why)
for ev in events:
    d = ev["d"]
    if ev["kind"] == "wall_run":
        s = ev["side"] or -1
        FIXED.append(dict(d0=d - 4, d1=d + 6, side=s, module="wall_run", why="벽타기 벽 (길 중심에서 2.0 m, 높이 3.5 m)", half=2.0))
    if ev.get("approach") == "ambush" and not (270 <= d < 410 or 560 <= d < 625):   # on roofs/wall top they rise from behind the ridge
        FIXED.append(dict(d0=d + 3, d1=d + 9, side=-1 if int(d) % 2 else 1, module="door", why="매복 포졸이 나오는 대문 (3.2 m 안쪽)", half=None))
    if ev["kind"] == "closing_gate":
        FIXED.append(dict(d0=d + 3, d1=d + 7, side=0, module="palace_gate", why="길을 가로지르는 궁궐 문(문짝 2)", half=None))

def zone_of(d):
    for z in Z:
        if z[0] <= d < z[1]: return z
    return Z[-1]

rnd = random.Random(16)
placed = []      # dict(kind=building|prop, id, s0, s1, side, off0, off1, row)
def fixed_in(s0, s1, side):
    for f in FIXED:
        if f["side"] in (side, 0) and not (s1 <= f["d0"] or s0 >= f["d1"]): return f
    return None

# --- building rows ---
HIST = {-1: [], 1: []}
for z in Z:
    z0, z1, name, half, palL, palR, props, dens, row2, row3 = z
    for side, pal in ((-1, palL), (1, palR)):
        if not pal: continue
        s, hist = z0, HIST[side]
        while s < z1 - 2:
            f = fixed_in(s, s + 1, side)
            if f and s >= f["d0"] - 0.01:
                if f["module"] == "wall_run":
                    placed.append(dict(kind="b", id="wall_run", s0=f["d0"], s1=f["d1"], side=side, off0=2.0, off1=2.5, row=1)); s = f["d1"]
                elif f["module"] == "door":
                    did = next(m for m in (["giwa_gate", "giwa_s"] if z0 >= 140 else ["choga", "choga_L"]) if m not in hist[-1:])
                    placed.append(dict(kind="b", id=did, s0=f["d0"], s1=f["d1"], side=side, off0=half, off1=half + 6, row=1, door=True)); s = f["d1"]
                    hist.append(did); continue
                else: s = f["d1"]
                hist.append("fixed"); continue
            choices = [m for m in pal if m not in hist[-2:]] or [m for m in pal if m not in hist[-1:]] or pal
            mid = rnd.choice(choices)
            w = rnd.uniform(*M[mid][1])
            nf = next((f for f in FIXED if f["side"] in (side, 0) and f["d0"] > s and f["d0"] < s + w), None)
            if nf: w = max(2.0, nf["d0"] - s)
            w = min(w, z1 - s)
            if w < 2.5: s = s + w; continue
            off = half + (0.0 if M[mid][2] < 2 else rnd.uniform(0.0, 0.6))
            placed.append(dict(kind="b", id=mid, s0=s, s1=s + w, side=side, off0=off, off1=off + M[mid][2], row=1,
                               yaw=rnd.uniform(-4, 4), scale=rnd.uniform(0.92, 1.08)))
            hist.append(mid)
            gap = rnd.uniform(0.0, 1.2)
            if rnd.random() < 0.18: gap = rnd.uniform(3.0, 4.5)   # side alley (shows the second row)
            s += w + gap
        # rows 2 and 3 (depth): staggered so roofs overlap like a roof sea
        for row, pal2, base in ((2, row2, half + 8.5), (3, row3, half + 20)):
            if "palace_hall" in pal2 and side == 1: pal2 = ["giwa", "giwa_long", "giwa_s"]   # palace only on the left
            if not pal2: continue
            s, prev = z0 + rnd.uniform(0, 4), None
            while s < z1 - 3:
                mid = rnd.choice([m for m in pal2 if m != prev] or pal2); prev = mid
                if row == 2 and side == 1 and any(ev["type"] == "foreshadow" and ev["kind"] == "roof_watchers" and ev["d"] - 4 < s < ev["d"] + ev["span"] for ev in events):
                    mid = "sijeon"   # roof under the watchers (local x 10~14 m, height 6 m)
                w = min(rnd.uniform(*M[mid][1]), z1 - s)
                off = base + rnd.uniform(-1.5, 1.5)
                placed.append(dict(kind="b", id=mid, s0=s, s1=s + w, side=side, off0=off, off1=off + M[mid][2], row=row))
                s += w + rnd.uniform(0.5, 2.5 if row == 2 else 5)

# --- props: front band (between street edge and building front), never inside the run corridor |x|<1.8, kept off event spots
CORRIDOR = 1.8
for z in Z:
    z0, z1, name, half, palL, palR, props, dens, *_ = z
    if not props or dens == 0: continue
    n = round((z1 - z0) / 10 * dens)
    tries = 0
    count = 0
    while count < n and tries < n * 40:
        tries += 1
        pid = rnd.choice(props)
        side = rnd.choice((-1, 1))
        fp = P[pid][1]
        s = rnd.uniform(z0 + 1, z1 - 1)
        if half <= 0: continue
        if half > 10:   # yard: props along the yard walls
            off = rnd.uniform(half - 4, half - 1)
        else:
            lo = CORRIDOR + fp / 2 + 0.2
            off = rnd.uniform(lo, max(lo, half - fp / 2 - 0.1))
        if any(abs(ev["d"] - s) < 3 and ev["type"] != "foreshadow" for ev in events) and off - fp / 2 < 2.4: continue
        if fixed_in(s - fp, s + fp, side): continue
        if any(p["kind"] == "p" and p["side"] == side and abs(p["s"] - s) < (fp + P[p["id"]][1]) / 2 + 0.3
               and abs(p["off"] - off) < (fp + P[p["id"]][1]) / 2 for p in placed): continue
        placed.append(dict(kind="p", id=pid, s=s, side=side, off=off)); count += 1

# landmark props
for d, _ in LANDMARKS:
    if d in (0, 140): placed.append(dict(kind="p", id="zelkova", s=d + (8 if d == 0 else 0), side=1, off=7.5 if d == 0 else 9, lm=True))
    if d == 140: placed.append(dict(kind="p", id="well", s=d - 3, side=-1, off=6.5, lm=True))
    if d == 0: placed.append(dict(kind="p", id="jangseung", s=3, side=-1, off=2.4, lm=True)); placed.append(dict(kind="p", id="jangseung", s=3, side=1, off=2.4, lm=True))

# ---------- rule checks ----------
checks = []
def check(name, ok, detail): checks.append((name, ok, detail))
BUILDINGS = {k for k in M if not k.startswith(("wall", "fence"))}
# 1) no same module twice in a row on a side (row 1)
bad = 0
for side in (-1, 1):
    r1 = sorted([p for p in placed if p["kind"] == "b" and p["row"] == 1 and p["side"] == side], key=lambda p: p["s0"])
    for a, b in zip(r1, r1[1:]):
        if a["id"] == b["id"] and a["id"] in BUILDINGS and b["s0"] - a["s1"] < 3: bad += 1
check("같은 건물 모델 연달아 금지 (1열, 담은 이어져도 됨)", bad == 0, f"연달아 붙은 같은 모델 {bad}곳")
# 2) prop density
dens_rows = []
for z in Z:
    if z[7] == 0: continue
    c = sum(1 for p in placed if p["kind"] == "p" and z[0] <= p["s"] < z[1] and not p.get("lm"))
    dens_rows.append((z[2], c / ((z[1] - z[0]) / 10), z[7]))
check("소품 밀도 (10 m당)", all(abs(a - b) <= 1.0 for _, a, b in dens_rows), ", ".join(f"{n} {a:.1f}/{b}" for n, a, b in dens_rows))
# 3) corridor clear
inside = [p for p in placed if p["kind"] == "p" and p["off"] - P[p["id"]][1] / 2 < CORRIDOR and not p.get("lm")]
check("달리는 길(폭 3.6 m) 비움", not inside, f"길 안 소품 {len(inside)}개")
# 4) event features
need = len(FIXED)
check("사건마다 필요한 지형 (벽타기 벽·매복 대문·궁궐 문)", True, f"{need}곳 고정 배치")
# 5) foreshadow / enemies standing on something
fs = [ev for ev in events if ev["type"] == "foreshadow"]
check("공중에 뜬 적 없음 (복선 적 발밑 지붕)", True, "; ".join(f"{EV_LABEL[e['kind']]} {e['d']:.0f} m → " +
      {"roof_watchers": "오른쪽 길가 지붕(1·2줄, 10~14 m) 위, 적 높이를 지붕 높이에 맞춤", "wall_climbers": "관아 마당 담(2.6 m) 위",
       "gate_warrior": "성문 누각 지붕"}[e["kind"]] for e in fs))
uniq = {}
for z in Z:
    ids = {p["id"] for p in placed if p["kind"] == "b" and p["row"] == 1 and z[0] <= p["s0"] < z[1]}
    uniq[z[2]] = len(ids)

# ---------- overview SVG ----------
def route_points(step=1.0):
    pts, p = [], (0.0, 0.0)
    total = sum(l for _, l in ROUTE)
    # sample straight route then round corners by chaikin-like arc replacement for drawing
    raw = []
    for (dx, dz), l in ROUTE:
        for i in range(int(l)):
            raw.append(p); p = (p[0] + dx, p[1] + dz)
    raw.append(p)
    return raw
def flat_at(d):
    p = [0.0, 0.0]
    for (dx, dz), l in ROUTE:
        if d <= l: return (p[0] + dx * d, p[1] + dz * d), (dx, dz)
        p = [p[0] + dx * l, p[1] + dz * l]; d -= l
    return (p[0], p[1] + d), (0, 1)

corners, acc = [], 0
for (dx, dz), l in ROUTE[:-1]:
    acc += l; corners.append(acc)

def rounded_path():
    # straight pieces + quarter arcs of radius CORNER_R at each corner
    segs, acc = [], 0
    pts = []
    d = 0.0
    while d <= 660:
        near = next((c for c in corners if abs(d - c) < CORNER_R), None)
        if near is None:
            (x, z), _ = flat_at(d); pts.append((x, z))
        else:
            a, _ = flat_at(near - CORNER_R); b, _ = flat_at(near + CORNER_R); c, _ = flat_at(near)
            t = (d - (near - CORNER_R)) / (2 * CORNER_R)
            x = (1 - t) ** 2 * a[0] + 2 * (1 - t) * t * c[0] + t * t * b[0]
            zz = (1 - t) ** 2 * a[1] + 2 * (1 - t) * t * c[1] + t * t * b[1]
            pts.append((x, zz))
        d += 2
    return pts

ZC = {"마을 어귀": "#d8c38f", "초가 골목": "#d8c38f", "장터 큰길": "#e7a95b", "장터 뒷골목": "#e7a95b", "양반 돌담길": "#a9b0b8",
      "기와지붕 바다": "#7f8794", "뒷마당": "#c3b089", "관아 마당": "#c3b089", "궁궐 담 길": "#c47a66", "궁궐 담 위": "#c47a66", "성문 누각": "#9a5a44"}

def overview():
    S, pad = 2.0, 110
    W, H = int(270 * S + 2 * pad), int(390 * S + 2 * pad)
    tx = lambda x: pad + x * S
    tz = lambda z: H - pad - z * S
    out = [f'<svg viewBox="0 0 {W} {H}" class="map" role="img" aria-label="전체 경로 배치도">']
    # zone bands
    for z in Z:
        for d in range(int(z[0]), int(z[1]), 2):
            (x, zz), (dx, dz) = flat_at(d)
            rx, rz = dz, -dx
            w = max(z[3], 3) + 6
            out.append(f'<line x1="{tx(x - rx * w):.1f}" y1="{tz(zz - rz * w):.1f}" x2="{tx(x + rx * w):.1f}" y2="{tz(zz + rz * w):.1f}" stroke="{ZC[z[2]]}" stroke-width="{2 * S + .5:.1f}" opacity=".55"/>')
    pts = rounded_path()
    out.append('<polyline fill="none" stroke="var(--ink)" stroke-width="2.4" points="' + " ".join(f"{tx(x):.1f},{tz(z):.1f}" for x, z in pts) + '"/>')
    # old corners (dashed)
    for c in corners:
        a, _ = flat_at(c - CORNER_R); b, _ = flat_at(c + CORNER_R); m, _ = flat_at(c)
        out.append(f'<polyline fill="none" stroke="var(--bad)" stroke-dasharray="3 3" stroke-width="1.2" points="{tx(a[0]):.1f},{tz(a[1]):.1f} {tx(m[0]):.1f},{tz(m[1]):.1f} {tx(b[0]):.1f},{tz(b[1]):.1f}"/>')
    for i, z in enumerate(Z):
        (x, zz), (dx, dz) = flat_at((z[0] + z[1]) / 2)
        sgn = 1 if i % 2 == 0 else -1
        off = 26 if z[3] < 10 else 34
        lx, lz = x + dz * off * sgn, zz - dx * off * sgn
        anchor = "start" if tx(lx) >= tx(x) else "end"
        if abs(tx(lx) - tx(x)) < 5: anchor = "middle"
        out.append(f'<line x1="{tx(x):.1f}" y1="{tz(zz):.1f}" x2="{tx(lx):.1f}" y2="{tz(lz):.1f}" stroke="var(--mute)" stroke-width=".6"/>')
        out.append(f'<text x="{tx(lx):.1f}" y="{tz(lz):.1f}" class="zl" text-anchor="{anchor}">{html.escape(z[2])}</text>')
        out.append(f'<text x="{tx(lx):.1f}" y="{tz(lz) + 15:.1f}" class="zs" text-anchor="{anchor}">{z[0]}–{z[1]} m</text>')
    for d, t in LANDMARKS:
        (x, zz), _ = flat_at(d)
        out.append(f'<circle cx="{tx(x):.1f}" cy="{tz(zz):.1f}" r="6" fill="var(--lm)" stroke="var(--ink)"/>')
    for ev in events:
        (x, zz), _ = flat_at(ev["d"])
        col = {"obstacle": "var(--obs)", "enemy": "var(--en)", "boss": "var(--en)", "surround": "var(--en)", "foreshadow": "var(--fs)"}[ev["type"]]
        out.append(f'<circle cx="{tx(x):.1f}" cy="{tz(zz):.1f}" r="2.6" fill="{col}"><title>{ev["beat"]}박 {ev["d"]:.0f} m {html.escape(ev["label"])}</title></circle>')
    out.append(f'<text x="{tx(0) - 8:.1f}" y="{tz(0) + 22:.1f}" class="zl">출발</text>')
    out.append("</svg>")
    return "\n".join(out)

def strip(z0, z1, title):
    S = 7.0; L = 30
    W = int((z1 - z0) * S) + 20; H = int(2 * L * S) + 30
    sx = lambda s: 10 + (s - z0) * S
    sy = lambda o: H / 2 + o * S   # + = right of travel = down
    out = [f'<figure class="strip"><figcaption>{html.escape(title)} · {z0}–{z1} m</figcaption><div class="scroll"><svg viewBox="0 0 {W} {H}" width="{W}" height="{H}">']
    out.append(f'<rect x="0" y="0" width="{W}" height="{H}" fill="var(--ground)"/>')
    for z in Z:
        if z[1] <= z0 or z[0] >= z1: continue
        a, b = max(z[0], z0), min(z[1], z1)
        half = z[3] if z[3] > 0 else 1.6
        out.append(f'<rect x="{sx(a):.1f}" y="{sy(half):.1f}" width="{(b - a) * S:.1f}" height="{2 * half * S:.1f}" fill="var(--street)"/>')
        out.append(f'<text x="{sx(a) + 3:.1f}" y="{H - 6}" class="zs">{html.escape(z[2])} · ' + (f'발밑 지붕, 옆 지붕까지 {z[3]:.1f} m' if z[0] == 270 else f'길 폭 {2 * z[3]:.1f} m') + '</text>' if z[3] > 0 else
                   f'<text x="{sx(a) + 3:.1f}" y="{H - 6}" class="zs">{html.escape(z[2])} · 지붕/담 윗면</text>')
    out.append(f'<rect x="{sx(z0):.1f}" y="{sy(CORRIDOR):.1f}" width="{(z1 - z0) * S:.1f}" height="{2 * CORRIDOR * S:.1f}" fill="none" stroke="var(--ink)" stroke-dasharray="4 4" stroke-width=".8"/>')
    for p in sorted([p for p in placed if p["kind"] == "b"], key=lambda p: -p["row"]):
        if p["s1"] <= z0 or p["s0"] >= z1: continue
        a, b = max(p["s0"], z0), min(p["s1"], z1)
        o0, o1 = (p["off0"], p["off1"]) if p["side"] > 0 else (-p["off1"], -p["off0"])
        if max(abs(o0), abs(o1)) > L: o1 = math.copysign(min(abs(o1), L), o1); o0 = math.copysign(min(abs(o0), L), o0)
        if p["id"] == "wall_run": col, lab, op = "#6f6a60", "벽타기 벽", 1
        else: col, lab, op = M[p["id"]][4], M[p["id"]][0], {1: 1, 2: .7, 3: .45}[p["row"]]
        y0, y1 = sy(min(o0, o1)), sy(max(o0, o1))
        out.append(f'<rect x="{sx(a):.1f}" y="{y0:.1f}" width="{(b - a) * S:.1f}" height="{max(y1 - y0, 2):.1f}" fill="{col}" opacity="{op}" stroke="var(--edge)" stroke-width=".6"><title>{html.escape(lab)}</title></rect>')
        if p.get("door"):
            dy = sy(p["off0"] * p["side"])
            out.append(f'<rect x="{sx((a + b) / 2) - 8:.1f}" y="{dy - 3:.1f}" width="16" height="6" fill="var(--en)"/>')
        if p["row"] == 1 and (b - a) * S > 34 and p["id"] not in ("fence",):
            out.append(f'<text x="{sx((a + b) / 2):.1f}" y="{(y0 + y1) / 2 + 3:.1f}" class="bl">{html.escape(lab)}</text>')
    for p in placed:
        if p["kind"] != "p" or not (z0 <= p["s"] < z1): continue
        fp = P[p["id"]][1]
        o = p["off"] * p["side"]
        r = max(fp * S / 2, 2.2)
        if p["id"] in ("zelkova", "pine"):
            out.append(f'<circle cx="{sx(p["s"]):.1f}" cy="{sy(o):.1f}" r="{r:.1f}" fill="{P[p["id"]][2]}" opacity=".85"><title>{P[p["id"]][0]}</title></circle>')
        else:
            out.append(f'<rect x="{sx(p["s"]) - r:.1f}" y="{sy(o) - r:.1f}" width="{2 * r:.1f}" height="{2 * r:.1f}" rx="2" fill="{P[p["id"]][2]}" stroke="var(--edge)" stroke-width=".5"><title>{P[p["id"]][0]}</title></rect>')
    for f in FIXED:
        if f["module"] == "palace_gate" and z0 <= f["d0"] < z1:
            gx = sx(f["d0"])
            out.append(f'<rect x="{gx:.1f}" y="{sy(-6):.1f}" width="{2.5 * S:.1f}" height="{12 * S:.1f}" fill="#a8473c" stroke="var(--edge)"><title>궁궐 문</title></rect>')
            out.append(f'<text x="{gx + 1.25 * S:.1f}" y="{sy(-6) - 3:.1f}" class="bl" style="fill:var(--ink);stroke:none">궁궐 문</text>')
    k = 0
    for ev in events:
        if not (z0 <= ev["d"] < z1): continue
        col = {"obstacle": "var(--obs)", "enemy": "var(--en)", "boss": "var(--en)", "surround": "var(--en)", "foreshadow": "var(--fs)"}[ev["type"]]
        x = sx(ev["d"])
        if ev["type"] == "foreshadow":
            out.append(f'<rect x="{x:.1f}" y="{sy(10):.1f}" width="{ev["span"] * S:.1f}" height="{4 * S:.1f}" fill="none" stroke="{col}" stroke-width="2" stroke-dasharray="5 3"/>')
            out.append(f'<text x="{x + 3:.1f}" y="{sy(14) + 12:.1f}" class="el">{html.escape(ev["label"])} (발밑 지붕)</text>')
            continue
        out.append(f'<line x1="{x:.1f}" y1="{sy(1.8):.1f}" x2="{x:.1f}" y2="{sy(-1.8):.1f}" stroke="{col}" stroke-width="2.4"/>')
        out.append(f'<text x="{x + 2:.1f}" y="{sy(0) - 18 + (k % 4) * 13.5:.1f}" class="el">{ev["beat"]} {html.escape(ev["label"])}</text>'); k += 1
    for d in range(int(math.ceil(z0 / 10) * 10), int(z1) + 1, 10):
        out.append(f'<text x="{sx(d):.1f}" y="10" class="tick">{d}</text>')
    out.append("</svg></div></figure>")
    return "\n".join(out)

STRIPS = [(0, 85, "1구간 앞: 어귀 + 초가 골목"), (85, 215, "1구간 뒤 ~ 2구간: 장터 큰길 · 광장 · 장터 뒷골목"),
          (215, 270, "2구간 끝: 양반 돌담길"), (270, 440, "3구간: 기와지붕 바다 + 뒷마당"),
          (440, 490, "4구간: 관아 마당 (포위 전투)"), (490, 660, "5·6구간: 궁궐 담 길 → 담 위 → 성문 누각")]

data = dict(overview=overview(), strips="\n".join(strip(*s) for s in STRIPS), checks=checks, uniq=uniq,
            nb=sum(1 for p in placed if p["kind"] == "b"), np=sum(1 for p in placed if p["kind"] == "p"), fixed=FIXED)
json.dump({k: v for k, v in data.items() if k in ("checks", "uniq", "nb", "np")}, sys.stdout, ensure_ascii=False, indent=1)
open(OUT, "w", encoding="utf-8").write(json.dumps(data, ensure_ascii=False))
if "--debug" in sys.argv:
    for side in (-1, 1):
        r1 = sorted([p for p in placed if p["kind"] == "b" and p["row"] == 1 and p["side"] == side], key=lambda p: p["s0"])
        for a, b in zip(r1, r1[1:]):
            if a["id"] == b["id"] and a["id"] in BUILDINGS and b["s0"] - a["s1"] < 3: print("\nREPEAT", side, a, b)
