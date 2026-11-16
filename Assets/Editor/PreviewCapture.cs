using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RhythmActionGame.EditorTools
{
    /// <summary>
    /// Game 씬을 열어 1인칭·3인칭 미리보기 PNG를 만든다 (작업 T13 눈 확인용, 씬은 저장하지 않음).
    /// 실행: harness/scripts/unity-exec.ps1 -Method RhythmActionGame.EditorTools.PreviewCapture.CaptureT13
    /// 결과: harness/reports/preview/t13_*.png
    /// </summary>
    public static class PreviewCapture
    {
        const string OutDir = "harness/reports/preview";

        public static void CaptureT13()
        {
            EditorSceneManager.OpenScene(SceneBuilder.GameScenePath, OpenSceneMode.Single);
            var body = Object.FindAnyObjectByType<PlayerBody>();
            var cam = GameObject.Find("PlayerCamera").GetComponent<Camera>();
            body.BuildHeadlessCopy();
            var ctrl = (AnimatorController)body.animator.runtimeAnimatorController;
            void Pose(string state, float normalized)
            {
                var a = body.animator;
                a.Rebind();
                a.Play(state, 0, normalized);
                a.Update(0f);
            }
            var model = body.animator.gameObject;
            Directory.CreateDirectory(OutDir);
            void LogBones(string tag)
            {
                var a = body.animator; var t = body.transform;
                Vector3 L(HumanBodyBones b) => t.InverseTransformPoint(a.GetBoneTransform(b).position);
                Debug.Log($"[PreviewCapture] {tag} Lshoulder={L(HumanBodyBones.LeftUpperArm)} Rshoulder={L(HumanBodyBones.RightUpperArm)} hips={L(HumanBodyBones.Hips)} head={L(HumanBodyBones.Head)} Ltoe={L(HumanBodyBones.LeftFoot)} modelRot={a.transform.localEulerAngles}");
            }
            LogBones("bind");

            // 1인칭: 달리기, 정면 / 조금 아래를 봄
            Pose(BodyMoves.Run, 0.2f);
            LogBones("run");
            Shot(cam, $"{OutDir}/t13_fp_run.png");
            cam.transform.localRotation = Quaternion.Euler(35f, 0f, 0f);
            Shot(cam, $"{OutDir}/t13_fp_run_down.png");
            Pose("Vault_Perfect", 0.4f);
            Shot(cam, $"{OutDir}/t13_fp_vault_down.png");
            cam.transform.localRotation = Quaternion.identity;

            // 3인칭: 몸 전체 (원래 메시를 보이게)
            body.body.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            body.VisibleBody.enabled = false;
            Pose(BodyMoves.Run, 0.2f);
            var p = body.transform;
            cam.transform.position = p.position + p.right * 2.2f + p.forward * 1.6f + Vector3.up * 1.4f;
            cam.transform.LookAt(p.position + Vector3.up * 0.95f);
            Shot(cam, $"{OutDir}/t13_tp_run.png");
            cam.transform.position = p.position + p.forward * 2.4f + Vector3.up * 1.5f;
            cam.transform.LookAt(p.position + Vector3.up * 1.2f);
            Shot(cam, $"{OutDir}/t13_tp_front.png");
            Debug.Log("[PreviewCapture] T13 미리보기 저장");
        }

        static void Shot(Camera cam, string path)
        {
            var rt = new RenderTexture(1280, 720, 24);
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = prev;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
        }
    }
}
