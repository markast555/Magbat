using Magbat.Core.Abilities;

namespace Magbat.Abilities.Demo
{
    /// <summary>
    /// Демонстрационная способность: добавляет к урону атак союзников
    /// фиксированную величину. Самого владельца не усиливает.
    /// Нужна, чтобы показать, что одна фигура может нести несколько
    /// независимых способностей и что их вклады складываются.
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
}
