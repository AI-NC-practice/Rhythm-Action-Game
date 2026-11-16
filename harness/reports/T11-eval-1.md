# T11 평가 보고서
판정: PASS

| # | 기준 | 결과 | 근거 |
|---|---|---|---|
| 1 | 시작 전 예상 포인트를 알리고 승인받음 | PASS | log.md T11 항목 "사용자 포인트 승인(예상 약 1,000~2,200)". (호출자 메모: 사용자가 2026-11-16 채팅에서 "승인"이라고 답함.) |
| 2 | player(humanoid-fingers), enemy_sword, enemy_spear, boss(humanoid)를 T자 자세로 → Rig → animations별 Animate | PASS | T11_EachAnimation_HasHumanoidFbx_ValidAvatar_AndClip: 캐릭터 4종, 동작 수만큼 FBX, FBX마다 클립 하나. humanoid-import.md 38개(player 14, enemy_sword 8, enemy_spear 8, boss 8). player 손가락 뼈 검사. |
| 3 | FBX를 Assets/Art/Models/<id>/에 저장, 텍스처 연결, Humanoid 아바타 valid=true | PASS | animationType=Human, avatar.isValid, isHuman을 FBX마다 단언. T11_MeshFbx_HasTexturesNextToIt_AndFingersOnPlayer: base_color.png·normal.png, 메시 FBX 1개, 삼각형 5,000 이하. HumanoidImport.cs 머티리얼은 텍스처 이름 기준. humanoid-import.md 38개 모두 valid·human True. |
| 4 | isPlus 거부 시 교체·기록 | PASS | 38개 모두 성공, 거부 없음 → 해당 없음. |
| 5 | unity-verify 전체 통과 | PASS | 직접 재실행 OVERALL: PASS. |

## 실패 항목과 수정 지시
- 없음.
- 참고: 메시 FBX는 캐릭터당 하나, 나머지는 뼈+동작만(용량 절감, 테스트가 의도된 구조로 검사).
- 참고: enemy_spear two_hand_attack 클립 31.9초 — T14에서 필요한 구간만.

## 검증 실행 결과
- unity-verify: 컴파일 OK, EditMode 178/178, PlayMode 154/154
