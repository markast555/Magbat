using Magbat.Core.Abilities;

namespace Magbat.Abilities.Tests
{
    /// <summary>
    /// Тестовая способность «плоская прибавка»: добавляет к урону атак
    /// союзников владельца фиксированную величину. Самого владельца не усиливает.
    /// Нужна, чтобы проверить композицию: у одной фигуры могут быть несколько
    /// разных способностей, и их вклады складываются.
    /// </summary>
    public class FlatDamageAbility : IAbility, IDamageModifier
    {
        private readonly IPieceView _owner;
        private readonly int _bonus;

        public FlatDamageAbility(IPieceView owner, int bonus)
        {
            _owner = owner;
            _bonus = bonus;
        }

        /// <summary>
        /// Id фигуры-владельца.
        /// </summary>
        public int OwnerId => _owner.Id;

        /// <summary>
        /// Добавка к урону атак союзников владельца.
        /// </summary>
        public int ModifyAttackDamage(DamageContext context)
        {
            // Владельца самого не усиливаем
            if (context.Attacker.Id == _owner.Id)
                return 0;

            // Усиливаем только союзников
            if (context.Attacker.Owner != _owner.Owner)
                return 0;

            return _bonus;
        }
    }

    /// <summary>
    /// Тестовая способность со собственным состоянием: считает, сколько раз
    /// её спросили при расчёте урона. Урон не меняет.
    /// Нужна, чтобы доказать, что у двух фигур одного типа — независимые
    /// экземпляры способностей: состояние не общее.
    /// </summary>
    public class CountingDamageAbility : IAbility, IDamageModifier
    {
        private readonly IPieceView _owner;
        private int _calls;

        public CountingDamageAbility(IPieceView owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// Id фигуры-владельца.
        /// </summary>
        public int OwnerId => _owner.Id;

        /// <summary>
        /// Сколько раз способность была опрошена при расчёте урона.
        /// </summary>
        public int Calls => _calls;

        public int ModifyAttackDamage(DamageContext context)
        {
            _calls++;
            return 0;
        }
    }
}
