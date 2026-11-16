using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RhythmActionGame.EditorTools
{
    /// <summary>
    /// VARCO 캐릭터 FBX를 Unity Humanoid로 설정한다 (작업 T11, varco3d-asset 8번).
    /// Assets/Art/Models/&lt;id&gt;/&lt;id&gt;_&lt;동작&gt;.fbx 마다:
    /// - 애니메이션 종류 Humanoid, 파일마다 아바타를 만든다(CreateFromThisModel). 메시가 든 FBX는 캐릭터당 하나(첫 동작), 나머지는 뼈+동작만.
    /// - 머티리얼은 같은 폴더의 텍스처를 이름으로 찾는다(base_color.png, normal.png).
    /// - 이름에 looping·forward_run·idle이 있으면 반복 재생.
    /// 결과를 "[HumanoidImport] ..." 로그와 harness/reports/humanoid-import.md에 남긴다 (avatar valid·human, 뼈 수, 클립 길이).
    /// 실행: harness/scripts/unity-exec.ps1 -Method RhythmActionGame.EditorTools.HumanoidImport.ConfigureAll
    /// </summary>
    public static class HumanoidImport
    {
        public const string Root = "Assets/Art/Models";
        public static readonly string[] Characters = { "player", "enemy_sword", "enemy_spear", "boss" };

        public static bool IsLoop(string clipFileName)
        {
            string n = clipFileName.ToLowerInvariant();
            return n.Contains("looping") || n.Contains("forward_run") || n.Contains("idle");   // run_jump·wall_side_walk 같은 한 번짜리는 반복 아님
        }

        /// <summary>한 캐릭터 폴더의 FBX 경로 (이름순). 첫 번째가 아바타 원본.</summary>
        public static List<string> FbxOf(string id) =>
            Directory.Exists($"{Root}/{id}")
                ? Directory.GetFiles($"{Root}/{id}", "*.fbx").Select(p => p.Replace('\\', '/')).OrderBy(p => p, System.StringComparer.Ordinal).ToList()
                : new List<string>();

        [MenuItem("RhythmActionGame/Configure Humanoid Characters")]
        public static void ConfigureAll()
        {
            var report = new StringBuilder("# Humanoid import report (T11)\n\n| 캐릭터 | FBX | avatar valid | human | 뼈 | 클립 | 길이(초) | 반복 |\n|---|---|---|---|---|---|---|---|\n");
            foreach (var id in Characters)
            {
                var files = FbxOf(id);
                if (files.Count == 0) { Debug.LogWarning($"[HumanoidImport] {id}: FBX 없음"); continue; }
                Avatar source = null;
                for (int i = 0; i < files.Count; i++)
                {
                    var imp = (ModelImporter)AssetImporter.GetAtPath(files[i]);
                    imp.animationType = ModelImporterAnimationType.Human;
                    // 동작 FBX는 Blender로 메시를 뺀 뼈+동작만이라 계층 경로가 기본 FBX와 다를 수 있다 → 각자 아바타를 만들고
                    // Humanoid 근육 공간으로 리타기팅한다 (CopyFromOther는 경로가 같아야 함).
                    imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                    imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
                    imp.materialName = ModelImporterMaterialName.BasedOnTextureName;
                    imp.materialSearch = ModelImporterMaterialSearch.Local;
                    bool loop = IsLoop(Path.GetFileNameWithoutExtension(files[i]));
                    var clips = imp.defaultClipAnimations;
                    foreach (var c in clips) { c.loopTime = loop; c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = true; }
                    imp.clipAnimations = clips;
                    imp.SaveAndReimport();
                    var avatar = AssetDatabase.LoadAllAssetsAtPath(files[i]).OfType<Avatar>().FirstOrDefault();
                    if (i == 0) source = avatar;
                    var clip = AssetDatabase.LoadAllAssetsAtPath(files[i]).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
                    var a = avatar != null ? avatar : source;
                    int bones = a != null && a.isHuman ? a.humanDescription.human.Length : 0;
                    string line = $"{id} {Path.GetFileName(files[i])} avatar valid={(a != null && a.isValid).ToString().ToLowerInvariant()} human={(a != null && a.isHuman).ToString().ToLowerInvariant()} bones={bones} clip={(clip != null ? clip.name : "-")} length={(clip != null ? clip.length : 0f):0.00}s loop={loop.ToString().ToLowerInvariant()}";
                    Debug.Log("[HumanoidImport] " + line);
                    report.Append($"| {id} | {Path.GetFileName(files[i])} | {(a != null && a.isValid)} | {(a != null && a.isHuman)} | {bones} | {(clip != null ? clip.name : "-")} | {(clip != null ? clip.length : 0f):0.00} | {loop} |\n");
                }
            }
            Directory.CreateDirectory("harness/reports");
            File.WriteAllText("harness/reports/humanoid-import.md", report.ToString(), new UTF8Encoding(false));
            AssetDatabase.SaveAssets();
        }
    }
}
