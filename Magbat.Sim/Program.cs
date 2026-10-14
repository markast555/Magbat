using Magbat.Core;
using Magbat.Sim;

var rules = new GameRules();

GameResult Run(int seed) => GameRunner.Play(
    GameFactory.NewGame(), rules,
    new[] { new RandomBot(seed * 2), new RandomBot(seed * 2 + 1) });

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