using Magbat.Core;

namespace Magbat.Sim;

public class ClientMessage
{
    public int GameId { get; }
    public Command Command { get; }
    public long SentAt { get; } // Stopwatch.GetTimestamp() в момент отправки

    public ClientMessage(int gameId, Command command, long sentAt)
    {
        GameId = gameId;
        Command = command;
        SentAt = sentAt;
    }
}