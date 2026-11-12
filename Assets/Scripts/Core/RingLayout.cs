using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 판정 원 화면 배치 계산 (GDD-beta B1.3). 순수 함수라 EditMode에서 검사한다.
    /// 좌표는 캔버스 로컬(가운데 0,0, 1080p 기준 px).
    /// </summary>
    public static class RingLayout
    {
        /// <summary>안전 타원 반지름 (가로 = 폭 × 22%, 세로 = 높이 × 18%).</summary>
        public static Vector2 SafeRadii(Vector2 canvasSize) =>
            new Vector2(canvasSize.x * GameConfig.RingSafeHalfWidthRatio, canvasSize.y * GameConfig.RingSafeHalfHeightRatio);

        /// <summary>점이 타원 안(경계 포함)인지.</summary>
        public static bool Inside(Vector2 p, Vector2 radii) => Norm(p, radii) <= 1f + 1e-5f;

        static float Norm(Vector2 p, Vector2 radii)
        {
            float x = p.x / radii.x, y = p.y / radii.y;
            return Mathf.Sqrt(x * x + y * y);
        }

        /// <summary>타원 밖이면 가운데→점 방향으로 타원 경계에 끌어온다.</summary>
        public static Vector2 Clamp(Vector2 p, Vector2 radii, out bool clamped)
        {
            float n = Norm(p, radii);
            clamped = n > 1f + 1e-5f;
            return clamped ? p / n : p;
        }

        /// <summary>방향만 있을 때(카메라 뒤) 타원 경계 위의 점. 방향이 0이면 아래쪽.</summary>
        public static Vector2 OnBoundary(Vector2 dir, Vector2 radii)
        {
            if (dir.sqrMagnitude < 1e-8f) dir = Vector2.down;
            return dir / Norm(dir, radii);
        }

        /// <summary>
        /// 연속 원 묶음(count개)의 index번째 자리. 묶음 가운데를 anchor에 두고 가로 160 px 간격, 먼저 올 원이 왼쪽.
        /// 묶음 전체가 타원 가로 범위를 넘지 않게 가운데를 옮긴다.
        /// </summary>
        public static Vector2 ChainSlot(Vector2 anchor, int index, int count, Vector2 radii)
        {
            if (count <= 1) return anchor;
            float half = (count - 1) * 0.5f * GameConfig.RingChainSpacingPx;
            float cx = half >= radii.x ? 0f : Mathf.Clamp(anchor.x, -radii.x + half, radii.x - half);
            return new Vector2(cx - half + index * GameConfig.RingChainSpacingPx, anchor.y);
        }

        /// <summary>판정 문구의 기본 위치 (원 아래 110 px).</summary>
        public static Vector2 LabelPosition(Vector2 ringPos) => ringPos + new Vector2(0f, GameConfig.JudgeLabelOffsetY);

        /// <summary>문구 나이(초)에 따른 배율·세로 오프셋·알파. 끝나면 false.</summary>
        public static bool LabelPose(float age, out float scale, out float rise, out float alpha)
        {
            scale = 1f; rise = 0f; alpha = 1f;
            if (age < 0f) age = 0f;
            if (age >= GameConfig.JudgeLabelTotalSeconds) { alpha = 0f; rise = GameConfig.JudgeLabelRisePx; return false; }
            if (age < GameConfig.JudgeLabelPopSeconds)
            {
                scale = Mathf.Lerp(GameConfig.JudgeLabelPopScale, 1f, age / GameConfig.JudgeLabelPopSeconds);
                return true;
            }
            float u = (age - GameConfig.JudgeLabelPopSeconds) / GameConfig.JudgeLabelRiseSeconds;
            rise = GameConfig.JudgeLabelRisePx * u;
            alpha = 1f - u;
            return true;
        }
    }
}
