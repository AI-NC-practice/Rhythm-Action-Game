# T15 평가 보고서 (3회차)
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 회색 상자 → VARCO 모델 (충돌 유지) | PASS | T15_Map_ModelsReplaceGrayBoxes_Atmosphere 재실행 통과, t15_ch3_140.png |
| 2 | 챕터 컨셉 배치 | HUMAN | 챕터별 장식 테스트·미리보기 6장, 분위기·밀도는 사람 판단 |
| 3 | 외곽 착시 + 가까운 곳 실제 오브젝트 | HUMAN | 먼 산·지붕 띠·실루엣 + 가까운 모델 3줄. 지평선 건물 반복은 사람 판단 |
| 4 | 로우폴리 remesh + 노멀 맵 | PASS (코드·로그 근거) | T11R 굽기(T11R-eval-1.md), 툰 셰이더 노멀 맵 |
| 5 | 드로우콜 전후 기록 | PASS | drawcalls.md 778 → 290, SetPass 675 → 268, 테스트 통과 |
| 6 | 해 질 녘 방향광·하늘·안개 | PASS / 색감 HUMAN | 테스트 + 미리보기 |
| 7 | BuildFinal exe | PASS | Builds/Final/RhythmActionGame.exe |
| 8 | smoke-test | PASS | 12초 깨끗한 로그 |
| 9 | unity-verify | PASS | EditMode 189/189, PlayMode 163/163 |

## 실패 항목과 수정 지시
- 없음. B28은 허용치 0.02 그대로, 측정 시점만 프레임 끝으로(검사 10회 초과 단언).
- 참고: exe 파일 시각 17:05 — 확인 결과 빌드 안 게임 코드(RhythmActionGame.Game.dll)는 17:24로 마지막 PlayerBody 수정(17:21) 뒤에 만들어짐.

## 검증 실행 결과
- unity-verify: EditMode 189/189, PlayMode 163/163
- smoke-test-build: 통과
