namespace Art.View
{
    public interface IBattleArtScene
    {
        void Enter();
        void BattleEnd(bool isWin);
        void Exit();
    }
}