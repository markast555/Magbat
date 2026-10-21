using System.Collections.Generic;

namespace Magbat.Core.Abilities
{
    /// <summary>
    /// Контекст атаки, передаваемый модификаторам урона.
    /// </summary>
    public class DamageContext
    {
        /// <summary>
        /// Атакующая фигура.
        /// </summary>
        public Piece Attacker { get; }

        /// <summary>
        /// Цель атаки.
        /// </summary>
        public Piece Target { get; }

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
        public DamageContext(Piece attacker, Piece target, int baseDamage, IGameFieldView field)
        {
            Attacker = attacker;
            Target = target;
            BaseDamage = baseDamage;
            Field = field;
        }
    }
}