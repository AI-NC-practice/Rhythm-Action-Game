using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>판정 원 UI와 회색 화면·줌 연출 (GDD 4.2). 박자와 실제 시간은 주입해서 결정적으로 검사한다.</summary>
    public class T03_JudgePresentationTests
    {
        double now;
        float realTime;
        GameObject root;
        BeatClock clock;
        JudgeSystem judge;
        JudgeRingView view;
        JudgeFx fx;
        Camera cam;

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

            var canvas = UiFactory.CreateCanvas("JudgeCanvas", GameConfig.RingCanvasOrder);
            canvas.transform.SetParent(root.transform);
            view = canvas.gameObject.AddComponent<JudgeRingView>();
            view.RealTime = () => realTime;
            view.displayLatencyMs = 0f;   // 원 크기 공식(GDD 4.2) 검사: 지연 보정(B8.1)은 B12 테스트가 따로 본다
            view.Bind(judge, clock, cam);

            fx = root.AddComponent<JudgeFx>();
            fx.judge = judge;
            fx.cam = cam;
            fx.DeltaTime = () => 0f;   // 프레임이 흘러도 연출은 Step으로만 진행
            fx.EnsureSetup();

            clock.StartClock();
        }

        [TearDown]
        public void TearDown() => Object.Destroy(root);

        [UnityTest]
        public IEnumerator T03_Ring_AppearsTwoBeatsBefore_AndShrinks()
        {
            judge.Schedule(4, ArrowKey.Up);
            now = 0.99; judge.Tick(); view.Refresh();
            Assert.AreEqual(0, view.Rings.Count, "2박자 전보다 일찍 나타나면 안 됨");

            now = 1.0; judge.Tick(); view.Refresh();
            Assert.AreEqual(1, view.Rings.Count, "목표 2박자 전 등장");
            var ring = view.Rings[0];
            Assert.AreEqual(360f, ring.Outer.rectTransform.sizeDelta.x, 0.5f);
            Assert.AreEqual(120f, ring.Target.rectTransform.sizeDelta.x, 0.01f);
            Assert.AreEqual("↑", ring.Arrow.text);

            now = 1.5; view.Refresh();   // 3박: 1박 남음
            Assert.AreEqual(240f, ring.Outer.rectTransform.sizeDelta.x, 0.5f);
            now = 1.75; view.Refresh();  // 3.5박
            Assert.AreEqual(180f, ring.Outer.rectTransform.sizeDelta.x, 0.5f);
            now = 2.0; view.Refresh();   // 목표
            Assert.AreEqual(120f, ring.Outer.rectTransform.sizeDelta.x, 0.5f, "목표 시점에 두 원이 겹침");
            yield return null;
        }

        [UnityTest]
        public IEnumerator T03_Label_ShownFor0_45Seconds_GddBeta()
        {
            // beta1-1: GDD-beta B1.2가 GDD 4.2의 0.6초를 0.45초로 바꿨다.
            judge.Schedule(4, ArrowKey.Right);
            now = 1.0; judge.Tick();
            now = 2.03; realTime = 5f;
            judge.Submit(ArrowKey.Right, now);         // +30 ms → PERFECT
            Assert.AreEqual(0, view.Rings.Count, "판정되면 원이 사라짐");
            Assert.AreEqual(1, view.Labels.Count);
            Assert.AreEqual("PERFECT", view.Labels[0].Text.text);

            realTime = 5.44f; view.Refresh();
            Assert.AreEqual(1, view.Labels.Count, "0.45초 전에는 보임");
            realTime = 5.46f; view.Refresh();
            Assert.AreEqual(0, view.Labels.Count, "0.45초 뒤 사라짐");
            yield return null;
        }

        [UnityTest]
        public IEnumerator T03_Labels_NormalAndFail()
        {
            judge.Schedule(4, ArrowKey.Up);
            judge.Schedule(8, ArrowKey.Down);
            now = 1.0; judge.Tick();
            judge.Submit(ArrowKey.Up, 2.1);            // +100 ms → NORMAL
            Assert.AreEqual("NORMAL", view.Labels[view.Labels.Count - 1].Text.text);
            now = 3.0; judge.Tick();
            now = 4.25; judge.Tick();                  // 무입력 +250 ms → FAIL (beta1-1: NORMAL 창 240 ms, GDD-beta B12.1)
            Assert.AreEqual("FAIL", view.Labels[view.Labels.Count - 1].Text.text);
            yield return null;
        }

        [UnityTest]
        public IEnumerator T03_Font_HasJudgeGlyphs()
        {
            const string s = "PERFECTNORMALFAIL↑↓←→";
            var font = UiFactory.Font;
            font.RequestCharactersInTexture(s, 56);
            foreach (var ch in s)
                Assert.IsTrue(font.GetCharacterInfo(ch, out _, 56), $"글자 없음: {ch}");
            yield return null;
        }

        [UnityTest]
        public IEnumerator T03_Fx_GrayAndZoom_EnterAndExit()
        {
            Assert.AreEqual(0f, fx.Saturation, 1e-3f);
            Assert.AreEqual(70f, fx.Fov, 1e-3f);

            judge.Schedule(4, ArrowKey.Up, root.transform.position + new Vector3(10f, 0f, 10f)); // 45° 오른쪽
            now = 1.0; judge.Tick();
            fx.Step(0.075f);                                 // 들어가는 중 절반
            Assert.AreEqual(-30f, fx.Saturation, 0.5f);
            Assert.AreEqual(64f, fx.Fov, 0.1f);
            fx.Step(0.075f);                                 // 0.15초에 완료
            Assert.AreEqual(-60f, fx.Saturation, 1e-3f);
            Assert.AreEqual(58f, fx.Fov, 1e-3f);
            Assert.AreEqual(25f, Quaternion.Angle(Quaternion.identity, cam.transform.localRotation), 0.1f, "최대 25° 회전");

            judge.Submit(ArrowKey.Up, 2.0); judge.Tick();
            fx.Step(0.1f);                                   // 나오는 중 절반 (0.2초 중 0.1초)
            Assert.AreEqual(-30f, fx.Saturation, 0.5f);
            Assert.AreEqual(64f, fx.Fov, 0.1f);
            fx.Step(0.1f);
            Assert.AreEqual(0f, fx.Saturation, 1e-3f, "0.2초 뒤 채도 복귀");
            Assert.AreEqual(70f, fx.Fov, 1e-3f, "0.2초 뒤 FOV 복귀");
            Assert.AreEqual(0f, Quaternion.Angle(Quaternion.identity, cam.transform.localRotation), 0.1f);
            yield return null;
        }
    }
}
