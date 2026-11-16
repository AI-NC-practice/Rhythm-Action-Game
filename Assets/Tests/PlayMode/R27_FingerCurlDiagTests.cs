using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// 작업 R27 진단 (사용자 피드백 "손가락이 이상한 데로 꺾이지 않았나"): 플레이어·무사 오른손을 손가락 굽힘 값별로 네 방향에서 확대해 찍고,
    /// 굽힐수록 손가락 끝이 손바닥 쪽으로 가는지(손가락 끝-손바닥 가운데 거리가 줄어드는지) 잰다. → harness/reports/BundleE/finger_*.png
    /// </summary>
    public class R27_FingerCurlDiagTests
    {
        const string Out = "harness/reports/BundleE";

        [UnityTest]
        public IEnumerator R27_FingerCurl_BendsTowardPalm()
        {
            Directory.CreateDirectory(Out);
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var body = Object.FindAnyObjectByType<PlayerBody>();
            body.enabled = false;   // 이 테스트가 직접 자세를 잡는다
            var models = new (string name, Animator a)[] { ("player", body.animator), ("boss", GameBootstrap.Instance.director.boss.GetComponentInChildren<Animator>()) };
            var lines = new System.Collections.Generic.List<string>();
            foreach (var (name, a) in models)
            {
                foreach (var grip in a.GetComponentsInParent<FingerGrip>(true)) grip.enabled = false;
                foreach (var grip in a.GetComponentsInChildren<FingerGrip>(true)) grip.enabled = false;
                var ea = a.GetComponentInParent<EnemyAnimator>(); if (ea != null) ea.enabled = false;
                a.enabled = false;
                var h = new HumanPoseHandler(a.avatar, a.transform);
                var pose = new HumanPose();
                var hand = a.GetBoneTransform(HumanBodyBones.RightHand);
                var tips = new[] { HumanBodyBones.RightIndexDistal, HumanBodyBones.RightMiddleDistal, HumanBodyBones.RightRingDistal, HumanBodyBones.RightLittleDistal }.Select(a.GetBoneTransform).ToArray();
                var prox = new[] { HumanBodyBones.RightIndexProximal, HumanBodyBones.RightLittleProximal }.Select(a.GetBoneTransform).ToArray();
                foreach (float v in new[] { 0f, -0.3f, -0.6f, -0.9f, 0.6f })
                {
                    a.Rebind(); a.Update(0f);
                    h.GetHumanPose(ref pose);
                    var names = HumanTrait.MuscleName;
                    for (int i = 0; i < names.Length; i++)
                    {
                        string n = names[i].Replace('.', ' ');
                        if (!(n.StartsWith("Right ") || n.StartsWith("RightHand "))) continue;
                        if (n.Contains("Stretched") && (n.Contains("Index") || n.Contains("Middle") || n.Contains("Ring") || n.Contains("Little"))) pose.muscles[i] = v;
                        if (n.Contains("Spread")) pose.muscles[i] = 0f;
                    }
                    h.SetHumanPose(ref pose);
                    yield return null;
                    // 손바닥 가운데 = 손 뼈와 검지·새끼 첫 마디의 가운데
                    var palm = (hand.position + prox[0].position + prox[1].position) / 3f;
                    float d = tips.Where(t => t != null).Average(t => Vector3.Distance(t.position, palm));
                    lines.Add($"{name} 굽힘 {v:+0.0;-0.0;0}: 손가락 끝-손바닥 평균 {d:0.000} m");
                    var fwd = (prox[0].position + prox[1].position) * 0.5f - hand.position;
                    var side = Vector3.Cross(fwd, Vector3.up).normalized;
                    int k = 0;
                    foreach (var dir in new[] { side, -side, Vector3.up, -Vector3.up })
                    {
                        var go = new GameObject("FingerCam"); var cam = go.AddComponent<Camera>(); cam.enabled = false; cam.nearClipPlane = 0.01f; cam.fieldOfView = 30f;
                        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.2f, 0.2f, 0.3f);
                        go.transform.position = palm + dir * 0.35f; go.transform.LookAt(palm);
                        var rt = new RenderTexture(320, 240, 24); cam.targetTexture = rt; cam.Render();
                        var prev = RenderTexture.active; RenderTexture.active = rt;
                        var tex = new Texture2D(320, 240, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 320, 240), 0, 0); RenderTexture.active = prev;
                        File.WriteAllBytes($"{Out}/finger_{name}_{(v < 0 ? "c" : "o")}{Mathf.Abs(v) * 10:0}_{k++}.png", tex.EncodeToPNG());
                        Object.Destroy(tex); rt.Release(); Object.Destroy(go);
                    }
                }
            }
            File.WriteAllLines($"{Out}/finger_summary.txt", lines);
            foreach (var l in lines) Debug.Log("[R27 손가락] " + l);
            Assert.Pass();
        }
    }
}
