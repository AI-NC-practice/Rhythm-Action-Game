using NUnit.Framework;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    /// <summary>판정 UI 가독성 (GDD-beta B1). 기대값은 docs/GDD-beta.md가 기준이다.</summary>
    public class B01_RingLayoutTests
    {
        const float Eps = 1e-4f;
        static readonly Vector2 Hd = new Vector2(1920, 1080);

        [Test]
        public void B01_Config_MatchGddBetaB1()
        {
            Assert.AreEqual(20, GameConfig.RingCanvasOrder);
            Assert.AreEqual(10, GameConfig.JudgeFxCanvasOrder);
            Assert.AreEqual(5, GameConfig.HudCanvasOrder);
            Assert.Greater(GameConfig.MenuCanvasOrder, GameConfig.RingCanvasOrder, "메뉴는 판정 원보다 위");
            Assert.AreEqual(-110f, GameConfig.JudgeLabelOffsetY, Eps);
            Assert.AreEqual(44, GameConfig.JudgeLabelFontSize);
            Assert.AreEqual(0.1f, GameConfig.JudgeLabelPopSeconds, Eps);
            Assert.AreEqual(1.3f, GameConfig.JudgeLabelPopScale, Eps);
            Assert.AreEqual(0.35f, GameConfig.JudgeLabelRiseSeconds, Eps);
            Assert.AreEqual(20f, GameConfig.JudgeLabelRisePx, Eps);
            Assert.AreEqual(0.45f, GameConfig.JudgeLabelTotalSeconds, Eps);
            Assert.AreEqual(200f, GameConfig.JudgeLabelDimRadiusPx, Eps);
            Assert.AreEqual(0.3f, GameConfig.JudgeLabelDimAlpha, Eps);
            Assert.AreEqual(0.22f, GameConfig.RingSafeHalfWidthRatio, Eps);
            Assert.AreEqual(0.18f, GameConfig.RingSafeHalfHeightRatio, Eps);
            Assert.AreEqual(0.1f, GameConfig.RingLockSeconds, Eps);
            Assert.AreEqual(40f, GameConfig.RingChevronSizePx, Eps);
            Assert.AreEqual(160f, GameConfig.RingChainSpacingPx, Eps);
            Assert.AreEqual(1f, GameConfig.RingChainMaxGapBeats, Eps);
            Assert.AreEqual(0.5f, GameConfig.RingGlowAlpha, Eps);
            Assert.AreEqual(-0.4f, GameConfig.JudgeExposure, Eps);
            Assert.AreEqual(0.3f, GameConfig.JudgeVignette, Eps);
        }

        [Test]
        public void B01_SafeEllipse_RadiiAndClamp()
        {
            var r = RingLayout.SafeRadii(Hd);
            Assert.AreEqual(422.4f, r.x, 0.01f);
            Assert.AreEqual(194.4f, r.y, 0.01f);

            var inside = RingLayout.Clamp(new Vector2(100, 50), r, out bool c1);
            Assert.IsFalse(c1);
            Assert.AreEqual(new Vector2(100, 50), inside);

            var right = RingLayout.Clamp(new Vector2(900, 0), r, out bool c2);
            Assert.IsTrue(c2);
            Assert.AreEqual(422.4f, right.x, 0.01f);
            Assert.AreEqual(0f, right.y, 0.01f);

            var diag = RingLayout.Clamp(new Vector2(800, 400), r, out bool c3);
            Assert.IsTrue(c3);
            Assert.AreEqual(1f, (diag.x / r.x) * (diag.x / r.x) + (diag.y / r.y) * (diag.y / r.y), 1e-3f, "타원 경계 위");
            Assert.AreEqual(800f / 400f, diag.x / diag.y, 1e-3f, "가운데→점 방향 유지");

            var behind = RingLayout.OnBoundary(new Vector2(-3, 0), r);
            Assert.AreEqual(-422.4f, behind.x, 0.01f);
            var none = RingLayout.OnBoundary(Vector2.zero, r);
            Assert.AreEqual(-194.4f, none.y, 0.01f, "방향이 없으면 아래쪽");
        }

        [Test]
        public void B01_Chain_160pxApart_FirstLeft_InsideEllipse()
        {
            var r = RingLayout.SafeRadii(Hd);
            var a = new Vector2(0, -50);
            var p0 = RingLayout.ChainSlot(a, 0, 4, r);
            var p3 = RingLayout.ChainSlot(a, 3, 4, r);
            for (int i = 1; i < 4; i++)
                Assert.AreEqual(160f, RingLayout.ChainSlot(a, i, 4, r).x - RingLayout.ChainSlot(a, i - 1, 4, r).x, Eps);
            Assert.Less(p0.x, p3.x, "먼저 올 원이 왼쪽");
            Assert.AreEqual(-50f, p3.y, Eps);
            Assert.AreEqual(0f, (p0.x + p3.x) * 0.5f, Eps, "묶음 가운데가 기준점");

            // 기준점이 오른쪽 끝이면 묶음 전체가 타원 가로 범위 안으로 들어옴
            var edge = new Vector2(r.x, 0);
            Assert.LessOrEqual(RingLayout.ChainSlot(edge, 3, 4, r).x, r.x + Eps);
            Assert.AreEqual(edge, RingLayout.ChainSlot(edge, 0, 1, r), "한 개면 그대로");
        }

        [Test]
        public void B01_LabelPose_PopThenRise_045s()
        {
            Assert.AreEqual(new Vector2(30, -160), RingLayout.LabelPosition(new Vector2(30, -50)));

            Assert.IsTrue(RingLayout.LabelPose(0f, out float s, out float rise, out float a));
            Assert.AreEqual(1.3f, s, Eps); Assert.AreEqual(0f, rise, Eps); Assert.AreEqual(1f, a, Eps);
            RingLayout.LabelPose(0.05f, out s, out rise, out a);
            Assert.AreEqual(1.15f, s, Eps);
            RingLayout.LabelPose(0.1f, out s, out rise, out a);
            Assert.AreEqual(1f, s, Eps); Assert.AreEqual(0f, rise, Eps); Assert.AreEqual(1f, a, Eps);
            RingLayout.LabelPose(0.275f, out s, out rise, out a);
            Assert.AreEqual(10f, rise, 1e-3f); Assert.AreEqual(0.5f, a, 1e-3f);
            Assert.IsTrue(RingLayout.LabelPose(0.449f, out _, out _, out _));
            Assert.IsFalse(RingLayout.LabelPose(0.45f, out _, out rise, out a), "0.45초에 끝");
            Assert.AreEqual(20f, rise, Eps);
            Assert.AreEqual(0f, a, Eps);
        }
    }
}
