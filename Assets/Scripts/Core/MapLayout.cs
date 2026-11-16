using System.Collections.Generic;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 맵 배치 데이터 (GDD 4.9). SceneBuilder와 게임 코드, 테스트가 함께 쓴다.
    /// 경로는 수평(XZ) 직선 구간을 이은 것이고, 위치는 "경로 거리"(수평 거리, m)로 나타낸다.
    /// 높이는 달리기 박자별 키프레임으로 따로 정한다(오르기·건너뛰기·뛰어내리기는 장애물 4박자 안에서 바뀐다).
    /// </summary>
    public static class MapLayout
    {
        public struct Segment
        {
            public Vector3 Direction;
            public float Length;
            public Segment(Vector3 dir, float len) { Direction = dir; Length = len; }
        }

        /// <summary>지붕 덩어리 (기와집·궁궐 담·성문 윗면). 경로 거리 [Start, End], 윗면 높이, 폭.</summary>
        public struct Roof
        {
            public string AssetId;
            public float Start, End, Height, Width;
            public Roof(string id, float s, float e, float h, float w = 6f) { AssetId = id; Start = s; End = e; Height = h; Width = w; }
        }

        public struct Section
        {
            public int Number;
            public string Name;
            public int StartBeat, EndBeat;
            public Section(int n, string name, int s, int e) { Number = n; Name = name; StartBeat = s; EndBeat = e; }
        }

        /// <summary>경로 구간: +Z 140 → +X 130 → +Z 220 → +X 140 → +Z 30 = 660 m (264박자 × 2.5 m).</summary>
        public static readonly Segment[] Route =
        {
            new Segment(Vector3.forward, 140f),
            new Segment(Vector3.right, 130f),
            new Segment(Vector3.forward, 220f),
            new Segment(Vector3.right, 140f),
            new Segment(Vector3.forward, 30f),
        };

        public static readonly Vector3 Start = Vector3.zero;

        /// <summary>구간 6개 (달리기 박자 기준).</summary>
        public static readonly Section[] Sections =
        {
            new Section(1, "마을 골목", 0, 56),
            new Section(2, "돌담과 지붕 오르기", 56, 108),
            new Section(3, "기와지붕", 108, 176),
            new Section(4, "마당 포위 전투", 176, 196),
            new Section(5, "궁궐 담", 196, 252),
            new Section(6, "성문 위 결전", 252, 264),
        };

        /// <summary>높이 키프레임 (달리기 박자, 높이 m). 사이는 선형.</summary>
        public static readonly Vector2[] HeightKeys =
        {
            new Vector2(0, 0), new Vector2(100, 0), new Vector2(104, 5f),       // 지붕 오르기
            new Vector2(112, 5f), new Vector2(116, 4.5f),                        // 지붕 건너뛰기
            new Vector2(120, 4.5f), new Vector2(124, 5.5f),                      // 지붕 건너뛰기
            new Vector2(144, 5.5f), new Vector2(148, 5f),                        // 지붕 건너뛰기
            new Vector2(164, 5f), new Vector2(168, 0),                           // 뛰어내리기
            new Vector2(224, 0), new Vector2(228, 5f),                           // 궁궐 담 오르기
            new Vector2(248, 5f), new Vector2(252, 6f),                          // 성문 오르기
            new Vector2(264, 6f),
        };

        /// <summary>지붕 덩어리. 지붕 사이 틈은 4 m(GDD 3~5 m), 높이 4.5~6 m(GDD 4~6 m).
        /// 작업 R03: 기와지붕·궁궐 담 윗면은 폭 8 m — 옆에서 나란히 달리는 추격 적(옆 3.5 m)도 지붕 위에 선다.</summary>
        public static readonly Roof[] Roofs =
        {
            new Roof("house_giwa", 255f, 283f, 5f, 8f),
            new Roof("house_giwa", 287f, 303f, 4.5f, 8f),
            new Roof("house_giwa", 307f, 363f, 5.5f, 8f),
            new Roof("house_giwa", 367f, 415f, 5f, 8f),
            new Roof("wall_palace", 565f, 625f, 5f, 8f),
            new Roof("gate", 625f, 665f, 6f),
        };

        public static float TotalLength
        {
            get { float t = 0; foreach (var s in Route) t += s.Length; return t; }
        }

        /// <summary>모퉁이(구간 경계)의 경로 거리.</summary>
        public static IEnumerable<float> Corners
        {
            get { float acc = 0; for (int i = 0; i < Route.Length - 1; i++) { acc += Route[i].Length; yield return acc; } }
        }

        /// <summary>d가 모퉁이 곡선 구간 [c - R, c + R] 안이면 그 모퉁이 거리 c, 아니면 -1.</summary>
        public static float CornerNear(float d)
        {
            foreach (var c in Corners) if (Mathf.Abs(d - c) < GameConfig.Map.CornerRadius) return c;
            return -1f;
        }

        /// <summary>직선 구간만 이은 경로(모퉁이 꺾임 그대로)의 수평 위치와 방향. 지붕·담처럼 곧은 상자를 놓을 때 쓴다.</summary>
        public static Vector3 VertexPointAt(float d, out Vector3 forward)
        {
            var p = Start;
            foreach (var s in Route)
            {
                if (d <= s.Length) { forward = s.Direction; return p + s.Direction * Mathf.Max(0f, d); }
                p += s.Direction * s.Length;
                d -= s.Length;
            }
            forward = Route[Route.Length - 1].Direction;
            return p + forward * d;
        }

        /// <summary>경로 거리 d의 수평 위치(y=0)와 진행 방향.
        /// 작업 R03: 모퉁이 앞뒤 CornerRadius 안은 2차 베지어 곡선으로 잇는다 (꺾임에서 카메라가 순간이동처럼 돌지 않게).
        /// 곡선 밖의 위치와 경로 거리·박자 관계는 예전과 같다.</summary>
        public static Vector3 FlatPointAt(float d, out Vector3 forward)
        {
            float c = CornerNear(d);
            if (c < 0f) return VertexPointAt(d, out forward);
            float r = GameConfig.Map.CornerRadius;
            var a = VertexPointAt(c - r, out _);
            var v = VertexPointAt(c, out _);
            var b = VertexPointAt(c + r, out _);
            float t = (d - (c - r)) / (2f * r), u = 1f - t;
            var tangent = 2f * u * (v - a) + 2f * t * (b - v);
            forward = tangent.sqrMagnitude > 1e-8f ? tangent.normalized : Vector3.forward;
            return u * u * a + 2f * u * t * v + t * t * b;
        }

        /// <summary>달리기 박자 b에서의 발 높이 (m).</summary>
        public static float HeightAtBeat(float b)
        {
            if (b <= HeightKeys[0].x) return HeightKeys[0].y;
            for (int i = 1; i < HeightKeys.Length; i++)
            {
                if (b <= HeightKeys[i].x)
                {
                    var a = HeightKeys[i - 1]; var c = HeightKeys[i];
                    return Mathf.Lerp(a.y, c.y, (b - a.x) / (c.x - a.x));
                }
            }
            return HeightKeys[HeightKeys.Length - 1].y;
        }

        /// <summary>경로 거리 d의 발 위치 (높이 포함).</summary>
        public static Vector3 PointAt(float d, out Vector3 forward)
        {
            var p = FlatPointAt(d, out forward);
            p.y = HeightAtBeat(d / GameConfig.MetersPerBeat);
            return p;
        }

        public static Vector3 PointAt(float d) => PointAt(d, out _);

        /// <summary>진행 방향 기준 오른쪽.</summary>
        public static Vector3 RightOf(Vector3 forward) => Vector3.Cross(Vector3.up, forward).normalized;

        /// <summary>[start, end] 거리 범위를 경로 구간 경계에서 나눈다 (모퉁이를 넘는 상자를 쪼개기 위해).</summary>
        public static List<Vector2> SplitAtCorners(float start, float end)
        {
            var result = new List<Vector2>();
            float acc = 0;
            foreach (var s in Route)
            {
                float a = Mathf.Max(start, acc), b = Mathf.Min(end, acc + s.Length);
                if (b > a) result.Add(new Vector2(a, b));
                acc += s.Length;
            }
            if (end > acc) result.Add(new Vector2(Mathf.Max(start, acc), end));
            return result;
        }

        /// <summary>땅 위 길처럼 곡선을 따라야 하는 띠를 조각낸다: 곡선 밖은 직선 조각, 곡선 안은 maxChunk(m) 이하 조각.</summary>
        public static List<Vector2> SplitAlongCurve(float start, float end, float maxChunk = 2f)
        {
            var cuts = new List<float> { start, end };
            foreach (var c in Corners)
            {
                float r = GameConfig.Map.CornerRadius;
                for (float x = c - r; x <= c + r + 1e-3f; x += maxChunk) if (x > start && x < end) cuts.Add(x);
                if (c + r > start && c + r < end) cuts.Add(c + r);
            }
            cuts.Sort();
            var result = new List<Vector2>();
            for (int i = 1; i < cuts.Count; i++) if (cuts[i] - cuts[i - 1] > 1e-3f) result.Add(new Vector2(cuts[i - 1], cuts[i]));
            return result;
        }

        /// <summary>점 p(수평)에서 경로 중심선까지 수평 거리 (0.5 m 간격 표본).</summary>
        /// <summary>경로 중심선 표본 (0.5 m 간격, 앞뒤 10 m 더).</summary>
        public static Vector3[] PathSamples
        {
            get
            {
                if (pathSamples == null)
                {
                    var list = new List<Vector3>();
                    for (float d = -10f; d <= TotalLength + 10f; d += 0.5f) list.Add(FlatPointAt(d, out _));
                    pathSamples = list.ToArray();
                }
                return pathSamples;
            }
        }

        public static float DistanceToPath(Vector3 p)
        {
            float best = float.MaxValue;
            foreach (var s in PathSamples) { float dx = s.x - p.x, dz = s.z - p.z; float q = dx * dx + dz * dz; if (q < best) best = q; }
            return Mathf.Sqrt(best);
        }
        static Vector3[] pathSamples;

        /// <summary>경로 거리 d에서 길 위 지붕의 반폭 (없으면 0).</summary>
        public static float RoofHalfAt(float d)
        {
            foreach (var r in Roofs) if (d >= r.Start && d <= r.End) return r.Width * 0.5f;
            return 0f;
        }

        /// <summary>섹션 번호에 해당하는 구간.</summary>
        public static Section SectionOf(int beat)
        {
            foreach (var s in Sections) if (beat >= s.StartBeat && beat < s.EndBeat) return s;
            return Sections[Sections.Length - 1];
        }
    }
}
