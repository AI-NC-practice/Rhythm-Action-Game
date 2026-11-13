using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>무사 결투 1인칭 가상 카메라 2대 (GDD-beta B11.5, Cinemachine).</summary>
    public class B27_DuelCameraTests
    {
        double now;
        float real;
        GameBootstrap boot;
        StageDirector dir;
        DuelCamera duel;
        Camera cam;

        IEnumerator Load(double start)
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            boot = GameBootstrap.Instance;
            dir = boot.director;
            duel = Object.FindFirstObjectByType<DuelCamera>();
            cam = boot.playerCamera;
            now = 0; real = 100f;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            dir.RewindRealTime = () => real;
            dir.slowMotion.RealTime = () => real;
            boot.StartRun(start);
            yield return null;
        }

        [TearDown]
        public void TearDown() { if (dir != null) dir.slowMotion.Cancel(); Time.timeScale = 1f; }

        IEnumerator AdvanceTo(double t, double step = 0.01)
        {
            while (now < t - 1e-9) { now = System.Math.Min(t, now + step); yield return null; }
        }

        IEnumerator UntilCombat()
        {
            for (int i = 0; i < 3000 && dir.State != StageState.Combat; i++) { now += 0.02; yield return null; }
            Assert.AreEqual(StageState.Combat, dir.State);
        }

        IEnumerator Hit(JudgeCue cue, double? delay)
        {
            yield return AdvanceTo(boot.clock.TimeOfBeat(cue.TargetBeat) - 0.03);
            if (delay.HasValue)
            {
                now = boot.clock.TimeOfBeat(cue.TargetBeat) + delay.Value;
                boot.judge.Submit(cue.Key, now);
                yield return null;
            }
            else yield return AdvanceTo(boot.clock.TimeOfBeat(cue.TargetBeat) + 0.2);
        }

        [Test]
        public void B27_Config_MatchGddBetaB115()
        {
            Assert.AreEqual(-0.1f, GameConfig.DuelGuardCamHeight);
            Assert.AreEqual(62f, GameConfig.DuelGuardCamFov);
            Assert.AreEqual(0.4f, GameConfig.DuelStrikeCamForward);
            Assert.AreEqual(54f, GameConfig.DuelStrikeCamFov);
            Assert.AreEqual(6f, GameConfig.DuelStrikeCamRoll);
            Assert.AreEqual(0.3f, GameConfig.DuelCamBlendSeconds);
            Assert.AreEqual(-6f, GameConfig.DuelClashPunchFov);
            Assert.AreEqual(0.15f, GameConfig.DuelClashPunchSeconds);
            Assert.AreEqual(-6f, DuelCamera.Punch(0f), 1e-5f, "부딪친 순간 −6°");
            Assert.Less(DuelCamera.Punch(0.1f), 0f);
            Assert.Greater(DuelCamera.Punch(0.1f), -6f, "돌아오는 중");
            Assert.AreEqual(0f, DuelCamera.Punch(0.15f), 1e-5f, "0.15초에 끝");
        }

        [UnityTest]
        public IEnumerator B27_Brain_OnlyDuringDuel()
        {
            // 일반 적과 싸울 때는 기존 카메라 구조 그대로
            yield return Load(89);
            Assert.IsNotNull(duel, "결투 카메라");
            Assert.IsNotNull(duel.Brain, "Cinemachine Brain");
            Assert.IsFalse(duel.Brain.enabled, "평소에는 Brain 꺼짐");
            yield return UntilCombat();
            yield return null;
            Assert.IsFalse(duel.Brain.enabled, "일반 적 전투에서도 꺼짐");
            Assert.AreEqual(DuelCamera.Mode.Off, duel.Current);
            Assert.IsFalse(boot.fx.CameraOverride, "JudgeFx가 카메라를 맡음");

            yield return Load(256);
            Assert.IsFalse(duel.Brain.enabled);
            yield return UntilCombat();
            yield return null;
            Assert.IsTrue(duel.Brain.enabled, "무사 결투 동안 Brain 켜짐");
            Assert.IsTrue(boot.fx.CameraOverride);
            Assert.AreEqual(DuelCamera.Mode.Guard, duel.Current, "첫 판정(A6)은 막기 카메라");
            Assert.IsTrue(duel.GuardCam.gameObject.activeInHierarchy && duel.StrikeCam.gameObject.activeInHierarchy);
            Assert.AreEqual(duel.fxOffset, duel.GuardCam.transform.parent, "가상 카메라는 연출 층 아래");
            foreach (var c in dir.CurrentEncounter.Cues.ToList()) yield return Hit(c, 0.0);
            Assert.AreEqual(StageState.Cleared, dir.State);
            yield return null;
            Assert.IsFalse(duel.Brain.enabled, "결투가 끝나면 Brain 꺼짐");
            Assert.AreEqual(DuelCamera.Mode.Off, duel.Current);
            Assert.IsFalse(boot.fx.CameraOverride, "기존 카메라 구조로 돌아옴");
            Assert.AreEqual(Vector3.zero, cam.transform.localPosition, "카메라 자리도 연출 층 기준으로");
        }

        [UnityTest]
        public IEnumerator B27_GuardAndStrikeCams_SwitchByNextStep_Blend03()
        {
            yield return Load(256);
            yield return UntilCombat();
            var cues = dir.CurrentEncounter.Cues.ToList();
            Assert.AreEqual(CinemachineBlendDefinition.Styles.EaseInOut, duel.Brain.DefaultBlend.Style, "Ease In-Out");
            Assert.AreEqual(0.3f, duel.Brain.DefaultBlend.Time, 1e-5f, "0.3초 블렌드");
            Assert.AreEqual(new Vector3(0f, -0.1f, 0f), duel.GuardCam.transform.localPosition, "막기 카메라 −0.1 m");
            Assert.AreEqual(new Vector3(0f, 0f, 0.4f), duel.StrikeCam.transform.localPosition, "공격 카메라 앞으로 0.4 m");
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual(DuelCamera.Mode.Guard, duel.Current);
            Assert.AreSame(duel.GuardCam, duel.Brain.ActiveVirtualCamera as CinemachineCamera);
            Assert.Less(Vector3.Distance(cam.transform.position, duel.GuardCam.transform.position), 1e-3f, "카메라 = 막기 카메라 자리");
            Assert.AreEqual(GameConfig.DuelGuardCamFov + boot.fx.FxFov, cam.fieldOfView, 0.05f, "FOV 62 (+연출 층)");

            yield return Hit(cues[0], 0.0);
            yield return Hit(cues[1], 0.0);                                  // 다음은 S10 → 공격 카메라
            yield return null;
            Assert.AreEqual(DuelCamera.Mode.Strike, duel.Current, "다음 판정이 내 공격이면 공격 카메라");
            Assert.IsTrue(duel.Brain.IsBlending, "두 카메라 사이를 블렌드");
            Assert.AreEqual(0.3f, duel.Brain.ActiveBlend.Duration, 1e-3f);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsFalse(duel.Brain.IsBlending, "0.3초 뒤 블렌드 끝");
            Assert.AreSame(duel.StrikeCam, duel.Brain.ActiveVirtualCamera as CinemachineCamera);
            Assert.Less(Vector3.Distance(cam.transform.position, duel.StrikeCam.transform.position), 1e-3f, "카메라 = 공격 카메라 자리");
            Assert.AreEqual(GameConfig.DuelStrikeCamFov + boot.fx.FxFov, cam.fieldOfView, 0.05f, "FOV 54 (+연출 층)");
            float roll = Vector3.SignedAngle(duel.GuardCam.transform.up, cam.transform.up, cam.transform.forward);   // 막기 카메라(판정 회전만) 기준
            Assert.AreEqual(6f, Mathf.Abs(roll), 1.5f, "베는 쪽 롤 6°");
            // S10(→)은 오른쪽으로 벤다 → 화면 위쪽이 오른쪽으로 기움
            Assert.Greater(Vector3.Dot(cam.transform.up, duel.GuardCam.transform.right), 0f, "→ 베기는 오른쪽으로 기움");

            yield return Hit(cues[2], 0.0);
            yield return Hit(cues[3], 0.0);                                  // 다음은 A13 → 막기 카메라
            yield return null;
            Assert.AreEqual(DuelCamera.Mode.Guard, duel.Current, "다시 막기 카메라");
            Assert.IsTrue(duel.Brain.IsBlending);
        }

        [UnityTest]
        public IEnumerator B27_ClashPunch_And_FxLayerOnTop()
        {
            yield return Load(256);
            yield return UntilCombat();
            duel.RealTime = () => real;
            var cues = dir.CurrentEncounter.Cues.ToList();
            yield return new WaitForSecondsRealtime(0.4f);
            int p0 = duel.PunchCount;
            yield return Hit(cues[0], 0.0);
            Assert.AreEqual(p0 + 1, duel.PunchCount, "칼이 부딪침 → 줌 펀치");
            yield return null;
            Assert.AreEqual(-6f, duel.PunchFov, 1e-4f, "FOV −6°");
            Assert.AreEqual(GameConfig.DuelGuardCamFov + boot.fx.FxFov - 6f, duel.GuardCam.Lens.FieldOfView, 0.05f, "막기 카메라 FOV에 펀치");
            real += 0.16f;
            yield return null;
            Assert.AreEqual(0f, duel.PunchFov, 1e-4f, "0.15초 뒤 원래대로");

            // 기존 연출 층: 흔들림·밀림이 가상 카메라 위에 그대로 얹힘
            var pd = boot.presentation;
            pd.Play(GameConfig.Fx.ParryPerfect, 1f, null, null);
            pd.cameraFx.Step(0.03f);
            yield return null;
            Assert.Greater(duel.fxOffset.localPosition.magnitude, 0.005f, "연출 층이 움직임");
            Assert.Less(Vector3.Distance(duel.GuardCam.transform.position, duel.fxOffset.TransformPoint(new Vector3(0f, -0.1f, 0f))), 1e-4f, "가상 카메라가 연출 층을 따라감");
            Assert.AreNotEqual(0f, boot.fx.FxFov, "연출 층 FOV");
            Assert.AreEqual(GameConfig.DuelGuardCamFov + boot.fx.FxFov + duel.PunchFov, duel.GuardCam.Lens.FieldOfView, 0.05f, "연출 층 FOV도 더함");

            // FAIL은 칼이 부딪치지 않음
            int p1 = duel.PunchCount;
            yield return Hit(cues[1], null);
            Assert.AreEqual(p1, duel.PunchCount, "FAIL은 줌 펀치 없음");
        }
    }
}
