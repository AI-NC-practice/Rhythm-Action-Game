using System.Collections.Generic;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 새 사건·안 멈추는 적의 카메라 동작 (GDD-beta B2.2 결과 표, 제안서 0.1.1·2장 G). 회색 상자 단계의 표현.
    /// 사건 안 판정 i의 결과가 나오면, 판정 i 시점부터 다음 판정(또는 사건 끝)까지 결과별 동작 하나를 한다.
    /// 판정 전·결과 대기 중에는 0. 사건 끝(p=1)에서 오프셋은 0으로 돌아온다.
    /// pos: 진행 방향 기준 로컬(x 오른쪽, y 위, z 앞, m). euler: (pitch +아래, yaw, roll) 도.
    /// </summary>
    public static class EventMotion
    {
        /// <summary>판정 하나의 결과별 동작 세기.</summary>
        struct Move
        {
            public float X, Y, Z, Pitch, Roll, Shake, Wobble;
            public Move(float x = 0, float y = 0, float z = 0, float pitch = 0, float roll = 0, float shake = 0, float wobble = 0)
            { X = x; Y = y; Z = z; Pitch = pitch; Roll = roll; Shake = shake; Wobble = wobble; }
        }

        /// <summary>
        /// kind의 slot번째 판정 결과 r에 대한 동작. side: 판정 키 방향(← −1, → +1, 그 밖 0).
        /// 회피 계열은 키 쪽으로 몸을 뺀다.
        /// </summary>
        static Move MoveFor(string kind, int slot, Judgement r, float side)
        {
            bool P = r == Judgement.Perfect, N = r == Judgement.Normal;
            switch (kind)
            {
                case "tile_slip":
                    if (slot == 0) // ↓ 버티기: 처마 쪽으로 미끄러짐
                        return P ? new Move(y: -0.2f, pitch: 10f) : N ? new Move(y: -0.5f, pitch: 18f, wobble: 6f) : new Move(y: -0.9f, pitch: 25f, shake: 10f);
                    // ↑ 다시 올라가기
                    return P ? new Move(y: 1.0f, pitch: -8f, z: 0.3f) : N ? new Move(y: -0.6f, pitch: 25f, wobble: 5f) : new Move(y: -1.2f, z: -0.5f, pitch: 30f, shake: 12f);
                case "rope_swing": // ↑ 줄 잡고 포졸 머리 위로
                    return P ? new Move(y: 1.6f, z: 0.5f, pitch: 35f) : N ? new Move(y: 1.0f, pitch: 10f, wobble: 5f) : new Move(y: -0.6f, z: -1.2f, pitch: -15f, shake: 10f);
                case "rolling_jars": // 키 쪽으로 피하기
                    return P ? new Move(x: side * 0.8f, roll: -side * 8f) : N ? new Move(x: side * 0.5f, roll: side * 8f, wobble: 6f) : new Move(y: -0.4f, z: -0.6f, shake: 10f);
                case "awning_bounce": // ↑ 차양 반동
                    return P ? new Move(y: 2.0f, pitch: -12f) : N ? new Move(y: 1.2f, pitch: 15f, wobble: 4f) : new Move(y: -0.8f, pitch: -20f, shake: 10f);
                case "closing_gate": // ↓ 문틈 미끄러져 통과
                    return P ? new Move(y: -0.8f, pitch: 6f) : N ? new Move(y: -0.6f, roll: 8f, wobble: 6f) : new Move(y: -0.5f, z: -1.0f, shake: 12f);
                case "arrow_volley": // 키 쪽으로 피하기 (짧게)
                    return P ? new Move(x: side * 0.5f, roll: -side * 6f) : N ? new Move(x: side * 0.3f, wobble: 6f) : new Move(y: -0.3f, shake: 8f);
                case "long_jump":
                    if (slot == 0) // ↑ 도약
                        return P ? new Move(y: 1.4f, pitch: -5f) : N ? new Move(y: 0.9f, wobble: 4f) : new Move(y: -0.5f, z: -0.8f, shake: 10f);
                    // ↓ 착지 구르기
                    return P ? new Move(y: -0.3f, pitch: 30f) : N ? new Move(y: -0.15f, wobble: 4f) : new Move(y: -0.5f, pitch: -10f, shake: 10f);
                case "banner_slide": // ↓ 깃발 줄 타고 내려옴
                    return P ? new Move(y: -0.4f, pitch: 15f) : N ? new Move(y: -0.6f, wobble: 5f) : new Move(y: -0.9f, pitch: -10f, shake: 10f);
                case EventCatalog.Charge: // ↑ 엇갈리며 베기
                    return P ? new Move(y: 0.2f, pitch: -4f, roll: 6f) : N ? new Move(z: -0.4f, wobble: 6f) : new Move(y: -0.7f, z: -1.2f, shake: 12f);
                case EventCatalog.Ambush:
                    return P ? new Move(x: side * 0.7f, roll: -side * 8f) : N ? new Move(x: side * 0.4f, wobble: 6f) : new Move(z: -0.6f, shake: 10f);
                case EventCatalog.Chase:
                    return P ? new Move(x: side * 0.5f, roll: -side * 6f) : N ? new Move(x: side * 0.3f, wobble: 5f) : new Move(z: -0.4f, shake: 8f);
                default:
                    return new Move();
            }
        }

        /// <summary>
        /// 사건 진행도 p(0~1)에서의 카메라 오프셋.
        /// slotOffsets: 판정 박자(사건 시작 기준), keys: 판정별 키, results: 판정별 결과(아직이면 null), beats: 사건 길이.
        /// </summary>
        public static void Offset(string kind, IList<double> slotOffsets, IList<ArrowKey> keys, IList<Judgement?> results, int beats, float p,
            out Vector3 pos, out Vector3 euler)
        {
            pos = Vector3.zero;
            euler = Vector3.zero;
            p = Mathf.Clamp01(p);
            for (int i = 0; i < slotOffsets.Count; i++)
            {
                if (!results[i].HasValue) continue;
                float a = (float)(slotOffsets[i] / beats);
                float b = i + 1 < slotOffsets.Count ? (float)(slotOffsets[i + 1] / beats) : 1f;
                if (p < a || p > b || b - a < 1e-5f) continue;
                float u = (p - a) / (b - a);
                float bell = Mathf.Sin(u * Mathf.PI);                  // 0 → 1 → 0
                float side = keys[i] == ArrowKey.Left ? -1f : keys[i] == ArrowKey.Right ? 1f : 0f;
                var m = MoveFor(kind, i, results[i].Value, side);
                float wob = Mathf.Sin(u * Mathf.PI * 6f) * bell;
                float shk = Mathf.Sin(u * Mathf.PI * 18f) * (1f - u);
                pos += new Vector3(m.X, m.Y, m.Z) * bell;
                euler += new Vector3(m.Pitch * bell, 0f, m.Roll * bell + m.Wobble * wob + m.Shake * shk);
            }
        }

        /// <summary>판정 결과가 모두 같을 때의 오프셋 (테스트·미리보기용).</summary>
        public static void OffsetUniform(string kind, Judgement r, float p, out Vector3 pos, out Vector3 euler)
        {
            var def = EventCatalog.Get(kind);
            var offs = new List<double>();
            var keys = new List<ArrowKey>();
            var res = new List<Judgement?>();
            for (int i = 0; i < def.Slots.Length; i++)
            {
                offs.Add(def.Slots[i].Offset);
                keys.Add(def.Slots[i].Key ?? (i % 2 == 0 ? ArrowKey.Left : ArrowKey.Right));
                res.Add(r);
            }
            Offset(kind, offs, keys, res, def.Beats, p, out pos, out euler);
        }
    }
}
