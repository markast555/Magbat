using Magbat.Core;

namespace Magbat.Sim;

public class RandomBot
{
    private readonly Random _random;
    private readonly double _passChance;

    public RandomBot(int seed, double passChance = 0.05)
    {
        _random = new Random(seed);
        _passChance = passChance;
    }

    public Command Choose(List<Command> legal)
    {
        var groups = new List<List<Command>>();
        AddGroup(groups, legal.OfType<PlaceCommand>());
        AddGroup(groups, legal.OfType<MoveCommand>());
        AddGroup(groups, legal.OfType<AttackCommand>());

        if (groups.Count == 0 || _random.NextDouble() < _passChance)
            return legal.OfType<PassCommand>().First();

        var group = groups[_random.Next(groups.Count)];
        return group[_random.Next(group.Count)];
    }

    private static void AddGroup<T>(List<List<Command>> groups, IEnumerable<T> commands) where T : Command
    {
        var list = commands.Cast<Command>().ToList();

        if (list.Count > 0) groups.Add(list);
    }
}