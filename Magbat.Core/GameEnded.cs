namespace Magbat.Core
{
    /// <summary>
    /// Партия окончена
    /// </summary>
    public class GameEnded : Change
    {
        public int Winner { get; }

        public GameEnded(int winner)
        {
            Winner = winner;
        }
    }
}