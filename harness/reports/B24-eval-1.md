# B24 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 챕터 되감기 0.8초 동안 플레이어 보이는 위치가 되감는 지점까지 뒤로 움직임 (중간값이 시작과 끝 사이, 끝에서 일치) | PASS | B24_ChapterRewind_PlayerSlidesBackAlongPath 통과. 중간의 VisualRunBeat는 시작값보다 1박 넘게 뒤이고 56+1보다 크다. 실제 transform이 MapLayout 경로 위에 있고, 끝에서 RunBeat가 56이며 VisualRunBeat와 같다. 구현은 StageDirector.Rewind.cs의 UpdateRewind가 VisualOverride를 보간하는 방식이다. |
| 2 | 무사·윈드밀 되감기 동안 적 무기 자세가 마지막 휘두르기에서 거꾸로 돌아감 | PASS | B24_BossRewind_WeaponSwingReverses 통과. 각도가 a1 > 10, a2 < a1, a3 < a2, a3 < 5이고 State는 "Rewinding"이다. 무사만 직접 검사한다. 윈드밀은 직접 테스트가 없지만, UpdateRewind가 encounter의 모든 Actors에 RewindSwing을 호출하므로 같은 코드 경로를 지난다(코드로 확인). |
| 3 | 테이프 되감기 겹화면: 먹 줄기 3개 이상 오른쪽→왼쪽, 가로 줄무늬, "◀◀", 0.8초 뒤 사라짐 | PASS | B24_TapeOverlay_StreaksScanlinesIcon 통과. 줄기 3개 이상의 x가 Step 뒤 모두 줄어들고, 줄무늬 스프라이트는 Scanlines이며 y가 일렁이고, IconText는 "◀◀"이며, RewindSeconds 뒤 Active가 false다. B24_Font_HasRewindIcon도 통과해 폰트에 ◀ 글자가 있다. |
| 4 | 기존 B19 되감기 테스트 통과 | PASS | 전체 PlayMode 116/116 통과, 실패 없음. |
| 5 | unity-verify.ps1 전체 통과 | PASS | 직접 재실행한 결과 OVERALL: PASS. |

## 실패 항목과 수정 지시
- 없음. 참고: 윈드밀 되감기 전용 테스트는 없다. 필수는 아니지만 보강하면 좋다.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 130/130, PlayMode 116/116, OVERALL: PASS
- 빌드나 에셋 작업이 아니라서 smoke-test와 check-glb는 실행하지 않았다.
