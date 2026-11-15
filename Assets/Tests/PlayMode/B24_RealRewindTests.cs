using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>되감기를 실제로 거꾸로 (GDD-beta B10.3). 실제 시간을 주입해 되감기 도중 모습을 검사한다.</summary>
    public class B24_RealRewindTests
    {
        double now;
        float real;
        GameBootstrap boot;
        StageDirector dir;

        IEnumerator Load(double start)
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            boot = GameBootstrap.Instance;
            dir = boot.director;
            now = 0; real = 100f;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            dir.RewindRealTime = () => real;
            dir.FailHitSeconds = 0f;   // beta1-1: 피격 연출(GDD-beta B12.2)은 B30 테스트에서 따로 본다. 여기서는 바로 되감기
            dir.slowMotion.RealTime = () => real;
            boot.presentation.vfxPool.Rewind.DeltaTime = () => 0f;
            boot.StartRun(start);
            yield return null;
        }

        [TearDown]
        public void TearDown() { if (dir != null) dir.slowMotion.Cancel(); Time.timeScale = 1f; }

        [UnityTest]
        public IEnumerator B24_ChapterRewind_PlayerSlidesBackAlongPath()
        {
            yield return Load(70);                                 // 2구간(56~)
            for (int i = 0; i < 40; i++) { now += 0.01; yield return null; }
            double from = boot.player.RunBeat;
            dir.Health.Damage(); dir.Health.Damage(); dir.Health.Damage();
            yield return null;
            Assert.IsTrue(dir.Rewinding);
            Assert.AreEqual(RewindKind.Chapter, dir.LastRewind);
            real += 0.4f; yield return null;
            double mid = boot.player.VisualRunBeat;
            Assert.Less(mid, from - 1.0, "되감는 중: 보이는 위치가 뒤로");
            Assert.Greater(mid, 56.0 + 1.0, "아직 도착 전");
            var expected = MapLayout.PointAt((float)(mid * GameConfig.MetersPerBeat));
            Assert.Less(Vector3.Distance(expected, boot.player.transform.position), 0.05f, "실제 플레이어 위치도 경로를 따라 뒤로");
            real += 0.2f; yield return null;
            Assert.Less(boot.player.VisualRunBeat, mid, "계속 뒤로");
            real += 0.25f; yield return null; yield return null;
            Assert.IsFalse(dir.Rewinding);
            Assert.AreEqual(56.0, boot.player.RunBeat, 0.1, "끝에서 되감은 지점과 일치");
            Assert.AreEqual(boot.player.RunBeat, boot.player.VisualRunBeat, 1e-9, "보이는 위치 = 실제 위치");
        }

        [UnityTest]
        public IEnumerator B24_BossRewind_WeaponSwingReverses()
        {
            yield return Load(256);
            for (int i = 0; i < 3000 && dir.State != StageState.Combat; i++) { now += 0.02; yield return null; }
            var enc = dir.CurrentEncounter;
            var boss = enc.Actors[0];
            var c0 = enc.Cues[0];
            while (boot.clock.CurrentBeat < c0.TargetBeat - 0.05) { now += 0.01; yield return null; }
            var swingPose = boss.weaponPivot.localRotation;
            Assert.Greater(Quaternion.Angle(Quaternion.identity, swingPose), 20f, "휘두르는 자세");
            while (!dir.Rewinding && now < 300) { now += 0.01; yield return null; }    // 무입력 FAIL → 되감기
            Assert.IsTrue(dir.Rewinding);
            real += 0.05f; yield return null;
            float a1 = Quaternion.Angle(Quaternion.identity, boss.weaponPivot.localRotation);
            real += 0.35f; yield return null;
            float a2 = Quaternion.Angle(Quaternion.identity, boss.weaponPivot.localRotation);
            real += 0.35f; yield return null;
            float a3 = Quaternion.Angle(Quaternion.identity, boss.weaponPivot.localRotation);
            Assert.Greater(a1, 10f, "되감기 시작: 휘두른 자세에서");
            // GDD-beta B18.3: 적은 실제로 놓친(가장 이른) 공격을 휘두르므로, 다 휘두른 공격을 거꾸로 감으면
            // 내려친 자세 → 들어 올린 자세(크게) → 처음 자세로 돌아간다. 각도가 한 번 커졌다가 0으로 간다.
            Assert.Greater(Mathf.Abs(a2 - a1), 1f, "자세가 되감기며 바뀜");
            Assert.Less(a3, a2, "끝으로 갈수록 처음 자세로");
            Assert.Less(a3, 5f, "끝에서 처음 자세");
            Assert.AreEqual("Rewinding", boss.State);
        }

        [UnityTest]
        public IEnumerator B24_TapeOverlay_StreaksScanlinesIcon()
        {
            yield return Load(256);
            var ov = boot.presentation.vfxPool.Rewind;
            Assert.IsFalse(ov.Active);
            for (int i = 0; i < 3000 && dir.State != StageState.Combat; i++) { now += 0.02; yield return null; }
            while (!dir.Rewinding && now < 300) { now += 0.01; yield return null; }
            Assert.IsTrue(ov.Active, "되감기 겹화면");
            Assert.GreaterOrEqual(ov.Streaks.Count, 3, "먹 줄기 3개 이상");
            Assert.IsNotNull(ov.Scanlines, "가로 줄무늬");
            Assert.AreEqual(VfxFactory.Scanlines, ov.Scanlines.sprite);
            Assert.AreEqual("◀◀", ov.IconText.text, "◀◀ 표시");
            Assert.AreEqual(GameConfig.JudgeFxCanvasOrder, ov.Streaks[0].canvas.rootCanvas.sortingOrder, "판정 원 층보다 아래");
            var x0 = ov.Streaks.Select(s => s.rectTransform.anchoredPosition.x).ToArray();
            ov.Step(0.05f);
            var x1 = ov.Streaks.Select(s => s.rectTransform.anchoredPosition.x).ToArray();
            for (int i = 0; i < x0.Length; i++) Assert.Less(x1[i], x0[i], $"줄기 {i}: 오른쪽 → 왼쪽");
            float y0 = ov.Scanlines.rectTransform.anchoredPosition.y;
            ov.Step(0.013f);
            Assert.AreNotEqual(y0, ov.Scanlines.rectTransform.anchoredPosition.y, "줄무늬 일렁임");
            ov.Step(GameConfig.RewindSeconds);
            Assert.IsFalse(ov.Active, "0.8초 뒤 사라짐");
            Assert.IsFalse(ov.Streaks[0].gameObject.activeInHierarchy);
        }

        [UnityTest]
        public IEnumerator B24_Font_HasRewindIcon()
        {
            UiFactory.Font.RequestCharactersInTexture("◀", 96);
            Assert.IsTrue(UiFactory.Font.GetCharacterInfo('◀', out _, 96), "◀ 글자");
            yield return null;
        }
    }
}
