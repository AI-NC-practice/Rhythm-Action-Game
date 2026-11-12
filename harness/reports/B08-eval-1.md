# B08 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | SfxSynth가 Assets/Audio/Sfx/*.wav를 생성한다 (챙, 쿵, 탕, 타격, 바람, 먼지, 귀 울림, 북·징, UI) | PASS | `Assets/Audio/Sfx`에 wav 13개가 있다. 테스트 B08_AllKinds_Synthesize_NormalizedAndFadeOut, B08_Wav_Header16BitMono44k, B08_WavFiles_InAssetsAudioSfx, B08_Timbre_*가 통과했다. |
| 2 | SfxPlayer: 풀 8개, 같은 종류 동시 3개 | PASS | B08_Pool8_SameKindMax3 통과. 같은 소리를 5번 재생해도 동시에 울리는 것은 3개이고, AudioSource는 8개에서 늘지 않는다. |
| 3 | SfxPlayer: Music/SFX 믹서 그룹, 덕킹은 Music 볼륨만 | PASS (참고) | B08_Duck_OnlyMusicVolume_DeepestWins 통과. 겹치면 가장 깊은 값(−8 dB)을 쓰고, 끝나면 원래 음량으로 돌아온다. 음악의 pitch와 time은 그대로이고, Music 그룹 밖 소리의 음량도 그대로다. 실제 AudioMixer 에셋은 없고 `SfxPlayer.musicSources` 목록이 Music 그룹 역할을 한다(이유는 `SfxPlayer.cs:10`에 적혀 있다). 동작은 기준과 같다. 실제 곡을 넣을 때 AudioMixer로 바꿀지는 B10에서 정하면 된다. |
| 4 | 상황별 효과음 연결 | PASS | B08_Presets_PlaySoundsAndDuck(PERFECT 패링은 챙+쿵과 −4 dB, 피격은 타격+귀 울림, NORMAL은 둔한 탕, 윈드밀 화살표는 북)과 B08_SoundsPerSituation_MatchProposalColumn 통과. |
| 5 | 장단 반주가 메트로놈을 대신하고 첫 박 오차 10 ms 이하 | PASS | B08_GameScene_JangguBacking_ReplacesMetronome_BeatAccurate 통과. 씬에 Metronome이 없고, 첫 박 오차가 0.010초 이하다. 판정으로 속도가 바뀐 뒤에도 매 박의 예약 시각이 박자 시계와 맞는다. 박 순서는 0, 1, 1.5, 2, 3, 3.5다. |
| 6 | unity-verify 전체 통과 | PASS | 직접 다시 실행한 결과 OVERALL: PASS. |

## 실패 항목과 수정 지시
- 없음.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 111/111, PlayMode 80/80 (OVERALL: PASS)
- 에셋이 GLB가 아니라 wav라서 check-glb는 실행하지 않았다. 빌드 작업이 아니라서 smoke-test도 실행하지 않았다.
- 사람이 판단할 항목(HUMAN): 합성음의 실제 음색과 듣는 느낌은 판정에서 뺐다. B09 플레이 확인에서 사용자가 볼 부분이다.
