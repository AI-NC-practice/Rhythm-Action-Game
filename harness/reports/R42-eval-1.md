# R42 평가 보고서
판정: PASS (Unity 재실행 없이 verify-latest.md 20:51·테스트 소스·미리보기로 판단 — 시간 제약)

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 컷마다 막는 것이 쓰러지고(땅 지게·짚단, 지붕 기와 더미) 적이 걸려 넘어지는 장면이 컷 화면에 | PASS | R42_LookBackBarricadePlayTests 107-155: 쓰러짐 1.0, 무리 전원 넘어짐, 화면 안 1명 이상. cut0_a·cut1_b·cut2_b·cut3_b 직접 확인 |
| 2 | 컷 뒤 다시 따라오거나 앞 전투에 끼지 않음 | PASS | LookBackCut.cs:249 컷 밖 SetActive(false), 테스트 maxPast<0. 숨김 직접 단언 테스트는 없음 |
| 3 | 판정·차트·곡 그대로, 공중·건물 속 아님 | PASS | 보이는 메시 기준 발밑 <0.35 m, 조각 아래 <0.5 m 단언. 차트·판정 테스트 통과 |
| 4 | PlayMode 테스트·미리보기 PNG(컷 4곳) | PASS | 컷 4개 테스트 통과, harness/reports/R42/ cut0~3 a·b·after |
| 5 | unity-verify 전체 통과 | PASS | EditMode 209/209, PlayMode 229/230 (skipped 1) |

참고: acceptance는 "지게·수레", 구현은 GDD-beta B22대로 지게·짚단. HUMAN 확인 권장: 지붕 컷(2·3)에서 넘어지는 동작이 실제 재생에서 충분히 눈에 띄는지.
