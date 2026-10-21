using System;
using System.Collections.Generic;
using Magbat.Core.Abilities;

namespace Magbat.Core
{
    /// <summary>
    /// Шаблон фигуры
    /// </summary>
    public class PieceDefinition
    {
        public string Name { get; }
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

        /// <summary>
        /// Фабрика способностей фигуры. Вызывается один раз на каждый экземпляр фигуры,
        /// поэтому каждая фигура получает собственные экземпляры способностей.
        /// Фабрика может вернуть несколько способностей — они все будут у фигуры
        /// и все будут независимы друг от друга.
        /// null означает, что у фигуры нет способностей.
        /// </summary>
        public Func<IPieceView, IReadOnlyList<IAbility>>? AbilityFactory { get; }

        public PieceDefinition(string name, PieceType type, int defHp, int defDamage, int moveRange, int attackRange,
            Func<IPieceView, IReadOnlyList<IAbility>>? abilityFactory = null)
        {
            Name = name; Type = type; DefHp = defHp; DefDamage = defDamage;
            MoveRange = moveRange; AttackRange = attackRange;
            AbilityFactory = abilityFactory;
        }
    }
}
