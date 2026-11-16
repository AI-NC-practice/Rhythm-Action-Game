# T14 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 적 캡슐 → enemy_sword/spear/boss 모델, 무기를 손 뼈에 | PASS | EditMode T14_GameScene_EveryEnemy_HasModel_WeaponOnHandBone_NoCapsule(모든 EnemyActor: Humanoid, SkinnedMesh, 캡슐 없음, weaponVisual 부모 = RightHand, 종류별 무기), PlayMode 같은 항목. t14_sword_windup.png·t14_boss_hit.png에서 손에 쥔 칼. |
| 2 | 공격별 상태, 0.1~0.15초 크로스페이드, 타격 순간 = 목표 박 | PASS | CrossFadeSeconds 0.12, CrossFadeInFixedTime. 3연속 공격: 상태, AttackClipTime≈타격(0.02초), Animator 시간≈타격(0.03초), 판정 원 ≤5cm, 공격 종류가 바뀔 때 IsInTransition. 3개 모델 모든 상태·클립·타격 시각이 클립 안. |
| 3 | 처형에서 같은 박 시작, 위치·방향 맞춤 | PASS | 칼 뺏기: StateStartBeat == ExecStartBeat(1e-9), 방향 2도, 거리, 칼 사라짐. 발차기 같은 박. 찌르기는 PlayMode 테스트 없음(같은 정렬 경로). |
| 4 | 피격·쓰러짐·지붕에서 떨어짐·윈드밀 | PASS | Hit 상태, 윈드밀 1m 넘게 날아감, 플레이어 피격, 지붕 아래 2m 넘게 떨어짐. |
| 5 | unity-verify | PASS | 재실행 OVERALL PASS. |

## 실패 항목과 수정 지시
- 없음. 참고: 찌르기 처형 PlayMode 테스트는 다음에 보강하면 좋음.

## 검증 실행 결과
- unity-verify: EditMode 189/189, PlayMode 161/161.
