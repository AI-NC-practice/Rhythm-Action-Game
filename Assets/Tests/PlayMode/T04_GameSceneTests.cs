using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    public class T04_GameSceneTests
    {
        [UnityTest]
        public IEnumerator T04_GameScene_HasSectionsLightAndPlaceholders()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            for (int n = 1; n <= 6; n++) Assert.IsNotNull(GameObject.Find($"Section_{n}"), $"Section_{n}");
            var lights = Object.FindObjectsByType<Light>();
            Assert.IsTrue(lights.Any(l => l.type == LightType.Directional), "방향광");
            var all = Object.FindObjectsByType<Transform>();
            // beta1-1: GDD-beta B3 차트 — 기존 장애물은 Obstacle_*, 새 사건·안 멈추는 적·복선은 EventProp_*
            var chart = StageChart.Load();
            Assert.AreEqual(chart.Obstacles.Count(e => !EventProps.Needs(e)), all.Count(t => t.name.StartsWith("Obstacle_")), "기존 장애물 소품");
            Assert.AreEqual(chart.events.Count(EventProps.Needs), Object.FindObjectsByType<EventProp>(FindObjectsInactive.Include).Length, "새 사건 소품을 SceneBuilder가 만듦 (복선은 숨김 상태 포함)");
            Assert.AreEqual(4, all.Count(t => System.Text.RegularExpressions.Regex.IsMatch(t.name, @"^Enemy_\d\d_")), "일반 적 4명");
            Assert.AreEqual(5, all.Count(t => t.name.StartsWith("Enemy_Surround_")), "포위 5명");
            Assert.IsNotNull(GameObject.Find("Enemy_Boss"));
            Assert.Greater(all.Count(t => t.name.StartsWith("Prop_house_giwa")), 0, "기와지붕 회색 상자");
            Assert.Greater(all.Count(t => t.name.StartsWith("Prop_gate_")), 0, "성문 회색 상자");
            Assert.Greater(all.Count(t => t.name.StartsWith("Prop_wall_stone")), 0, "돌담 회색 상자");
            Assert.IsNotNull(GameObject.Find("Ground"), "지면");
        }

        [UnityTest]
        public IEnumerator T04_Player_EyeHeight_AndMoves10mIn4Beats()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            var boot = GameBootstrap.Instance;
            Assert.IsNotNull(boot);
            var cam = boot.playerCamera;
            Assert.AreEqual(GameConfig.EyeHeight, cam.transform.position.y - boot.player.transform.position.y, 1e-3f, "눈높이 1.65 m");

            double now = 0;
            boot.clock.TimeSource = () => now;
            boot.StartRun();
            yield return null;
            var start = boot.player.transform.position;
            Assert.AreEqual(0f, Vector3.Distance(start, MapLayout.PointAt(0)), 0.01f);

            now = 4 * GameConfig.SecondsPerBeat;   // 4박
            yield return null;
            yield return null;
            Assert.AreEqual(10f, boot.player.Distance, 0.1f, "경로 거리");
            Assert.AreEqual(10f, Vector3.Distance(start, boot.player.transform.position), 0.1f, "실제 이동 거리");

            // 속도 배율이 1.3이어도 4박에 10 m
            for (int i = 0; i < 6; i++) boot.clock.ApplyJudgement(Judgement.Perfect);
            boot.clock.ApplyPendingSpeedNow();   // GDD-beta B15.2: 속도는 다음 마디 첫 박에 들어오므로 바로 적용
            Assert.AreEqual(1.3f, boot.clock.SpeedMultiplier, 1e-4f);
            var p0 = boot.player.Distance;
            now += 4 * GameConfig.SecondsPerBeat / 1.3;
            yield return null;
            yield return null;
            Assert.AreEqual(10f, boot.player.Distance - p0, 0.1f, "1.3배에서도 4박 = 10 m");
        }

        [UnityTest]
        public IEnumerator T04_Player_StopsWhenNotRunning()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            var boot = GameBootstrap.Instance;
            double now = 0;
            boot.clock.TimeSource = () => now;
            boot.StartRun();
            yield return null;
            boot.player.Running = false;
            now = 2.0;
            yield return null;
            Assert.AreEqual(0f, boot.player.Distance, 1e-4f, "전투 중에는 멈춤");
            boot.player.Running = true;
            yield return null;
            now = 2.5;
            yield return null;
            Assert.AreEqual(2.5f, boot.player.Distance, 0.05f, "다시 달리면 그때부터 이어짐");
        }
    }
}
