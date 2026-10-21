using System;
using System.Collections.Generic;

namespace Magbat.Core
{
    public static class PieceCatalog
    {
        private static readonly Dictionary<string, PieceDefinition> _definitions =
            new Dictionary<string, PieceDefinition>();

        static PieceCatalog()
        {
            Register(new PieceDefinition("Warrior_One",   PieceType.Warrior, 10, 3, 1, 1));
            Register(new PieceDefinition("Warrior_Two",   PieceType.Warrior, 14, 2, 1, 2));
            Register(new PieceDefinition("Warrior_Three", PieceType.Warrior, 20, 1, 3, 1));
            Register(new PieceDefinition("Mage_One",      PieceType.Mage,     6, 4, 1, 3));
            Register(new PieceDefinition("Mage_Two",      PieceType.Mage,     12, 4, 2, 2));
            Register(new PieceDefinition("Mage_Three",    PieceType.Mage,     5, 10, 1, 3));
            Register(new PieceDefinition("Cannon_One",    PieceType.Cannon,   8, 5, 0, 4));
            Register(new PieceDefinition("Cannon_Two",    PieceType.Cannon,   20, 3, 0, 5));
            Register(new PieceDefinition("Cannon_Three",  PieceType.Cannon,   7, 8, 0, 3));
        }

        /// <summary>
        /// Регистрирует определение фигуры. Позволяет внешним библиотекам
        /// добавлять фигуры со способностями.
        /// </summary>
        /// <param name="definition">Определение фигуры</param>
        public static void Register(PieceDefinition definition)
        {
            _definitions.Add(definition.Name, definition);
        }

        public static PieceDefinition Get(string name)
        {
            if (_definitions.TryGetValue(name, out var definition))
                return definition;
            throw new ArgumentException("Unknown piece: " + name);
        }

        public static IEnumerable<PieceDefinition> GetAll() => _definitions.Values;
    }
}