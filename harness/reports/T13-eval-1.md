# T13 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | URP 툰 셰이더(2~3단계) + 외곽선이 모든 캐릭터·에셋 머티리얼에 | PASS | ToonForward·ToonOutline·ShadowCaster 패스, 에셋 11개 Toon(_Steps 3, 외곽선 0.0025), Game 씬 모든 Mesh/SkinnedMesh 머티리얼 Toon(반투명 Ghost만 제외). t13_tp_front.png·t13_fp_run.png에서 외곽선·단계 명암 확인. |
| 2 | 캡슐 → player 모델, 카메라 머리 뼈 추적(눈높이 1.65), 머리 ShadowsOnly | PASS | Humanoid Animator, 툰, 원래 메시 ShadowsOnly + 머리·목 뺀 VisibleBody, 리그 높이 ≈1.65(±0.15), 머리 뼈 ≈1.65(±0.25), 몸 방향·엉덩이 위치 확인. |
| 3 | PERFECT/NORMAL/FAIL별 Animator 상태 + 4박 재생 속도 — PlayMode 테스트 | PASS | Vault_Perfect/Normal/Fail 전환과 속도 = 클립/(4박×SecondsPerBeat), 왼쪽 반전 후 Run 복귀, 컨트롤러 18 상태·전이·속도 파라미터. |
| 4 | unity-verify 전체 통과 | PASS | 재실행 OVERALL PASS. |

## 실패 항목과 수정 지시
- 없음. 참고: 허용 오차가 넓은 편이나 기준 검사에 충분. 미관은 HUMAN 영역.

## 검증 실행 결과
- unity-verify: EditMode 186/186, PlayMode 157/157.
