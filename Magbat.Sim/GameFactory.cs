using Magbat.Core;

namespace Magbat.Sim;

public static class GameFactory
{
    public static GameState NewGame()
    {
        var hand = new[] { 
            "Warrior_One", "Warrior_Two", "Warrior_Three",
            "Mage_One", "Mage_Two", "Mage_Three",
            "Cannon_One", "Cannon_Two", "Cannon_Three" };
        return new GameState(new GameField(10, 10), new List<string>(hand), new List<string>(hand));
    }
}