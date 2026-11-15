namespace RhythmActionGame
{
    /// <summary>입력 결과 → 히트사운드 종류와 음량 (GDD-beta B14). 순수 함수.</summary>
    public static class HitSoundRules
    {
        /// <summary>outcome이 Judged면 result로 고르고, 아니면(원 없음·너무 이름) 작은 "틱". 음량을 돌려준다.</summary>
        public static float Pick(InputOutcome outcome, Judgement result, out SfxKind kind)
        {
            if (outcome == InputOutcome.Judged)
            {
                switch (result)
                {
                    case Judgement.Perfect: kind = SfxKind.HitPerfect; return GameConfig.HitSound.PerfectVolume;
                    case Judgement.Normal: kind = SfxKind.HitNormal; return GameConfig.HitSound.NormalVolume;
                    default: kind = SfxKind.HitFail; return GameConfig.HitSound.FailVolume;
                }
            }
            kind = SfxKind.HitEmpty;
            return GameConfig.HitSound.EmptyVolume;
        }

        /// <summary>
        /// 달리기 장단 노드의 히트사운드 (GDD-beta B18.2, B18.5): 맞추면 Q 궁편 "덩"(HitPerfect 소리), W 채편 "덕"(HitNormal 소리).
        /// 음량은 PERFECT/NORMAL 음량. FAIL은 Pick과 같다.
        /// </summary>
        public static float PickRhythm(Judgement result, ArrowKey key, out SfxKind kind)
        {
            if (result == Judgement.Fail) return Pick(InputOutcome.Judged, result, out kind);
            kind = key == ArrowKey.Deok || key == ArrowKey.Right ? SfxKind.HitNormal : SfxKind.HitPerfect;   // W 채편 덕 / Q 궁편 덩
            return result == Judgement.Perfect ? GameConfig.HitSound.PerfectVolume : GameConfig.HitSound.NormalVolume;
        }

        /// <summary>
        /// 히트사운드를 몇 초 뒤에 울릴지 (GDD-beta B15.3 박 맞춤). PERFECT이고 목표 박이 아직 오지 않았으면
        /// 목표 박까지 기다린다(최대 SnapMaxWaitSeconds). 그 밖(늦은 PERFECT, NORMAL, FAIL, 원 없음)은 0 = 바로.
        /// </summary>
        public static double SnapDelay(InputOutcome outcome, Judgement result, double secondsUntilTarget)
        {
            if (outcome != InputOutcome.Judged || result != Judgement.Perfect) return 0;
            if (double.IsNaN(secondsUntilTarget) || secondsUntilTarget <= 0) return 0;
            return System.Math.Min(secondsUntilTarget, GameConfig.HitSound.SnapMaxWaitSeconds);
        }
    }
}
