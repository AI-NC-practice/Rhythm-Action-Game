using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RhythmActionGame.EditorTools
{
    /// <summary>
    /// Windows 빌드. 실행: harness/scripts/unity-exec.ps1 -Method RhythmActionGame.EditorTools.BuildScript.BuildGray
    /// </summary>
    public static class BuildScript
    {
        const string ExeName = "RhythmActionGame.exe";

        [MenuItem("RhythmActionGame/Build Gray (Windows)")]
        public static void BuildGray() => Build("Builds/Gray", "RAG_DEV");   // 개발자 모드 포함

        [MenuItem("RhythmActionGame/Build Final (Windows)")]
        public static void BuildFinal() => Build("Builds/Final");            // 개발자 모드 없음

        static void Build(string dir, params string[] defines)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) throw new Exception("Build Settings에 씬이 없다. SceneBuilder.BuildAll을 먼저 실행할 것.");

            Directory.CreateDirectory(dir);
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(dir, ExeName),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
                extraScriptingDefines = defines,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"[BuildScript] {s.result} → {options.locationPathName} ({s.totalSize / (1024 * 1024)} MB, errors {s.totalErrors})");
            if (s.result != BuildResult.Succeeded)
                throw new Exception("Build failed: " + s.result);
        }
    }
}
