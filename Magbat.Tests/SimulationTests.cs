using Magbat.Core;
using Magbat.Sim;

namespace Magbat.Tests;

public class SimulationTests
{
    [Fact]
    public void EventServer_PlaysSameGamesAsDirectRunner()
    {
        var rules = new GameRules();
        var seeds = Enumerable.Range(1, 100).ToList();
        using var simulation = new GameSimulation(new EventServer(rules), rules, delayMs: 0);

        var result = simulation.Run(seeds, TimeSpan.FromSeconds(60));

        foreach (int seed in seeds)
        {
            var direct = GameFactory.NewGame();
            GameRunner.Play(direct, rules, new[] { new RandomBot(seed * 2), new RandomBot(seed * 2 + 1) });

            StateAssert.Same(direct, result.ServerStates[seed]);
            StateAssert.Same(direct, result.ClientStates[(seed, 0)]);
            StateAssert.Same(direct, result.ClientStates[(seed, 1)]);
        }

        Assert.NotEmpty(result.LatenciesMs);
    }
}