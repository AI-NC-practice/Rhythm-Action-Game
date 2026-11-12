# 작업 로그

작업이 끝날 때마다 Claude가 한 단락씩 추가합니다. (무엇을 했고, 평가 결과, 다음 작업)
VARCO를 쓴 작업은 실제로 쓴 포인트도 적습니다.

## 제안
작업 범위 밖이라 하지 않은 개선 아이디어를 여기에 적습니다. 넣을지 말지는 사용자가 정합니다.
- 음악 교체: 메트로놈(120 BPM) 대신 AI 생성곡(Suno 등, 국악 퓨전, BPM 고정, 보컬 없음). 곡이 준비되면 교체 작업을 추가한다.
- varco3d-asset 스킬 9번("Mixamo는 사용자가 받는다")을 이 프로젝트의 결정(로그인만 사용자, 다운로드는 Claude)에 맞게 고칠지.

---

### 2026-11-11 초기 설정 (project-setup)
Unity 6000.4.0f1 URP 프로젝트(Hub가 만든 하위 폴더)를 저장소 루트로 옮기고, project.json·CLAUDE.md 값(namespace/exe `RhythmActionGame`)을 채웠다. asmdef 4개, GameConfig.cs, BuildScript.cs를 템플릿에서 만들고 glTFast 6.10.1을 추가했다. unity-verify.ps1 통과(테스트 0개).
GDD를 사용자와 함께 쓰고 확정했다(harness/gdd.lock). 작업 목록 16개와 VARCO 에셋 명세 11개를 사용자가 승인했다.
하네스 수정: 훅 4개(protect-gdd, varco-guard, git-guard, stop-check)가 표준 입력을 CP949로 읽어 한글 입력에서 JSON 파싱에 실패했다 → UTF-8 StreamReader로 읽도록 고쳤다.
환경: VARCO 등급 CREATOR, Blender 5.2.3, GPU RTX 3060 8GB. VARCO 포인트 사용 0. 다음 작업: T01.

### 2026-11-12 T01 기본 설정 + GameConfig 수치 — done
템플릿 잔여물(Readme, TutorialInfo, SampleScene, SampleSceneProfile)을 지우고 Build Settings 씬 목록을 비웠다. GameConfig.cs에 GDD 4.1~4.9 수치를 넣고 GDD 값과 비교하는 EditMode 테스트 6개를 만들었다. 첫 검증은 Unity 라이선스 오류(exit 198)로 실행되지 않아 사용자가 Hub에서 다시 로그인했다. 평가 PASS(1회차), EditMode 6/6. 다음 작업: T02.

### 2026-11-12 T02 박자 시계 + 메트로놈 + 속도 배율 — done
순수 로직 BeatTimeline(dspTime → 박자, 속도 변경·일시정지에도 박자 연속), SpeedRules/SpeedState(GDD 4.1 속도 규칙), ClickSynth(클릭음 파형, 4박 강세)와 MonoBehaviour BeatClock(음악 pitch = s, 이동 5×s), Metronome(PlayScheduled 예약)을 만들었다. 음 높이 보정(Pitch Shifter 1/s)은 실제 곡을 넣을 때 AudioMixer로 추가한다. 평가 PASS(1회차), EditMode 21/21. 다음 작업: T03.

### 2026-11-12 T03 판정 시스템 + 판정 원 UI + 회색 화면/줌 연출 — done
JudgeRules(±60/±150 ms, −300 ms 무시, 보정 ms, 원 지름 360→120), GameSettings(보정·음량 PlayerPrefs), JudgeSystem(Input System 방향키, 원 등장·무입력 FAIL, 속도 반영), JudgeRingView(코드 생성 원 스프라이트, 맑은 고딕 문구 0.6초), JudgeFx(URP Volume 채도 −60, FOV 70→58, 최대 25° 회전, 0.15초/0.2초)를 만들었다. 판정 원을 판정 지점에 투영하는 코드는 있으나, 실제 장애물 위치 연결은 T05/T06에서 한다. 평가 PASS(1회차), EditMode 45/45, PlayMode 5/5. 다음 작업: T04.

### 2026-11-12 T04 회색 상자 맵 + 경로 + 차트 — done
MapLayout(+Z/+X로 꺾이는 수평 경로 660 m, 구간 6개, 높이 키프레임, 지붕 덩어리: 기와집 4채 4.5~5.5 m·틈 4 m, 궁궐 담 5 m, 성문 6 m), 차트 JSON(Resources/Charts/stage1.json: 장애물 20, 일반 적 4, 포위 1, 무사 1, 위치는 달리기 박자), PathFollower(1박 2.5 m, 전투 중 정지), GameBootstrap, SceneBuilder(Title·Game 씬 생성, Build Settings 등록, 회색 상자 머티리얼 Assets/Art/Materials/Gray)를 만들었다. PlayMode 중에는 테스트 러너가 Build Settings에 임시 씬을 더해 씬 수 검사는 EditMode로 옮겼다. 평가 PASS(1회차), EditMode 55/55, PlayMode 8/8. 다음 작업: T05.

### 2026-11-12 T05 장애물 파쿠르 + 체력 — done
StageDirector(차트 장애물을 3박 앞에서 판정 예약, 판정 원 위치는 장애물 중심, 4박 통과 동안 결과별 카메라 연출, FAIL 시 체력 −1, 체력 0이면 GameOver), Traversal(장애물 6종 분류·키 검사·결과별 궤적, 상태 이름 예: Vault_Perfect), PlayerHealth를 만들었다. 카메라 계층은 CameraRig → Motion → PlayerCamera. 평가 PASS(1회차), EditMode 60/60, PlayMode 13/13. 평가자 참고: 4박 PlayMode 검증이 수레 한 종류에만 있음. 다음 작업: T06.
