namespace Magbat.Core
{
    /// <summary>
    /// Команда перемещения
    /// </summary>
    public class MoveCommand : Command
    {
        public int PieceId { get; set; }
        public int ToX { get; set; }
        public int ToY { get; set; }

        public MoveCommand(int player, int pieceId, int toX, int toY) : base(player)
        {
            PieceId = pieceId;
            ToX = toX;
            ToY = toY;
        }

        public MoveCommand()
        {
            PieceId = -1;
            ToX = -1;
            ToY = -1;
        }

        public override string ToString()
        {
            return $"Player {Player}: Move {PieceId} to ({ToX}, {ToY})";
        }
    }
}