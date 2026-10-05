namespace Magbat.Core
{
    public class GameState
    {
        public GameField Field { get; }
        public int CurrentPlayer { get; set; }
        public int? Winner { get; set; }
        public int NextPieceId { get; set; }

        public GameState(GameField field)
        {
            Field = field;
            CurrentPlayer = 0;
            Winner = null;
            NextPieceId = 1;
        }
    }
}