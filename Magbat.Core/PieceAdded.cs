namespace Magbat.Core
{
    /// <summary>
    /// Фигура выставлена на поле
    /// </summary>
    public class PieceAdded : Change
    {
        public int PieceId { get; }
        public int Owner { get; }
        public string PieceName { get; }
        public int X { get; }
        public int Y { get; }

        public PieceAdded(int pieceId, int owner, string pieceName, int x, int y)
        {
            PieceId = pieceId;
            Owner = owner;
            PieceName = pieceName;
            X = x;
            Y = y;
        }
    }
}