using Magbat.Core;

namespace Magbat.Sim;

/// <summary>
/// Бот, выбирающий случайную команду из случайной категории действий
/// </summary>
/// <remarks>Не потокобезопасен: у каждого клиента должен быть свой бот</remarks>
public class RandomBot
{
    private readonly Random _random;
    private readonly double _passChance;

    /// <summary>
    /// Создаёт бота
    /// </summary>
    /// <param name="seed">Основа генератора случайных чисел</param>
    /// <param name="passChance">Вероятность пропуска хода</param>
    public RandomBot(int seed, double passChance = 0.05)
    {
        _random = new Random(seed);
        _passChance = passChance;
    }

    /// <summary>
    /// Выбирает случайную команду из случайной категории действия
    /// </summary>
    /// <param name="legal">Доступные команды</param>
    /// <returns>Выбранная команда</returns>
    public Command Choose(List<Command> legal)
    {
        // Сначала выбирается категория, а потом команда внутри неё: команд выставления
        // сотни, а атак единицы, и при равномерном выборе из общего списка бот почти не ходил
        // бы и не атаковал
        var groups = new List<List<Command>>();
        AddGroup(groups, legal.OfType<PlaceCommand>());
        AddGroup(groups, legal.OfType<MoveCommand>());
        AddGroup(groups, legal.OfType<AttackCommand>());

        if (groups.Count == 0 || _random.NextDouble() < _passChance)
            return legal.OfType<PassCommand>().First();

        var group = groups[_random.Next(groups.Count)];
        return group[_random.Next(group.Count)];
    }

    /// <summary>
    /// Добавляет категорию команд в список, если в ней есть хотя бы одна команда
    /// </summary>
    /// <param name="groups">Список категорий</param>
    /// <param name="commands">Команды одной категории</param>
    /// <typeparam name="T">Тип команд категории</typeparam>
    private static void AddGroup<T>(List<List<Command>> groups, IEnumerable<T> commands) where T : Command
    {
        var list = commands.Cast<Command>().ToList();

        if (list.Count > 0) groups.Add(list);
    }
    
    /// <summary>
    /// Создаёт бота для игрока указанной партии
    /// </summary>
    /// <param name="gameSeed">Номер партии</param>
    /// <param name="player">Номер игрока</param>
    public static RandomBot ForGame(int gameSeed, int player)
    {
        return new RandomBot(gameSeed * 2 + player);
    }
}