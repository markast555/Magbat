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
                AssertSameState(real, copy);
                turns++;
            }
        }
    }


    private static void AssertSameState(GameState a, GameState b)
    {
        Assert.Equal(a.CurrentPlayer, b.CurrentPlayer);
        Assert.Equal(a.Winner, b.Winner);
        Assert.Equal(a.Hands[0], b.Hands[0]);
        Assert.Equal(a.Hands[1], b.Hands[1]);

        for (int x = 0; x < a.Field.Width; x++)
        for (int y = 0; y < a.Field.Height; y++)
        {
            var pa = a.Field.GetAt(x, y);
            var pb = b.Field.GetAt(x, y);
            Assert.Equal(pa == null, pb == null);
            if (pa == null || pb == null) continue;
            Assert.Equal(pa.Id, pb.Id);
            Assert.Equal(pa.Owner, pb.Owner);
            Assert.Equal(pa.Definition.Name, pb.Definition.Name);
            Assert.Equal(pa.Hp, pb.Hp);
        }
    }
}