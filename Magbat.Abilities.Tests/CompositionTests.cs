using Magbat.Core;
using Magbat.Core.Abilities;

namespace Magbat.Abilities.Tests
{
    /// <summary>
    /// Тесты композиции способностей — ключевое утверждение тезиса:
    /// фигура состоит из нескольких независимых способностей.
    /// Проверяются два свойства:
    /// 1) одна фигура может нести несколько разных способностей, и все они
    ///    применяются (вклады складываются);
    /// 2) у двух фигур одного типа — независимые экземпляры способностей,
    ///    состояние одного не видно другому.
    /// Тесты изолированы: описания фигур создаются прямо в тесте,
    /// поэтому глобальный каталог ядра не затрагивается.
    /// </summary>
    public class CompositionTests
    {
        // ---------- вспомогательные методы ----------

        /// <summary>
        /// Описание фигуры с двумя разными способностями: аура знаменосца
        /// и плоская прибавка к урону союзников.
        /// </summary>
        private static PieceDefinition ComposerDefinition() => new PieceDefinition(
            name: "Test_Composer",
            type: PieceType.Support,
            defHp: 10,
            defDamage: 3,
            moveRange: 1,
            attackRange: 1,
            abilityFactory: piece => new IAbility[]
            {
                new StandardBearerAbility(piece),
                new FlatDamageAbility(piece, 2)
            });

        /// <summary>
        /// Описание фигуры с одной способностью, у которой есть собственное состояние.
        /// </summary>
        private static PieceDefinition CounterDefinition() => new PieceDefinition(
            name: "Test_Counter",
            type: PieceType.Support,
            defHp: 10,
            defDamage: 3,
            moveRange: 1,
            attackRange: 1,
            abilityFactory: piece => new IAbility[] { new CountingDamageAbility(piece) });

        private static GameState NewState()
            => new GameState(new GameField(10, 10), new List<string>(), new List<string>());

        private static Piece Put(GameState state, int id, int owner, PieceDefinition definition, int x, int y)
        {
            var piece = new Piece(id, owner, definition, x, y);
            state.Field.Put(piece);
            return piece;
        }

        private static Piece PutWarrior(GameState state, int id, int owner, int x, int y)
            => Put(state, id, owner, PieceCatalog.Get("Warrior_One"), x, y);

        // ---------- тесты ----------

        [Fact]
        public void Figure_WithoutFactory_HasNoAbilities()
        {
            var definition = new PieceDefinition("Test_Plain", PieceType.Warrior, 10, 3, 1, 1);
            var piece = new Piece(1, 0, definition, 3, 3);

            Assert.Empty(piece.Abilities);
        }

        [Fact]
        public void Figure_WithFactory_GetsAllAbilitiesInFactoryOrder()
        {
            var piece = new Piece(1, 0, ComposerDefinition(), 3, 3);

            Assert.Equal(2, piece.Abilities.Count);
            Assert.IsType<StandardBearerAbility>(piece.Abilities[0]);
            Assert.IsType<FlatDamageAbility>(piece.Abilities[1]);
        }

        [Fact]
        public void Figure_WithTwoAbilities_AppliesBothOfThem()
        {
            // Настоящая атака через GameRules:
            // базовый урон 3 + аура знаменосца 1 + плоская прибавка 2.
            var rules = new GameRules();
            var state = NewState();
            Put(state, 1, 0, ComposerDefinition(), 3, 3);       // носитель двух способностей
            var attacker = PutWarrior(state, 2, 0, 4, 4);         // союзник в радиусе 2
            var target = PutWarrior(state, 3, 1, 4, 5);           // цель

            state.CurrentPlayer = 0;
            var result = rules.Apply(state, new AttackCommand(0, attacker.Id, target.Id));

            Assert.True(result.Success, result.Error.ToString());
            Assert.Equal(4, state.Field.GetAt(4, 5)!.Hp); // 10 - (3 + 1 + 2)
        }

        [Fact]
        public void Figure_WithTwoAbilities_AppliesOnlyThoseThatMatchTheAttack()
        {
            // Вражеская атака: ни аура знаменосца, ни прибавка союзника не действуют.
            var rules = new GameRules();
            var state = NewState();
            Put(state, 1, 0, ComposerDefinition(), 3, 3);       // носитель двух способностей
            var attacker = PutWarrior(state, 2, 1, 4, 4);         // враг
            var target = PutWarrior(state, 3, 0, 4, 5);           // цель игрока 0

            state.CurrentPlayer = 1;
            var result = rules.Apply(state, new AttackCommand(1, attacker.Id, target.Id));

            Assert.True(result.Success, result.Error.ToString());
            Assert.Equal(7, state.Field.GetAt(4, 5)!.Hp); // 10 - 3, бонусов нет
        }

        [Fact]
        public void TwoFigures_OfSameDefinition_GetDifferentAbilityInstances()
        {
            var definition = CounterDefinition();
            var a = new Piece(1, 0, definition, 3, 3);
            var b = new Piece(2, 0, definition, 5, 5);

            Assert.NotSame(a.Abilities[0], b.Abilities[0]);
        }

        [Fact]
        public void AbilityState_IsNotSharedBetweenFigures()
        {
            // Обе фигуры на поле, обе способности опрошены при расчёте урона.
            // Если бы экземпляр был общим, счётчик показал бы 2, а не 1.
            var definition = CounterDefinition();
            var a = new Piece(1, 0, definition, 3, 3);
            var b = new Piece(2, 0, definition, 5, 5);

            var state = NewState();
            state.Field.Put(a);
            state.Field.Put(b);
            var attacker = PutWarrior(state, 3, 0, 4, 4);
            var target = PutWarrior(state, 4, 1, 4, 5);

            AttackDamageCalculator.Calculate(state, attacker, target);

            var counterA = Assert.IsType<CountingDamageAbility>(a.Abilities[0]);
            var counterB = Assert.IsType<CountingDamageAbility>(b.Abilities[0]);
            Assert.Equal(1, counterA.Calls);
            Assert.Equal(1, counterB.Calls);
        }

        [Fact]
        public void AbilityState_OfFigureOutOfPlay_StaysUntouched()
        {
            var definition = CounterDefinition();
            var onBoard = new Piece(1, 0, definition, 3, 3);
            var offBoard = new Piece(2, 0, definition, 5, 5);

            var state = NewState();
            state.Field.Put(onBoard);
            var attacker = PutWarrior(state, 3, 0, 4, 4);
            var target = PutWarrior(state, 4, 1, 4, 5);

            AttackDamageCalculator.Calculate(state, attacker, target);

            var counterOnBoard = Assert.IsType<CountingDamageAbility>(onBoard.Abilities[0]);
            var counterOffBoard = Assert.IsType<CountingDamageAbility>(offBoard.Abilities[0]);
            Assert.Equal(1, counterOnBoard.Calls);
            Assert.Equal(0, counterOffBoard.Calls);
        }

        [Fact]
        public void Abilities_AreNotAffectedByLaterChangesOfFactoryList()
        {
            // Список, отданный фабрикой, копируется: изменить способности фигуры снаружи нельзя.
            var created = new List<IAbility>();
            var definition = new PieceDefinition(
                name: "Test_Mutable",
                type: PieceType.Support,
                defHp: 10,
                defDamage: 3,
                moveRange: 1,
                attackRange: 1,
                abilityFactory: piece => created);

            var piece = new Piece(1, 0, definition, 3, 3);
            created.Add(new FlatDamageAbility(piece, 5));

            Assert.Empty(piece.Abilities);
        }
    }
}
