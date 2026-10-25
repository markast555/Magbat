namespace Magbat.Core
{
    /// <summary>
    /// Фигура в составе снимка состояния
    /// </summary>
    public class PieceSnapshot
    {
        public int Id { get; }
        public int Owner { get; }
        public string PieceName { get; }
        public int X { get; }
        public int Y { get; }
        public int Hp { get; }

        public PieceSnapshot(int id, int owner, string pieceName, int x, int y, int hp)
        {
            Id = id;
            Owner = owner;
            PieceName = pieceName;
            X = x;
            Y = y;
            Hp = hp;
        }
    }
}