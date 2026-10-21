namespace Magbat.Core.Abilities
{
    /// <summary>
    /// Точка расширения для модификации урона атаки.
    /// </summary>
    public interface IDamageModifier : IAbility
    {
        /// <summary>
        /// Возвращает добавку к базовому урону атаки.
        /// </summary>
        /// <param name="context">Контекст атаки (атакующий, цель, базовый урон, поле).</param>
        int ModifyAttackDamage(DamageContext context);
    }
}