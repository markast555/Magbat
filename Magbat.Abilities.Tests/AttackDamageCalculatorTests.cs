using Magbat.Core;
using Magbat.Core.Abilities;

namespace Magbat.Abilities.Tests
{
    /// <summary>
    /// Тесты компонента расчёта урона атаки — точки входа, выбранной на этапе 1
    /// (вариант А: одно обращение к новому компоненту расчёта урона).
    /// </summary>
    public class AttackDamageCalculatorTests
    {
        private const string Bearer = AbilityCatalog.StandardBearerName;

        static AttackDamageCalculatorTests()
        {
            AbilityCatalog.RegisterStandardBearer();
        }

        private static GameState NewState()
            => new GameState(new GameField(10, 10), new List<string>(), new List<string>());

        private static Piece Put(GameState state, int id, int owner, string name, int x, int y)
        {
            var piece = new Piece(id, owner, PieceCatalog.Get(name), x, y);
            state.Field.Put(piece);
            return piece;
        }

        [Fact]
        public void WithoutAbilities_ReturnsBaseDamage()
        {
            var state = NewState();
            var attacker = Put(state, 1, 0, "Warrior_One", 3, 3);
            var target = Put(state, 2, 1, "Warrior_One", 4, 3);

            int damage = AttackDamageCalculator.Calculate(state, attacker, target);

            Assert.Equal(3, damage);
        }

        [Fact]
        public void BearerNextToAlly_AddsOneToDamage()
        {
            var state = NewState();
            Put(state, 1, 0, Bearer, 3, 3);
            var attacker = Put(state, 2, 0, "Warrior_One", 4, 4);
            var target = Put(state, 3, 1, "Warrior_One", 4, 5);

            int damage = AttackDamageCalculator.Calculate(state, attacker, target);

            Assert.Equal(4, damage);
        }

        [Fact]
        public void TwoBearers_AddTwoToDamage()
        {
            var state = NewState();
            Put(state, 1, 0, Bearer, 3, 3);
            Put(state, 2, 0, Bearer, 3, 5);
            var attacker = Put(state, 3, 0, "Warrior_One", 4, 4);
            var target = Put(state, 4, 1, "Warrior_One", 4, 5);

            int damage = AttackDamageCalculator.Calculate(state, attacker, target);

            Assert.Equal(5, damage);
        }
    }
}
