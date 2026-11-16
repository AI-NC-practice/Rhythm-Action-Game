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
                    imp.isReadable = Path.GetFileNameWithoutExtension(files[i]) == BodyMoves.RunClip;   // T13: PlayerBody가 머리 삼각형을 빼려면 메시를 읽어야 한다
                    bool loop = IsLoop(Path.GetFileNameWithoutExtension(files[i]));
                    var clips = imp.defaultClipAnimations;
                    foreach (var c in clips) { c.loopTime = loop; c.lockRootRotation = false; c.lockRootHeightY = true; c.lockRootPositionXZ = false; c.keepOriginalOrientation = false; c.keepOriginalPositionXZ = false; c.keepOriginalPositionY = false; c.heightFromFeet = true; }   // T13: 앞뒤·좌우 이동과 방향은 루트로 빼서 버림(applyRootMotion 끔 → 제자리), 높이는 자세에 남김(점프), 발 높이 기준
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
            ConfigureMixamo();
        }

        public const string MixamoRoot = "Assets/Art/Animations/Mixamo";

        /// <summary>Mixamo 동작 FBX (작업 T12, Without Skin). 이름순.</summary>
        public static List<string> MixamoFbx() =>
            Directory.Exists(MixamoRoot)
                ? Directory.GetFiles(MixamoRoot, "*.fbx").Select(p => p.Replace('\\', '/')).OrderBy(p => p, System.StringComparer.Ordinal).ToList()
                : new List<string>();

        /// <summary>
        /// Mixamo 동작(뼈+동작만)을 Humanoid로 들인다 (작업 T12). 파일마다 아바타를 만들고(mixamorig 뼈),
        /// Humanoid 근육 공간으로 player(VARCO) 아바타에 리타기팅된다. 한 번짜리 동작이라 반복 없음, 뿌리 고정.
        /// 결과: "[MixamoImport] ..." 로그와 harness/reports/mixamo-import.md.
        /// 실행: harness/scripts/unity-exec.ps1 -Method RhythmActionGame.EditorTools.HumanoidImport.ConfigureMixamo
        /// </summary>
        [MenuItem("RhythmActionGame/Configure Mixamo Clips")]
        public static void ConfigureMixamo()
        {
            var player = AssetDatabase.LoadAllAssetsAtPath($"{Root}/player/player_forward_run_1.fbx").OfType<Avatar>().FirstOrDefault();
            var report = new StringBuilder("# Mixamo import report (T12)\n\n");
            report.Append($"리타깃 대상: player 아바타 valid={(player != null && player.isValid)} human={(player != null && player.isHuman)}\n\n");
            report.Append("| FBX | avatar valid | human | 뼈 | 클립 | 길이(초) | humanMotion |\n|---|---|---|---|---|---|---|\n");
            foreach (var path in MixamoFbx())
            {
                var imp = (ModelImporter)AssetImporter.GetAtPath(path);
                imp.animationType = ModelImporterAnimationType.Human;
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                imp.materialImportMode = ModelImporterMaterialImportMode.None;
                var clips = imp.defaultClipAnimations;
                string nameMx = Path.GetFileNameWithoutExtension(path);
                bool loopMx = nameMx == "walk" || nameMx == "falling_idle";   // 작업 R18 후속: 걷기, R18c: 공중 자세는 반복
                foreach (var c in clips) { c.loopTime = loopMx; c.lockRootRotation = false; c.lockRootHeightY = true; c.lockRootPositionXZ = false; c.keepOriginalOrientation = false; c.keepOriginalPositionXZ = false; c.keepOriginalPositionY = false; c.heightFromFeet = true; }   // T13: 앞뒤·좌우 이동과 방향은 루트로 빼서 버림(applyRootMotion 끔 → 제자리), 높이는 자세에 남김(점프), 발 높이 기준
                imp.clipAnimations = clips;
                imp.SaveAndReimport();
                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                var avatar = assets.OfType<Avatar>().FirstOrDefault();
                var clip = assets.OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
                bool valid = avatar != null && avatar.isValid, human = avatar != null && avatar.isHuman;
                int bones = human ? avatar.humanDescription.human.Length : 0;
                Debug.Log($"[MixamoImport] {Path.GetFileName(path)} avatar valid={valid.ToString().ToLowerInvariant()} human={human.ToString().ToLowerInvariant()} bones={bones} clip={(clip != null ? clip.name : "-")} length={(clip != null ? clip.length : 0f):0.00}s humanMotion={(clip != null && clip.humanMotion).ToString().ToLowerInvariant()} retarget=player");
                report.Append($"| {Path.GetFileName(path)} | {valid} | {human} | {bones} | {(clip != null ? clip.name : "-")} | {(clip != null ? clip.length : 0f):0.00} | {clip != null && clip.humanMotion} |\n");
            }
            Directory.CreateDirectory("harness/reports");
            File.WriteAllText("harness/reports/mixamo-import.md", report.ToString(), new UTF8Encoding(false));
            AssetDatabase.SaveAssets();
        }
    }
}
