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
    
    [Fact]
    public void PacedClients_PlayGamesSameAsDirectRunner()
    {
        var rules = new GameRules();
        AssertMatchesDirectPlay(new EventServer(rules), rules, games: 5,
            new SimulationOptions { MeanThinkMs = 4 });
    }

    [Fact]
    public void Measure_ReturnsMetrics()
    {
        var rules = new GameRules();
        using var simulation = new GameSimulation(new EventServer(rules), rules,
            new SimulationOptions { MeanThinkMs = 20, CountTraffic = true });

        var metrics = simulation.Measure(Enumerable.Range(1, 5).ToList(), TimeSpan.FromSeconds(1));

        Assert.True(metrics.UpMessages > 0);
        Assert.True(metrics.UpBytes > 0);
        Assert.True(metrics.ServerBusyMs > 0);
        Assert.NotEmpty(metrics.LatenciesMs);
    }

    private static void AssertMatchesDirectPlay(IGameServer server, GameRules rules, int games,
        SimulationOptions? options = null)
    {
        var seeds = Enumerable.Range(1, games).ToList();
        using var simulation = new GameSimulation(server, rules, options ?? new SimulationOptions());

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