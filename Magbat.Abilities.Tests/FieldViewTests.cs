using Magbat.Core;
using Magbat.Core.Abilities;

namespace Magbat.Abilities.Tests
{
    /// <summary>
    /// Тесты read-only представления поля, которым пользуются способности.
    /// Проверяют контракт IGameFieldView, а не поведение самой игры.
    /// </summary>
    public class FieldViewTests
    {
        private static Piece Put(GameField field, int id, int owner, string name, int x, int y)
        {
            var piece = new Piece(id, owner, PieceCatalog.Get(name), x, y);
            field.Put(piece);
            return piece;
        }

        [Fact]
        public void GetDistance_UsesChebyshevMetric()
        {
            var field = new GameField(10, 10);

            Assert.Equal(0, field.GetDistance(2, 2, 2, 2));
            Assert.Equal(3, field.GetDistance(0, 0, 3, 1));
            Assert.Equal(3, field.GetDistance(0, 0, 1, 3));
        }

        [Fact]
        public void GetNeighbors_ReturnsPiecesFromSurroundingCells()
        {
            var field = new GameField(10, 10);
            Put(field, 1, 0, "Warrior_One", 4, 4); // центр — не сосед самой себе
            Put(field, 2, 0, "Warrior_One", 5, 5); // диагональный сосед
            Put(field, 3, 0, "Warrior_One", 5, 4); // сосед сбоку
            Put(field, 4, 0, "Warrior_One", 6, 6); // вне радиуса 1

            var neighbors = field.GetNeighbors(4, 4).ToList();

            Assert.Equal(2, neighbors.Count);
            Assert.Contains(neighbors, p => p.Id == 2);
            Assert.Contains(neighbors, p => p.Id == 3);
            Assert.DoesNotContain(neighbors, p => p.Id == 1);
            Assert.DoesNotContain(neighbors, p => p.Id == 4);
        }

        [Fact]
        public void GetNeighbors_DoesNotLeaveField()
        {
            var field = new GameField(10, 10);
            Put(field, 1, 0, "Warrior_One", 1, 0);
            Put(field, 2, 0, "Warrior_One", 0, 1);
            Put(field, 3, 0, "Warrior_One", 1, 1);

            var neighbors = field.GetNeighbors(0, 0).ToList();

            // у угловой клетки всего три соседа внутри поля, а не восемь
            Assert.Equal(3, neighbors.Count);
        }

        [Fact]
        public void GetPiecesInRadius_TakesCellsWithinRadius()
        {
            var field = new GameField(10, 10);
            Put(field, 1, 0, "Warrior_One", 5, 5); // расстояние 0
            Put(field, 2, 0, "Warrior_One", 7, 5); // расстояние 2
            Put(field, 3, 0, "Warrior_One", 8, 5); // расстояние 3

            var inRadius = field.GetPiecesInRadius(5, 5, 2).ToList();

            Assert.Equal(2, inRadius.Count);
            Assert.Contains(inRadius, p => p.Id == 1);
            Assert.Contains(inRadius, p => p.Id == 2);
        }

        [Fact]
        public void GetAllPieces_ReturnsPiecesOrderedById()
        {
            var field = new GameField(10, 10);
            Put(field, 7, 0, "Warrior_One", 0, 0);
            Put(field, 2, 1, "Warrior_One", 1, 0);
            Put(field, 5, 0, "Warrior_One", 2, 0);

            var ids = field.GetAllPieces().Select(p => p.Id).ToList();

            Assert.Equal(new[] { 2, 5, 7 }, ids);
        }
    }
}
