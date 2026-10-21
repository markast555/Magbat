using System.Collections.Generic;
using System.Linq;
using Magbat.Core;
using Magbat.Core.Abilities;

namespace Magbat.Abilities
{
    /// <summary>
    /// Способность «Знаменосец».
    /// Усиливает атаки союзных фигур (кроме себя) в радиусе 2 на +1 к урону.
    /// Бонусы нескольких знаменосцев складываются.
    /// Мёртвый знаменосец не даёт бонуса (HP <= 0).
    /// </summary>
    public class StandardBearerAbility : IAbility, IDamageModifier
    {
        private readonly Piece _owner;

        public StandardBearerAbility(Piece owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// Возвращает добавку (delta) к урону атаки.
        /// 0 — знаменосец не влияет на эту атаку, 1 — даёт бонус +1 к урону.
        /// Несколько знаменосцев складывают свои добавки.
        /// </summary>
        public int ModifyAttackDamage(DamageContext context)
        {
            // Знаменосец не усиливает себя
            if (context.Attacker.Id == _owner.Id)
                return 0;

            // Знаменосец усиливает только союзников
            if (context.Attacker.Owner != _owner.Owner)
                return 0;

            // Знаменосец должен быть жив
            if (_owner.Hp <= 0)
                return 0;

            // Союзник должен быть в радиусе 2
            if (context.Field.GetDistance(_owner.X, _owner.Y, context.Attacker.X, context.Attacker.Y) > 2)
                return 0;

            return 1;
        }
    }
}