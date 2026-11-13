using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// 무사 결투 3인칭 컷 A~D (GDD-beta B11.5). 결투 전체를 한 프레임 0.01초로 돌리고,
    /// Cinemachine 블렌드도 같은 0.01초씩 진행시켜(UniformDeltaTimeOverride) 시계와 카메라 시간을 맞춘다.
    /// </summary>
    public class B28_DuelCutTests
    {
        const double Dt = 0.01;
        double now;
        float real;
        GameBootstrap boot;
        StageDirector dir;
        DuelCamera duel;

        [TearDown]
        public void TearDown()
        {
            CinemachineCore.UniformDeltaTimeOverride = -1f;
            if (dir != null) dir.slowMotion.Cancel();
            Time.timeScale = 1f;
        }

        IEnumerator Load(double start)
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            boot = GameBootstrap.Instance;
            dir = boot.director;
            duel = Object.FindFirstObjectByType<DuelCamera>();
            now = 0; real = 100f;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            dir.RewindRealTime = () => real;
            dir.FailHitSeconds = 0f;   // beta1-1: 피격 연출(GDD-beta B12.2)은 B30 테스트에서 따로 본다. 여기서는 바로 되감기
            dir.slowMotion.RealTime = () => real;
            CinemachineCore.UniformDeltaTimeOverride = (float)Dt;
            boot.StartRun(start);
            yield return null;
        }

        class Frame { public double T, Rel; public string Cut; public bool Third, Blending; public float BlendDur; public ICinemachineCamera Active; }

        [UnityTest]
        public IEnumerator B28_Cuts_ABCD_Timing_Blend_Body_NoRings([Values(false, true)] bool perfect)
        {
            yield return Load(256);
            var appear = new List<double>();
            boot.judge.CueAppeared += c => { if (c.Tag is CombatStep) appear.Add(now); };
            var frames = new List<Frame>();
            var seen = new HashSet<string>();
            var minAngle = new Dictionary<string, float>(); var maxAngle = new Dictionary<string, float>();
            double delay = perfect ? 0.0 : 0.12;                               // NORMAL은 속도를 바꾸지 않는다 (1.0 유지)
            string prevCut = null;
            StageDirector.Encounter enc = null;
            for (int f = 0; f < 8000 && dir.State != StageState.Cleared; f++)
            {
                now += Dt; real += (float)Dt;
                foreach (var c in boot.judge.Cues.ToArray())
                    if (!c.Judged && c.Appeared && now >= boot.clock.TimeOfBeat(c.TargetBeat) + delay - 1e-9) { boot.judge.Submit(c.Key, boot.clock.TimeOfBeat(c.TargetBeat) + delay); break; }
                yield return null;
                if (dir.CurrentEncounter != null && dir.CurrentEncounter.IsBossDuel) enc = dir.CurrentEncounter;
                if (enc == null || duel.Current == DuelCamera.Mode.Off) continue;
                var b = duel.Brain;
                var fr = new Frame { T = now, Rel = boot.clock.CurrentBeat - enc.StartBeat, Cut = duel.Cut, Third = duel.ThirdPerson, Blending = b.IsBlending,
                    BlendDur = b.ActiveBlend != null ? b.ActiveBlend.Duration : 0f, Active = b.ActiveVirtualCamera };
                frames.Add(fr);

                // 몸: 3인칭이 화면에 영향을 주는 동안만 보임
                foreach (var r in duel.playerBody)
                    Assert.AreEqual(fr.Third ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly, r.shadowCastingMode, $"{fr.Rel:0.00}박 {r.name} 표시");
                if (fr.Third) Assert.AreEqual(0, boot.ringView.Rings.Count, $"{fr.Rel:0.00}박: 3인칭 동안 판정 원 없음");

                if (fr.Cut != null)
                {
                    seen.Add(fr.Cut);
                    var cc = duel.CutCams[fr.Cut];
                    if (fr.Cut != prevCut)
                    {
                        if (fr.Cut == "D" || fr.Cut == "K" || fr.Cut == "S")   // beta1-1 B13.4: 옆차기·자리 바꾸기 컷도 바로 자름
                        {
                            Assert.IsFalse(fr.Blending, $"{fr.Cut} 진입은 바로 자름");
                            Assert.AreSame(cc, fr.Active as CinemachineCamera);
                        }
                        else
                        {
                            Assert.IsTrue(fr.Blending, $"{fr.Cut} 진입 블렌드");
                            Assert.AreEqual(0.4f, fr.BlendDur, 1e-3f, $"{fr.Cut} 진입 0.4초");
                        }
                        // 바라보는 점 = 두 사람 중간점 (컷 시작 때 고정. D는 무사가 날아가기 시작하므로 크게 봐 준다)
                        var mid = (boot.player.transform.position + dir.boss.transform.position) * 0.5f;
                        var la = duel.CutLookAt;
                        Assert.Less(new Vector2(la.x - mid.x, la.z - mid.z).magnitude, fr.Cut == "D" ? 0.5f : fr.Cut == "K" ? 0.15f : 0.05f, $"{fr.Cut}: 두 사람 중간점");
                    }
                    var flat = cc.transform.position - duel.CutLookAt; flat.y = 0;
                    var def = GameConfig.DuelCuts.Single(x => x.name == fr.Cut);
                    if (fr.Cut != "S" && !duel.CutTopDown)   // beta1-1 B13.4: 컷마다 반지름 (A·D·K 3.5~5 m, B 칼날 1.2 m), S는 어깨 너머
                        Assert.That(flat.magnitude, Is.InRange(Mathf.Min(def.r0, def.r1) - 1e-3f, Mathf.Max(def.r0, def.r1) + 1e-3f), $"{fr.Cut} 반지름");
                    Assert.Greater(Vector3.Dot(cc.transform.forward, (duel.CutLookAt - cc.transform.position).normalized), 0.999f, $"{fr.Cut}: 중간점을 바라봄");
                    if (!fr.Blending && ReferenceEquals(fr.Active, cc))
                        Assert.Less(Vector3.Distance(boot.playerCamera.transform.position, cc.transform.position), 0.02f, "화면 = 3인칭 카메라");   // beta1-1 B13: 다음 프레임 Update에서 플레이어(카메라 부모)가 움직인 만큼 차이 (Brain이 LateUpdate마다 다시 맞춤)
                    minAngle[fr.Cut] = minAngle.TryGetValue(fr.Cut, out var mn) ? Mathf.Min(mn, duel.CutAngle) : duel.CutAngle;
                    maxAngle[fr.Cut] = maxAngle.TryGetValue(fr.Cut, out var mx) ? Mathf.Max(mx, duel.CutAngle) : duel.CutAngle;
                }
                else if (prevCut == "K" || prevCut == "S") Assert.IsFalse(fr.Blending, $"{prevCut} → 1인칭 바로 자름");
                else if (prevCut != null && prevCut != "D")
                {
                    Assert.IsTrue(fr.Blending, $"{prevCut} → 1인칭 블렌드");
                    Assert.AreEqual(0.4f, fr.BlendDur, 1e-3f, $"{prevCut} → 1인칭 0.4초");
                }
                prevCut = fr.Cut;
            }
            Assert.AreEqual(StageState.Cleared, dir.State, "결투 클리어");

            // 판정 원이 나타나기 0.4초 전에는 이미 1인칭
            foreach (var t in appear)
            {
                var bad = frames.FirstOrDefault(x => x.Third && x.T > t - 0.4 + 2 * Dt && x.T <= t);
                Assert.IsNull(bad, $"원 등장 {t:0.00}초: {bad?.T:0.00}초에 아직 3인칭");
            }

            Assert.IsTrue(seen.Contains("A") && seen.Contains("D") && seen.Contains("K"), "컷 A·D·K (beta1-1 B13.4: C는 옆차기 다운으로): " + string.Join(",", seen));
            Assert.IsTrue(seen.Contains("B"), $"컷 B (속도 {boot.clock.SpeedMultiplier:0.00}에서도, 칼 겨루기 연장)");
            foreach (var name in seen.Where(n => n != "S"))
            {
                float sweep = GameConfig.DuelCuts.Single(c => c.name == name).sweep;
                Assert.AreEqual(sweep, maxAngle[name] - minAngle[name], sweep * 0.15f, $"{name} 도는 각도 {sweep}°");
            }
        }
    }
}
