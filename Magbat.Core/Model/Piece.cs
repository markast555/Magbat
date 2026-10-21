using System;
using System.Collections.Generic;
using System.Linq;
using Magbat.Core.Abilities;

namespace Magbat.Core
{
    /// <summary>
    /// Экземпляр фигуры
    /// </summary>
    public class Piece : IPieceView
    {
        public int Id { get; }
        public int Owner { get; }
        public PieceDefinition Definition { get; }
        public int Hp { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        /// <summary>
        /// Тип фигуры из её определения.
        /// </summary>
        public PieceType Type => Definition.Type;

        /// <summary>
        /// Имя типа фигуры из её определения.
        /// </summary>
        public string Name => Definition.Name;

        /// <summary>
        /// Дальность хода из определения фигуры.
        /// </summary>
        public int MoveRange => Definition.MoveRange;

        /// <summary>
        /// Дальность атаки из определения фигуры.
        /// </summary>
        public int AttackRange => Definition.AttackRange;

        /// <summary>
        /// Базовый урон атаки из определения фигуры, до применения способностей.
        /// </summary>
        public int DefDamage => Definition.DefDamage;

        /// <summary>
        /// Базовое здоровье из определения фигуры.
        /// </summary>
        public int DefHp => Definition.DefHp;

        /// <summary>
        /// Способности этой фигуры. Каждый экземпляр фигуры получает собственные
        /// экземпляры способностей, созданные фабрикой из определения фигуры.
        /// Фабрика может вернуть несколько способностей — все они попадают сюда.
        /// </summary>
        public IReadOnlyList<IAbility> Abilities { get; }

        public Piece(int id, int owner, PieceDefinition definition, int x, int y)
        {
            Id = id; Owner = owner; Definition = definition;
            Hp = definition.DefHp; X = x; Y = y;
            Abilities = CreateAbilities(definition, this);
        }

        private static IReadOnlyList<IAbility> CreateAbilities(PieceDefinition definition, Piece piece)
        {
            Func<IPieceView, IReadOnlyList<IAbility>>? factory = definition.AbilityFactory;
            if (factory == null)
                return Array.Empty<IAbility>();

            var abilities = factory(piece);
            if (abilities == null)
                return Array.Empty<IAbility>();

            // Копия: список способностей фигуры нельзя изменить снаружи.
            return abilities.ToArray();
        }
    }
}
