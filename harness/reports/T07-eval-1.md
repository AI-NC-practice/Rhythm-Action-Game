# T07 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 적 5명(칼 3, 창 2), 반지름 4 m 원 배치 | PASS | T07_Surround_Layout_FiveEnemiesRadius4 (칼 3, 창 2, 거리 4±0.01 단언) |
| 2 | 1단계 순차 5회(1박 간격), 2단계 동시 3회(원 1·키 1), 3단계 반 박자 간격 화살표 4개 | PASS | EditMode T07_PatternTests (1박 간격, 동시 3회, 3단계 0.5박 간격 4개) + PlayMode 차트 생성 테스트(Cues 12개 = 5+3+4) |
| 3 | 3단계 4개 NORMAL 이상이면 5명 쓰러짐 + 슬로모션 + 카메라 360° 회전. FAIL이면 HP −1 후 다음 마디에 3단계만 재시도 | PASS | T07_Surround_AllSuccess_Windmill_DefeatsAll_SlowMo_Spin360, T07_Windmill_Fail_RetriesOnlyPhase3NextBar |
| 4 | 1·2단계 FAIL은 HP −1, 전투 지속 | PASS | T07_Surround_Phase1And2Fail_LoseHp_ButContinue |
| 5 | 무사: 공격 5회(1~2박 간격 혼합) + 마무리, 쓰러뜨리면 스테이지 클리어 | PASS | T07_Boss_FiveAttacks_ThenFinisher_StageClear, EditMode 간격 1~2박이 섞이는지 확인, T07_Boss_Fail_RetriesFromFirstAttack |
| 6 | unity-verify.ps1 전체 통과 | PASS | 직접 재실행, OVERALL: PASS |

## 실패 항목과 수정 지시
- 없음

## 검증 실행 결과
- unity-verify: 컴파일 OK(OVERALL PASS), EditMode 68/68, PlayMode 25/25
- 참고: 에셋과 빌드 작업이 아니라서 check-glb와 smoke-test는 실행하지 않았다.
