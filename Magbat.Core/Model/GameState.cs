using System.Collections.Generic;

namespace Magbat.Core
{
    /// <summary>
    /// Состояние игры
    /// </summary>
    public class GameState
    {
        public GameField Field { get; }
        public List<string>[] Hands { get; }
        public int CurrentPlayer { get; set; }
        public int? Winner { get; set; }
        public int NextPieceId { get; set; }
        public int[] ConsecutivePasses { get; } = new int[2];

        public GameState(GameField field, List<string> hand0, List<string> hand1)
        {
            Field = field;
            Hands = new[] { hand0, hand1 };
            CurrentPlayer = 0;
            Winner = null;
            NextPieceId = 1;
        }
    }
}