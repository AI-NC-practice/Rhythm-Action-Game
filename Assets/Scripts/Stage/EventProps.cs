using System;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 새 사건·안 멈추는 적·복선의 회색 상자 소품을 코드로 만든다 (GDD-beta B2.2, B2.3, B6).
    /// SceneBuilder(에디터, 머티리얼 에셋)와 EventPropDirector(실행 중, 시험용 차트)가 함께 쓴다.
    /// 기존 장애물 6종은 SceneBuilder.BuildObstacles가 만든다.
    /// </summary>
    public static class EventProps
    {
        /// <summary>이 이벤트에 회색 상자 소품이 필요한지 (기존 장애물·멈추는 전투 제외).</summary>
        public static bool Needs(ChartEvent e)
        {
            if (e.IsForeshadow) return true;
            if (!e.IsRunEvent) return false;
            var def = EventCatalog.Get(e.EventKind);
            return !def.Category.HasValue;
        }

        static readonly Dictionary<string, Material> runtimeMats = new Dictionary<string, Material>();

        /// <summary>실행 중 머티리얼 (툰, 작업 T13 — 없으면 URP Lit). 에디터에서는 SceneBuilder가 에셋 머티리얼을 넘긴다.</summary>
        public static Material RuntimeMat(string name, Color color)
        {
            if (runtimeMats.TryGetValue(name, out var m) && m != null) return m;
            var shader = Shader.Find("RhythmActionGame/Toon") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            m = new Material(shader) { name = name, color = color };
            if (m.HasProperty("_Steps"))
            {
                m.SetFloat("_Steps", GameConfig.Body.ToonSteps);
                m.SetFloat("_ShadowLevel", GameConfig.Body.ToonShadowLevel);
                m.SetFloat("_OutlineWidth", GameConfig.Body.OutlineWidth);
            }
            runtimeMats[name] = m;
            return m;
        }

        static readonly Color Wood = new Color(0.55f, 0.38f, 0.22f);
        static readonly Color Rope = new Color(0.85f, 0.78f, 0.6f);
        static readonly Color Tile = new Color(0.25f, 0.26f, 0.3f);
        static readonly Color Clay = new Color(0.45f, 0.28f, 0.18f);
        static readonly Color Cloth = new Color(0.75f, 0.3f, 0.25f);
        static readonly Color Enemy = new Color(0.8f, 0.2f, 0.2f);
        static readonly Color Shadow = new Color(0.15f, 0.12f, 0.12f);

        /// <summary>소품 하나를 만들어 EventProp을 붙여 돌려준다.</summary>
        public static EventProp Build(ChartEvent e, Transform parent, Func<string, Color, Material> mat = null)
        {
            mat = mat ?? RuntimeMat;
            string kind = e.IsForeshadow ? StageChart.TypeForeshadow : e.EventKind;
            int beats = e.IsForeshadow ? 0 : EventCatalog.Get(kind).Beats;
            var slots = e.IsForeshadow ? new List<EventSlot>() : EventCatalog.SlotsFor(e);

            float centerBeat = e.runBeat + beats * 0.5f;
            var p = MapLayout.PointAt(centerBeat * GameConfig.MetersPerBeat, out var fwd);
            var root = new GameObject($"EventProp_{e.runBeat:000}_{kind}").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(p, Quaternion.LookRotation(fwd));
            var prop = root.gameObject.AddComponent<EventProp>();
            prop.runBeat = e.runBeat;
            prop.kind = kind;
            prop.beats = beats;
            prop.span = e.span;
            prop.companion = e.IsObstacle && !string.IsNullOrEmpty(e.approach) ? e.approach : "";
            float roofHalf = MapLayout.RoofHalfAt(e.Distance);
            prop.chaseLateral = roofHalf > 0f ? Mathf.Min(GameConfig.Map.ChaseLateral, roofHalf - GameConfig.Map.RoofRunnerInset) : GameConfig.Map.ChaseLateral;   // 작업 R03
            prop.slotOffsets = new float[slots.Count];
            prop.slotSides = new float[slots.Count];
            for (int i = 0; i < slots.Count; i++)
            {
                prop.slotOffsets[i] = (float)slots[i].Offset;
                var k = slots[i].Key.Value;
                prop.slotSides[i] = k == ArrowKey.Left ? -1f : k == ArrowKey.Right ? 1f : 0f;
            }
            float Z(double beatFromStart) => (float)((beatFromStart - beats * 0.5) * GameConfig.MetersPerBeat);
            var parts = new List<Transform>();

            switch (kind)
            {
                // 작업 R04: ModelOverride(에디터)가 있으면 회색 도형 대신 R02 모델. 부품 이름·위치·움직임(pivot)은 그대로.
                case "tile_slip":
                    for (int i = 0; i < 3; i++)
                        parts.Add(Part(root, $"Prop_tile_{i}", new Vector3(-0.6f + i * 0.6f, 0.05f, Z(2) + 0.3f * i), new Vector3(0.5f, 0.08f, 0.7f), mat("Tile", Tile), "eave_low", new Vector3(0.6f, 0.22f, 0.7f)));
                    if (!Models) Box(root, "Prop_eave_edge", new Vector3(1.8f, -0.2f, Z(3)), new Vector3(0.3f, 0.3f, 4f), mat("Tile", Tile));   // 모델 지붕에는 처마가 있다
                    break;
                case "rope_swing":
                    if (Models) Part(root, "Prop_rope", new Vector3(0, 1.6f, Z(2)), Vector3.one, null, "rope_swing", new Vector3(4.4f, 3.2f, 0.6f));
                    else
                    {
                        Box(root, "Prop_rope", new Vector3(0, 3.0f, Z(2)), new Vector3(4.0f, 0.06f, 0.06f), mat("Rope", Rope));
                        Box(root, "Prop_rope_PostL", new Vector3(-2.0f, 1.5f, Z(2)), new Vector3(0.15f, 3.0f, 0.15f), mat("Wood", Wood));
                        Box(root, "Prop_rope_PostR", new Vector3(2.0f, 1.5f, Z(2)), new Vector3(0.15f, 3.0f, 0.15f), mat("Wood", Wood));
                    }
                    break;
                case "rolling_jars":
                    for (int i = 0; i < prop.slotOffsets.Length; i++)
                        parts.Add(Part(root, $"Prop_jar_{i}", new Vector3(-prop.slotSides[i] * 0.8f, 0.45f, Z(prop.slotOffsets[i]) + 12f), Vector3.one * 0.9f, mat("Jars", Clay), "rolling_jar", new Vector3(1.0f, 0.88f, 0.88f), sphere: true));
                    break;
                case "awning_bounce":
                    if (Models) parts.Add(Part(root, "Prop_awning", new Vector3(0, 0.9f, Z(2)), Vector3.one, null, "stall_b", new Vector3(3.0f, 1.8f, 2.0f)));   // 차양 꼭대기 1.8 m, 출렁임은 좌판째
                    else
                    {
                        parts.Add(Box(root, "Prop_awning", new Vector3(0, 1.6f, Z(2)), new Vector3(3.0f, 0.1f, 2.0f), mat("Awning", Cloth)));
                        Box(root, "Prop_awning_PostL", new Vector3(-1.4f, 0.8f, Z(2) - 0.9f), new Vector3(0.1f, 1.6f, 0.1f), mat("Wood", Wood));
                        Box(root, "Prop_awning_PostR", new Vector3(1.4f, 0.8f, Z(2) - 0.9f), new Vector3(0.1f, 1.6f, 0.1f), mat("Wood", Wood));
                    }
                    break;
                case "closing_gate":
                    parts.Add(Part(root, "Prop_gate_door_L", new Vector3(-2.2f, 1.6f, Z(2)), new Vector3(1.6f, 3.2f, 0.2f), mat("GateDoor", Wood), "gate_door", new Vector3(1.6f, 3.2f, 0.25f)));
                    var doorR = Part(root, "Prop_gate_door_R", new Vector3(2.2f, 1.6f, Z(2)), new Vector3(1.6f, 3.2f, 0.2f), mat("GateDoor", Wood), "gate_door", new Vector3(1.6f, 3.2f, 0.25f));
                    if (Models) doorR.localRotation = Quaternion.Euler(0, 180f, 0);   // 오른쪽 문짝은 뒤집어 (고리가 안쪽)
                    parts.Add(doorR);
                    if (Models) Part(root, "Prop_gate_lintel", new Vector3(0, 2.1f, Z(2)), Vector3.one, null, "palace_gate", new Vector3(6.6f, 4.2f, 1.2f));   // 궁궐 문틀
                    else Box(root, "Prop_gate_lintel", new Vector3(0, 3.4f, Z(2)), new Vector3(6.0f, 0.4f, 0.4f), mat("Wood", Wood));
                    break;
                case "arrow_volley":
                    for (int i = 0; i < prop.slotOffsets.Length; i++)
                    {
                        var a = Part(root, $"Prop_arrow_{i}", new Vector3(-prop.slotSides[i] * 6f, 1.6f, Z(prop.slotOffsets[i])), new Vector3(0.9f, 0.04f, 0.04f), mat("Arrow", Wood), "arrow", new Vector3(0.9f, 0.1f, 0.1f));
                        if (Models && prop.slotSides[i] > 0f) a.localRotation = Quaternion.Euler(0, 180f, 0);   // 날아오는 쪽으로 촉
                        a.gameObject.SetActive(false);
                        parts.Add(a);
                    }
                    break;
                case "long_jump":
                    Part(root, "Prop_gap_edge_near", new Vector3(0, -0.15f, Z(2)), new Vector3(3.0f, 0.3f, 0.3f), mat("Tile", Tile), "eave_low", new Vector3(3.0f, 0.3f, 0.6f));
                    Part(root, "Prop_gap_edge_far", new Vector3(0, -0.15f, Z(4)), new Vector3(3.0f, 0.3f, 0.3f), mat("Tile", Tile), "eave_low", new Vector3(3.0f, 0.3f, 0.6f));
                    break;
                case "banner_slide":
                    if (Models) Part(root, "Prop_banner", new Vector3(1.4f, 3.0f, Z(1)), Vector3.one, null, "banner_flag", new Vector3(2.0f, 6.0f, 0.8f));   // 깃대·깃발·비스듬한 줄
                    else
                    {
                        var line = Box(root, "Prop_banner_line", new Vector3(0, 2.5f, Z(2)), new Vector3(0.05f, 0.05f, 6f), mat("Rope", Rope));
                        line.localRotation = Quaternion.Euler(30f, 0, 0);
                        Box(root, "Prop_banner", new Vector3(0.6f, 3.2f, Z(1)), new Vector3(0.05f, 1.4f, 0.8f), mat("Banner", Cloth));
                    }
                    break;
                case StageChart.TypeForeshadow:
                    // 먼 지붕·담 위 그림자 3명. 작업 R03: 종류마다 정한 자리(MapPlan.ForeshadowStand, 그 아래 실제 지붕·담) —
                    // 높이는 SceneBuilder가 발밑 지붕·담 윗면에 맞춘다. 자리가 없는 종류(시험 차트)는 예전처럼 옆 12 m, 높이 6 m.
                    bool hasStand = Array.Exists(GameConfig.Map.ForeshadowStands, s => s.Kind == e.kind);
                    for (int i = 0; i < 3; i++)
                    {
                        var local = hasStand ? root.InverseTransformPoint(MapPlan.ForeshadowStand(e, i)) : new Vector3(10f + i * 1.2f, 6f, 25f + i * 1.5f);
                        if (hasStand) local.y = -p.y;   // 땅(0) 기준. 지지면 높이는 빌드 때 맞춘다
                        EnemyFigure(root, $"Prop_foreshadow_{i}", local, mat("Foreshadow", Shadow));
                    }
                    break;
            }

            string enemyHow = prop.companion != "" ? prop.companion : (EventCatalog.Has(kind) && EventCatalog.Get(kind).IsEnemy ? kind : "");
            if (enemyHow != "")
            {
                float side = prop.slotSides.Length > 0 ? prop.slotSides[0] : 1f;
                var start = enemyHow == EventCatalog.Ambush ? new Vector3(-side * 3.2f, 0, Z(2))
                          : enemyHow == EventCatalog.Chase ? new Vector3(-side * prop.chaseLateral, 0.5f, Z(0) + 2f)
                          : new Vector3(0, 0, Z(2) + 8f);
                prop.enemy = EnemyFigure(root, $"Enemy_{enemyHow}", start, mat("EnemySword", Enemy));
            }
            prop.parts = parts.ToArray();
            prop.ResetProp();
            return prop;
        }

        /// <summary>
        /// 에디터(SceneBuilder)가 넣는 모델 만들기 (작업 R04): (축, 에셋 id, 크기) → 축 가운데에 그 크기로 맞춘 모델.
        /// 없으면(실행 중 시험 차트) 회색 도형.
        /// </summary>
        public static Func<Transform, string, Vector3, Transform> ModelOverride;
        static bool Models => ModelOverride != null;

        /// <summary>부품 하나: 모델이 있으면 같은 이름의 빈 축 + 그 아래 모델(축 가운데에 modelSize), 없으면 회색 상자/공.</summary>
        static Transform Part(Transform root, string name, Vector3 localPos, Vector3 size, Material m, string model, Vector3 modelSize, bool sphere = false)
        {
            if (Models && model != null)
            {
                var pivot = new GameObject(name).transform;
                pivot.SetParent(root, false);
                pivot.localPosition = localPos;
                ModelOverride(pivot, model, modelSize);
                return pivot;
            }
            return sphere ? Sphere(root, name, localPos, size.x, m) : Box(root, name, localPos, size, m);
        }

        static Transform Box(Transform parent, string name, Vector3 localPos, Vector3 size, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            var c = go.GetComponent<Collider>(); if (c != null) UnityEngine.Object.DestroyImmediate(c);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            return go.transform;
        }

        static Transform Sphere(Transform parent, string name, Vector3 localPos, float d, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            var c = go.GetComponent<Collider>(); if (c != null) UnityEngine.Object.DestroyImmediate(c);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * d;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            return go.transform;
        }

        /// <summary>에디터(SceneBuilder)가 넣는 적 모델 만들기 (작업 T14). 없으면(실행 중 시험 차트) 회색 상자.</summary>
        public static Func<Transform, string, Vector3, Transform> FigureOverride;

        /// <summary>적: 모델(FigureOverride) 또는 회색 상자(발 위치 루트 + 몸 캡슐 + 칼 막대).</summary>
        static Transform EnemyFigure(Transform parent, string name, Vector3 feetLocal, Material m)
        {
            if (FigureOverride != null) return FigureOverride(parent, name, feetLocal);
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = feetLocal;
            root.localRotation = Quaternion.Euler(0, 180f, 0);   // 플레이어 쪽을 봄
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            var c = body.GetComponent<Collider>(); if (c != null) UnityEngine.Object.DestroyImmediate(c);
            body.transform.SetParent(root, false);
            body.transform.localPosition = new Vector3(0, 0.875f, 0);
            body.transform.localScale = new Vector3(0.6f, 0.875f, 0.6f);
            body.GetComponent<MeshRenderer>().sharedMaterial = m;
            Box(root, "Weapon", new Vector3(0.4f, 1.2f, 0.3f), new Vector3(0.06f, 0.06f, 1.0f), m);
            return root;
        }
    }
}
