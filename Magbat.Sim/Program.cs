using Magbat.Core;
using Magbat.Sim;
using System.Diagnostics;

var rules = new GameRules();

GameResult Run(int seed) => GameRunner.Play(
    GameFactory.NewGame(), rules,
    new[] { RandomBot.ForGame(seed, 0), RandomBot.ForGame(seed, 1) });

// проверка воспроизводимости: один и тот же seed даёт одну и ту же партию
var a = Run(1);
var b = Run(1);
Console.WriteLine($"Reproducible: {a.Winner == b.Winner && a.Turns == b.Turns}");

int won = 0, limit = 0;
long totalTurns = 0;
for (int seed = 1; seed <= 1000; seed++)
{
    var result = Run(seed);
    totalTurns += result.Turns;
    if (result.HitTurnLimit) limit++; else won++;
}
Console.WriteLine($"Finished with winner: {won}, hit turn limit: {limit}, average turns: {totalTurns / 1000.0:F1}");

using var simulationEventServer = new GameSimulation(new EventServer(rules), rules, delayMs: 0);
var watch = Stopwatch.StartNew();
var simulationResult = simulationEventServer.Run(Enumerable.Range(1, 1000).ToList(), TimeSpan.FromMinutes(2));
Console.WriteLine($"EventServer: {simulationResult.ServerStates.Count} games in {watch.Elapsed.TotalSeconds:F1} s, " +
                  $"average latency {simulationResult.LatenciesMs.Average():F3} ms");

foreach (var (hz, mode) in new[] { (30, BroadcastMode.Delta), (30, BroadcastMode.FullSnapshot), (10, BroadcastMode.Delta) })
{
    var server = new TickServer(rules, hz, mode);
    var simulationTickServer = new GameSimulation(server, rules, 0);
    var result = simulationTickServer.Run(Enumerable.Range(1, 1000).ToList(), TimeSpan.FromMinutes(3));
    simulationTickServer.Dispose();

    Console.WriteLine($"TickServer {hz} Hz {mode}: average latency {result.LatenciesMs.Average():F1} ms, " +
                      $"ticks {server.Ticks} (idle {server.IdleTicks}), " +
                      $"interval mean {server.MeanIntervalMs:F1} / min {server.MinIntervalMs:F1} / max {server.MaxIntervalMs:F1} ms");
}                  