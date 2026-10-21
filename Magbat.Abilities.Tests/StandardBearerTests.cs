using Magbat.Core;

namespace Magbat.Abilities.Tests
{
    /// <summary>
    /// Тесты способности «Знаменосец».
    /// Проверяют бонус через настоящую атаку в GameRules, а не напрямую.
    /// </summary>
    public class StandardBearerTests
    {
        private const string Bearer = AbilityCatalog.StandardBearerName;

        static StandardBearerTests()
        {
            AbilityCatalog.RegisterStandardBearer();
        }

        // ---------- вспомогательные методы ----------

        private static GameState NewState(string[] hand0, string[] hand1)
            => new GameState(new GameField(10, 10), new List<string>(hand0), new List<string>(hand1));

        /// <summary>
        /// Выставляет фигуру от имени игрока и возвращает её Id.
        /// </summary>
        private static int PlaceAs(GameRules rules, GameState state, int player, string name, int x, int y)
        {
            state.CurrentPlayer = player;
            var result = rules.Apply(state, new PlaceCommand(player, name, x, y));
            Assert.True(result.Success, result.Error.ToString());
            return state.Field.GetAt(x, y)!.Id;
        }

        /// <summary>
        /// Наносит атаку от имени игрока и возвращает HP цели после удара.
        /// </summary>
        private static int AttackAndGetTargetHp(GameRules rules, GameState state, int player, int attackerId, int targetId)
        {
            state.CurrentPlayer = player;
            var result = rules.Apply(state, new AttackCommand(player, attackerId, targetId));
            Assert.True(result.Success, result.Error.ToString());
            return state.Field.GetAt(state.Field.FindById(targetId)!.X, state.Field.FindById(targetId)!.Y)!.Hp;
        }

        // ---------- тесты ----------

        [Fact]
        public void Attack_WithoutAbilities_UsesBaseDamage()
        {
            var rules = new GameRules();
            var state = NewState(new[] { "Warrior_One" }, new[] { "Warrior_One" });
            PlaceAs(rules, state, 0, "Warrior_One", 3, 3); // Id = 1
            PlaceAs(rules, state, 1, "Warrior_One", 4, 3); // Id = 2

            int hp = AttackAndGetTargetHp(rules, state, 0, 1, 2);

            Assert.Equal(7, hp); // 10 - 3, базовый урон Warrior_One
        }

        [Fact]
        public void AllyInRadius_GetsPlusOneDamage()
        {
            var rules = new GameRules();
            var state = NewState(new[] { Bearer, "Warrior_One" }, new[] { "Warrior_One" });
            PlaceAs(rules, state, 0, Bearer, 3, 3);        // Id = 1, знаменосец
            PlaceAs(rules, state, 0, "Warrior_One", 4, 4); // Id = 2, атакующий союзник, расстояние 1
            PlaceAs(rules, state, 1, "Warrior_One", 4, 5); // Id = 3, цель

            int hp = AttackAndGetTargetHp(rules, state, 0, 2, 3);

            Assert.Equal(6, hp); // 10 - (3 + 1)
        }

        [Fact]
        public void AllyOutsideRadius_GetsNoBonus()
        {
            var rules = new GameRules();
            var state = NewState(new[] { Bearer, "Warrior_One" }, new[] { "Warrior_One" });
            PlaceAs(rules, state, 0, Bearer, 3, 3);        // Id = 1, знаменосец
            PlaceAs(rules, state, 0, "Warrior_One", 7, 7); // Id = 2, расстояние 4
            PlaceAs(rules, state, 1, "Warrior_One", 7, 8); // Id = 3, цель

            int hp = AttackAndGetTargetHp(rules, state, 0, 2, 3);

            Assert.Equal(7, hp); // 10 - 3, бонуса нет
        }

        [Theory]
        [InlineData(1, true)]  // расстояние 1 — внутри радиуса
        [InlineData(2, true)]  // расстояние 2 — граница радиуса, включается
        [InlineData(3, false)] // расстояние 3 — вне радиуса
        public void BearerRadius_MatchesFieldRadiusSemantics(int offsetX, bool expectedBonus)
        {
            var rules = new GameRules();
            var state = NewState(new[] { Bearer, "Warrior_One" }, new[] { "Warrior_One" });
            PlaceAs(rules, state, 0, Bearer, 3, 3);                 // Id = 1, знаменосец
            PlaceAs(rules, state, 0, "Warrior_One", 3 + offsetX, 3); // Id = 2, атакующий союзник
            PlaceAs(rules, state, 1, "Warrior_One", 3 + offsetX, 4); // Id = 3, цель

            // Решение поля о радиусе и решение знаменосца обязаны совпадать.
            bool inRadius = state.Field.GetPiecesInRadius(3, 3, 2).Any(p => p.Id == 2);
            Assert.Equal(expectedBonus, inRadius);

            int hp = AttackAndGetTargetHp(rules, state, 0, 2, 3);

            Assert.Equal(10 - (3 + (expectedBonus ? 1 : 0)), hp);
        }

        [Fact]
        public void EnemyNextToBearer_GetsNoBonus()
        {
            var rules = new GameRules();
            var state = NewState(new[] { Bearer, "Warrior_One" }, new[] { "Warrior_One" });
            PlaceAs(rules, state, 0, Bearer, 3, 3);        // Id = 1, знаменосец игрока 0
            PlaceAs(rules, state, 1, "Warrior_One", 4, 4); // Id = 2, атакующий враг, расстояние 1
            PlaceAs(rules, state, 0, "Warrior_One", 4, 5); // Id = 3, цель игрока 0

            int hp = AttackAndGetTargetHp(rules, state, 1, 2, 3);

            Assert.Equal(7, hp); // 10 - 3, враг не получает бонус союзника
        }

        [Fact]
        public void DeadBearer_GivesNoBonus()
        {
            var rules = new GameRules();
            var state = NewState(
                new[] { Bearer, "Warrior_One" },
                new[] { "Mage_Three", "Warrior_One" });
            PlaceAs(rules, state, 0, Bearer, 3, 3);        // Id = 1, знаменосец
            PlaceAs(rules, state, 0, "Warrior_One", 4, 4); // Id = 2, атакующий союзник
            PlaceAs(rules, state, 1, "Mage_Three", 2, 2);  // Id = 3, убийца знаменосца, урон 10
            PlaceAs(rules, state, 1, "Warrior_One", 4, 5); // Id = 4, цель

            // Mage_Three убивает знаменосца: HP 10 - 10 = 0, фигура уходит с поля
            state.CurrentPlayer = 1;
            var kill = rules.Apply(state, new AttackCommand(1, 3, 1));
            Assert.True(kill.Success, kill.Error.ToString());
            Assert.Null(state.Field.GetAt(3, 3));

            int hp = AttackAndGetTargetHp(rules, state, 0, 2, 4);

            Assert.Equal(7, hp); // 10 - 3, мёртвый знаменосец не даёт бонуса
        }

        [Fact]
        public void TwoBearers_GivePlusTwoDamage()
        {
            var rules = new GameRules();
            var state = NewState(
                new[] { Bearer, Bearer, "Warrior_One" },
                new[] { "Warrior_One" });
            PlaceAs(rules, state, 0, Bearer, 3, 3);        // Id = 1, расстояние 1 до атакующего
            PlaceAs(rules, state, 0, Bearer, 3, 5);        // Id = 2, расстояние 1 до атакующего
            PlaceAs(rules, state, 0, "Warrior_One", 4, 4); // Id = 3, атакующий союзник
            PlaceAs(rules, state, 1, "Warrior_One", 4, 5); // Id = 4, цель

            int hp = AttackAndGetTargetHp(rules, state, 0, 3, 4);

            Assert.Equal(5, hp); // 10 - (3 + 1 + 1)
        }

        [Fact]
        public void BearerAttacking_Self_GetsNoBonus()
        {
            var rules = new GameRules();
            var state = NewState(new[] { Bearer }, new[] { "Warrior_One" });
            PlaceAs(rules, state, 0, Bearer, 3, 3);        // Id = 1, знаменосец атакует сам
            PlaceAs(rules, state, 1, "Warrior_One", 3, 4); // Id = 2, цель

            int hp = AttackAndGetTargetHp(rules, state, 0, 1, 2);

            Assert.Equal(8, hp); // 10 - 2, базовый урон знаменосца, без бонуса себе
        }
    }
}
