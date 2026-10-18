namespace Magbat.Core
{
    /// <summary>
    /// Ход перешёл к другому игроку
    /// </summary>
    public class TurnChanged : Change
    {
        public int Player { get; } // кто ходит теперь

        public TurnChanged(int player)
        {
            Player = player;
        }
    }
}