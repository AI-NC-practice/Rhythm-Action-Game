using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RhythmActionGame.EditorTools
{
    /// <summary>
    /// 효과음 생성 도구 (GDD-beta B7). SfxSynth로 합성한 소리를 Assets/Audio/Sfx/Sfx_&lt;종류&gt;.wav로 저장한다.
    /// 실행: harness/scripts/unity-exec.ps1 -Method RhythmActionGame.EditorTools.SfxSynthTool.GenerateAll
    /// (SceneBuilder.BuildAll도 이 도구를 부른다.)
    /// </summary>
    public static class SfxSynthTool
    {
        public const string Dir = "Assets/Audio/Sfx";

        public static string PathOf(SfxKind k) => $"{Dir}/Sfx_{k}.wav";

        [MenuItem("RhythmActionGame/Generate Sfx")]
        public static void GenerateAll() => Generate();

        /// <summary>모든 효과음 WAV를 (다시) 만들고 AudioClip 목록을 돌려준다.</summary>
        public static List<AudioClip> Generate()
        {
            Directory.CreateDirectory(Dir);
            foreach (SfxKind k in System.Enum.GetValues(typeof(SfxKind)))
                File.WriteAllBytes(PathOf(k), SfxSynth.ToWav(SfxSynth.Create(k)));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var clips = new List<AudioClip>();
            foreach (SfxKind k in System.Enum.GetValues(typeof(SfxKind)))
            {
                var path = PathOf(k);
                var imp = AssetImporter.GetAtPath(path) as AudioImporter;
                if (imp != null)
                {
                    var s = imp.defaultSampleSettings;
                    s.loadType = AudioClipLoadType.DecompressOnLoad;   // 짧은 효과음: 지연 없이 재생
                    s.compressionFormat = AudioCompressionFormat.PCM;
                    imp.defaultSampleSettings = s;
                    imp.forceToMono = true;
                    imp.SaveAndReimport();
                }
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null) clips.Add(clip);
            }
            Debug.Log($"[SfxSynthTool] {clips.Count}개 효과음 → {Dir}");
            return clips;
        }
    }
}
