namespace Magbat.Core
{
    /// <summary>
    /// Фигура убита и убрана с поля
    /// </summary>
    public class PieceRemoved : Change
    {
        public int PieceId { get; }

        public PieceRemoved(int pieceId)
        {
            PieceId = pieceId;
        }
    }
}