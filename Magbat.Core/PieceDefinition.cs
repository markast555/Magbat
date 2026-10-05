namespace Magbat.Core
{
    /// <summary>
    /// Шаблон фигуры
    /// </summary>
    public class PieceDefinition
    {
        public PieceType Type { get; }
        /// <summary>
        /// Базовое значение ХП
        /// </summary>
        public int DefHp { get; }
        /// <summary>
        /// Базовое значение урона
        /// </summary>
        public int DefDamage { get; }
        public int MoveRange { get; }
        public int AttackRange { get; }

        public PieceDefinition(PieceType type, int defHp, int defDamage, int moveRange, int attackRange)
        {
            Type = type; DefHp = defHp; DefDamage = defDamage;
            MoveRange = moveRange; AttackRange = attackRange;
        }
    }
}