using System.Collections.Generic;

namespace Magbat.Core
{
    /// <summary>
    /// Правила игры
    /// </summary>
    public class GameRules
    {
        public ApplyResult Apply(GameState state, Command command)
        {
            return new ApplyResult();
        }

        public List<Command> GetLegalCommands(GameState state)
        {
            return new List<Command>();
        }
    }
}