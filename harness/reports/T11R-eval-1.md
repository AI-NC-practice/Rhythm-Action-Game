# T11R 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 사용자 승인(포인트, 11개 전부, 상한 15,000, 4면도 검수 + 한 장 생성) | PASS | tasks.json 기준 1번 승인 기록, log.md에 같은 결정. 11개(캐릭터 4 + 정적 7) 모두 처리. |
| 2 | 4면도 먼저 → 4면도 참조 정면(캐릭터 T자) 한 장으로 고폴리 3D | PASS | sheets/ 11장, v2/ 11장(+house_giwa_r2). sheets/boss.png 정면·옆·뒤·3/4 일관된 T자. 고폴리 GLB 남아 있음. |
| 3 | 캐릭터: 손가락 안 갈라진 손, 구멍·뒤집힘 없음, 경계 변 0 | PASS | preview/player_hand.png 평평한 손. mesh_check 재실행: 4개 모두 12,800, boundary 0, nonmanifold 0. |
| 4 | 정적: 바닥 면 삭제 → 로우폴리 → 색·노멀 굽기 | PASS | cull_bake.py, T11R_StaticBakeTests 통과, log에 삭제 수, preview/house_giwa.png 정상. |
| 5 | 캐릭터 15,000·정적 5,000 이하, 15 MB 이하, 38개 Humanoid valid | PASS | check-glb 7개 OK(1,164~4,845, 1.3~8.05 MB). T11 테스트 상한 15,000 통과. humanoid-import.md 38개 valid·human. |
| 6 | unity-verify 통과 | PASS | 재실행 OVERALL PASS. |

## 실패 항목과 수정 지시
- 없음.
- 참고: 첫 unity-verify에서 PlayMode B19_RewindTests.B19_RewindPresentation_PausedClock_FxSoundStreak 1회 실패, 재실행 통과 — 에셋과 무관한 타이밍 불안정(flaky)으로 보임.

## 검증 실행 결과
- unity-verify: 1차 EditMode 179/179, PlayMode 153/154(B19 1건) → 재실행 OVERALL PASS.
- check-glb 7개 OK, mesh_check 캐릭터 4개 경계 변 0.
