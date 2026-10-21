using System;
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
        /// null означает, что у фигуры нет способностей.
        /// </summary>
        public Func<Piece, IAbility>? AbilityFactory { get; }

        public PieceDefinition(string name, PieceType type, int defHp, int defDamage, int moveRange, int attackRange,
            Func<Piece, IAbility>? abilityFactory = null)
        {
            Name = name; Type = type; DefHp = defHp; DefDamage = defDamage;
            MoveRange = moveRange; AttackRange = attackRange;
            AbilityFactory = abilityFactory;
        }
    }
}