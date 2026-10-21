namespace Magbat.Core.Abilities
{
    /// <summary>
    /// Контекст атаки, передаваемый модификаторам урона.
    /// Фигуры отдаются как <see cref="IPieceView"/> — только для чтения.
    /// </summary>
    public class DamageContext
    {
        /// <summary>
        /// Атакующая фигура.
        /// </summary>
        public IPieceView Attacker { get; }

        /// <summary>
        /// Цель атаки.
        /// </summary>
        public IPieceView Target { get; }

        /// <summary>
        /// Базовый урон до применения модификаторов.
        /// </summary>
        public int BaseDamage { get; }

        /// <summary>
        /// Read-only представление игрового поля для чтения состояния.
        /// </summary>
        public IGameFieldView Field { get; }

        /// <summary>
        /// Создает контекст атаки.
        /// </summary>
        public DamageContext(IPieceView attacker, IPieceView target, int baseDamage, IGameFieldView field)
        {
            Attacker = attacker;
            Target = target;
            BaseDamage = baseDamage;
            Field = field;
        }
    }
}
