namespace Magbat.Core
{
    /// <summary>
    /// Команда атаки
    /// </summary>
    public class AttackCommand : Command
    {
        public int AttackerId { get; set; }
        public int TargetId { get; set; }

        public AttackCommand(int player, int attackerId, int targetId) : base(player)
        {
            AttackerId = attackerId;
            TargetId = targetId;
        }

        public AttackCommand()
        {
            AttackerId = -1;
            TargetId = -1;
        }

        public override string ToString()
        {
            return $"Player {Player}: attack {TargetId} with {AttackerId}";
        }
    }
}