# T10 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 시작 전에 예상 포인트를 알리고 사용자 승인을 받음 | PASS | log.md T10 절 "사용자: 플로우 만듦 + 포인트 승인(예상 약 1,540~3,220)". (호출자 메모: 사용자가 2026-11-16 채팅에서 "플로우 만듦, 승인"이라고 답함.) |
| 2 | 7개를 GLB로 생성해 Assets/Art/Models/<id>.glb에 저장 | PASS | 7개 파일 모두 있음. 미리보기 gate.png(돌 받침·아치·붉은 문·기와 누각), weapon_sword.png(강철 날·둥근 날밑·검은 손잡이) 확인. |
| 3 | check-glb로 삼각형 5,000 이하, 15 MB 이하 | PASS | 직접 실행 전부 OK: 1500, 1132, 4892, 2428, 4678, 2396, 2018 / 2.66~9.12 MB. |
| 4 | asset_specs status/triangles/file_mb/calls 기록, 이미지 최대 3회·3D 최대 2회 | PASS | 7개 generated, triangles·file_mb가 check-glb와 일치. image 최대 2(칼·성문), mesh 최대 2(성문). run_flow 6회 (한도 45). flow_id 기록. |
| 5 | unity-verify 전체 통과 (GLB 임포트 오류 없음) | PASS | 직접 재실행 OVERALL: PASS, 리포트에 error 0건. |

## 실패 항목과 수정 지시
- 없음.
- 참고: 칼 미리보기는 날 끝이 뭉툭하고 표면이 울퉁불퉁하다(외형 — HUMAN 판단, 판정 제외).
- 참고: 고폴리 원본(house_giwa/wall_stone/gate _high.glb)은 T15 노멀 맵 굽기용으로 harness/reports/varco/에 보관.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 176/176, PlayMode 154/154
