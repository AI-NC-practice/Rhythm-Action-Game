namespace RhythmActionGame.EditorTools
{
    public static partial class SceneBuilder
    {
        /// <summary>포위 전투 적 5명과 최종 무사를 StageDirector에 연결한다 (T07).</summary>
        static partial void BuildDirectorExtra(StageDirector director, EnemySet enemies)
        {
            director.surroundEnemies = enemies.Surround.ToArray();
            director.boss = enemies.Boss;
        }
    }
}
