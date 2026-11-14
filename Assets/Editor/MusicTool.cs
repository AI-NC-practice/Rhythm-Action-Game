using UnityEditor;
using UnityEngine;

namespace RhythmActionGame.EditorTools
{
    /// <summary>
    /// 배경 음악(B10) 가져오기 설정. SceneBuilder.BuildAll이 씬을 만들기 전에 부른다.
    /// 루프 파일은 harness/scripts/music/make-loop.py로 만든다.
    /// </summary>
    public static class MusicTool
    {
        [MenuItem("RhythmActionGame/Music/Apply Import Settings")]
        public static void ApplyImportSettings()
        {
            var imp = AssetImporter.GetAtPath(GameConfig.Music.ClipPath) as AudioImporter;
            if (imp == null) { Debug.LogWarning($"[MusicTool] 음악 파일 없음: {GameConfig.Music.ClipPath}"); return; }
            var s = imp.defaultSampleSettings;
            // 되감기·속도 변경 때 정확히 찾아가도록 메모리에 풀어 둔다. 빌드 용량은 Vorbis로 줄인다.
            bool changed = s.loadType != AudioClipLoadType.DecompressOnLoad || s.compressionFormat != AudioCompressionFormat.Vorbis
                || !Mathf.Approximately(s.quality, 0.7f) || !s.preloadAudioData || imp.forceToMono || imp.loadInBackground;
            if (!changed) return;
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.7f;
            s.preloadAudioData = true;
            imp.defaultSampleSettings = s;
            imp.forceToMono = false;
            imp.loadInBackground = false;
            imp.SaveAndReimport();
            Debug.Log($"[MusicTool] import settings: {GameConfig.Music.ClipPath}");
        }

        /// <summary>음악 클립 (없으면 null).</summary>
        public static AudioClip LoadClip() => AssetDatabase.LoadAssetAtPath<AudioClip>(GameConfig.Music.ClipPath);
    }
}
