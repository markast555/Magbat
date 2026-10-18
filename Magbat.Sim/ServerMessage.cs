using Magbat.Core;

namespace Magbat.Sim;

public class ServerMessage
{
    public int GameId { get; }
    public int Player { get; }          // кто отправил исходную команду
    public long CommandSentAt { get; }  // время её отправки: от него клиент считает задержку
    public ApplyError Error { get; }
    public IReadOnlyList<Change> Changes { get; }

    public ServerMessage(int gameId, int player, long commandSentAt, ApplyError error, IReadOnlyList<Change> changes)
    {
        GameId = gameId;
        Player = player;
        CommandSentAt = commandSentAt;
        Error = error;
        Changes = changes;
    }
}