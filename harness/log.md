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

### 2026-11-12 T06 일반 적 전투 — done
적 위치에 도착하면 멈추고 다음 마디 첫 박에 원이 뜨도록 공격 2회(2박 간격)와 마무리(↑)를 예약한다. FAIL이면 체력 −1 후 다음 마디부터 그 적을 처음부터 다시 한다. 마무리 PERFECT는 지붕이면 발차기, 땅이면 칼 뺏기/찌르기(적 번호로 번갈아), NORMAL은 밀쳐 쓰러뜨리기. 처형 PERFECT에 슬로모션(timeScale 0.3, 실제 0.4초)을 건다. EnemyActor(회색 상자 적: 무기 축·무기·무기 끝 트레일, 잔상 0.1초 간격 최대 3개, 상태 이름), 판정 원과 카메라가 무기 끝을 따라간다. JudgeSystem에 Cancel 추가. 평가 PASS(1회차), EditMode 64/64, PlayMode 18/18. 다음 작업: T07.

### 2026-11-12 T07 포위 전투 + 최종 무사 — done
포위 전투: 1단계 순차 공격 5회(1박 간격, 키 ↓←↑→↓), 2단계 동시 공격 3회(2~3명, 원 1개), 3단계 윈드밀 ←↑→↓(반 박 간격). 1·2단계 FAIL은 체력만 깎고 이어 가며, 3단계 FAIL은 다음 마디에 3단계만 다시 한다. 성공하면 5명 모두 날아가고 슬로모션과 카메라 360° 회전(0.8초)이 나온다. 카메라는 공격하는 적 쪽으로 0.2초 안에 돌고, 동시 공격 때 0.6 m 낮아진다. 무사: 공격 5회(간격 차트 gap 2,1,1,2,1) + 마무리, 쓰러뜨리면 Cleared. 단계 간격·키 배치는 GDD에 없어 Claude가 정함(CombatPatterns 주석). 평가 PASS(1회차), EditMode 68/68, PlayMode 25/25. 다음 작업: T08.

### 2026-11-12 T08 UI/메뉴 + 점수·랭크 + 속도감 효과 — done
타이틀 메뉴(시작·설정·종료, 설정: 타이밍 보정 슬라이더 5 ms 단위·음량, PlayerPrefs), HUD(하트 3, x1.00, 점수, 콤보 2 이상), 일시정지(Esc, 박자 시계·음악 정지, timeScale 0), 결과 화면(클리어/게임 오버, 판정 수, 최고 속도, 점수, 랭크, 다시 하기·타이틀로), 집중선(코드로 만든 텍스처, 세기 (s−1)/0.3)과 FOV +5°를 만들었다. Unity는 MonoBehaviour 파일 이름이 클래스 이름과 같아야 씬에 저장되므로 UI 컴포넌트를 파일 하나에 하나씩 나눴다. 평가 PASS(1회차), EditMode 73/73, PlayMode 33/33. Esc 키와 폰트 이름은 T09 플레이로 확인. 다음 작업: T09(사람 확인).

### 2026-11-12 T09 [사용자] 회색 상자 플레이 확인 — needs_user
BuildScript.BuildGray로 Builds/Gray/RhythmActionGame.exe(99 MB)를 만들고 smoke-test-build.ps1을 통과했다. 기본 smoke-test는 타이틀만 돌기 때문에 실행 인자 -startGame(타이틀이 바로 게임 씬을 연다)을 추가해 게임 씬도 화면 없이 20초 돌려 로그에 오류가 없음을 확인했다. 사용자 플레이 피드백을 기다린다.
