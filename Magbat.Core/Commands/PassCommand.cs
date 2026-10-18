namespace Magbat.Core
{
    /// <summary>
    /// Команда пропуска хода
    /// </summary>
    public class PassCommand : Command
    {
        public PassCommand(int player) : base(player)
        {
        }

        public PassCommand()
        {
        }

        public override string ToString()
        {
            return $"Player {Player}: pass";
        }
    }
}