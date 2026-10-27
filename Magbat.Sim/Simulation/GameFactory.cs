using Magbat.Core;

namespace Magbat.Sim;

/// <summary>
/// Фабрика начальных состояний партий для эксперимента
/// </summary>
public static class GameFactory
{
    /// <summary>
    /// Создаёт новое начальное состояние партии
    /// </summary>
    /// <remarks>
    /// Каждый вызов возвращает независимый объект, который нельзя делить между партиями
    /// </remarks>
    /// <returns>Состояние игры</returns>
    public static GameState NewGame()
    {
        // Рука одинаковая у обоих игроков. В настоящей игре она собиралась бы
        // из набора игрока, который хранится на сервере
        var hand = new[] { 
            "Warrior_One", "Warrior_Two", "Warrior_Three",
            "Mage_One", "Mage_Two", "Mage_Three",
            "Cannon_One", "Cannon_Two", "Cannon_Three" };
        return new GameState(new GameField(10, 10), new List<string>(hand), new List<string>(hand));
    }
}