using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace RhythmActionGame.EditorTools
{
    /// <summary>
    /// 툰 머티리얼과 플레이어 Animator (작업 T13).
    /// - 툰 셰이더(RhythmActionGame/Toon) 값은 GameConfig.Body에서 넣는다.
    /// - 캐릭터 4종: Assets/Art/Models/&lt;id&gt;/base_color.png·normal.png로 Assets/Art/Materials/Toon/&lt;id&gt;.mat
    /// - 정적 에셋 7종: GLB 안에 구운 색·노멀 텍스처로 Assets/Art/Materials/Toon/&lt;id&gt;.mat (배치는 T14·T15)
    /// - Player.controller: Run(반복) + BodyMoves 동작×결과 상태, 끝나면 Run으로.
    /// </summary>
    public static class ToonMaterials
    {
        public const string ShaderName = "RhythmActionGame/Toon";
        public const string Dir = "Assets/Art/Materials/Toon";
        public const string ControllerPath = "Assets/Art/Animations/Player.controller";
        public static readonly string[] Statics = { "house_giwa", "wall_stone", "gate", "cart", "jars", "weapon_sword", "weapon_spear" };

        public static Shader Shader => Shader.Find(ShaderName);

        /// <summary>툰 값(단계·어두운 밝기·외곽선)을 머티리얼에 넣는다.</summary>
        public static void ApplyToon(Material m)
        {
            if (m.shader == null || m.shader.name != ShaderName) m.shader = Shader;
            m.SetFloat("_Steps", GameConfig.Body.ToonSteps);
            m.SetFloat("_ShadowLevel", GameConfig.Body.ToonShadowLevel);
            m.SetFloat("_OutlineWidth", GameConfig.Body.OutlineWidth);
        }

        static Material Load(string id)
        {
            Directory.CreateDirectory(Dir);
            string path = $"{Dir}/{id}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader); AssetDatabase.CreateAsset(m, path); }
            ApplyToon(m);
            return m;
        }

        /// <summary>캐릭터 툰 머티리얼. normal.png는 선형(sRGB 끔) 일반 텍스처로 둔다 (셰이더가 RGB 노멀로 푼다).</summary>
        public static Material Character(string id)
        {
            string dir = $"{HumanoidImport.Root}/{id}";
            var nrmPath = $"{dir}/normal.png";
            if (AssetImporter.GetAtPath(nrmPath) is TextureImporter ti && (ti.sRGBTexture || ti.textureType != TextureImporterType.Default))
            {
                ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = false;
                ti.SaveAndReimport();
            }
            var m = Load(id);
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/base_color.png"));
            m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(nrmPath));
            m.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>정적 에셋 툰 머티리얼 (GLB 안 텍스처: 이름에 base_color / normal).</summary>
        public static Material Static(string id)
        {
            var texs = AssetDatabase.LoadAllAssetsAtPath($"Assets/Art/Models/{id}.glb").OfType<Texture2D>().ToList();
            var m = Load(id);
            m.SetTexture("_BaseMap", texs.FirstOrDefault(t => t.name.Contains("base_color")) ?? texs.FirstOrDefault(t => !t.name.Contains("normal")));
            m.SetTexture("_BumpMap", texs.FirstOrDefault(t => t.name.Contains("normal")));
            m.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(m);
            return m;
        }

        [MenuItem("RhythmActionGame/Build Toon Materials")]
        public static void BuildAll()
        {
            foreach (var id in HumanoidImport.Characters) Character(id);
            foreach (var id in Statics) Static(id);
            BuildPlayerController(out _, out _);
            AssetDatabase.SaveAssets();
        }

        static AnimationClip ClipOf(string name)
        {
            string path = name.StartsWith("player_") ? $"{HumanoidImport.Root}/player/{name}.fbx" : $"{HumanoidImport.MixamoRoot}/{name}.fbx";
            var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (clip == null) Debug.LogError($"[ToonMaterials] 클립 없음: {path}");
            return clip;
        }

        /// <summary>플레이어 Animator 컨트롤러를 (다시) 만든다. 상태 이름·클립 길이를 돌려준다.</summary>
        public static AnimatorController BuildPlayerController(out string[] names, out float[] seconds)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ControllerPath));
            AssetDatabase.DeleteAsset(ControllerPath);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            ctrl.AddParameter(PlayerBody.MirrorParam, AnimatorControllerParameterType.Bool);
            ctrl.AddParameter(PlayerBody.ActionSpeedParam, AnimatorControllerParameterType.Float);
            ctrl.AddParameter(PlayerBody.RunSpeedParam, AnimatorControllerParameterType.Float);
            ctrl.parameters = ctrl.parameters.Select(p => { if (p.type == AnimatorControllerParameterType.Float) p.defaultFloat = 1f; return p; }).ToArray();
            var sm = ctrl.layers[0].stateMachine;
            var n = new List<string>();
            var s = new List<float>();

            var runClip = ClipOf(BodyMoves.RunClip);
            var run = sm.AddState(BodyMoves.Run, new Vector3(300, 0, 0));
            run.motion = runClip;
            run.speedParameterActive = true;
            run.speedParameter = PlayerBody.RunSpeedParam;
            sm.defaultState = run;
            n.Add(BodyMoves.Run); s.Add(runClip != null ? runClip.length : 1f);

            int row = 0;
            foreach (var action in BodyMoves.Actions)
            {
                int col = 0;
                foreach (var r in BodyMoves.Results)
                {
                    string name = BodyMoves.State(action, r);
                    var clip = ClipOf(BodyMoves.ClipFor(action, r));
                    var st = sm.AddState(name, new Vector3(col * 220, 120 + row * 70, 0));
                    st.motion = clip;
                    st.speedParameterActive = true;
                    st.speedParameter = PlayerBody.ActionSpeedParam;
                    st.mirrorParameterActive = true;
                    st.mirrorParameter = PlayerBody.MirrorParam;
                    var back = st.AddTransition(run);
                    back.hasExitTime = true;
                    back.exitTime = 0.95f;
                    back.hasFixedDuration = true;
                    back.duration = GameConfig.Body.CrossFadeSeconds;
                    n.Add(name); s.Add(clip != null ? clip.length : 1f);
                    col++;
                }
                row++;
            }
            // 처형·피격 (작업 T14): 원래 속도로 한 번, 끝나면 Run
            var extra = BodyMoves.Executions.Select(h => (BodyMoves.ExecState(h), BodyMoves.ExecClip(h))).ToList();
            extra.Add((BodyMoves.Hit, BodyMoves.HitClip));
            int ex = 0;
            foreach (var (name, clipName) in extra)
            {
                var clip = ClipOf(clipName);
                var st = sm.AddState(name, new Vector3(ex++ * 220, 120 + row * 70, 0));
                st.motion = clip;
                var back = st.AddTransition(run);
                back.hasExitTime = true;
                back.exitTime = 0.95f;
                back.hasFixedDuration = true;
                back.duration = GameConfig.Body.CrossFadeSeconds;
                n.Add(name); s.Add(clip != null ? clip.length : 1f);
            }
            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            names = n.ToArray();
            seconds = s.ToArray();
            return ctrl;
        }
    }
}
