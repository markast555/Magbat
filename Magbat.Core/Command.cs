namespace Magbat.Core
{
    /// <summary>
    /// Команда
    /// </summary>
    public abstract class Command
    {
        public int Player { get; set; }

        protected Command()
        {
        }

        protected Command(int player)
        {
            Player = player;
        }
    }
}