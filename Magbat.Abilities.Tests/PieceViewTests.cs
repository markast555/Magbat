using Magbat.Core;
using Magbat.Core.Abilities;

namespace Magbat.Abilities.Tests
{
    /// <summary>
    /// Тесты read-only представления фигуры, которым пользуются способности.
    /// Проверяют контракт <see cref="IPieceView"/>: способность видит
    /// состояние фигуры, но не может его изменить.
    /// </summary>
    public class PieceViewTests
    {
        private static Piece Put(GameField field, int id, int owner, string name, int x, int y)
        {
            var piece = new Piece(id, owner, PieceCatalog.Get(name), x, y);
            field.Put(piece);
            return piece;
        }

        [Fact]
        public void PieceView_HasNoSetters()
        {
            // read-only гарантия проверяется структурно: у интерфейса нет ни одного сеттера.
            var setters = typeof(IPieceView).GetProperties()
                .Select(p => p.GetSetMethod())
                .Where(m => m != null)
                .ToList();

            Assert.Empty(setters);
        }

        [Fact]
        public void FieldView_ReturnsPiecesAsReadOnlyView()
        {
            var field = new GameField(10, 10);
            var piece = Put(field, 1, 0, "Warrior_One", 4, 4);

            IGameFieldView view = field;
            var viewPiece = view.GetAt(4, 4);

            Assert.NotNull(viewPiece);
            Assert.Same(piece, viewPiece);
            Assert.Equal(1, viewPiece!.Id);
            Assert.Equal(0, viewPiece.Owner);
            Assert.Equal(10, viewPiece.Hp);
            Assert.Equal(4, viewPiece.X);
            Assert.Equal(4, viewPiece.Y);
            Assert.Equal("Warrior_One", viewPiece.Name);
            Assert.Equal(PieceType.Warrior, viewPiece.Type);
        }

        [Fact]
        public void FieldView_ExposesCharacteristicsOfTheFigure()
        {
            var field = new GameField(10, 10);
            Put(field, 1, 0, "Warrior_Three", 4, 4);

            IGameFieldView view = field;
            var viewPiece = view.GetAt(4, 4)!;

            Assert.Equal(20, viewPiece.DefHp);
            Assert.Equal(1, viewPiece.DefDamage);
            Assert.Equal(3, viewPiece.MoveRange);
            Assert.Equal(1, viewPiece.AttackRange);
        }

        [Fact]
        public void FieldView_CollectionsContainReadOnlyViews()
        {
            var field = new GameField(10, 10);
            var piece = Put(field, 1, 0, "Warrior_One", 4, 4);

            IGameFieldView view = field;

            Assert.Same(piece, view.FindById(1));
            Assert.All(view.GetAllPieces(), p => Assert.Same(piece, p));
            Assert.All(view.GetNeighbors(4, 4), p => Assert.Same(piece, p));
            Assert.All(view.GetPiecesInRadius(4, 4, 1), p => Assert.Same(piece, p));
        }

        [Fact]
        public void AbilityFactory_ReceivesOwnerAsReadOnlyView()
        {
            IPieceView? received = null;
            var definition = new PieceDefinition(
                name: "Test_View",
                type: PieceType.Support,
                defHp: 10,
                defDamage: 3,
                moveRange: 1,
                attackRange: 1,
                abilityFactory: piece =>
                {
                    received = piece;
                    return Array.Empty<IAbility>();
                });

            var piece = new Piece(7, 1, definition, 2, 3);

            Assert.NotNull(received);
            Assert.Same(piece, received);
            Assert.Equal(7, received!.Id);
            Assert.Equal(1, received.Owner);
            Assert.Equal(2, received.X);
            Assert.Equal(3, received.Y);
            Assert.Equal(10, received.Hp);
        }

        [Fact]
        public void DamageContext_ExposesAttackerAndTargetAsReadOnlyView()
        {
            var field = new GameField(10, 10);
            var attacker = Put(field, 1, 0, "Warrior_One", 3, 3);
            var target = Put(field, 2, 1, "Warrior_One", 4, 3);

            var context = new DamageContext(attacker, target, 3, field);

            Assert.Same(attacker, context.Attacker);
            Assert.Same(target, context.Target);
            Assert.Equal(3, context.BaseDamage);
            Assert.Equal(1, context.Attacker.Id);
            Assert.Equal(2, context.Target.Id);
        }
    }
}
