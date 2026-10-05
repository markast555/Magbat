namespace Magbat.Core
{
    /// <summary>
    /// Команда выставления фигуры
    /// </summary>
    public class PlaceCommand : Command
    {
        public PieceType Type { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }
}