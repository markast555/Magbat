using System;
using System.Collections.Generic;
using Magbat.Core.Abilities;

namespace Magbat.Core
{
    /// <summary>
    /// Экземпляр фигуры
    /// </summary>
    public class Piece
    {
        public int Id { get; }
        public int Owner { get; }
        public PieceDefinition Definition { get; }
        public int Hp { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        /// <summary>
        /// Способности этой фигуры. Каждый экземпляр фигуры получает собственные
        /// экземпляры способностей, созданные фабрикой из определения фигуры.
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
            Func<Piece, IAbility>? factory = definition.AbilityFactory;
            if (factory == null)
                return Array.Empty<IAbility>();

            return new List<IAbility> { factory(piece) };
        }
    }
}