import json, sys, html
sys.stdout.reconfigure(encoding="utf-8")
data = json.load(open(sys.argv[1], encoding="utf-8"))
OUT = sys.argv[2]
e = html.escape

RULES = [
 ("길 폭 (담·건물 앞면 사이)", "초가 골목 6.6 m · 장터 큰길 10 m · 장터 뒷골목 9.2 m · 양반 돌담길 6.8 m · 궁궐 담 길 8.4 m · 관아 마당 30 m",
  "지금은 장식이 길에서 4.5 m + 모델 두께 절반 이상 떨어져 있어 거리가 휑했다. 골목은 좁게, 장터는 넓게 강약을 준다."),
 ("달리는 길", "가운데 폭 3.6 m는 항상 비움", "소품이 판정 동작을 가리지 않게 한다. 사건 지점 앞뒤 3 m는 길가 2.4 m 안쪽도 비운다."),
 ("건물 사이 틈", "0~1.2 m (붙여서 이어지는 거리), 18% 확률로 3~4.5 m 샛골목", "샛골목 사이로 뒤 줄 지붕이 보여 깊이가 생긴다."),
 ("같은 모델 반복", "같은 건물 모델이 연달아 붙지 않음, 같은 쪽 최근 2채와 다른 모델", "담은 이어져야 자연스러워서 예외."),
 ("변화", "회전 ±4°, 크기 0.92~1.08배, 기단 높이 0~0.4 m, 앞면 들쭉날쭉 0~0.6 m", "줄 맞춘 느낌을 없앤다."),
 ("깊이 (줄 수)", "1줄: 길가 · 2줄: 길에서 8.5 m 뒤(±1.5) · 3줄: 20 m 뒤(±1.5) · 그 너머 40~300 m: 낮은 지붕 바다(인스턴싱) · 300 m: 배경 그림", "가까운 곳은 실제 모델, 먼 곳은 그림 착시 (사용자 요구 사항)."),
 ("소품 밀도 (10 m당)", "어귀 3 · 초가 골목 3 · 장터 큰길 10 · 장터 뒷골목 6 · 양반 돌담길 3 · 뒷마당 4 · 관아 마당 2 · 궁궐 담 길 2", "장터가 가장 북적이고, 지붕·담 위는 비운다."),
 ("시선 끝 랜드마크", "출발(장승·느티나무), 140 m 장터 광장(큰 느티나무·우물), 270 m 누각, 440 m 관아 정문, 490 m 궁궐 전각, 630 m 성문 누각", "모퉁이마다 정면에 큰 것이 보여 길을 따라가는 이유가 생긴다."),
 ("모퉁이", "90° 꺾임을 반지름 8 m 곡선으로 (경로 길이·판정 박자 유지)", "순간이동처럼 보이던 꺾임을 없앤다. 곡선 안에 걸리는 사건은 화살 세례(198박, 495 m)와 깃발 줄(252박, 630 m) 두 개 — R03에서 곡선을 피하게 사건 자리를 몇 m 옮기거나 그 모퉁이만 반지름을 줄인다."),
]

SCENES = [
 ("1 · 마을 어귀 → 초가 골목 (0~85 m)",
  "장승 한 쌍 사이로 출발한다. 오른쪽에 큰 느티나무, 낮은 싸리 울타리 너머로 초가 지붕들이 보인다. "
  "곧 흙담과 초가가 붙어 선 6.6 m 골목으로 들어간다. 담 밑에는 장독대·장작더미·지게가 놓여 있고, 빨랫줄(12박)이 길을 가로지른다. "
  "매복 포졸은 초가 사립문에서 튀어나온다. 80 m 벽타기 지점은 골목이 2 m로 좁아지는 흙담 통로다."),
 ("1 뒤 ~ 2 · 장터 (85~215 m)",
  "통로를 빠져나오면 폭 10 m 장터 큰길이다. 양쪽에 2층 시전 상점채가 이어지고, 앞에는 흰 차양·색 천 차양 좌판, 옹기 진열, 포목 천 걸이, 짚단, 청사초롱 장대가 빽빽하다(10 m당 10개). "
  "110 m부터 오른쪽 길가 지붕 위에 감시 포졸이 서 있다(복선). 적 높이를 그 지붕 높이에 맞춰 공중에 뜨지 않게 한다. 140 m 모퉁이는 큰 느티나무와 우물이 있는 장터 광장이고, 곡선으로 돌아 장터 뒷골목으로 이어진다. "
  "뒷골목은 차양 튕기기(160 m), 밧줄 넘기(180 m), 낮은 처마(200 m)가 장터 소품 그대로 나온다."),
 ("2 끝 · 양반 돌담길 (215~270 m)",
  "장터 소음이 끝나고 돌담과 기와집, 솟을대문이 이어지는 조용한 길이다. 소나무가 담 위로 넘어온다. "
  "230 m 칼 포졸 전투는 우물이 있는 작은 공터에서 한다. 길 끝 정면에 2층 누각이 보이고, 250 m에서 기와지붕으로 올라간다."),
 ("3 · 기와지붕 바다 (270~440 m)",
  "발밑은 기와지붕, 양옆과 앞으로 기와집·작은 기와집·긴 행랑채 지붕이 겹겹이 이어진다. 높이는 4.5~5.5 m로 지붕마다 조금씩 다르다. "
  "추격 포졸은 3.5 m 옆 지붕을 달리고, 지붕 위 창 포졸 전투에서 PERFECT면 그 포졸이 발차기에 맞아 골목으로 떨어진다. "
  "410 m에서 뒷마당으로 뛰어내리면 장독대와 평상이 있고, 낮은 담을 넘어 관아 마당으로 들어간다."),
 ("4 · 관아 마당 (440~490 m)",
  "30 m 마당을 높이 2.6 m 담이 둘러싼다. 가운데는 비워 두고, 담 쪽에 느티나무·평상·장독대·깃발 장대를 둔다. 포졸들이 담을 넘어 들어온다(복선, 발밑에 담이 있다)."),
 ("5 · 6 · 궁궐 담 → 성문 (490~660 m)",
  "모퉁이를 돌면 정면에 궁궐 전각 지붕이 크게 보인다. 왼쪽은 5 m 궁궐 담, 오른쪽은 관청 기와집과 솟을대문이다. "
  "505 m 닫히는 대문은 길을 가로지르는 궁궐 문(문짝 두 짝)이다. 560 m부터는 궁궐 담 위를 달리며 왼쪽 아래로 전각, 오른쪽 아래로 도성 지붕 바다가 보인다. "
  "마지막은 성문 누각과 깃발, 무사 결전이다."),
]

ASSETS = [
 ("건물", [("choga", "초가집"), ("choga_L", "ㄱ자 초가"), ("giwa_s", "작은 기와집"), ("giwa_long", "긴 행랑채"),
          ("giwa_gate", "솟을대문 집"), ("sijeon", "2층 시전 상점채"), ("palace_hall", "궁궐 전각"), ("pavilion", "2층 누각")]),
 ("담·울타리", [("wall_earth", "흙담(기와 갓)"), ("wall_palace", "궁궐 담"), ("fence", "싸리 울타리")]),
 ("장터", [("stall_a", "흰 차양 좌판"), ("stall_b", "색 천 차양 좌판"), ("stall_pot", "옹기 진열"), ("cloth_rack", "포목 천 걸이"),
          ("straw", "짚단·멍석"), ("lantern", "청사초롱 장대"), ("basket", "광주리 더미")]),
 ("마을 소품·나무", [("jangdok", "장독대"), ("well", "우물"), ("pine", "소나무"), ("zelkova", "느티나무"), ("jangseung", "장승 한 쌍"),
          ("firewood", "장작더미"), ("pyeongsang", "평상"), ("jige", "지게"), ("banner_pole", "깃발 장대")]),
 ("장애물·사건 소품 (R04)", [("clothesline", "빨랫줄(기둥+옷)"), ("eave_low", "낮은 처마"), ("rope_swing", "밧줄+기둥"),
          ("rolling_jar", "굴러오는 술독"), ("arrow", "화살"), ("banner_flag", "깃발 줄"), ("gate_door", "대문 문짝"), ("palace_gate", "궁궐 문틀")]),
]
n_assets = sum(len(v) for _, v in ASSETS)
per = 300
base = n_assets * per
backdrop = 6 * 50
retry = round(base * 0.25 / 50) * 50

checks = "".join(f'<li class="{"ok" if ok else "bad"}"><b>{"통과" if ok else "실패"}</b> {e(n)} <span>{e(d)}</span></li>' for n, ok, d in data["checks"])
rules = "".join(f"<tr><th>{e(a)}</th><td>{e(b)}</td><td class='why'>{e(c)}</td></tr>" for a, b, c in RULES)
scenes = "".join(f"<article><h3>{e(t)}</h3><p>{e(d)}</p></article>" for t, d in SCENES)
assets = "".join(f"<tr><th>{e(g)}</th><td>{len(v)}</td><td>{e(', '.join(n for _, n in v))}</td></tr>" for g, v in ASSETS)

page = f"""<!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>맵 재설계 안</title>
<style>
:root{{--bg:#f6f2ea;--card:#fffdf8;--ink:#2b2622;--mute:#6f665c;--line:#ddd3c4;--ground:#e9e1d1;--street:#d8ccb6;--edge:#5a5048;
--obs:#2f6fb0;--en:#c0392b;--fs:#7a4fb0;--lm:#f2c14e;--bad:#c0392b;--ok:#2e7d4f}}
@media (prefers-color-scheme:dark){{:root:not([data-theme="light"]){{--bg:#1d1b19;--card:#262320;--ink:#eee6da;--mute:#b3a898;--line:#3d3833;--ground:#2f2b27;--street:#433d36;--edge:#141210;--obs:#6aa8e6;--en:#ef6f60;--fs:#b58ae6;--ok:#5fbf86}}}}
:root[data-theme="dark"]{{--bg:#1d1b19;--card:#262320;--ink:#eee6da;--mute:#b3a898;--line:#3d3833;--ground:#2f2b27;--street:#433d36;--edge:#141210;--obs:#6aa8e6;--en:#ef6f60;--fs:#b58ae6;--ok:#5fbf86}}
body{{margin:0;background:var(--bg);color:var(--ink);font:15px/1.6 "Pretendard","Malgun Gothic",system-ui,sans-serif}}
main{{max-width:1100px;margin:0 auto;padding:24px 16px 60px}}
h1{{font-size:26px;margin:0 0 4px}} h2{{font-size:19px;margin:36px 0 10px;border-bottom:1px solid var(--line);padding-bottom:6px}} h3{{font-size:15px;margin:0 0 4px}}
.sub{{color:var(--mute)}} .card{{background:var(--card);border:1px solid var(--line);border-radius:10px;padding:14px 16px}}
table{{border-collapse:collapse;width:100%}} th,td{{text-align:left;vertical-align:top;padding:7px 8px;border-bottom:1px solid var(--line)}} th{{white-space:nowrap;font-weight:600}}
td.why{{color:var(--mute);font-size:13px}}
.grid{{display:grid;grid-template-columns:repeat(auto-fit,minmax(300px,1fr));gap:12px}} article{{background:var(--card);border:1px solid var(--line);border-radius:10px;padding:12px 14px}} article p{{margin:0;font-size:14px}}
svg.map{{width:100%;max-width:760px;height:auto;display:block;margin:auto}}
svg text.zl{{font-size:12px;font-weight:700;fill:var(--ink)}} svg text.zs{{font-size:10px;fill:var(--mute)}} svg text.bl{{font-size:9px;fill:#fff;text-anchor:middle;paint-order:stroke;stroke:#0006;stroke-width:2px}}
svg text.el{{font-size:9.5px;fill:var(--ink);font-weight:700;paint-order:stroke;stroke:var(--card);stroke-width:3px}} svg text.tick{{font-size:9px;fill:var(--mute)}}
.strip{{margin:0 0 18px}} .strip figcaption{{font-weight:600;margin-bottom:4px}} .scroll{{overflow-x:auto;border:1px solid var(--line);border-radius:8px}}
.legend{{display:flex;flex-wrap:wrap;gap:6px 16px;font-size:13px;color:var(--mute)}} .legend i{{display:inline-block;width:12px;height:12px;border-radius:3px;margin-right:5px;vertical-align:-1px}}
ul.checks{{list-style:none;padding:0;margin:0}} ul.checks li{{padding:4px 0}} ul.checks b{{display:inline-block;min-width:40px}} li.ok b{{color:var(--ok)}} li.bad b{{color:var(--bad)}} ul.checks span{{color:var(--mute);font-size:13px}}
.big{{font-size:28px;font-weight:700}} .kpis{{display:grid;grid-template-columns:repeat(auto-fit,minmax(160px,1fr));gap:10px}} .kpis>div{{background:var(--card);border:1px solid var(--line);border-radius:10px;padding:10px 14px}} .kpis small{{color:var(--mute)}}
ol.ask li{{margin:6px 0}}
</style></head><body><main>
<h1>맵 재설계 안 (R01)</h1>
<p class="sub">2026-11-16 · 경로 660 m · 실제 차트(stage1.json)의 사건 59개 위치에 맞춰 계산한 배치. 승인되면 R02(에셋) → R03(맵) → R04(회색 상자 교체)로 진행.</p>

<div class="kpis">
<div><small>배치 계산 결과</small><div class="big">{data['nb']}채</div><small>건물·담 (1~3줄)</small></div>
<div><small>길가 소품</small><div class="big">{data['np']}개</div><small>달리는 길 밖에만</small></div>
<div><small>새 에셋</small><div class="big">{n_assets}개</div><small>기존 11개는 계속 사용</small></div>
<div><small>예상 VARCO 포인트</small><div class="big">{base + backdrop:,}</div><small>다시 만들기 포함 최대 약 {base + backdrop + retry:,}</small></div>
</div>

<h2>1. 전체 경로</h2>
<div class="card">{data['overview']}
<div class="legend"><span><i style="background:var(--lm)"></i>랜드마크</span><span><i style="background:var(--obs)"></i>장애물·사건</span><span><i style="background:var(--en)"></i>적</span><span><i style="background:var(--fs)"></i>복선</span><span><i style="border:1px dashed var(--bad)"></i>지금의 90° 꺾임 → 곡선</span></div></div>

<h2>2. 1인칭 눈높이에서 보이는 장면</h2>
<div class="grid">{scenes}</div>

<h2>3. 배치 원칙 (수치는 GameConfig.Map으로)</h2>
<div class="card"><table>{rules}</table></div>

<h2>4. 구간별 배치도 (경로를 펴서 위에서 본 그림)</h2>
<p class="sub">가로 = 경로 거리(m), 세로 = 달리는 방향 기준 왼쪽(위)·오른쪽(아래). 점선 상자 = 달리는 길 3.6 m. 진한 건물 = 길가 1줄, 옅을수록 뒤 줄. 빨간 막대 = 매복 대문. 마우스를 올리면 이름이 나온다. 가로로 스크롤된다.</p>
<div class="legend" style="margin-bottom:10px"><span><i style="background:#c9a86a"></i>초가</span><span><i style="background:#6d7480"></i>기와</span><span><i style="background:#8a6f58"></i>시전</span><span><i style="background:#b08b62"></i>흙담</span><span><i style="background:#8d8d88"></i>돌담</span><span><i style="background:#b5654f"></i>궁궐 담</span><span><i style="background:#e8e0cc"></i>좌판</span><span><i style="background:#5f8f4f"></i>나무</span></div>
{data['strips']}

<h2>5. 규칙 자동 검사</h2>
<div class="card"><ul class="checks">{checks}</ul>
<p class="sub" style="margin:8px 0 0">R03에서는 같은 검사를 EditMode 테스트로 만들어 맵을 바꿀 때마다 확인한다.</p></div>

<h2>6. 먼 배경</h2>
<div class="card"><p>지금은 코드로 그린 산·지붕 띠를 원통에 반복해서 붙여 조잡하다. 바꿀 방법:</p>
<ul><li>VARCO 이미지로 <b>손그림 느낌 파노라마</b>를 층마다 2장씩 그린다: 먼 산(능선) · 중간(성곽·소나무 언덕) · 가까운 지붕 바다 실루엣. 6장 × 50 = 300포인트.</li>
<li>층마다 반지름을 다르게 해(250·280·300 m) 움직일 때 시차가 생기게 하고, 같은 그림이 나란히 반복되지 않게 2장을 번갈아 다른 길이로 붙인다.</li>
<li>40~300 m 사이는 작은 기와집 저폴리 모델을 인스턴싱한 "지붕 바다"로 채운다 (R10에서 LOD·GPU Resident Drawer로 최적화).</li></ul></div>

<h2>7. 새 에셋과 포인트 견적</h2>
<div class="card"><table><tr><th>분류</th><th>개수</th><th>목록</th></tr>{assets}</table>
<p>에셋 한 개 = 4면도 이미지 50 + 정면 이미지 50 + 3D 생성 200 = <b>300포인트</b> (T11R 방식: 4면도 검수 → 정면 한 장 → 고폴리 → 바닥 면 삭제·로우폴리·색·노멀 굽기, 굽기는 무료).</p>
<table><tr><th>새 에셋 {n_assets}개 × 300</th><td>{base:,}</td></tr><tr><th>배경 파노라마 6장 × 50</th><td>{backdrop:,}</td></tr>
<tr><th>합계 (예상)</th><td><b>{base + backdrop:,}</b></td></tr><tr><th>다시 만들기 여유 (25%)</th><td>최대 +{retry:,} → 약 {base + backdrop + retry:,}</td></tr></table>
<p class="sub">기존 에셋(기와집·돌담·성문·수레·항아리 등)은 그대로 쓴다. 실제 청구액은 실행 결과의 포인트로 확인해 log.md에 남긴다. 에셋을 묶어서 실행하므로 VARCO 실행 횟수 한도(45회) 안에 들어갈 것으로 본다. 넘으면 다시 묻는다.</p></div>

<h2>8. 승인받을 것</h2>
<ol class="ask"><li>장면 구성(장터 85~215 m, 관아 마당, 궁궐 담 위 등)과 배치 원칙 수치</li>
<li>새 에셋 {n_assets}개 + 배경 6장, 예상 {base + backdrop:,}포인트 (최대 약 {base + backdrop + retry:,})</li>
<li>모퉁이 곡선(반지름 8 m): 경로 길이와 판정 박자는 그대로</li></ol>
</main></body></html>"""
open(OUT, "w", encoding="utf-8", newline="\n").write(page)
print(n_assets, base, backdrop, retry)
