namespace Magbat.Core
{
    /// <summary>
    /// Фигура получила урон и осталась на поле
    /// </summary>
    public class PieceDamaged : Change
    {
        public int PieceId { get; }
        public int Hp { get; } // оставшееся здоровье

        public PieceDamaged(int pieceId, int hp)
        {
            PieceId = pieceId;
            Hp = hp;
        }
    }
}