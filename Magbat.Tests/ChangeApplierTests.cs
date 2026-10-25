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
            var bots = new[] { RandomBot.ForGame(seed, 0), RandomBot.ForGame(seed, 1) };

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
    
    [Fact]
    public void Snapshot_ReproducesRealState()
    {
        var rules = new GameRules();
        var real = GameFactory.NewGame();
        var bots = new[] { RandomBot.ForGame(7, 0), RandomBot.ForGame(7, 1) };

        for (int i = 0; i < 40 && real.Winner == null; i++)
            rules.Apply(real, bots[real.CurrentPlayer].Choose(rules.GetLegalCommands(real)));

        var copy = GameFactory.NewGame();
        ChangeApplier.Apply(copy, new Change[] { StateSnapshot.From(real) });

        StateAssert.Same(real, copy);
    }
}