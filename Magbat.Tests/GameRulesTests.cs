using Magbat.Core;

namespace Magbat.Tests;

public class GameRulesTests
{
    // ---------- вспомогательные методы ----------

    private static GameState NewState(params string[] hand)
        => new GameState(new GameField(10, 10), new List<string>(hand), new List<string>(hand));

    private static GameState NewStateWithHands(string[] hand0, string[] hand1)
        => new GameState(new GameField(10, 10), new List<string>(hand0), new List<string>(hand1));

    // Выставляет фигуру от имени игрока, независимо от того, чья сейчас очередь.
    private static void PlaceAs(GameRules rules, GameState state, int player, string name, int x, int y)
    {
        state.CurrentPlayer = player;
        var result = rules.Apply(state, new PlaceCommand(player, name, x, y));
        Assert.True(result.Success, result.Error.ToString());
    }

    // ---------- выставление фигур ----------

    [Fact]
    public void Place_PieceInHand_MovesItToField()
    {
        var rules = new GameRules();
        var state = NewState("Warrior_One");

        var result = rules.Apply(state, new PlaceCommand(0, "Warrior_One", 3, 3));

        Assert.True(result.Success);
        Assert.NotNull(state.Field.GetAt(3, 3));
        Assert.Empty(state.Hands[0]);
    }

    [Fact]
    public void Place_OnOccupiedCell_IsRejected()
    {
        var rules = new GameRules();
        var state = NewState("Warrior_One");

        rules.Apply(state, new PlaceCommand(0, "Warrior_One", 3, 3));
        var result = rules.Apply(state, new PlaceCommand(1, "Warrior_One", 3, 3));

        Assert.False(result.Success);
        Assert.Equal(ApplyError.CellOccupied, result.Error);
        Assert.Equal(0, state.Field.GetAt(3, 3)!.Owner);
        Assert.Single(state.Hands[1]);
        Assert.Equal(1, state.CurrentPlayer);
    }

    [Fact]
    public void Place_PieceNotInHand_IsRejected()
    {
        var rules = new GameRules();
        var state = NewState("Warrior_One");

        var result = rules.Apply(state, new PlaceCommand(0, "Mage_One", 3, 3));

        Assert.False(result.Success);
        Assert.Equal(	ApplyError.PieceNotInHand, result.Error);
        Assert.Null(state.Field.GetAt(3, 3));
    }

    [Fact]
    public void Place_OutOfBounds_IsRejected()
    {
        var rules = new GameRules();
        var state = NewState("Warrior_One");

        var result = rules.Apply(state, new PlaceCommand(0, "Warrior_One", 10, 0));

        Assert.False(result.Success);
        Assert.Equal(ApplyError.OutOfBounds, result.Error);
        Assert.Single(state.Hands[0]);
    }

    // ---------- очередь хода и конец партии ----------

    [Fact]
    public void Apply_OtherPlayersTurn_IsRejected()
    {
        var rules = new GameRules();
        var state = NewState("Warrior_One");

        var result = rules.Apply(state, new PlaceCommand(1, "Warrior_One", 3, 3));

        Assert.False(result.Success);
        Assert.Equal(ApplyError.NotYourTurn, result.Error);
        Assert.Null(state.Field.GetAt(3, 3));
        Assert.Single(state.Hands[1]);
    }

    [Fact]
    public void Apply_SuccessfulCommand_PassesTurn()
    {
        var rules = new GameRules();
        var state = NewState("Warrior_One");

        rules.Apply(state, new PlaceCommand(0, "Warrior_One", 3, 3));

        Assert.Equal(1, state.CurrentPlayer);
    }

    [Fact]
    public void Apply_RejectedCommand_KeepsTurn()
    {
        var rules = new GameRules();
        var state = NewState("Warrior_One");

        var result = rules.Apply(state, new PlaceCommand(0, "Mage_One", 3, 3));

        Assert.False(result.Success);
        Assert.Equal(0, state.CurrentPlayer);
    }

    [Fact]
    public void Apply_AfterGameOver_IsRejected()
    {
        var rules = new GameRules();
        var state = NewState("Warrior_One");
        state.Winner = 0;

        var result = rules.Apply(state, new PassCommand(0));

        Assert.False(result.Success);
        Assert.Equal(	ApplyError.GameOver, result.Error);
    }

    // ---------- перемещение ----------

    [Fact]
    public void Move_WithinRange_MovesPiece()
    {
        var rules = new GameRules();
        var state = NewState("Warrior_One");
        PlaceAs(rules, state, 0, "Warrior_One", 3, 3); // получит Id = 1
        state.CurrentPlayer = 0;

        var result = rules.Apply(state, new MoveCommand(0, 1, 4, 3));

        Assert.True(result.Success);
        Assert.Null(state.Field.GetAt(3, 3));
        Assert.Equal(1, state.Field.GetAt(4, 3)!.Id);
    }

    [Fact]
    public void Move_BeyondRange_IsRejected()
    {
        var rules = new GameRules();
        var state = NewState("Warrior_One");
        PlaceAs(rules, state, 0, "Warrior_One", 3, 3);
        state.CurrentPlayer = 0;

        var result = rules.Apply(state, new MoveCommand(0, 1, 5, 3)); // расстояние 2, а дальность хода 1

        Assert.False(result.Success);
        Assert.Equal(ApplyError.TooFar, result.Error);
        Assert.NotNull(state.Field.GetAt(3, 3));
    }

    [Fact]
    public void Move_ToOccupiedCell_IsRejected()
    {
        var rules = new GameRules();
        var state = NewState("Warrior_One", "Warrior_One");
        PlaceAs(rules, state, 0, "Warrior_One", 3, 3); // Id = 1
        PlaceAs(rules, state, 0, "Warrior_One", 4, 3); // Id = 2
        state.CurrentPlayer = 0;

        var result = rules.Apply(state, new MoveCommand(0, 1, 4, 3));

        Assert.False(result.Success);
        Assert.Equal(ApplyError.CellOccupied, result.Error);
        Assert.Equal(1, state.Field.GetAt(3, 3)!.Id);
    }

    [Fact]
    public void Move_OtherPlayersPiece_IsRejected()
    {
        var rules = new GameRules();
        var state = NewState("Warrior_One");
        PlaceAs(rules, state, 1, "Warrior_One", 6, 6); // фигура игрока 1, Id = 1
        state.CurrentPlayer = 0;

        var result = rules.Apply(state, new MoveCommand(0, 1, 6, 7));

        Assert.False(result.Success);
        Assert.Equal(ApplyError.NoSuchPiece, result.Error);
        Assert.NotNull(state.Field.GetAt(6, 6));
    }

    // ---------- атака ----------

    [Fact]
    public void Attack_InRange_ReducesTargetHp()
    {
        var rules = new GameRules();
        var state = NewState("Warrior_One");
        PlaceAs(rules, state, 0, "Warrior_One", 3, 3); // Id = 1
        PlaceAs(rules, state, 1, "Warrior_One", 4, 3); // Id = 2
        state.CurrentPlayer = 0;

        var result = rules.Apply(state, new AttackCommand(0, 1, 2));

        Assert.True(result.Success);
        Assert.Equal(7, state.Field.GetAt(4, 3)!.Hp); // 10 - 3
    }

    [Fact]
    public void Attack_AtMaxRange_Succeeds()
    {
        var rules = new GameRules();
        var state = NewState("Warrior_One", "Mage_One");
        PlaceAs(rules, state, 0, "Mage_One", 0, 0);    // Id = 1, дальность атаки 3
        PlaceAs(rules, state, 1, "Warrior_One", 3, 0); // Id = 2, расстояние ровно 3
        state.CurrentPlayer = 0;

        var result = rules.Apply(state, new AttackCommand(0, 1, 2));

        Assert.True(result.Success);
        Assert.Equal(6, state.Field.GetAt(3, 0)!.Hp); // 10 - 4
    }

    [Fact]
    public void Attack_OutOfRange_IsRejected()
    {
        var rules = new GameRules();
        var state = NewState("Warrior_One");
        PlaceAs(rules, state, 0, "Warrior_One", 3, 3);
        PlaceAs(rules, state, 1, "Warrior_One", 6, 3); // расстояние 3, а дальность атаки 1
        state.CurrentPlayer = 0;

        var result = rules.Apply(state, new AttackCommand(0, 1, 2));

        Assert.False(result.Success);
        Assert.Equal(ApplyError.TargetOutOfRange, result.Error);
        Assert.Equal(10, state.Field.GetAt(6, 3)!.Hp);
    }

    [Fact]
    public void Attack_OwnPiece_IsRejected()
    {
        var rules = new GameRules();
        var state = NewState("Warrior_One", "Warrior_One");
        PlaceAs(rules, state, 0, "Warrior_One", 3, 3); // Id = 1
        PlaceAs(rules, state, 0, "Warrior_One", 4, 3); // Id = 2
        state.CurrentPlayer = 0;

        var result = rules.Apply(state, new AttackCommand(0, 1, 2));

        Assert.False(result.Success);
        Assert.Equal(ApplyError.InvalidTarget, result.Error);
        Assert.Equal(10, state.Field.GetAt(4, 3)!.Hp);
    }

    [Fact]
    public void Attack_KillingBlow_RemovesPieceFromField()
    {
        var rules = new GameRules();
        var state = NewState("Warrior_One", "Warrior_One");
        PlaceAs(rules, state, 0, "Warrior_One", 3, 3);
        PlaceAs(rules, state, 1, "Warrior_One", 4, 3);
        state.Field.GetAt(4, 3)!.Hp = 1;
        state.CurrentPlayer = 0;

        var result = rules.Apply(state, new AttackCommand(0, 1, 2));

        Assert.True(result.Success);
        Assert.Null(state.Field.GetAt(4, 3));
    }

    // ---------- победа ----------

    [Fact]
    public void Attack_KillingLastFieldPieceWithCardsInHand_DoesNotEndGame()
    {
        var rules = new GameRules();
        var state = NewStateWithHands(
            new[] { "Warrior_One" },
            new[] { "Warrior_One", "Warrior_One" });
        PlaceAs(rules, state, 0, "Warrior_One", 3, 3);
        PlaceAs(rules, state, 1, "Warrior_One", 4, 3); // в руке игрока 1 ещё одна фигура
        state.Field.GetAt(4, 3)!.Hp = 1;
        state.CurrentPlayer = 0;

        rules.Apply(state, new AttackCommand(0, 1, 2));

        Assert.Null(state.Field.GetAt(4, 3));
        Assert.Null(state.Winner);
    }

    [Fact]
    public void Attack_KillingLastPieceWithEmptyHand_EndsGame()
    {
        var rules = new GameRules();
        var state = NewState("Warrior_One");
        PlaceAs(rules, state, 0, "Warrior_One", 3, 3);
        PlaceAs(rules, state, 1, "Warrior_One", 4, 3); // руки обоих пусты
        state.Field.GetAt(4, 3)!.Hp = 1;
        state.CurrentPlayer = 0;

        rules.Apply(state, new AttackCommand(0, 1, 2));

        Assert.Equal(0, state.Winner);
    }

    // ---------- пропуск хода ----------

    [Fact]
    public void Pass_ThreeInARow_LosesGame()
    {
        var rules = new GameRules();
        var state = NewState("Warrior_One");

        rules.Apply(state, new PassCommand(0));
        rules.Apply(state, new PassCommand(1));
        rules.Apply(state, new PassCommand(0));
        rules.Apply(state, new PassCommand(1));
        Assert.Null(state.Winner); // у каждого пока по два пропуска

        rules.Apply(state, new PassCommand(0)); // третий пропуск игрока 0

        Assert.Equal(1, state.Winner);
    }

    [Fact]
    public void Pass_ActionBetweenPasses_ResetsCounter()
    {
        var rules = new GameRules();
        var state = NewState("Warrior_One");

        rules.Apply(state, new PassCommand(0));
        state.CurrentPlayer = 0;
        rules.Apply(state, new PassCommand(0));
        PlaceAs(rules, state, 0, "Warrior_One", 3, 3); // действие сбрасывает счётчик
        state.CurrentPlayer = 0;
        rules.Apply(state, new PassCommand(0));
        state.CurrentPlayer = 0;
        rules.Apply(state, new PassCommand(0));

        Assert.Null(state.Winner); // без сброса было бы уже четыре пропуска
    }

    // ---------- GetLegalCommands ----------

    [Fact]
    public void GetLegalCommands_AtStart_OnlyPlaceAndPass()
    {
        var rules = new GameRules();
        var state = NewState("Warrior_One", "Mage_One");

        var commands = rules.GetLegalCommands(state);

        Assert.Equal(201, commands.Count); // 2 названия × 100 клеток + пропуск
        Assert.All(commands, c => Assert.True(c is PlaceCommand || c is PassCommand));
    }

    [Fact]
    public void GetLegalCommands_EveryCommandIsAccepted()
    {
        var rules = new GameRules();
        var commands = rules.GetLegalCommands(BuildMidgame(rules));

        Assert.Contains(commands, c => c is MoveCommand);
        Assert.Contains(commands, c => c is AttackCommand);

        foreach (var command in commands)
        {
            var fresh = BuildMidgame(rules); // каждая команда проверяется на свежем состоянии
            var result = rules.Apply(fresh, command);
            Assert.True(result.Success, $"{command}: {result.Error}");
        }
    }

    private static GameState BuildMidgame(GameRules rules)
    {
        var state = NewStateWithHands(
            new[] { "Warrior_One", "Cannon_One", "Mage_One" },
            new[] { "Warrior_One", "Mage_One" });
        PlaceAs(rules, state, 0, "Mage_One", 3, 3);
        PlaceAs(rules, state, 1, "Warrior_One", 5, 3); // в зоне атаки мага
        state.CurrentPlayer = 0;
        return state;
    }
}