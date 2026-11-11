---
name: blender-tools
description: (선택) Blender를 화면 없이 실행해 3D 에셋을 다루는 절차. 모델 미리보기 렌더링, GLB→FBX 변환(텍스처 포함), 삼각형 줄이기. Blender가 설치돼 있을 때만 사용하고, 없으면 건너뛴다.
---

# Blender 도구 (선택)

모든 실행은 `harness/scripts/blender-run.ps1`을 거친다. Blender가 없으면 종료 코드 4로 끝나니 그 단계만 건너뛰고 보고한다.

## 확인
`powershell -NoProfile -ExecutionPolicy Bypass -File harness/scripts/blender-run.ps1 -Check`

## 1. 미리보기 렌더링 — 모델을 눈으로 확인할 때
```
blender-run.ps1 -Script render_preview -ScriptArgs "--in","Assets/Art/Models/<id>.glb","--out","harness/reports/preview/<id>.png"
```
- 결과 PNG를 Read 도구로 열어 본다. 모양, 방향(정면), 텍스처, 크기 비율을 확인한다.
- 로그에 경계 크기(m)와 삼각형 수가 나온다.
- 에셋 작업에서는 GLB를 받은 직후 미리보기를 남기면 평가자도 같은 이미지를 볼 수 있다.

## 2. GLB → FBX (텍스처 포함) — 리깅된 캐릭터를 Unity Humanoid로 쓸 때
```
blender-run.ps1 -Script convert_to_fbx -ScriptArgs "--in","<model.glb>","--out","Assets/Art/Models/<id>/<id>.fbx"
```
- 뼈대, 스킨, 애니메이션을 유지하고 텍스처를 FBX 안에 넣는다.
- Blender를 쓸 수 없으면 대신 `harness/scripts/extract-glb-textures.ps1`로 GLB 이미지를 꺼내 VARCO FBX 옆에 둔다.

## 3. 삼각형 줄이기 — GLB가 상한을 넘을 때 (정적 모델만)
```
blender-run.ps1 -Script decimate -ScriptArgs "--in","<model.glb>","--out","<model_small.glb>","--max-tris","5000"
```
- 리깅된 모델은 여기서 줄이지 말고, 리깅 전에 VARCO `Remesh` 노드로 줄인다.

## 주의
- 원본 파일은 덮어쓰지 않는다. 결과는 새 경로에 쓴다.
- 새 Blender 스크립트가 필요하면 `harness/scripts/blender/`에 추가하고 `_common.py`의 `args/ok/fail`을 쓴다(`[OK]`/`[FAIL]` 줄로 결과를 알린다).
