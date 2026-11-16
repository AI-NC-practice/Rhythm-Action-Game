# R06 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 실패 동작 중 머리 없는 몸이 화면 정면에 보이지 않음 | PASS | R06_Fail_HeadlessBodyNeverInFront: 7개 동작 FAIL과 Hit에서 목 보임 0프레임, 가림 최대 ≤ 0.02 (measure.md 전부 0.000). _Fail PNG에도 몸이 보이지 않음. |
| 2 | 벽타기 약 45° 기울기 + 카메라 기울기 (GameConfig.Body) | PASS | WallRunLeanDegrees=45, WallRunCameraRoll=20. 좌우 45°±3, 몸 기울기 >40°, 발이 머리보다 벽 쪽 >0.8 m, 카메라 롤 ≥18°, 실제 스테이지 벽 WallFootOffset 1.7±0.3. PNG에서 지평선 기울어짐 확인. |
| 3 | 미끄러지기 화면 가림 상한 이하 (PlayMode 측정) | PASS | 상한 SlideMaxScreenCover=0.2. Slide_Perfect 0.011, Slide_Normal 0.103. |
| 4 | 결과별 미리보기 PNG | PASS | harness/reports/R06/ 7동작 × 3결과 a·b, Hit_a, 벽타기 스테이지 1·3인칭, all.png 확인. |
| 5 | unity-verify.ps1 전체 통과 | PASS | 재실행: EditMode 200/200, PlayMode 173/173, OVERALL: PASS. |

## 실패 항목과 수정 지시
- 없음.

## 참고 (판정에 영향 없음)
- measure.md의 Dodge_Normal 가림 최대 0.835(평균 0.033, 순간 최대). 기준 3은 미끄러지기만 상한. 후속에서 사람 눈으로 확인 권장.
- Drop_Perfect(앞구르기) all.png에서 화면 일부를 돌이 덮음. 측정 0.045 (< 0.3).
- 몸이 보이는 장면의 시각 품질은 HUMAN 판단.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 200/200, PlayMode 173/173 (R06 테스트 5개 모두 Passed)
- GLB 검사·빌드 스모크: 해당 없음.
