using Magbat.Core;
using Magbat.Sim;

namespace Magbat.Tests;

public class SimulationTests
{
    [Fact]
    public void EventServer_PlaysSameGamesAsDirectRunner()
    {
        var rules = new GameRules();
        AssertMatchesDirectPlay(new EventServer(rules), rules, games: 100);
    }

    [Theory]
    [InlineData(30, BroadcastMode.Delta)]
    [InlineData(30, BroadcastMode.FullSnapshot)]
    [InlineData(10, BroadcastMode.Delta)]
    public void TickServer_PlaysSameGamesAsDirectRunner(int hz, BroadcastMode mode)
    {
        var rules = new GameRules();
        AssertMatchesDirectPlay(new TickServer(rules, hz, mode), rules, games: 30);
    }

    private static void AssertMatchesDirectPlay(IGameServer server, GameRules rules, int games)
    {
        var seeds = Enumerable.Range(1, games).ToList();
        using var simulation = new GameSimulation(server, rules, delayMs: 0);

        var result = simulation.Run(seeds, TimeSpan.FromSeconds(120));

        foreach (int seed in seeds)

        {
            var direct = GameFactory.NewGame();
            GameRunner.Play(direct, rules, new[] { RandomBot.ForGame(seed, 0), RandomBot.ForGame(seed, 1) });

            StateAssert.Same(direct, result.ServerStates[seed]);
            StateAssert.Same(direct, result.ClientStates[(seed, 0)]);
            StateAssert.Same(direct, result.ClientStates[(seed, 1)]);
        }

        Assert.NotEmpty(result.LatenciesMs);
    }
}