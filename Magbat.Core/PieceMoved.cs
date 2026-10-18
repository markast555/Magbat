namespace Magbat.Core
{
    /// <summary>
    /// Фигура перемещена на другую клетку
    /// </summary>
    public class PieceMoved : Change
    {
        public int PieceId { get; }
        public int ToX { get; }
        public int ToY { get; }

        public PieceMoved(int pieceId, int toX, int toY)
        {
            PieceId = pieceId;
            ToX = toX;
            ToY = toY;
        }
    }
}