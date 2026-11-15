namespace RhythmActionGame
{
    /// <summary>곡 위치 계획 (GDD-beta B16.3). 순수 함수.</summary>
    public static class MusicPlan
    {
        /// <summary>
        /// 달리기 박자 runBeat에서 스테이지를 시작할 때 곡이 있어야 할 곡 박: 그 챕터가 시작되는 곡 박 + 챕터 안에서 달린 박.
        /// (챕터 시작 지점이면 정확히 챕터 시작 곡 박. 챕터 안 전투로 늘어난 박은 챕터 시작이 아닐 때만 생기는 작은 차이다.)
        /// </summary>
        public static double SongBeatAtRun(double runBeat)
        {
            var sec = MapLayout.SectionOf((int)System.Math.Floor(runBeat));
            int i = System.Math.Max(0, System.Math.Min(GameConfig.Music.ChapterSongBeats.Length - 1, sec.Number - 1));
            return GameConfig.Music.ChapterSongBeats[i] + (runBeat - sec.StartBeat);
        }
    }
}
