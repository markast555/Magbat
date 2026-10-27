using Magbat.Core;

namespace Magbat.Sim;

/// <summary>
/// Запускает прогоны экспериментов
/// </summary>
public static class ExperimentRunner
{
    /// <summary>Сравниваемые модели сервера</summary>
    public static readonly (ServerKind Kind, int Hz, BroadcastMode Mode)[] Servers =
    {
        (ServerKind.Event, 0, BroadcastMode.Delta),
        (ServerKind.Tick, 30, BroadcastMode.Delta),
        (ServerKind.Tick, 30, BroadcastMode.FullSnapshot),
        (ServerKind.Tick, 10, BroadcastMode.Delta),
        (ServerKind.Tick, 60, BroadcastMode.Delta)
    };

    /// <summary>
    /// Выполняет прогон с новым сервером
    /// </summary>
    public static ExperimentResult Execute(ExperimentConfig config)
    {
        var rules = new GameRules();
        IGameServer server = config.Server == ServerKind.Event
            ? new EventServer(rules)
            : new TickServer(rules, config.TickHz, config.Mode);

        var options = new SimulationOptions
        {
            DelayMs = config.DelayMs,
            MeanThinkMs = config.MeanThinkMs,
            CountTraffic = true

        };

        var simulation = new GameSimulation(server, rules, options);
        try
        {
            // Номера партий зависят от повтора, а не от модели: все модели играют одни и те же партии
            var seeds = Enumerable.Range(config.Repeat * 10000 + 1, config.Games).ToList();
            var metrics = simulation.Measure(seeds, TimeSpan.FromSeconds(config.DurationSec));
            return new ExperimentResult(config, metrics, server as TickServer);
        }
        finally
        {
            simulation.Dispose();
        }
    }

    /// <summary>
    /// Прогревает все пути кода короткими прогонами. Результаты не сохраняются
    /// </summary>
    public static void Warmup()
    {
        foreach (var (kind, hz, mode) in Servers)
        {
            Execute(new ExperimentConfig
            {
                Server = kind, TickHz = hz, Mode = mode,
                Games = 10, MeanThinkMs = 100, DurationSec = 3, Repeat = 99
            });
        }
    }
}