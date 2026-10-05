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
    }
}