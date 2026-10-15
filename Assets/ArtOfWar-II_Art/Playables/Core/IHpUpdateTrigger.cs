namespace Playables.Core
{
    public interface IHpUpdateTrigger
    {
        void SetBloodVal(int hp);
        void UpdateBlood();
    }
}