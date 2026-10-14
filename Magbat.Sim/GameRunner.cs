using Magbat.Core;

namespace Magbat.Sim;

public static class GameRunner
{
    public static GameResult Play(GameState state, GameRules rules, RandomBot[] bots, int maxTurns = 500)
    {
        int turns = 0;
        while (state.Winner == null && turns < maxTurns)
        {
            var legal = rules.GetLegalCommands(state);
            var command = bots[state.CurrentPlayer].Choose(legal);
            var result = rules.Apply(state, command);
            if (!result.Success)
                throw new InvalidOperationException($"Illegal command {command}: {result.Error}");
            turns++;
        }
        return new GameResult { Winner = state.Winner, Turns = turns, HitTurnLimit = state.Winner == null };
    }
}