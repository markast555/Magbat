namespace Magbat.Core.Abilities
{
    /// <summary>
    /// Компонент расчёта урона атаки.
    /// Берёт все фигуры на поле в порядке возрастания Id, у каждой собирает
    /// способности-модификаторы урона, складывает их добавки с базовым уроном
    /// и возвращает итоговый урон.
    /// Это единственная точка входа, через которую ядро подключает способности.
    /// </summary>
    public static class AttackDamageCalculator
    {
        /// <summary>
        /// Считает итоговый урон атаки с учётом способностей всех фигур на поле.
        /// </summary>
        /// <param name="state">Текущее состояние игры</param>
        /// <param name="attacker">Атакующая фигура</param>
        /// <param name="target">Цель атаки</param>
        /// <returns>Итоговый урон атаки</returns>
        public static int Calculate(GameState state, Piece attacker, Piece target)
        {
            int damage = attacker.Definition.DefDamage;
            var context = new DamageContext(attacker, target, damage, state.Field);

            foreach (var piece in state.Field.GetAllPieces())
            foreach (var ability in piece.Abilities)
            {
                if (ability is IDamageModifier modifier)
                    damage += modifier.ModifyAttackDamage(context);
            }

            return damage;
        }
    }
}
