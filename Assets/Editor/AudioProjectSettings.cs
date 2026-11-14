using UnityEditor;
using UnityEngine;

namespace RhythmActionGame.EditorTools
{
    /// <summary>
    /// 오디오 프로젝트 설정 (GDD-beta B14): 히트사운드 지연을 줄이려고 DSP 버퍼를 GameConfig.HitSound.DspBufferSize로 둔다.
    /// ProjectSettings 파일을 손으로 고치지 않고 SceneBuilder.BuildAll에서 코드로 적용한다.
    /// </summary>
    public static class AudioProjectSettings
    {
        [MenuItem("RhythmActionGame/Audio/Apply Project Settings")]
        public static void Apply()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/AudioManager.asset");
            if (assets == null || assets.Length == 0) { Debug.LogWarning("[AudioProjectSettings] AudioManager 없음"); return; }
            var so = new SerializedObject(assets[0]);
            bool changed = false;
            foreach (var name in new[] { "m_RequestedDSPBufferSize", "m_DSPBufferSize" })
            {
                var p = so.FindProperty(name);
                if (p != null && p.intValue != GameConfig.HitSound.DspBufferSize) { p.intValue = GameConfig.HitSound.DspBufferSize; changed = true; }
            }
            if (!changed) return;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log($"[AudioProjectSettings] DSP buffer = {GameConfig.HitSound.DspBufferSize}");
        }
    }
}
