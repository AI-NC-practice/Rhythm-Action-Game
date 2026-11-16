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
                case "tile_slip":
                    for (int i = 0; i < 3; i++)
                        parts.Add(Box(root, $"Prop_tile_{i}", new Vector3(-0.6f + i * 0.6f, 0.05f, Z(2) + 0.3f * i), new Vector3(0.5f, 0.08f, 0.7f), mat("Tile", Tile)));
                    Box(root, "Prop_eave_edge", new Vector3(1.8f, -0.2f, Z(3)), new Vector3(0.3f, 0.3f, 4f), mat("Tile", Tile));
                    break;
                case "rope_swing":
                    Box(root, "Prop_rope", new Vector3(0, 3.0f, Z(2)), new Vector3(4.0f, 0.06f, 0.06f), mat("Rope", Rope));
                    Box(root, "Prop_rope_PostL", new Vector3(-2.0f, 1.5f, Z(2)), new Vector3(0.15f, 3.0f, 0.15f), mat("Wood", Wood));
                    Box(root, "Prop_rope_PostR", new Vector3(2.0f, 1.5f, Z(2)), new Vector3(0.15f, 3.0f, 0.15f), mat("Wood", Wood));
                    break;
                case "rolling_jars":
                    for (int i = 0; i < prop.slotOffsets.Length; i++)
                        parts.Add(Sphere(root, $"Prop_jar_{i}", new Vector3(-prop.slotSides[i] * 0.8f, 0.45f, Z(prop.slotOffsets[i]) + 12f), 0.9f, mat("Jars", Clay)));
                    break;
                case "awning_bounce":
                    parts.Add(Box(root, "Prop_awning", new Vector3(0, 1.6f, Z(2)), new Vector3(3.0f, 0.1f, 2.0f), mat("Awning", Cloth)));
                    Box(root, "Prop_awning_PostL", new Vector3(-1.4f, 0.8f, Z(2) - 0.9f), new Vector3(0.1f, 1.6f, 0.1f), mat("Wood", Wood));
                    Box(root, "Prop_awning_PostR", new Vector3(1.4f, 0.8f, Z(2) - 0.9f), new Vector3(0.1f, 1.6f, 0.1f), mat("Wood", Wood));
                    break;
                case "closing_gate":
                    parts.Add(Box(root, "Prop_gate_door_L", new Vector3(-2.2f, 1.6f, Z(2)), new Vector3(1.6f, 3.2f, 0.2f), mat("GateDoor", Wood)));
                    parts.Add(Box(root, "Prop_gate_door_R", new Vector3(2.2f, 1.6f, Z(2)), new Vector3(1.6f, 3.2f, 0.2f), mat("GateDoor", Wood)));
                    Box(root, "Prop_gate_lintel", new Vector3(0, 3.4f, Z(2)), new Vector3(6.0f, 0.4f, 0.4f), mat("Wood", Wood));
                    break;
                case "arrow_volley":
                    for (int i = 0; i < prop.slotOffsets.Length; i++)
                    {
                        var a = Box(root, $"Prop_arrow_{i}", new Vector3(-prop.slotSides[i] * 6f, 1.6f, Z(prop.slotOffsets[i])), new Vector3(0.9f, 0.04f, 0.04f), mat("Arrow", Wood));
                        a.gameObject.SetActive(false);
                        parts.Add(a);
                    }
                    break;
                case "long_jump":
                    Box(root, "Prop_gap_edge_near", new Vector3(0, -0.15f, Z(2)), new Vector3(3.0f, 0.3f, 0.3f), mat("Tile", Tile));
                    Box(root, "Prop_gap_edge_far", new Vector3(0, -0.15f, Z(4)), new Vector3(3.0f, 0.3f, 0.3f), mat("Tile", Tile));
                    break;
                case "banner_slide":
                    var line = Box(root, "Prop_banner_line", new Vector3(0, 2.5f, Z(2)), new Vector3(0.05f, 0.05f, 6f), mat("Rope", Rope));
                    line.localRotation = Quaternion.Euler(30f, 0, 0);
                    Box(root, "Prop_banner", new Vector3(0.6f, 3.2f, Z(1)), new Vector3(0.05f, 1.4f, 0.8f), mat("Banner", Cloth));
                    break;
                case StageChart.TypeForeshadow:
                    // 먼 지붕 위 그림자 2~3명 (경로 옆 12 m, 높이 6 m)
                    for (int i = 0; i < 3; i++)
                        EnemyFigure(root, $"Prop_foreshadow_{i}", new Vector3(10f + i * 1.2f, 6f, 25f + i * 1.5f), mat("Foreshadow", Shadow));
                    break;
            }

            string enemyHow = prop.companion != "" ? prop.companion : (EventCatalog.Has(kind) && EventCatalog.Get(kind).IsEnemy ? kind : "");
            if (enemyHow != "")
            {
                float side = prop.slotSides.Length > 0 ? prop.slotSides[0] : 1f;
                var start = enemyHow == EventCatalog.Ambush ? new Vector3(-side * 3.2f, 0, Z(2))
                          : enemyHow == EventCatalog.Chase ? new Vector3(-side * 3.5f, 0.5f, Z(0) + 2f)
                          : new Vector3(0, 0, Z(2) + 8f);
                prop.enemy = EnemyFigure(root, $"Enemy_{enemyHow}", start, mat("EnemySword", Enemy));
            }
            prop.parts = parts.ToArray();
            prop.ResetProp();
            return prop;
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

        /// <summary>회색 상자 적: 발 위치 루트 + 몸 캡슐 + 칼 막대.</summary>
        static Transform EnemyFigure(Transform parent, string name, Vector3 feetLocal, Material m)
        {
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
