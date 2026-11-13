# B26 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | B11.1 표의 박과 종류(A/S/L/F)대로 예약, 컷 A·B·C 구간에 원 없음 | PASS | EditMode B26_Steps_MatchTable_KindsAndBeats, B26_PhaseStarts_And_Length72, B26_NoRingDuringCutsABC, B26_AttackKeys_FollowGdd45_Flurry_Alternates 통과. PlayMode B26_Duel_Strike_Posture_PhaseRewind_Clear는 실제 예약 42개의 TargetBeat가 O+표 오프셋과 일치하는지 검사 |
| 2 | B11.2 내 공격 원은 주홍 빛 + 칼 아이콘, PERFECT는 충돌 연출과 무사 밀림, NORMAL은 내가 밀림 | PASS | PlayMode B26_StrikeRing_VermilionGlow_SwordIcon에서 주홍 빛, 칼 아이콘 위치(오른쪽 위), 막기 원은 청록이고 아이콘 없음을 확인. B26_Duel_Strike_Posture_PhaseRewind_Clear에서 StrikePerfect와 무사 Pushed 상태·PushOffset, StrikeNormal에서 무사가 밀리지 않고 카메라가 뒤로 밀림을 확인 |
| 3 | B11.3 자세 게이지 PERFECT +100/N, NORMAL +50/N, 3단계 끝에 가득 + 무너짐 연출, 결투 중에만 화면 위 가운데 표시 | PASS | 같은 테스트에서 3+0.5 단위 값(Units), 3단계 끝 PostureFull, PostureBreak 연출, 붉은 번쩍임(FlashAlpha), anchor (0.5,1), 결투 전후에는 Visible=false 확인. EditMode B26_Posture_PerfectFullAtEnd_NormalHalf 통과 |
| 4 | B11.4 결투 FAIL은 체력 그대로, 그 단계 처음으로 되감기, 게이지 복원 | PASS | B26_Duel_Strike_Posture_PhaseRewind_Clear에서 2단계 FAIL 후 O+28로 되감기, HP 3 유지, 게이지 복원 확인. B26_Duel_FlurryAndFinisherFail_RewindToPhaseStart에서 3단계(O+56)와 마무리(O+66) 되감기 확인 |
| 5 | 모두 PERFECT면 72박 안에 클리어, 판정 수 42, 스테이지 전체 115~130 | PASS | 위 PlayMode 테스트에서 Cleared와 beat ≤ O+72, cue 42개 확인. EditMode B26_Counts_PerPhase_Total42, B26_Chart_Totals_115To130 통과. B04/B20 테스트의 범위도 115~130으로 바뀜 |
| 6 | 기존 무사 테스트(T07, B07, B19, B22, B23 등)를 새 구조에 맞게 바꾸고 근거 주석 | PASS | git diff에서 T07, B07, B19, B22, B23, B04, B20 수정 확인. 각 변경에 "beta1-1: GDD-beta B11.x" 근거 주석이 있음 |
| 7 | unity-verify.ps1 전체 통과 | PASS | 직접 재실행 결과 OVERALL: PASS |
| 8 | B11.6 긴 묶음이 동시에 보이는 원 수만큼 자리를 돌려 써서 안전 타원 안에 머묾 | PASS | EditMode B26_ChainSlots_Rule_And_StayInsideSafeEllipse, PlayMode B26_LongChain_ReusesSlots_InsideSafeEllipse 통과 |

## 실패 항목과 수정 지시
- 없음.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 138/138, PlayMode 120/120. B26 테스트는 EditMode 8개와 PlayMode 4개가 모두 통과했다.
- GLB 검사와 빌드 smoke-test는 이 작업 범위(에셋·빌드 없음)가 아니라서 실행하지 않았다.
- HUMAN 항목은 없다.
