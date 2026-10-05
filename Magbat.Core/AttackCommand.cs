namespace Magbat.Core
{
    /// <summary>
    /// Команда атаки
    /// </summary>
    public class AttackCommand : Command
    {
        public int AttackerId { get; set; }
        public int TargetId { get; set; }
    }
}