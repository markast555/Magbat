using Magbat.Core;
using Magbat.Sim;

namespace Magbat.Tests;

public class ChangeApplierTests
{
    [Fact]
    public void ReplayingChanges_ReproducesRealState()
    {
        var rules = new GameRules();

        for (int seed = 1; seed <= 20; seed++)
        {
            var real = GameFactory.NewGame();
            var copy = GameFactory.NewGame();
            var bots = new[] { new RandomBot(seed * 2), new RandomBot(seed * 2 + 1) };

            int turns = 0;
            while (real.Winner == null && turns < 500)
            {
                var command = bots[real.CurrentPlayer].Choose(rules.GetLegalCommands(real));
                var result = rules.Apply(real, command);
                Assert.True(result.Success);

                ChangeApplier.Apply(copy, result.Changes);
                StateAssert.Same(real, copy);
                turns++;
            }
        }
    }
}