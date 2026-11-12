using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>판정 UI 가독성 (GDD-beta B1). 박자·실제 시간·캔버스 크기를 주입해 결정적으로 검사한다.</summary>
    public class B01_JudgeUiTests
    {
        double now;
        float realTime;
        GameObject root;
        BeatClock clock;
        JudgeSystem judge;
        JudgeRingView view;
        JudgeFx fx;
        Camera cam;
        static readonly Vector2 Hd = new Vector2(1920, 1080);

        [SetUp]
        public void SetUp()
        {
            now = 0; realTime = 0;
            root = new GameObject("TestRoot");
            clock = root.AddComponent<BeatClock>();
            clock.TimeSource = () => now;
            judge = root.AddComponent<JudgeSystem>();
            judge.clock = clock;
            judge.readKeyboard = false;
            judge.OffsetSource = () => 0f;

            var rig = new GameObject("CameraRig");
            rig.transform.SetParent(root.transform);
            var camGo = new GameObject("Camera", typeof(Camera));
            camGo.transform.SetParent(rig.transform, false);
            cam = camGo.GetComponent<Camera>();
            cam.fieldOfView = 70f;

            var canvas = UiFactory.CreateCanvas("JudgeCanvas", GameConfig.RingCanvasOrder);
            canvas.transform.SetParent(root.transform);
            view = canvas.gameObject.AddComponent<JudgeRingView>();
            view.RealTime = () => realTime;
            view.CanvasSize = () => Hd;
            view.Bind(judge, clock, cam);

            fx = root.AddComponent<JudgeFx>();
            fx.judge = judge;
            fx.cam = cam;
            fx.DeltaTime = () => 0f;
            fx.EnsureSetup();
            clock.StartClock();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(root);
            if (view != null && view.labelLayer != null) Object.Destroy(view.labelLayer.gameObject);
        }

        static Vector2 Radii => RingLayout.SafeRadii(Hd);

        static float EllipseNorm(Vector2 p)
        {
            var r = Radii;
            return Mathf.Sqrt((p.x / r.x) * (p.x / r.x) + (p.y / r.y) * (p.y / r.y));
        }

        void Appear(double t) { now = t; judge.Tick(); view.Refresh(); }

        [UnityTest]
        public IEnumerator B01_GameScene_CanvasOrder_RingOnTop()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var boot = GameBootstrap.Instance;
            var ringCanvas = boot.ringView.GetComponent<Canvas>();
            Assert.AreEqual(20, ringCanvas.sortingOrder, "판정 원 Canvas 20");
            Assert.IsNotNull(boot.ringView.labelLayer);
            Assert.AreEqual(10, boot.ringView.labelLayer.GetComponent<Canvas>().sortingOrder, "판정 문구 Canvas 10");
            Assert.AreEqual(5, boot.hud.GetComponentInParent<Canvas>().sortingOrder, "HUD 5");
            Assert.Greater(boot.pause.GetComponentInParent<Canvas>().sortingOrder, 20, "메뉴는 원보다 위");
        }

        [UnityTest]
        public IEnumerator B01_Label_110Below_44pt_PopRise_045s_OnLabelLayer()
        {
            judge.Schedule(4, ArrowKey.Up, cam.transform.position + cam.transform.forward * 10f);   // 화면 가운데
            Appear(1.0);
            var ringPos = view.Rings[0].Root.anchoredPosition;
            realTime = 3f;
            judge.Submit(ArrowKey.Up, 2.0);
            Assert.AreEqual(1, view.Labels.Count);
            var label = view.Labels[0];
            Assert.AreEqual(view.labelLayer, label.Text.transform.parent, "문구는 원과 다른 층(Canvas 10)");
            Assert.AreEqual(44, label.Text.fontSize);
            Assert.AreEqual(ringPos.y - 110f, label.Text.rectTransform.anchoredPosition.y, 0.01f, "원 아래 110 px");
            Assert.AreEqual(ringPos.x, label.Text.rectTransform.anchoredPosition.x, 0.01f);
            Assert.AreEqual(1.3f, label.Text.rectTransform.localScale.x, 1e-3f, "등장 배율 1.3");

            realTime = 3.1f; view.Refresh();
            Assert.AreEqual(1f, label.Text.rectTransform.localScale.x, 1e-3f, "0.1초에 1.0");
            realTime = 3.275f; view.Refresh();
            Assert.AreEqual(ringPos.y - 110f + 10f, label.Text.rectTransform.anchoredPosition.y, 0.05f, "떠오르는 중");
            Assert.AreEqual(0.5f, label.Text.color.a, 0.01f);
            realTime = 3.44f; view.Refresh();
            Assert.AreEqual(1, view.Labels.Count);
            realTime = 3.46f; view.Refresh();
            Assert.AreEqual(0, view.Labels.Count, "0.45초 뒤 제거");
            yield return null;
        }

        [UnityTest]
        public IEnumerator B01_Label_DimmedWhenNewRingNearby()
        {
            var center = cam.transform.position + cam.transform.forward * 10f;
            judge.Schedule(4, ArrowKey.Up, center);
            judge.Schedule(7, ArrowKey.Down, center);      // 1박 이상 간격 → 같은 자리 근처에 뜸
            Appear(1.0);
            realTime = 3f;
            judge.Submit(ArrowKey.Up, 2.0);
            var label = view.Labels[0];
            realTime = 3.05f; view.Refresh();
            Assert.AreEqual(1f, label.Text.color.a, 1e-3f, "근처에 원이 없으면 그대로");
            Appear(2.5);                                   // 7박 원이 2박 전(5박)에 등장
            Assert.AreEqual(1, view.Rings.Count);
            Assert.LessOrEqual(Vector2.Distance(view.Rings[0].Root.anchoredPosition, label.Text.rectTransform.anchoredPosition), 200f);
            Assert.LessOrEqual(label.Text.color.a, 0.3f + 1e-3f, "새 원이 200 px 안 → 알파 0.3 이하");
            yield return null;
        }

        [UnityTest]
        public IEnumerator B01_Ring_ClampedToSafeEllipse_WithChevron()
        {
            var t = cam.transform;
            judge.Schedule(4, ArrowKey.Right, t.position + t.forward * 10f + t.right * 40f);   // 화면 오른쪽 밖
            judge.Schedule(10, ArrowKey.Up, t.position + t.forward * 10f + t.right * 1f);     // 화면 안쪽
            judge.Schedule(16, ArrowKey.Left, t.position - t.forward * 10f - t.right * 3f);   // 카메라 뒤 왼쪽
            Appear(1.0);
            var far = view.Rings[0];
            Assert.IsTrue(far.Clamped);
            Assert.AreEqual(1f, EllipseNorm(far.Root.anchoredPosition), 1e-3f, "타원 경계로 끌어옴");
            Assert.Greater(far.Root.anchoredPosition.x, 0f);
            Assert.IsTrue(far.Chevron.enabled, "방향 꺾쇠 표시");
            Assert.Greater(far.Chevron.rectTransform.anchoredPosition.x, 0f, "꺾쇠가 바깥쪽(오른쪽)");
            Assert.AreEqual(40f, far.Chevron.rectTransform.sizeDelta.x, 1e-3f);

            judge.Submit(ArrowKey.Right, 2.0);
            Appear(4.0);
            var near = view.Rings[0];
            Assert.IsFalse(near.Clamped);
            Assert.IsFalse(near.Chevron.enabled);
            Assert.Less(EllipseNorm(near.Root.anchoredPosition), 1f);

            judge.Submit(ArrowKey.Up, 5.0);
            Appear(7.0);
            var back = view.Rings[0];
            Assert.IsTrue(back.Clamped, "카메라 뒤도 끌어옴");
            Assert.AreEqual(1f, EllipseNorm(back.Root.anchoredPosition), 1e-3f);
            Assert.Less(back.Root.anchoredPosition.x, 0f, "뒤 왼쪽 → 화면 왼쪽 경계");
            Assert.IsTrue(back.Chevron.enabled);
            yield return null;
        }

        [UnityTest]
        public IEnumerator B01_Ring_LockedAfter0_1s()
        {
            var t = cam.transform;
            judge.Schedule(4, ArrowKey.Up, t.position + t.forward * 10f + t.right * 2f);
            realTime = 0f;
            Appear(1.0);
            var ring = view.Rings[0];
            var p0 = ring.Root.anchoredPosition;
            cam.transform.localRotation = Quaternion.Euler(0, 5, 0);
            realTime = 0.05f; view.Refresh();
            var p1 = ring.Root.anchoredPosition;
            Assert.AreNotEqual(p0.x, p1.x, "0.1초 전에는 따라감");
            realTime = 0.11f; view.Refresh();
            var p2 = ring.Root.anchoredPosition;
            cam.transform.localRotation = Quaternion.Euler(0, -10, 0);
            realTime = 0.5f; now = 1.5; view.Refresh();
            Assert.AreEqual(p2, ring.Root.anchoredPosition, "0.1초 뒤 고정");
            Assert.AreEqual(240f, ring.Outer.rectTransform.sizeDelta.x, 0.5f, "고정돼도 바깥 원은 계속 줄어듦");
            yield return null;
        }

        [UnityTest]
        public IEnumerator B01_Chain_SubBeatRings_SideBySide160px()
        {
            var center = cam.transform.position + cam.transform.forward * 10f;
            // 반 박 간격 4개 (윈드밀·화살 세례 모양) + 1박 간격 1개(묶음 아님)
            judge.Schedule(8.0, ArrowKey.Left, center);
            judge.Schedule(8.5, ArrowKey.Up, center);
            judge.Schedule(9.0, ArrowKey.Right, center);
            judge.Schedule(9.5, ArrowKey.Down, center);
            judge.Schedule(10.5, ArrowKey.Up, center);
            Appear(3.0); realTime = 0.2f; view.Refresh();   // 6박: 첫 원 등장, 고정
            Appear(3.25); Appear(3.5); Appear(3.75);        // 6.5, 7, 7.5박: 나머지 등장
            Assert.AreEqual(4, view.Rings.Count);
            for (int i = 1; i < 4; i++)
            {
                var a = view.Rings[i - 1].Root.anchoredPosition;
                var b = view.Rings[i].Root.anchoredPosition;
                Assert.AreEqual(160f, b.x - a.x, 0.01f, "가로 160 px, 먼저 올 원이 왼쪽");
                Assert.AreEqual(a.y, b.y, 0.01f);
            }
            foreach (var r in view.Rings) Assert.LessOrEqual(Mathf.Abs(r.Root.anchoredPosition.x), Radii.x + 0.01f);
            Assert.AreEqual(0f, (view.Rings[0].Root.anchoredPosition.x + view.Rings[3].Root.anchoredPosition.x) * 0.5f, 0.01f, "묶음 가운데가 원래 자리");

            // 9.5 → 10.5는 1박 간격이라 묶음이 아님: 단독 자리(가운데)
            Appear(4.25);                                   // 8.5박: 10.5박 원 등장
            var lone = view.Rings[view.Rings.Count - 1];
            Assert.AreEqual(10.5, lone.Cue.TargetBeat, 1e-9);
            Assert.IsNull(lone.Chain);
            Assert.AreEqual(0f, lone.Root.anchoredPosition.x, 0.01f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator B01_Ring_Glow_FinisherGold()
        {
            judge.Schedule(4, ArrowKey.Up, null, new CombatStep { Kind = CombatStepKind.Attack, Attack = "upward" });
            judge.Schedule(8, ArrowKey.Up, null, new CombatStep { Kind = CombatStepKind.Finisher, Attack = "finisher" });
            Appear(1.0);
            var normal = view.Rings[0];
            Assert.IsNotNull(normal.Glow);
            Assert.AreEqual(JudgeRingView.GlowColor, normal.Glow.color);
            Assert.AreEqual(0.5f, normal.Glow.color.a, 1e-3f);
            Assert.Greater(normal.Glow.rectTransform.sizeDelta.x, normal.Target.rectTransform.sizeDelta.x, "원 바깥 빛");
            judge.Submit(ArrowKey.Up, 2.0);
            Appear(3.0);
            Assert.AreEqual(JudgeRingView.FinisherGlowColor, view.Rings[0].Glow.color, "마무리 원은 금색");
            yield return null;
        }

        [UnityTest]
        public IEnumerator B01_JudgeScreen_ExposureAndVignette()
        {
            Assert.AreEqual(0f, fx.Exposure, 1e-4f);
            Assert.AreEqual(0f, fx.VignetteIntensity, 1e-4f);
            judge.Schedule(4, ArrowKey.Up);
            now = 1.0; judge.Tick();
            fx.Step(0.15f);
            Assert.AreEqual(-0.4f, fx.Exposure, 1e-4f, "노출 −0.4");
            Assert.AreEqual(0.3f, fx.VignetteIntensity, 1e-4f, "비네트 0.3");
            Assert.AreEqual(-60f, fx.Saturation, 1e-3f, "채도 −60 유지");
            Assert.AreEqual(58f, fx.Fov, 1e-3f, "FOV 58 유지");
            judge.Submit(ArrowKey.Up, 2.0); judge.Tick();
            fx.Step(0.2f);
            Assert.AreEqual(0f, fx.Exposure, 1e-4f);
            Assert.AreEqual(0f, fx.VignetteIntensity, 1e-4f);
            yield return null;
        }
    }
}
