namespace Magbat.Core
{
    /// <summary>
    /// Экземпляр фигуры
    /// </summary>
    public class Piece
    {
        public int Id { get; }
        public int Owner { get; }
        public PieceDefinition Definition { get; }
        public int Hp { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public Piece(int id, int owner, PieceDefinition definition, int x, int y)
        {
            Id = id; Owner = owner; Definition = definition;
            Hp = definition.DefHp; X = x; Y = y;
        }
    }
}