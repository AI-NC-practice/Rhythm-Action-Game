# B46 평가 보고서
판정: FAIL

(참고: 기계 검증 기준은 모두 충족한다. 아래 2번 기준 중 "테스트 빌드로 사용자 확인"이 아직 없어서 FAIL로 둔다. 이 항목은 사람 확인이 필요한 항목이다.)

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1a | call_response 사건이 차트에 있고 챕터마다 한 번 | PASS | `B46_Chart_OnePerChapter_BeforeBigCombat`는 챕터 1부터 6까지 `call_response`가 각 한 번씩 있는지, 큰 전투 바로 앞이나 챕터 끝에 있는지, 그 자리에 달리기 판정과 장단 노드가 없는지 검사한다. `stage1.json`에 `"call_response"`가 6개 있다. |
| 1b | 부르기 1마디 → 받기 1마디, 판정 2~4개(반 박 포함), 방향키 | PASS | `B46_Patterns_TwoToFourJudges_...`는 판정 개수 2~4, 반 박 포함, 한 마디 안, 방향키 여부를 검사한다. `B46_Plan_RestCallResponse_...`는 부르기와 받기가 각각 한 마디(4박)이고 같은 리듬이 한 마디 뒤에 오는지 검사한다. |
| 1c | 곡이 그 자리에서 멈췄다 다시 터짐 | PASS | `B46_AllPerfect_MusicStops_...`(PlayMode)는 `MusicHeld`가 true인지, 재개 뒤 `MusicBeatOffset`이 `offsetBefore - TotalBeats`인지, 쾅 연출이 나오는지, 심장박동이 나오는지, 전투가 이어지는지 검사한다. |
| 1d | 실패는 체력 −1, 되감기 없음 (GDD-beta B17.3) | PASS | `B46_Fail_LosesOneHp_NoRewind_NoBurst_MusicStillResumes`는 체력이 정확히 −1인지, `RewindCount`가 그대로인지, 곡이 재개되는지 검사한다. |
| 2a | unity-verify 통과 | PASS | 직접 다시 실행했다. 컴파일 OK, EditMode 173/173, PlayMode 149/149, OVERALL: PASS. B46 테스트 5개가 모두 Passed다. |
| 2b | 테스트 빌드로 사용자 확인 | HUMAN (미완) | `tasks.json`의 `notes`가 비어 있고 사용자 확인 기록이 없다. 사용자가 테스트 빌드로 확인한 뒤에 통과로 볼 수 있다. |

## 실패 항목과 수정 지시
- 코드나 테스트에는 고칠 곳이 없다. 2b는 테스트 빌드를 만들어 사용자에게 확인을 받아야 한다. `smoke-test-build.ps1`을 같이 실행하라. 그 확인이 끝나면 다시 평가를 요청하라.
- 이번 평가에서는 `smoke-test-build.ps1`을 실행하지 않았다. 빌드가 아직 없기 때문이다.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 173/173, PlayMode 149/149
