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

        /// <summary>지붕 덩어리 (기와집·궁궐 담·성문 윗면). 경로 거리 [Start, End], 윗면 높이.</summary>
        public struct Roof
        {
            public string AssetId;
            public float Start, End, Height;
            public Roof(string id, float s, float e, float h) { AssetId = id; Start = s; End = e; Height = h; }
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

        /// <summary>지붕 덩어리. 지붕 사이 틈은 4 m(GDD 3~5 m), 높이 4.5~6 m(GDD 4~6 m).</summary>
        public static readonly Roof[] Roofs =
        {
            new Roof("house_giwa", 255f, 283f, 5f),
            new Roof("house_giwa", 287f, 303f, 4.5f),
            new Roof("house_giwa", 307f, 363f, 5.5f),
            new Roof("house_giwa", 367f, 415f, 5f),
            new Roof("wall_stone", 565f, 625f, 5f),
            new Roof("gate", 625f, 665f, 6f),
        };

        public static float TotalLength
        {
            get { float t = 0; foreach (var s in Route) t += s.Length; return t; }
        }

        /// <summary>경로 거리 d의 수평 위치(y=0)와 진행 방향.</summary>
        public static Vector3 FlatPointAt(float d, out Vector3 forward)
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

        /// <summary>섹션 번호에 해당하는 구간.</summary>
        public static Section SectionOf(int beat)
        {
            foreach (var s in Sections) if (beat >= s.StartBeat && beat < s.EndBeat) return s;
            return Sections[Sections.Length - 1];
        }
    }
}
