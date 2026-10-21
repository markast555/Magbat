using Magbat.Abilities;
using Magbat.Core;

namespace Magbat.Abilities.Demo;

/// <summary>
/// Консольная демонстрация способности «Знаменосец».
/// Показывает три сценария реальной атаки через GameRules.
/// </summary>
public static class Program
{
    private const int Size = 10;
    private const string Bearer = AbilityCatalog.StandardBearerName;

    public static void Main()
    {
        AbilityCatalog.RegisterStandardBearer();
        var rules = new GameRules();

        Console.WriteLine("=== Демонстрация способности «Знаменосец» ===");
        Console.WriteLine("Правило: знаменосец усиливает атаки СОЮЗНИКОВ в радиусе 2 на +1 к урону.");
        Console.WriteLine("Сам себя знаменосец не усиливает, мёртвый знаменосец бонуса не даёт.");
        Console.WriteLine("Обозначения: W — воин, M — маг, B — знаменосец, C — пушка. Число — HP.");
        Console.WriteLine();

        ScenarioWithoutBearer(rules);
        ScenarioAllyNextToBearer(rules);
        ScenarioBearerDead(rules);

        Console.WriteLine("=== Демонстрация завершена ===");
    }

    // ---------- сценарии ----------

    private static void ScenarioWithoutBearer(GameRules rules)
    {
        Console.WriteLine("--- Сценарий 1: атака без знаменосца ---");
        var state = NewState(new[] { "Warrior_One" }, new[] { "Warrior_One" });
        PlaceAs(rules, state, 0, "Warrior_One", 3, 3);
        PlaceAs(rules, state, 1, "Warrior_One", 4, 3);
        PrintField(state);

        var r = Attack(rules, state, 0, 1, 2);

        Report(r, "без знаменосца урон базовый");
    }

    private static void ScenarioAllyNextToBearer(GameRules rules)
    {
        Console.WriteLine("--- Сценарий 2: союзник атакует рядом со знаменосцем ---");
        var state = NewState(new[] { Bearer, "Warrior_One" }, new[] { "Warrior_One" });
        PlaceAs(rules, state, 0, Bearer, 3, 3);
        PlaceAs(rules, state, 0, "Warrior_One", 4, 4);
        PlaceAs(rules, state, 1, "Warrior_One", 4, 5);
        PrintField(state);

        var r = Attack(rules, state, 0, 2, 3);

        Report(r, "знаменосец в радиусе 2 даёт +1 к урону");
    }

    private static void ScenarioBearerDead(GameRules rules)
    {
        Console.WriteLine("--- Сценарий 3: знаменосец погиб ---");
        var state = NewState(
            new[] { Bearer, "Warrior_One" },
            new[] { "Mage_Three", "Warrior_One" });
        PlaceAs(rules, state, 0, Bearer, 3, 3);
        PlaceAs(rules, state, 0, "Warrior_One", 4, 4);
        PlaceAs(rules, state, 1, "Mage_Three", 2, 2);
        PlaceAs(rules, state, 1, "Warrior_One", 4, 5);
        PrintField(state);

        // Маг наносит знаменосцу 10 урона при 10 HP — фигура гибнет и уходит с поля.
        state.CurrentPlayer = 1;
        var kill = rules.Apply(state, new AttackCommand(1, 3, 1));
        Console.WriteLine("Маг бьёт знаменосца: " + (kill.Success ? "удар нанесён" : kill.Error.ToString()));
        Console.WriteLine("Знаменосец на поле: " + (state.Field.GetAt(3, 3) == null ? "нет (погиб)" : "есть"));
        PrintField(state);

        var r = Attack(rules, state, 0, 2, 4);

        Report(r, "мёртвый знаменосец бонуса не даёт");
    }

    // ---------- вспомогательные методы ----------

    private static GameState NewState(string[] hand0, string[] hand1)
        => new GameState(new GameField(Size, Size), new List<string>(hand0), new List<string>(hand1));

    private static void PlaceAs(GameRules rules, GameState state, int player, string name, int x, int y)
    {
        state.CurrentPlayer = player;
        var result = rules.Apply(state, new PlaceCommand(player, name, x, y));
        if (!result.Success)
            throw new InvalidOperationException("Не удалось выставить фигуру: " + result.Error);
    }

    /// <summary>
    /// Выполняет настоящую атаку и возвращает фактические величины:
    /// HP цели до и после удара, нанесённый урон и базовый урон атакующего.
    /// </summary>
    private static AttackResult Attack(GameRules rules, GameState state, int player, int attackerId, int targetId)
    {
        var attacker = state.Field.FindById(attackerId)!;
        var target = state.Field.FindById(targetId)!;
        int hpBefore = target.Hp;
        int baseDamage = attacker.Definition.DefDamage;

        state.CurrentPlayer = player;
        var result = rules.Apply(state, new AttackCommand(player, attackerId, targetId));
        if (!result.Success)
            throw new InvalidOperationException("Атака не выполнена: " + result.Error);

        int hpAfter = state.Field.FindById(targetId)!.Hp;
        return new AttackResult(hpBefore, hpAfter, hpBefore - hpAfter, baseDamage);
    }

    /// <summary>
    /// Печатает результат атаки одинаковым форматом во всех сценариях.
    /// </summary>
    private static void Report(AttackResult r, string conclusion)
    {
        Console.WriteLine($"Базовый урон атакующего: {r.BaseDamage}. Нанесённый урон: {r.Damage}.");
        Console.WriteLine($"HP цели: {r.HpBefore} -> {r.HpAfter}.");
        Console.WriteLine($"Вывод: {conclusion} ({r.HpBefore} - {r.Damage} = {r.HpAfter}).");
        Console.WriteLine();
    }

    /// <summary>
    /// Результат одной атаки для вывода в демонстрации.
    /// </summary>
    private readonly struct AttackResult
    {
        public AttackResult(int hpBefore, int hpAfter, int damage, int baseDamage)
        {
            HpBefore = hpBefore;
            HpAfter = hpAfter;
            Damage = damage;
            BaseDamage = baseDamage;
        }

        public int HpBefore { get; }
        public int HpAfter { get; }
        public int Damage { get; }
        public int BaseDamage { get; }
    }

    private static void PrintField(GameState state)
    {
        for (int y = 0; y < state.Field.Height; y++)
        {
            for (int x = 0; x < state.Field.Width; x++)
            {
                var piece = state.Field.GetAt(x, y);
                if (piece == null)
                {
                    Console.Write(" .  ");
                }
                else
                {
                    Console.Write(Symbol(piece) + piece.Hp.ToString("D2") + " ");
                }
            }
            Console.WriteLine();
        }
    }

    private static string Symbol(Piece piece)
    {
        if (piece.Definition.Name == Bearer) return "B";
        return piece.Definition.Type switch
        {
            PieceType.Warrior => "W",
            PieceType.Mage => "M",
            PieceType.Cannon => "C",
            _ => "?"
        };
    }
}
