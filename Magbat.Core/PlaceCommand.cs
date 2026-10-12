namespace Magbat.Core
{
    /// <summary>
    /// Команда выставления фигуры
    /// </summary>
    public class PlaceCommand : Command
    {
        public string PieceName { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
    }
}