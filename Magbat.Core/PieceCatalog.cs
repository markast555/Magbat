using System;

namespace Magbat.Core
{
    public static class PieceCatalog
    {
        public static PieceDefinition Get(PieceType type)
        {
            switch (type)
            {
                case PieceType.Warrior: return new PieceDefinition(type, 10, 3, 1, 1);
                case PieceType.Mage:    return new PieceDefinition(type, 6, 4, 1, 3);
                case PieceType.Cannon:  return new PieceDefinition(type, 8, 5, 0, 4);
                default: throw new NotSupportedException(type.ToString());
            }
        }
    }
}