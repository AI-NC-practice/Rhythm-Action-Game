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
    }
}
