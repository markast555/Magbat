using Magbat.Core;

namespace Magbat.Sim;

/// <summary>
///  Играет партии напрямую, без сервера и каналов. Служит эталоном для проверки серверов
/// </summary>
public static class GameRunner
{
    /// <summary>
    /// Играет партию до победы или до лимита ходов
    /// </summary>
    /// <remarks>Изменяет переданное состояние: после вызова в нём итог партии</remarks>
    /// <param name="state">Начальное состояние партии</param>
    /// <param name="rules">Правила игры</param>
    /// <param name="bots">Боты, индекс соответствует номеру игрока</param>
    /// <param name="maxTurns">Лимит ходов: партия не обязана заканчиваться победой</param>
    /// <returns>Итог партии</returns>
    /// <exception cref="InvalidOperationException">
    /// Правила отклонили команду, выбранную из допустимых: ошибка в GetLegalCommands или Apply
    /// </exception>
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