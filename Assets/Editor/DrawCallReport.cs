using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RhythmActionGame.EditorTools
{
    /// <summary>
    /// 드로우콜 재기용 비교 씬 (작업 T15). Game 씬과 같고 정적 플래그만 뺀 Game_NoBatch를 만든다
    /// (정적 배칭 없음, 장식은 인스턴싱 대신 물체 하나하나·모두 그림자). 재기는 PlayMode 테스트 T15_DrawCallTests가 하고 harness/reports/drawcalls.md에 남긴다.
    /// SceneBuilder.BuildAll 끝에서 함께 만든다.
    /// </summary>
    public static class DrawCallReport
    {
        public const string NoBatchScenePath = "Assets/Scenes/Test/Game_NoBatch.unity";

        public static void BuildNoBatchScene()
        {
            var scene = EditorSceneManager.OpenScene(SceneBuilder.GameScenePath, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            // 장식: 인스턴싱 대신 물체 하나하나 (줄이기 전 상태)
            foreach (var inst in Object.FindObjectsByType<DressingInstancer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                int k = 0;
                foreach (var g in inst.groups)
                    foreach (var m in g.matrices)
                    {
                        var go = new GameObject($"DressPlain_{k++}", typeof(MeshFilter), typeof(MeshRenderer));
                        go.transform.SetParent(inst.transform, false);
                        go.transform.SetPositionAndRotation(m.GetPosition(), m.rotation);
                        go.transform.localScale = m.lossyScale;
                        go.GetComponent<MeshFilter>().sharedMesh = g.mesh;
                        var r = go.GetComponent<MeshRenderer>();
                        r.sharedMaterial = g.material;
                        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;   // 줄이기 전: 모두 그림자
                    }
                Object.DestroyImmediate(inst);
            }
            System.IO.Directory.CreateDirectory("Assets/Scenes/Test");
            EditorSceneManager.SaveScene(scene, NoBatchScenePath, true);
            AssetDatabase.Refresh();
        }
    }
}
